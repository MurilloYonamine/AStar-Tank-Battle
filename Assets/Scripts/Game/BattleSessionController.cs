using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Owns the timed elimination match and submits its immutable result through DataManager.
/// Selection, countdown and battle pauses do not count toward the time limit.
/// Performance: lowest completion time among victories; each player's best is ranked.
/// Equal stored milliseconds share competition ranks (1, 1, 3). Losses/draws stay in history.
/// The legacy points field is sent as zero for schema compatibility, never used as the criterion.
/// </summary>
public sealed class BattleSessionController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private UIDocument menuDocument;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private StyleSheet hudStyle;
    [SerializeField] private TankHealth playerHealth;
    [SerializeField] private TankHealth[] enemyHealths;

    [Header("Match objectives")]
    [SerializeField, Min(1f)] private float matchTimeLimitSeconds = 180f;
    [SerializeField, Min(1)] private int eliminationTarget = 10;

    [Header("Optional character selection and camera")]
    [SerializeField] private CharacterSelectionController characterSelection;
    [SerializeField] private TankAppearance playerAppearance;
    [SerializeField] private TankAppearance[] enemyAppearances;
    [SerializeField] private BattleCameraController battleCamera;
    [SerializeField] private EnemyTankSpawner enemySpawner;

    [Header("Match intro (unscaled seconds)")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.3f;
    [SerializeField, Min(0f)] private float cameraSettleTime = 0.75f;
    [SerializeField, Min(0.1f)] private float countdownStepDuration = 1f;
    [SerializeField, Min(0.1f)] private float goDuration = 0.5f;

    [Header("Suzuka ultimate feedback (cosmetic only)")]
    [SerializeField] private GameObject ultimateWindPrefab;
    [SerializeField, Min(.01f)] private float ultimateWindScale = .25f;

    private struct TankSpawn
    {
        public TankHealth Health;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private TankSpawn[] tankSpawns;
    private PlayerData observedPlayerData;
    private VisualElement hudRoot;
    private VisualElement battleOverlay;
    private VisualElement battleHud;
    private VisualElement defeatScreen;
    private VisualElement healthFill;
    private VisualElement mainMenu;
    private Label healthLabel;
    private Label timeLabel;
    private Label eliminationLabel;
    private Label resultTitleLabel;
    private Label resultEliminationsLabel;
    private Label resultTimeCaptionLabel;
    private Label finalTimeLabel;
    private Label resultRankingNoteLabel;
    private Label saveStatusLabel;
    private Button returnButton;
    private Button retrySaveButton;
    private Button replayButton;
    private Button loginButton;
    private ResultRankingPanel resultRanking;
    private VisualElement pauseScreen;
    private Button resumeButton;
    private readonly Dictionary<Animator, float> pausedAnimatorSpeeds = new();
    private struct PausedBody
    {
        public RigidbodyConstraints Constraints;
        public bool UseGravity;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
    }
    private readonly Dictionary<Rigidbody, PausedBody> pausedBodies = new();
    private readonly List<Behaviour> pausedProjectiles = new();
    private readonly List<ParticleSystem> pausedParticles = new();
    private bool roundActive;
    private VisualElement matchIntro;
    private VisualElement matchFade;
    private Label countdownLabel;
    private Coroutine roundIntro;
    private Coroutine roundResult;
    private VisualElement resultCharacterPreview;
    private int selectedCharacterIndex;
    private float hudSortingOrder;
    private DateTime roundStartedUtc;
    private DateTime roundFinishedUtc;
    private double roundStartedRealtime;
    private double pausedSeconds;
    private double pausedAtRealtime;
    private double roundDurationSeconds;
    private string roundId;
    private int enemiesKilled;
    private bool saveInProgress;
    private bool matchSaved;
    private string roundOutcome;
    private double activeTimeLimit;
    private int activeEliminationTarget;
    private readonly TankUltimateState playerUltimate = new();
    private VisualElement ultimateCard, ultimateIcon, ultimateMask;
    private Label ultimatePlaceholder, ultimateNameLabel, ultimateStatusLabel;
    private VisualElement ultimateScreenEffect;
    private GameObject ultimateWindInstance;
    private ParticleSystem[] ultimateWindParticles;
    private float ultimateVisualTime;
    private PauseAudioSettings pauseAudioSettings;

    public bool IsRoundActive => roundActive;
    public bool IsBattlePaused { get; private set; }
    public bool IsStartingRound { get; private set; }
    public double RemainingSeconds => Math.Max(0, activeTimeLimit - ElapsedSeconds);

    private double ElapsedSeconds => !roundActive ? roundDurationSeconds : Math.Max(0,
        (IsBattlePaused ? pausedAtRealtime : Time.realtimeSinceStartupAsDouble) -
        roundStartedRealtime - pausedSeconds);

    private void Awake()
    {
        if (playerHealth == null || enemyHealths == null)
        {
            Debug.LogError("BattleSessionController needs the player and enemy health references.", this);
            enabled = false;
            return;
        }

        tankSpawns = new TankSpawn[enemyHealths.Length + 1];
        tankSpawns[0] = CaptureSpawn(playerHealth);
        playerHealth.Died += OnPlayerDied;

        for (int i = 0; i < enemyHealths.Length; i++)
        {
            if (enemyHealths[i] == null)
            {
                Debug.LogError("BattleSessionController has a missing enemy health reference.", this);
                enabled = false;
                return;
            }

            tankSpawns[i + 1] = CaptureSpawn(enemyHealths[i]);
            enemyHealths[i].Died += OnEnemyDied;
        }
    }

    private void Start()
    {
        if (menuDocument == null || hudDocument == null || hudStyle == null)
        {
            Debug.LogError("BattleSessionController needs the menu and HUD documents.", this);
            enabled = false;
            return;
        }

        VisualElement menuRoot = menuDocument.rootVisualElement;
        mainMenu = menuRoot.Q<VisualElement>("MainMenu");
        hudRoot = hudDocument.rootVisualElement;
        hudRoot.RegisterCallback<GeometryChangedEvent>(OnHudGeometryChanged);
        if (!hudRoot.styleSheets.Contains(hudStyle)) hudRoot.styleSheets.Add(hudStyle);
        hudRoot.pickingMode = PickingMode.Ignore;

        battleOverlay = hudRoot.Q<VisualElement>("BattleOverlay");
        battleHud = hudRoot.Q<VisualElement>("BattleHud");
        defeatScreen = hudRoot.Q<VisualElement>("DefeatScreen");
        resultCharacterPreview = hudRoot.Q<VisualElement>("ResultCharacterPreview");
        healthFill = hudRoot.Q<VisualElement>("HealthFill");
        healthLabel = hudRoot.Q<Label>("HealthLabel");
        timeLabel = hudRoot.Q<Label>("TimeLabel");
        eliminationLabel = hudRoot.Q<Label>("EliminationLabel");
        resultTitleLabel = hudRoot.Q<Label>("ResultTitleLabel");
        resultEliminationsLabel = hudRoot.Q<Label>("ResultEliminationsLabel");
        resultTimeCaptionLabel = hudRoot.Q<Label>("ResultTimeCaptionLabel");
        finalTimeLabel = hudRoot.Q<Label>("FinalTimeLabel");
        resultRankingNoteLabel = hudRoot.Q<Label>("ResultRankingNoteLabel");
        saveStatusLabel = hudRoot.Q<Label>("SaveStatusLabel");
        returnButton = hudRoot.Q<Button>("ReturnToMenuButton");
        retrySaveButton = hudRoot.Q<Button>("RetrySaveButton");
        replayButton = hudRoot.Q<Button>("ReplayButton");
        loginButton = hudRoot.Q<Button>("ReturnToLoginButton");
        resultRanking = new ResultRankingPanel(hudRoot);
        pauseScreen = hudRoot.Q<VisualElement>("PauseScreen");
        resumeButton = hudRoot.Q<Button>("ResumeButton");
        matchIntro = hudRoot.Q<VisualElement>("MatchIntro");
        matchFade = hudRoot.Q<VisualElement>("MatchFade");
        countdownLabel = hudRoot.Q<Label>("CountdownLabel");
        ultimateCard = hudRoot.Q<VisualElement>("UltimateCard");
        ultimateIcon = hudRoot.Q<VisualElement>("UltimateIcon");
        ultimateMask = hudRoot.Q<VisualElement>("UltimateMask");
        ultimatePlaceholder = hudRoot.Q<Label>("UltimatePlaceholder");
        ultimateNameLabel = hudRoot.Q<Label>("UltimateNameLabel");
        ultimateStatusLabel = hudRoot.Q<Label>("UltimateStatusLabel");
        ultimateScreenEffect = hudRoot.Q<VisualElement>("UltimateScreenEffect");
        pauseAudioSettings = new PauseAudioSettings(menuRoot, hudRoot);
        hudSortingOrder = hudDocument.sortingOrder;
        returnButton.clicked += ReturnToMenu;
        retrySaveButton.clicked += RetrySaveMatch;
        if (replayButton != null) replayButton.clicked += ReplayRound;
        if (loginButton != null) loginButton.clicked += ReturnToLogin;
        retrySaveButton.style.display = DisplayStyle.None;
        if (resumeButton != null) resumeButton.clicked += ResumeFromButton;
        if (pauseScreen != null) pauseScreen.style.display = DisplayStyle.None;
        battleOverlay.style.display = DisplayStyle.None;
        defeatScreen.style.display = DisplayStyle.None;
        HideMatchIntro();

        if (characterSelection != null)
        {
            characterSelection.Selected += BeginSelectedRound;
            characterSelection.Cancelled += CancelCharacterSelection;
        }

        // Keep Murilo's StartGame/score API, but let this scene own Escape navigation.
        // GameManager's legacy Update would end/save the round and quit instead of resuming.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.enabled = false;
        }
    }

    private void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || hudRoot == null)
        {
            return;
        }

        PlayerData currentData = manager.PlayerData;
        bool startRequested = !roundActive && !IsStartingRound &&
            (characterSelection == null || !characterSelection.IsOpen) &&
            PauseManager.Instance != null && !PauseManager.Instance.IsPaused;
        if (currentData != null &&
            (!ReferenceEquals(currentData, observedPlayerData) || startRequested))
        {
            ClearBattlePause();
            StopRoundIntro();
            StopRoundResult();
            roundActive = false;
            enemySpawner?.StopSpawning(true);
            // AuthMenu may reuse a cached account. Keep it untouched and give this round
            // its own score, retaining the ID and fields used by Murilo's existing API.
            observedPlayerData = new PlayerData(currentData.id, currentData.name,
                currentData.email, currentData.password, 0,
                currentData.created_at, currentData.updated_at);
            manager.StartGame(observedPlayerData);
            battleOverlay.style.display = DisplayStyle.None;
            defeatScreen.style.display = DisplayStyle.None;
            menuDocument.rootVisualElement.style.display = DisplayStyle.None;
            BattleAudioController.StopMusic();
            if (characterSelection != null)
            {
                roundActive = false;
                GameManager.Instance.enabled = false;
                PauseManager.Instance.SetPause(true);
                characterSelection.Open();
            }
            else
            {
                BeginRound();
            }
        }

        if (roundActive)
        {
            // Resolve the deadline before accepting another pause or scoring a late kill.
            if (!IsBattlePaused && ElapsedSeconds >= activeTimeLimit)
            {
                FinishRound(playerHealth.IsDead ? "derrota" : "empate");
                return;
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetBattlePaused(!IsBattlePaused);
            UpdatePlayerUltimate();
            RefreshUltimateFeedback();
            RefreshHud();
        }
    }

    /// <summary>
    /// Pauses only a live battle. Suspends gameplay/physics without changing Time.timeScale,
    /// saving scores or changing the existing menu/selection pause state.
    /// </summary>
    public void SetBattlePaused(bool paused)
    {
        if (!roundActive || IsStartingRound || playerHealth.IsDead ||
            pauseScreen == null || paused == IsBattlePaused) return;

        if (paused)
        {
            IsBattlePaused = true;
            StartPauseTimer();
            PauseManager.Instance?.SetPause(true);
            // Keep the global Unity clock running. Retain each dynamic body's original
            // constraints, gravity and momentum so projectile trajectories can resume.
            foreach (Rigidbody body in FindObjectsByType<Rigidbody>(FindObjectsSortMode.None))
            {
                if (body.isKinematic) continue;
                pausedBodies.Add(body, new PausedBody
                {
                    Constraints = body.constraints,
                    UseGravity = body.useGravity,
                    Velocity = body.linearVelocity,
                    AngularVelocity = body.angularVelocity
                });
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                body.constraints = RigidbodyConstraints.FreezeAll;
            }
            // Prevent projectile FixedUpdate from replacing the saved momentum while paused.
            foreach (Shell shell in FindObjectsByType<Shell>(FindObjectsSortMode.None))
                SuspendProjectile(shell);
            foreach (AIShell shell in FindObjectsByType<AIShell>(FindObjectsSortMode.None))
                SuspendProjectile(shell);
            foreach (ParticleSystem particles in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (!particles.isPlaying) continue;
                pausedParticles.Add(particles);
                particles.Pause(false);
            }
            // Battle models use unscaled animation for death/results; freeze them explicitly here.
            foreach (TankSpawn spawn in tankSpawns)
            {
                if (spawn.Health == null) continue;
                foreach (Animator animator in spawn.Health.GetComponentsInChildren<Animator>(true))
                {
                    if (pausedAnimatorSpeeds.ContainsKey(animator)) continue;
                    pausedAnimatorSpeeds.Add(animator, animator.speed);
                    animator.speed = 0f;
                }
            }
            pauseScreen.style.display = DisplayStyle.Flex;
            pauseAudioSettings?.Refresh();
            hudRoot.pickingMode = PickingMode.Position;
            resumeButton?.Focus();
        }
        else
        {
            ClearBattlePause();
        }
    }

    private void ResumeFromButton()
    {
        if (!IsBattlePaused) return;
        BattleAudioController.PlayButtonClick();
        SetBattlePaused(false);
    }

    private void OnHudGeometryChanged(GeometryChangedEvent change)
    {
        // Long save errors must not squeeze the character preview out of a small Game view.
        hudRoot.EnableInClassList("battle-compact", change.newRect.height < 720f);
    }

    private void SuspendProjectile(Behaviour projectile)
    {
        if (!projectile.enabled) return;
        pausedProjectiles.Add(projectile);
        projectile.enabled = false;
    }

    /// <summary>Restores only state owned by this overlay, including scene unload and round end.</summary>
    private void ClearBattlePause()
    {
        if (IsBattlePaused)
        {
            FinishPauseTimer();
            foreach (KeyValuePair<Animator, float> entry in pausedAnimatorSpeeds)
                if (entry.Key != null) entry.Key.speed = entry.Value;
            pausedAnimatorSpeeds.Clear();
            foreach (KeyValuePair<Rigidbody, PausedBody> entry in pausedBodies)
            {
                if (entry.Key == null) continue;
                Rigidbody body = entry.Key;
                body.constraints = entry.Value.Constraints;
                body.useGravity = entry.Value.UseGravity;
                if (!body.isKinematic)
                {
                    body.linearVelocity = entry.Value.Velocity;
                    body.angularVelocity = entry.Value.AngularVelocity;
                }
            }
            pausedBodies.Clear();
            foreach (Behaviour projectile in pausedProjectiles)
                if (projectile != null) projectile.enabled = true;
            pausedProjectiles.Clear();
            foreach (ParticleSystem particles in pausedParticles)
                if (particles != null) particles.Play(false);
            pausedParticles.Clear();
            IsBattlePaused = false;
            PauseManager.Instance?.SetPause(false);
        }
        if (pauseScreen != null) pauseScreen.style.display = DisplayStyle.None;
        if (hudRoot != null) hudRoot.pickingMode = PickingMode.Ignore;
    }

    // O relogio real segue em Time.timeScale = 1; descontamos somente pausas da batalha.
    private void StartPauseTimer()
    {
        if (roundActive) pausedAtRealtime = Time.realtimeSinceStartupAsDouble;
    }

    private void FinishPauseTimer()
    {
        if (pausedAtRealtime <= 0) return;
        pausedSeconds += Time.realtimeSinceStartupAsDouble - pausedAtRealtime;
        pausedAtRealtime = 0;
    }

    private void OnDestroy()
    {
        resultRanking?.Dispose();
        pauseAudioSettings?.Dispose();
        if (ultimateWindInstance != null) Destroy(ultimateWindInstance);
        hudRoot?.UnregisterCallback<GeometryChangedEvent>(OnHudGeometryChanged);
        ClearBattlePause();
        if (characterSelection != null)
        {
            characterSelection.Selected -= BeginSelectedRound;
            characterSelection.Cancelled -= CancelCharacterSelection;
        }
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
        }

        if (enemyHealths != null)
        {
            foreach (TankHealth enemyHealth in enemyHealths)
            {
                if (enemyHealth != null)
                {
                    enemyHealth.Died -= OnEnemyDied;
                }
            }
        }

        if (returnButton != null)
        {
            returnButton.clicked -= ReturnToMenu;
        }
        if (retrySaveButton != null) retrySaveButton.clicked -= RetrySaveMatch;
        if (replayButton != null) replayButton.clicked -= ReplayRound;
        if (loginButton != null) loginButton.clicked -= ReturnToLogin;
        if (resumeButton != null) resumeButton.clicked -= ResumeFromButton;
    }

    private void OnDisable()
    {
        StopPlayerUltimate();
        ClearBattlePause();
        enemySpawner?.StopSpawning();
        StopRoundIntro();
        StopRoundResult();
    }

    private static TankSpawn CaptureSpawn(TankHealth health)
    {
        return new TankSpawn
        {
            Health = health,
            Position = health.transform.position,
            Rotation = health.transform.rotation
        };
    }

    private void BeginSelectedRound(int selectedIndex)
    {
        if (IsStartingRound) return;
        selectedCharacterIndex = selectedIndex;
        if (matchIntro != null && matchFade != null && countdownLabel != null)
        {
            IsStartingRound = true;
            roundIntro = StartCoroutine(PlayRoundIntro(selectedIndex));
            return;
        }

        characterSelection.Close();
        ApplySelectedCharacters(selectedIndex);
        BeginRound();
    }

    private void ApplySelectedCharacters(int selectedIndex)
    {
        // Keep stable gameplay actors: only their cosmetic model and weapon transforms change.
        // This preserves Murilo's public score API and the existing HP/death event subscriptions.
        CharacterSelectionController.CharacterOption selected = characterSelection.GetCharacter(selectedIndex);
        playerAppearance.ApplyCharacter(selected.ModelPrefab, selected.DisplayName, selected.ClassDefinition);

        List<int> candidates = characterSelection.CreateEnemyRoster(selectedIndex, enemyAppearances.Length);
        if (candidates.Count < enemyAppearances.Length)
        {
            Debug.LogError("Character selection needs four distinct enemy character families.", this);
            return;
        }

        for (int enemyIndex = 0; enemyIndex < enemyAppearances.Length; enemyIndex++)
        {
            if (enemyAppearances[enemyIndex] == null) continue;
            CharacterSelectionController.CharacterOption option =
                characterSelection.GetCharacter(candidates[enemyIndex]);
            enemyAppearances[enemyIndex].ApplyCharacter(option.ModelPrefab, option.DisplayName, option.ClassDefinition);
        }
    }

    private IEnumerator PlayRoundIntro(int selectedIndex)
    {
        // Cover the selection first; move/reset actors and switch cameras only behind the fade.
        roundActive = false;
        GameManager.Instance.enabled = false;
        PauseManager.Instance.SetPause(true);
        hudDocument.sortingOrder = Mathf.Max(30f, hudSortingOrder);
        hudRoot.pickingMode = PickingMode.Position;
        matchIntro.style.display = DisplayStyle.Flex;
        countdownLabel.style.display = DisplayStyle.None;
        yield return FadeMatch(0f, 1f);

        characterSelection.Close();
        ApplySelectedCharacters(selectedIndex);
        PrepareRound();
        // Cinemachine can finish the overview -> player blend while the screen is black.
        yield return new WaitForSecondsRealtime(cameraSettleTime);
        yield return FadeMatch(1f, 0f);

        countdownLabel.style.display = DisplayStyle.Flex;
        for (int number = 1; number <= 3; number++)
        {
            countdownLabel.text = number.ToString();
            BattleAudioController.PlayCountdown(number);
            yield return new WaitForSecondsRealtime(countdownStepDuration);
        }
        countdownLabel.text = "JÁ!";
        BattleAudioController.PlayMatchStart();
        yield return new WaitForSecondsRealtime(goDuration);

        HideMatchIntro();
        IsStartingRound = false;
        roundIntro = null;
        StartCombat();
    }

    private IEnumerator FadeMatch(float from, float to)
    {
        float elapsed = 0f;
        matchFade.style.opacity = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            matchFade.style.opacity = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        matchFade.style.opacity = to;
    }

    private void HideMatchIntro()
    {
        if (matchIntro != null) matchIntro.style.display = DisplayStyle.None;
        if (hudDocument != null) hudDocument.sortingOrder = hudSortingOrder;
        if (hudRoot != null) hudRoot.pickingMode = PickingMode.Ignore;
    }

    private void StopRoundIntro()
    {
        if (roundIntro != null) StopCoroutine(roundIntro);
        roundIntro = null;
        IsStartingRound = false;
        HideMatchIntro();
    }

    private void CancelCharacterSelection()
    {
        enemySpawner?.StopSpawning(true);
        // Cancelling an unstarted round must not submit a score to the server.
        GameManager.Instance.enabled = false;
        PauseManager.Instance.SetPause(true);
        ShowMainMenu();
        if (battleCamera != null) battleCamera.ShowMenu();
    }

    private void BeginRound()
    {
        PrepareRound();
        StartCombat();
    }

    private void PrepareRound()
    {
        StopPlayerUltimate();
        playerUltimate.Configure(playerAppearance != null ? playerAppearance.ClassDefinition : null);
        ClearBattlePause();
        ClearCombatObjects();
        activeTimeLimit = Math.Max(1, matchTimeLimitSeconds);
        activeEliminationTarget = Math.Max(1, eliminationTarget);
        enemiesKilled = 0;
        roundDurationSeconds = 0;
        roundOutcome = null;

        foreach (TankSpawn spawn in tankSpawns)
        {
            GameObject tank = spawn.Health.gameObject;
            tank.SetActive(false);
            ApplySupportedClassAttributes(tank);
            spawn.Health.RestoreFullHealth();
            tank.transform.SetPositionAndRotation(spawn.Position, spawn.Rotation);

            if (tank.TryGetComponent(out Rigidbody body))
            {
                body.position = spawn.Position;
                body.rotation = spawn.Rotation;
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
            }

            tank.SetActive(enemySpawner == null || spawn.Health == playerHealth);
        }

        Physics.SyncTransforms();

        if (playerHealth.TryGetComponent(out Drive drive) && drive.cannon != null)
        {
            drive.ResetAim();
        }

        foreach (TankHealth enemyHealth in enemyHealths)
        {
            if (enemyHealth.TryGetComponent(out AITank aiTank))
            {
                aiTank.ResetForNewRound();
            }

            if (enemyHealth.TryGetComponent(out EnemyTankStateMachine stateMachine))
            {
                stateMachine.ResetForNewRound();
            }
        }

        enemySpawner?.PrepareForRound();
        if (battleCamera != null) battleCamera.ShowBattle(playerHealth.transform);
        battleOverlay.style.display = DisplayStyle.Flex;
        hudRoot.pickingMode = IsStartingRound ? PickingMode.Position : PickingMode.Ignore;
        battleHud.style.display = DisplayStyle.Flex;
        defeatScreen.style.display = DisplayStyle.None;
        RefreshHud();
    }

    private void StartCombat()
    {
        playerUltimate.Configure(playerAppearance != null ? playerAppearance.ClassDefinition : null);
        BattleAudioController.PlayGameplayMusic();
        // Este é o início real da partida. O UUID identifica uma tentativa no MySQL;
        // seleção de personagem e contagem regressiva não entram na duração.
        GameManager.Instance.enabled = false;
        roundId = Guid.NewGuid().ToString("D");
        roundStartedUtc = DateTime.UtcNow;
        roundStartedRealtime = Time.realtimeSinceStartupAsDouble;
        pausedSeconds = 0;
        pausedAtRealtime = 0;
        enemiesKilled = 0;
        roundDurationSeconds = 0;
        roundOutcome = null;
        saveInProgress = false;
        matchSaved = false;
        saveStatusLabel.text = "";
        retrySaveButton.style.display = DisplayStyle.None;
        if (PauseManager.Instance != null) PauseManager.Instance.SetPause(false);
        roundActive = true;
        enemySpawner?.StartSpawning();
        RefreshHud();
    }

    private void OnEnemyDied(TankHealth enemyHealth, GameObject source)
    {
        if (!roundActive || IsBattlePaused || source != playerHealth.gameObject ||
            GameManager.Instance == null || GameManager.Instance.PlayerData == null)
        {
            return;
        }

        if (playerHealth.IsDead || ElapsedSeconds >= activeTimeLimit)
        {
            FinishRound(playerHealth.IsDead ? "derrota" : "empate");
            return;
        }

        playerUltimate.AddElimination();
        enemiesKilled++;
        RefreshHud();
        if (enemiesKilled >= activeEliminationTarget)
        {
            FinishRound("vitoria");
            return;
        }
        if (!playerHealth.IsDead && characterSelection != null &&
            selectedCharacterIndex >= 0 && selectedCharacterIndex < characterSelection.CharacterCount)
            BattleAudioController.TryPlayEliminationVoice(characterSelection.GetCharacter(selectedCharacterIndex).VoiceClip);
    }

    private void OnPlayerDied(TankHealth health, GameObject source)
    {
        FinishRound("derrota");
    }

    /// <summary>
    /// The first terminal condition wins. Snapshot all submitted data once so retries
    /// cannot change the duration, outcome or UUID of an already finished match.
    /// </summary>
    private void FinishRound(string outcome)
    {
        if (!roundActive) return;
        roundDurationSeconds = Math.Round(Math.Max(0.001, Math.Min(activeTimeLimit, ElapsedSeconds)),
            3, MidpointRounding.AwayFromZero);
        roundActive = false;
        // O timestamp é UTC; a duração vem do relógio real descontando pausas.
        // Isso mantém os horários comparáveis sem alterar Time.timeScale.
        roundFinishedUtc = DateTime.UtcNow;
        roundOutcome = outcome;
        StopPlayerUltimate();
        ClearBattlePause();
        enemySpawner?.StopSpawning();
        StopCombatDamage();
        BattleAudioController.StopMusic();
        BattleAudioController.StopCharacterVoice();

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetPause(true);
        }

        GameManager manager = GameManager.Instance;
        manager.enabled = false;

        resultTimeCaptionLabel.text = outcome == "vitoria" ? "TEMPO DE CONCLUSÃO" : "DURAÇÃO DA PARTIDA";
        finalTimeLabel.text = MatchTime.Format(roundDurationSeconds);
        resultTitleLabel.text = outcome == "vitoria" ? "VITÓRIA!" :
            outcome == "empate" ? "EMPATE — TEMPO ESGOTADO" : "DERROTA";
        resultEliminationsLabel.text = $"ELIMINAÇÕES: {enemiesKilled} / {activeEliminationTarget}";
        resultRankingNoteLabel.text = outcome == "vitoria"
            ? "Aguardando salvamento para consultar sua colocação."
            : "Apenas vitórias entram no ranking de conclusão.";
        resultRanking?.Prepare(outcome == "vitoria");
        // Only the new match endpoint is called, not the legacy account-score update.
        RetrySaveMatch();
        RefreshHud();
        if (playerHealth.IsDead && playerHealth.DeathAnimationDuration > 0f)
            roundResult = StartCoroutine(WaitForDeathThenShowResult(playerHealth.DeathAnimationDuration));
        else ShowRoundResult();
    }

    private IEnumerator WaitForDeathThenShowResult(float duration)
    {
        yield return new WaitForSecondsRealtime(duration + 0.15f);
        roundResult = null;
        ShowRoundResult();
    }

    private void ShowRoundResult()
    {
        BattleAudioController.PlayResult();
        hudRoot.pickingMode = PickingMode.Position;
        battleHud.style.display = DisplayStyle.None;
        defeatScreen.style.display = DisplayStyle.Flex;
        characterSelection?.ShowResultPreview(selectedCharacterIndex, resultCharacterPreview, roundOutcome);
    }

    private void RetrySaveMatch()
    {
        if (saveInProgress || matchSaved) return;
        saveInProgress = true;
        saveStatusLabel.text = "Salvando partida...";
        retrySaveButton.style.display = DisplayStyle.None;
        returnButton.SetEnabled(false);
        replayButton?.SetEnabled(false);
        loginButton?.SetEnabled(false);

        // Repetir usa o mesmo UUID e os mesmos dados; a Model não cria outra partida.
        string submittedId = roundId;
        // Keep the legacy points column/API compatible without awarding points per kill.
        // Performance is the existing duracao_segundos field, ranked only for victories.
        GameManager.Instance.DataManager.SaveMatch(observedPlayerData.id, submittedId,
            roundStartedUtc, roundFinishedUtc, roundDurationSeconds, roundOutcome, 0, enemiesKilled,
            (success, error) =>
            {
                if (this == null || submittedId != roundId) return;
                saveInProgress = false;
                returnButton.SetEnabled(true);
                replayButton?.SetEnabled(true);
                loginButton?.SetEnabled(true);
                if (success)
                {
                    matchSaved = true;
                    saveStatusLabel.text = "Partida salva com sucesso.";
                    // Query only after commit confirmation, so a new personal best is already visible.
                    if (roundOutcome == "vitoria")
                        resultRanking?.Load(GameManager.Instance.DataManager, observedPlayerData.id, roundId, roundDurationSeconds);
                }
                else
                {
                    saveStatusLabel.text = error + " Você pode tentar novamente ou voltar sem salvar.";
                    retrySaveButton.style.display = DisplayStyle.Flex;
                }
            });
    }

    private void StopRoundResult()
    {
        resultRanking?.Close();
        if (roundResult != null) StopCoroutine(roundResult);
        roundResult = null;
        characterSelection?.Close();
    }

    private void ReturnToMenu()
    {
        StopPlayerUltimate();
        BattleAudioController.PlayButtonClick();
        roundActive = false;
        ClearBattlePause();
        enemySpawner?.StopSpawning(true);
        StopRoundIntro();
        StopRoundResult();
        if (GameManager.Instance != null) GameManager.Instance.enabled = false;
        if (PauseManager.Instance != null) PauseManager.Instance.SetPause(true);
        if (battleCamera != null) battleCamera.ShowMenu();
        defeatScreen.style.display = DisplayStyle.None;
        battleOverlay.style.display = DisplayStyle.None;
        hudRoot.pickingMode = PickingMode.Ignore;
        ShowMainMenu();
    }

    private void ReplayRound()
    {
        if (roundActive || IsStartingRound || saveInProgress || observedPlayerData == null) return;
        BattleAudioController.PlayButtonClick();
        StopRoundResult();
        defeatScreen.style.display = DisplayStyle.None;
        battleOverlay.style.display = DisplayStyle.None;
        PlayerData player = observedPlayerData;
        // Reuse the authenticated account, not its finished round. Update opens selection again;
        // StartCombat supplies a fresh UUID and resets time, eliminations, HP and skill charge.
        GameManager.Instance.StartGame(new PlayerData(player.id, player.name, player.email, null, 0,
            player.created_at, player.updated_at));
    }

    private void ReturnToLogin()
    {
        ReturnToMenu();
        GameManager.Instance?.DataManager?.CancelLogin();
        MenuManager menu = menuManager != null ? menuManager : MenuManager.Instance;
        menu?.OpenPlayScreen();
    }

    private void ShowMainMenu()
    {
        BattleAudioController.PlayMenuMusic();
        menuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        // Use the public navigation API so login, score and settings are closed too.
        MenuManager menu = menuManager != null ? menuManager : MenuManager.Instance;
        if (menu != null) menu.OpenMainMenu();
        else if (mainMenu != null) mainMenu.style.display = DisplayStyle.Flex;
    }

    private void RefreshHud()
    {
        healthLabel.text = $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        float healthPercent = playerHealth.MaxHealth > 0
            ? 100f * playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;
        healthFill.style.width = new Length(healthPercent, LengthUnit.Percent);

        int seconds = (int)Math.Ceiling(RemainingSeconds);
        timeLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
        eliminationLabel.text = $"{enemiesKilled} / {activeEliminationTarget}";
        RefreshUltimateHud();
    }

    private static void ApplySupportedClassAttributes(GameObject tank)
    {
        CharacterClassDefinition definition = tank.GetComponent<TankAppearance>()?.ClassDefinition;
        bool active = definition != null && definition.GameplayEnabled;
        tank.GetComponent<TankHealth>()?.SetClassHealthMultiplier(active ? definition.HealthMultiplier : 1f);
        tank.GetComponent<Drive>()?.SetClassSpeedMultiplier(active ? definition.MovementSpeedMultiplier : 1f);
        tank.GetComponent<EnemyTankMovement>()?.SetClassSpeedMultiplier(active ? definition.MovementSpeedMultiplier : 1f);
    }

    private void UpdatePlayerUltimate()
    {
        if (!roundActive || IsStartingRound || IsBattlePaused || playerHealth.IsDead ||
            PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        playerUltimate.Tick(Time.unscaledDeltaTime);
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame))
            playerUltimate.TryActivate();
        playerHealth.GetComponent<Drive>()?.SetUltimateSpeedMultiplier(playerUltimate.SpeedMultiplier);
    }

    private void StopPlayerUltimate()
    {
        playerUltimate.Reset();
        StopUltimateFeedback();
        if (playerHealth != null) playerHealth.GetComponent<Drive>()?.SetUltimateSpeedMultiplier(1f);
    }

    private void RefreshUltimateFeedback()
    {
        if (!roundActive || !playerUltimate.IsActive || playerHealth == null || playerHealth.IsDead)
        {
            StopUltimateFeedback();
            return;
        }

        bool paused = IsBattlePaused || PauseManager.Instance != null && PauseManager.Instance.IsPaused;
        if (!paused) ultimateVisualTime += Time.unscaledDeltaTime;
        if (ultimateScreenEffect != null)
        {
            ultimateScreenEffect.style.display = DisplayStyle.Flex;
            ultimateScreenEffect.style.opacity = .7f + .12f * Mathf.Sin(ultimateVisualTime * 3f);
        }

        if (ultimateWindInstance == null && ultimateWindPrefab != null)
        {
            ultimateWindInstance = Instantiate(ultimateWindPrefab, playerHealth.transform);
            ultimateWindInstance.name = "Suzuka Ultimate Wind";
            ultimateWindInstance.transform.SetLocalPositionAndRotation(new Vector3(0f, .7f, 0f), Quaternion.identity);
            ultimateWindInstance.transform.localScale = Vector3.one * ultimateWindScale;
            ultimateWindParticles = ultimateWindInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particles in ultimateWindParticles)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.maxParticles = Mathf.Min(main.maxParticles, 96);
                main.startColor = new Color(.65f, .9f, 1f, .55f);
                var collision = particles.collision;
                collision.enabled = false; // A visual effect must never push a tank or block a projectile.
            }
            foreach (Collider collider in ultimateWindInstance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
        if (ultimateWindInstance == null) return;
        ultimateWindInstance.SetActive(true);
        foreach (ParticleSystem particles in ultimateWindParticles)
        {
            if (paused && particles.isPlaying) particles.Pause(false);
            else if (!paused && !particles.isPlaying) particles.Play(false);
        }
    }

    private void StopUltimateFeedback()
    {
        ultimateVisualTime = 0f;
        if (ultimateScreenEffect != null) ultimateScreenEffect.style.display = DisplayStyle.None;
        if (ultimateWindInstance == null || !ultimateWindInstance.activeSelf) return;
        foreach (ParticleSystem particles in ultimateWindParticles)
        {
            // Ending a paused round must not let ClearBattlePause restart this cosmetic effect.
            pausedParticles.Remove(particles);
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        ultimateWindInstance.SetActive(false);
    }

    private void RefreshUltimateHud()
    {
        if (ultimateCard == null) return;
        CharacterClassDefinition definition = playerUltimate.Definition;
        bool available = playerUltimate.IsEnabled;
        ultimateNameLabel.text = definition != null ? definition.UltimateName : "Unique Skill";
        ultimateStatusLabel.text = !available ? "EM PREPARAÇÃO" : playerUltimate.IsActive
            ? $"EM USO — {playerUltimate.RemainingDuration:F1} s" : playerUltimate.IsReady
                ? "SHIFT — PRONTA" : "CARREGANDO";
        float fill = playerUltimate.IsActive && definition != null
            ? playerUltimate.RemainingDuration / Mathf.Max(0.1f, definition.UltimateDurationSeconds)
            : playerUltimate.Charge / 100f;
        ultimateMask.style.height = new Length(100f * Mathf.Clamp01(fill), LengthUnit.Percent);
        Texture2D icon = definition != null ? definition.UltimateIcon : null;
        ultimateIcon.style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
        ultimatePlaceholder.style.display = icon != null ? DisplayStyle.None : DisplayStyle.Flex;
        ultimateCard.EnableInClassList("ultimate-ready", playerUltimate.IsReady);
        ultimateCard.EnableInClassList("ultimate-active", playerUltimate.IsActive);
        ultimateCard.EnableInClassList("ultimate-unavailable", !available);
    }

    private static void StopCombatDamage()
    {
        CombatEffects.ClearBursts();
        foreach (Shell shell in FindObjectsByType<Shell>(FindObjectsSortMode.None))
        {
            shell.gameObject.SetActive(false);
            Destroy(shell.gameObject);
        }

        foreach (AIShell shell in FindObjectsByType<AIShell>(FindObjectsSortMode.None))
        {
            shell.gameObject.SetActive(false);
            Destroy(shell.gameObject);
        }

        foreach (ExplosionDamage explosion in FindObjectsByType<ExplosionDamage>(FindObjectsSortMode.None))
        {
            if (explosion.TryGetComponent(out SphereCollider area))
            {
                area.enabled = false;
            }
        }
    }

    private static void ClearCombatObjects()
    {
        StopCombatDamage();
        foreach (ExplosionDamage explosion in FindObjectsByType<ExplosionDamage>(FindObjectsSortMode.None))
        {
            explosion.gameObject.SetActive(false);
            Destroy(explosion.gameObject);
        }
    }
}
