/*
 * UMATANK - MAIN PROJECT DOCUMENTATION
 * Activity 2: Unity / PHP / MySQL. Entry scene: Assets/Scenes/UmaTank.unity.
 * Group: Hugo Marcos Santos dos Reis; João Pedro Ribeiro Vivas;
 *        Murillo Gomes Yonamine.
 *
 * PROJECT ORIGIN AND THE USE OF MODELS INSTEAD OF PRIMITIVES
 * The group confirmed an in-class agreement with Professor Eduardo to reuse the
 * tank game made for the Intelligent Agents A* activity in this second activity.
 * We therefore retained that game's imported character/tank visuals instead of
 * rebuilding its main presentation with primitives, and extended the existing
 * game with the required Unity-PHP-MySQL integration and additional features.
 * This records the classroom authorization reported by the group; it is not a
 * waiver written in the activity PDF, nor permission to use third-party assets.
 * The earlier A* implementation was also reused with the professor's approval.
 * AStarPathfinder.cs identifies the original exercise file and adapted lines.
 *
 * GAME IDEA, CONTROLS AND MATCH RULES
 * A single-arena tank battle with characters from Umamusume: Pretty Derby.
 * Authenticate, read the pre-game information, choose a character, then start.
 * WASD/arrows drive/turn; Q/E change cannon elevation; Space/left mouse fire;
 * Shift activates the selected character's ready Unique Skill; Escape pauses.
 * The authored scene sets a target of 10 player-attributed eliminations within
 * 180 active seconds. The spawner reuses four enemy actors during the match.
 * Win: reach the elimination target before the deadline. Loss: player HP reaches
 * zero. Draw: the player survives until the deadline without reaching the target.
 * There is one level, not a sequence of stages; Inspector objectives are captured
 * for each attempt. The first resolved terminal condition freezes that attempt.
 * Results offer replay, return to menu/login and a retry if saving fails.
 *
 * TIME AND PERFORMANCE CRITERION
 * Lower completion time is better, and ONLY victories qualify for the ranking.
 * The PHP Model selects each player's MIN(duracao_segundos), orders ascending
 * and returns the top 10. Equal stored milliseconds share competition ranks
 * (1, 1, 3); player ID only stabilizes display order, not a performance tie-break.
 * Defeats/draws remain in match history for analysis; they are not zero-time wins.
 * Duration = monotonic realtime - start realtime - accumulated battle pauses.
 * Selection, fade, countdown, result animation and the results screen are outside
 * the timed interval. UTC start/end are separate audit timestamps: their wall
 * interval can include pauses and is not the performance value.
 * Save/display use three decimal places. Legacy pontuacao is sent as zero only
 * to preserve the existing schema/API; neither kills nor converted points rank.
 * PauseManager gates gameplay; this controller freezes/restores bodies, projectiles,
 * animation and particles WITHOUT assigning Time.timeScale = 0. Pause does not save.
 *
 * ORGANIZATION AND MAIN DECISIONS
 * BattleSessionController: intro, active clock, objectives, HUD, pause and result.
 * CharacterSelectionController / TankAppearance: authored UI and replaceable visual
 * models on stable gameplay actors; costumes may bind distinct class/skill profiles,
 * while a stable character-family ID still excludes all player skins from enemy selection.
 * CharacterClassDefinition: shared read-only design assets. TankHealth and
 * TankUltimateState: per-actor HP, buffs and per-match skill charge, not asset state.
 * TankGrid / GridNode / AStarPathfinder: our orthogonal navigation graph and A*.
 * EnemyTankStateMachine: Patrol, Chase, Attack and a separate Reposition state.
 * EnemyTankMovement: follows A* nodes, including recovery; no direct escape slide.
 * EnemyTankPerception / AITank: FOV, cover raycasts, ballistic aim and shot cadence.
 * TankCollisionGuard: hull clearance/reservations, not an alternative pathfinder.
 * EnemyTankSpawner: bounded actor pool, free/reachable spawn markers outside view.
 * Shell / AIShell / ExplosionDamage: projectile motion and radial falloff; only
 * the firing actor is exempt from its own explosion, not all friendly actors.
 * BattleAudioController / PauseAudioSettings reuse Murillo's AudioManager/mixer.
 * DataManager is the sole Unity HTTP adapter; Unity never connects to MySQL.
 * Existing menu/login APIs remain in place. GameManager's legacy points/end-game
 * loop is disabled for this scene so it cannot submit a second, incompatible result.
 *
 * DATA COLLECTION, PURPOSE AND RETENTION
 * Before selection, CharacterSelection.uxml explains controls, rules and data use.
 * Account name/email identify the player; the password is checked/hashed by PHP,
 * is not part of a match payload and is never included in ranking responses.
 * jogador_id links the attempt to its authenticated player; partida_uuid identifies
 * one attempt and makes retries idempotent. A new attempt receives a fresh UUID.
 * inicio_utc / fim_utc establish when the attempt occurred; duracao_segundos stores
 * active play time; resultado distinguishes victory/loss/draw; inimigos_eliminados
 * records objective progress. These support history and gameplay analysis; victory
 * duration additionally supports ranking. pontuacao remains compatibility-only.
 * A token authenticates protected requests for the matching player; the client
 * keeps it in memory, while PHP persists its association with a one-day expiry.
 * Records are stored in the configured MySQL through PHP, locally in XAMPP in this
 * setup, for an indefinite period. There is no automatic match-retention purge.
 * Token expiration is NOT deletion of account or history. The pre-game notice
 * describes this behavior; it is not a claim of legal/privacy certification.
 *
 * HOW TO CONFIGURE AND RUN THE DELIVERED PROJECT
 * 1. Open the project in Unity 6000.3.5f2 and the UmaTank scene, already in build settings.
 * 2. Copy Assets/Backend/gsd to the XAMPP htdocs/gsd directory; start Apache and MySQL.
 * 3. In phpMyAdmin, create/select an EMPTY tank_gamers database and import
 *    Assets/Backend/tank_gamers.sql (NOT tank_gamers.sql.meta). Back up an existing
 *    database before replacing it. This source update never imports the live DB.
 * 4. Configure gsd/model/DB.php for that installation's host, database and DB user.
 *    The dump supplies tables/data, not a MySQL login or its grants. Grant the
 *    configured user the project's required CRUD access; do not publish passwords.
 * 5. Set DataManager.URL_BASE to the reachable PHP address (default localhost/gsd).
 *    localhost always means the machine running Unity/the build, not the author PC.
 *    The local HTTP classroom setup is not a production HTTPS deployment.
 * 6. Register a test account or use an account whose credentials the group supplied,
 *    log in, read the information, select a character and play a complete attempt.
 *    The web history/analysis is at /gsd/?action=partidas and uses its web login.
 * Required tables: jogadores (accounts), partidas (per-attempt history), tokens
 * (authentication). partidas has a player FK and UNIQUE(jogador_id, partida_uuid).
 * All three tables, structure AND sample data, are in the delivered SQL.
 *
 * TRANSPORT, SAVE ACKNOWLEDGMENT AND REPLAY
 * UnityWebRequest POST -> PHP index.php action -> Controller -> Model -> PDO/MySQL.
 * DataManager.cs documents each route, form field and JSON response. Tokens and
 * player association are checked by PHP for protected operations, not trusted UI.
 * StartCombat assigns the UUID/timestamps only after the intro. FinishRound takes
 * one immutable snapshot; RetrySaveMatch reuses it instead of generating a new row.
 * Only successful HTTP + valid JSON + success + a positive partida_id confirm save.
 * Errors/timeouts keep retry available and never show a false save confirmation.
 * Rankings load AFTER acknowledgment; previous-best comparison excludes this UUID
 * when the server supports it. Missing comparison data never proves a new record.
 * Replay resets gameplay but retains the authenticated account; returning to login
 * clears the client's token and invalidates any pending login response.
 *
 * OBSERVATION FROM THE DELIVERED TEST DATA (24 OCT 2026, LOCAL REVIEW)
 * The updated SQL contains 3 players and 15 attempts: 3 victories, 12 defeats and
 * no draws. Defeats are 80% of this small sample. Mean duration across ALL attempts
 * is 64.900 seconds; this is not the mean victory completion time. Victorious
 * attempts range from 90.527 to 120.719 seconds. Two players have victories; one
 * has only defeats. Keeping history exposes failed attempts instead of replacing
 * them with a best score. This sample suggests checking difficulty/readability
 * with more players; it does not establish imbalance or explain the cause of loss.
 * Basic delivery checks: valid/invalid login, distinct saved outcomes, repeated
 * attempts for multiple players, web aggregates, time ranking, and failed network
 * responses that must not confirm a save. Do not add artificial matches to live data.
 *
 * CONTRIBUTIONS, REUSE AND GENERATIVE-AI ASSISTANCE
 * Hugo: original basic enemy behavior/state organization, earlier A* reuse and
 * gameplay integration; manual HUD/ranking work connected to Murillo's existing
 * backend APIs; sound-clip integration. Classes/Unique Skills and aiming/firing
 * refinements were developed with generative-AI assistance.
 * Murillo: original menu, authentication/backend/persistence and audio infrastructure,
 * developed without generative-AI assistance, as reported by the group. Subsequent
 * AI-assisted integrations/adaptations are credited separately below.
 * João Pedro: imported character/environment assets, map and animation preparation;
 * supplied skill icons, additional character voices and the defeat music track.
 * OpenAI ChatGPT/Codex assisted development/documentation: planning and stuck points,
 * Input System migration, rotating character previews, fade/countdown/audio sync,
 * collision/replanning and blocked-shot recovery, spawn planning, class/Unique Skill
 * and aiming/firing extensions, documentation and validation support. AI also assisted
 * later HUD/results/time-ranking adjustments; this is separate from the original
 * manual implementation and does not replace the group's authorship of that work.
 * Generative coding assistance is distinct from the game's enemy AI; the group's
 * responsibility is to review, understand and test the delivered code. No generated
 * image assets were requested for this project; supplied art is used instead.
 *
 * THIRD-PARTY ASSET PROVENANCE (NOT A BLANKET LICENSE GRANT)
 * Characters and original 3D models: Umamusume: Pretty Derby, owned by Cygames.
 * According to the group, these models were extracted from the original game and
 * made available by fans on Sketchfab for fan use, not originally modeled by those
 * uploaders. Credit Cygames for the originals and identify extractors/uploaders
 * separately where supplied. Five Sketchfab pages are listed below; individual
 * pages/uploader details for later imports still need to be supplied. Availability
 * for fan use is recorded as group-provided information, not a verified blanket
 * license, public-domain dedication or permission covering every possible use.
 * Character voice audio, character images and skill icons are identified by the
 * group as material taken from Umamusume: Pretty Derby. Copyright Cygames, Inc.;
 * all rights reserved to Cygames. The team does not claim authorship of these
 * original materials. Official game source: https://umamusume.jp/
 * See Assets/Audio/Voices/Credits.txt and Assets/Skills/Credits.txt. This credits
 * ownership/origin; it does not assert a license or permission that was not confirmed.
 * Five model sources supplied by the group:
 * Super Creek: https://sketchfab.com/3d-models/super-creek-mummy-chibi-35e8ec62805e4639971e16fe0d1c5486
 * Fenomeno: https://sketchfab.com/3d-models/fenomeno-chibi-4d70e9d3e9f84fcea59cea4c36a42fbb
 * Silence Suzuka: https://sketchfab.com/3d-models/silence-suzuka-chibi-7bbcd93026544ab185da740c7bf11444
 * Meisho Doto: https://sketchfab.com/3d-models/doto-chibi-6bcf14470d404d749124a395badcbf62
 * Matikanetannhauser: https://sketchfab.com/3d-models/mambo-8802ef2239ea4ef196f4239bfb6d4280
 * Additional models/animations, trees, bushes and font were imported by group
 * members; their individual authors/source/license details still need to be supplied
 * by the importer where absent. Do not infer a license from a download or credit.
 * JP's later additions: Assets/Skills (59 supplied PNG icons), 18 additional clips
 * in Assets/Audio/Voices and Assets/Audio/Music/sad_game_over.wav. Character media
 * origin/ownership is credited above as confirmed by the group. The separate defeat
 * track was modified/edited by João Pedro Ribeiro Vivas (JP), as the group confirmed.
 * This credits the modification, not an underlying original composition. Any underlying
 * source/composer/license still needs confirmation; do not attribute the track to Cygames
 * merely because it accompanies the characters.
 * Kenney UI Pack stars (CC0): https://kenney.nl/assets/ui-pack
 * Fantasy Skybox FREE / Render Knight:
 * https://assetstore.unity.com/packages/package/18353 (local Readme retained).
 * Free Stylized Smoke Effects Pack / Maiami Studio:
 * https://assetstore.unity.com/packages/vfx/particles/fire-explosions/free-stylized-smoke-effects-pack-226406
 * Combat VFX: Gabriel Aguiar Productions Free Quick Effects Vol. 1 (documentation
 * retained in Assets/GabrielAguiarProductions); Suzuka wind: Vefects/Wind VFX URP.
 * Exact audio sources/attribution and known license limitations are preserved in
 * Assets/Audio/{Shots,Results/JonKFite,Music,Countdown,Death/Hosch}/Credits.txt.
 * Shots: Michel Baradari / apollo-music.de, CC BY 3.0, original info.txt retained.
 * Death: hosch, CC BY-SA 4.0, https://hosch.itch.io
 * Result fanfare: "Victory!", composed by Jon K. Fite, CC BY 3.0,
 * https://opengameart.org/content/victory-2
 * Tank movement: https://pixabay.com/sound-effects/technology-driving-tank-engine-88503/
 * Countdown: https://www.youtube.com/watch?v=q_jtMhL-rm8
 * Menu excerpt: https://www.youtube.com/shorts/p6sZEQbwN4E
 * Full-track reference: https://www.youtube.com/watch?v=au2BMwKcJTw
 * Gameplay excerpt: https://www.youtube.com/shorts/dlwTd7LPBT8
 * Voice sources provided by the group:
 * Suzuka: https://sounds.spriters-resource.com/pc_computer/granbluefantasyversusrising/asset/445151/
 * Fenomeno: https://www.myinstants.com/en/instant/wowok-fenomeno-84122/
 * Creek: https://www.myinstants.com/en/instant/isshou-ni-ganbarimashou-ne-super-creek-8137/
 * Doto: https://www.zedge.net/notification-sounds/b8a7a6ea-9b80-453d-b4dd-6aa53c294719
 * Matikanetannhauser: https://www.myinstants.com/en/instant/matikanetannhauser-uma-musume-pretty-derby-13882/
 * Music excerpts, countdown and character voices are provenance records, not proof
 * of permission. Unconfirmed permissions must not be represented as cleared licenses.
 */

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

    // A monotonic clock avoids wall-clock corrections; the terminal snapshot never keeps ticking.
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

    // The Unity clock keeps running; only this match's active duration excludes its battle pauses.
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

    /// <summary>
    /// Restores pooled actors, class attributes and authored spawn poses while combat is suspended.
    /// This is preparation, not a persisted attempt: no UUID or match start time is assigned here.
    /// </summary>
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
            drive.ResetWeaponCooldown();
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

    /// <summary>
    /// Commits the intro-to-combat boundary: a fresh attempt starts its clock, skill and spawner.
    /// Returning to selection or cancelling the intro must never call this method implicitly.
    /// </summary>
    private void StartCombat()
    {
        playerUltimate.Configure(playerAppearance != null ? playerAppearance.ClassDefinition : null);
        playerHealth.SetUniqueSkillState(playerUltimate);
        BattleAudioController.PlayGameplayMusic();
        // A UUID identifies one attempt, not the account; selection/countdown are not timed.
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
        // UTC records the wall-clock endpoint; performance is the separate pause-adjusted duration.
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
        BattleAudioController.PlayResult(roundOutcome);
        hudRoot.pickingMode = PickingMode.Position;
        battleHud.style.display = DisplayStyle.None;
        defeatScreen.style.display = DisplayStyle.Flex;
        characterSelection?.ShowResultPreview(selectedCharacterIndex, resultCharacterPreview, roundOutcome);
    }

    /// <summary>
    /// Submits the frozen result once at a time. A retry uses the same UUID/payload, and only
    /// server acknowledgment unlocks the saved state and a query for the resulting ranking.
    /// </summary>
    private void RetrySaveMatch()
    {
        if (saveInProgress || matchSaved) return;
        saveInProgress = true;
        saveStatusLabel.text = "Salvando partida...";
        retrySaveButton.style.display = DisplayStyle.None;
        returnButton.SetEnabled(false);
        replayButton?.SetEnabled(false);
        loginButton?.SetEnabled(false);

        // The Model's player/UUID unique key makes retransmission safe after an uncertain response.
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
        tank.GetComponent<TankHealth>()?.ConfigureClass(definition);
        tank.GetComponent<Drive>()?.SetClassSpeedMultiplier(active ? definition.MovementSpeedMultiplier : 1f);
        tank.GetComponent<EnemyTankMovement>()?.SetClassSpeedMultiplier(active ? definition.MovementSpeedMultiplier : 1f);
    }

    private void UpdatePlayerUltimate()
    {
        if (!roundActive || IsStartingRound || IsBattlePaused || playerHealth.IsDead ||
            PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
        playerHealth.Heal(playerUltimate.Tick(Time.unscaledDeltaTime) * playerHealth.MaxHealth);
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame))
        {
            if (playerUltimate.TryActivate())
                playerHealth.Heal(playerUltimate.ActivationHealFraction * playerHealth.MaxHealth);
        }
        playerHealth.GetComponent<Drive>()?.SetUltimateSpeedMultiplier(playerUltimate.SpeedMultiplier);
    }

    private void StopPlayerUltimate()
    {
        playerUltimate.Reset();
        if (playerHealth != null) playerHealth.SetUniqueSkillState(null);
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
            Color tint = playerUltimate.FeedbackColor;
            tint.a = .25f;
            ultimateScreenEffect.style.borderLeftColor = tint;
            ultimateScreenEffect.style.borderRightColor = tint;
            ultimateScreenEffect.style.borderTopColor = tint;
            ultimateScreenEffect.style.borderBottomColor = tint;
        }

        if (ultimateWindInstance == null && ultimateWindPrefab != null)
        {
            ultimateWindInstance = Instantiate(ultimateWindPrefab, playerHealth.transform);
            ultimateWindInstance.name = "Unique Skill Feedback";
            ultimateWindInstance.transform.SetLocalPositionAndRotation(new Vector3(0f, .7f, 0f), Quaternion.identity);
            ultimateWindInstance.transform.localScale = Vector3.one * ultimateWindScale;
            ultimateWindParticles = ultimateWindInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particles in ultimateWindParticles)
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.maxParticles = Mathf.Min(main.maxParticles, 96);
                main.startColor = playerUltimate.FeedbackColor;
                var collision = particles.collision;
                collision.enabled = false; // A visual effect must never push a tank or block a projectile.
            }
            foreach (Collider collider in ultimateWindInstance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
        if (ultimateWindInstance == null) return;
        foreach (ParticleSystem particles in ultimateWindParticles)
        {
            var main = particles.main;
            main.startColor = playerUltimate.FeedbackColor;
        }
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
        bool empoweredShot = definition != null && definition.Effect == CharacterClassDefinition.UltimateEffect.EmpoweredShot;
        ultimateStatusLabel.text = !available ? "EM PREPARAÇÃO" : playerUltimate.IsActive
            ? $"{(empoweredShot ? "PRÓXIMO TIRO" : "EM USO")} — {playerUltimate.RemainingDuration:F1} s" : playerUltimate.IsReady
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
