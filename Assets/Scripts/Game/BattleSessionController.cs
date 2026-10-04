using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Connects the battle HUD and round lifecycle to the existing menu and score API.
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
    [SerializeField, Min(1)] private int pointsPerEnemy = 100;

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
    private Label scoreLabel;
    private Label finalScoreLabel;
    private Button returnButton;
    private VisualElement pauseScreen;
    private Button resumeButton;
    private float timeScaleBeforePause;
    private readonly Dictionary<Animator, float> pausedAnimatorSpeeds = new();
    private bool roundActive;
    private VisualElement matchIntro;
    private VisualElement matchFade;
    private Label countdownLabel;
    private Coroutine roundIntro;
    private Coroutine roundResult;
    private VisualElement resultCharacterPreview;
    private int selectedCharacterIndex;
    private float hudSortingOrder;

    public bool IsRoundActive => roundActive;
    public bool IsBattlePaused { get; private set; }
    public bool IsStartingRound { get; private set; }
    public int RoundScore => observedPlayerData != null ? observedPlayerData.pontos : 0;

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
        if (!hudRoot.styleSheets.Contains(hudStyle)) hudRoot.styleSheets.Add(hudStyle);
        hudRoot.pickingMode = PickingMode.Ignore;

        battleOverlay = hudRoot.Q<VisualElement>("BattleOverlay");
        battleHud = hudRoot.Q<VisualElement>("BattleHud");
        defeatScreen = hudRoot.Q<VisualElement>("DefeatScreen");
        resultCharacterPreview = hudRoot.Q<VisualElement>("ResultCharacterPreview");
        healthFill = hudRoot.Q<VisualElement>("HealthFill");
        healthLabel = hudRoot.Q<Label>("HealthLabel");
        scoreLabel = hudRoot.Q<Label>("ScoreLabel");
        finalScoreLabel = hudRoot.Q<Label>("FinalScoreLabel");
        returnButton = hudRoot.Q<Button>("ReturnToMenuButton");
        pauseScreen = hudRoot.Q<VisualElement>("PauseScreen");
        resumeButton = hudRoot.Q<Button>("ResumeButton");
        matchIntro = hudRoot.Q<VisualElement>("MatchIntro");
        matchFade = hudRoot.Q<VisualElement>("MatchFade");
        countdownLabel = hudRoot.Q<Label>("CountdownLabel");
        hudSortingOrder = hudDocument.sortingOrder;
        returnButton.clicked += ReturnToMenu;
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
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetBattlePaused(!IsBattlePaused);
            RefreshHud();
        }
    }

    /// <summary>
    /// Pauses only a live battle. Freezes physics and scaled timers without ending the run,
    /// saving scores or changing the existing menu/selection pause state.
    /// </summary>
    public void SetBattlePaused(bool paused)
    {
        if (!roundActive || IsStartingRound || playerHealth.IsDead ||
            pauseScreen == null || paused == IsBattlePaused) return;

        if (paused)
        {
            timeScaleBeforePause = Time.timeScale;
            IsBattlePaused = true;
            PauseManager.Instance?.SetPause(true);
            Time.timeScale = 0f;
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

    /// <summary>Releases only time owned by this overlay, including scene unload and round end.</summary>
    private void ClearBattlePause()
    {
        if (IsBattlePaused)
        {
            foreach (KeyValuePair<Animator, float> entry in pausedAnimatorSpeeds)
                if (entry.Key != null) entry.Key.speed = entry.Value;
            pausedAnimatorSpeeds.Clear();
            Time.timeScale = timeScaleBeforePause;
            IsBattlePaused = false;
            PauseManager.Instance?.SetPause(false);
        }
        if (pauseScreen != null) pauseScreen.style.display = DisplayStyle.None;
        if (hudRoot != null) hudRoot.pickingMode = PickingMode.Ignore;
    }

    private void OnDestroy()
    {
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
        if (resumeButton != null) resumeButton.clicked -= ResumeFromButton;
    }

    private void OnDisable()
    {
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
        playerAppearance.ApplyCharacter(selected.ModelPrefab, selected.DisplayName);
        int enemyIndex = 0;
        for (int i = 0; i < characterSelection.CharacterCount; i++)
        {
            if (i == selectedIndex) continue;
            CharacterSelectionController.CharacterOption option = characterSelection.GetCharacter(i);
            enemyAppearances[enemyIndex++].ApplyCharacter(option.ModelPrefab, option.DisplayName);
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
        ClearBattlePause();
        ClearCombatObjects();

        foreach (TankSpawn spawn in tankSpawns)
        {
            GameObject tank = spawn.Health.gameObject;
            tank.SetActive(false);
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
        BattleAudioController.PlayGameplayMusic();
        // StartGame, AddScore and EndGame remain callable; only the legacy Escape Update is disabled.
        GameManager.Instance.enabled = false;
        if (PauseManager.Instance != null) PauseManager.Instance.SetPause(false);
        roundActive = true;
        enemySpawner?.StartSpawning();
    }

    private void OnEnemyDied(TankHealth enemyHealth, GameObject source)
    {
        if (!roundActive || source != playerHealth.gameObject ||
            GameManager.Instance == null || GameManager.Instance.PlayerData == null)
        {
            return;
        }

        GameManager.Instance.AddScore(pointsPerEnemy);
        RefreshHud();
        if (!playerHealth.IsDead && characterSelection != null &&
            selectedCharacterIndex >= 0 && selectedCharacterIndex < characterSelection.CharacterCount)
            BattleAudioController.TryPlayEliminationVoice(characterSelection.GetCharacter(selectedCharacterIndex).VoiceClip);
    }

    private void OnPlayerDied(TankHealth health, GameObject source)
    {
        if (!roundActive)
        {
            return;
        }

        roundActive = false;
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
        int finalScore = RoundScore;
        if (manager != null && manager.PlayerData != null)
        {
            manager.EndGame();
            manager.enabled = false;
        }

        finalScoreLabel.text = finalScore.ToString();
        RefreshHud();
        if (health.DeathAnimationDuration > 0f)
            roundResult = StartCoroutine(WaitForDeathThenShowResult(health.DeathAnimationDuration));
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
        characterSelection?.ShowResultPreview(selectedCharacterIndex, resultCharacterPreview);
    }

    private void StopRoundResult()
    {
        if (roundResult != null) StopCoroutine(roundResult);
        roundResult = null;
        characterSelection?.Close();
    }

    private void ReturnToMenu()
    {
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

        scoreLabel.text = RoundScore.ToString();
    }

    private static void StopCombatDamage()
    {
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
