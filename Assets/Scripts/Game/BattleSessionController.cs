using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Connects the battle HUD and round lifecycle to the existing menu and score API.
/// </summary>
public sealed class BattleSessionController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private UIDocument menuDocument;
    [SerializeField] private UIDocument hudDocument;
    [SerializeField] private StyleSheet hudStyle;
    [SerializeField] private TankHealth playerHealth;
    [SerializeField] private TankHealth[] enemyHealths;
    [SerializeField, Min(1)] private int pointsPerEnemy = 100;

    private struct TankSpawn
    {
        public TankHealth Health;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    private TankSpawn[] tankSpawns;
    private Quaternion playerCannonRotation;
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
    private bool roundActive;

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

        if (playerHealth.TryGetComponent(out Drive drive) && drive.cannon != null)
        {
            playerCannonRotation = drive.cannon.localRotation;
        }

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
        hudRoot.styleSheets.Add(hudStyle);
        hudRoot.pickingMode = PickingMode.Ignore;

        battleOverlay = hudRoot.Q<VisualElement>("BattleOverlay");
        battleHud = hudRoot.Q<VisualElement>("BattleHud");
        defeatScreen = hudRoot.Q<VisualElement>("DefeatScreen");
        healthFill = hudRoot.Q<VisualElement>("HealthFill");
        healthLabel = hudRoot.Q<Label>("HealthLabel");
        scoreLabel = hudRoot.Q<Label>("ScoreLabel");
        finalScoreLabel = hudRoot.Q<Label>("FinalScoreLabel");
        returnButton = hudRoot.Q<Button>("ReturnToMenuButton");
        returnButton.clicked += ReturnToMenu;
        battleOverlay.style.display = DisplayStyle.None;
        defeatScreen.style.display = DisplayStyle.None;

        // The existing menu starts the session through StartGame even while Update is disabled.
        // This prevents Escape in the menu from submitting an unfinished score.
        if (GameManager.Instance != null && GameManager.Instance.PlayerData == null)
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
        if (currentData != null && !ReferenceEquals(currentData, observedPlayerData))
        {
            observedPlayerData = currentData;
            BeginRound();
        }

        if (roundActive)
        {
            RefreshHud();
        }
    }

    private void OnDestroy()
    {
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

    private void BeginRound()
    {
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
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            tank.SetActive(true);
        }

        Physics.SyncTransforms();

        if (playerHealth.TryGetComponent(out Drive drive) && drive.cannon != null)
        {
            drive.cannon.localRotation = playerCannonRotation;
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

        GameManager.Instance.enabled = true;
        roundActive = true;
        battleOverlay.style.display = DisplayStyle.Flex;
        hudRoot.pickingMode = PickingMode.Ignore;
        battleHud.style.display = DisplayStyle.Flex;
        defeatScreen.style.display = DisplayStyle.None;
        RefreshHud();
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
    }

    private void OnPlayerDied(TankHealth health, GameObject source)
    {
        if (!roundActive)
        {
            return;
        }

        roundActive = false;
        StopCombatDamage();

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.SetPause(true);
        }

        GameManager manager = GameManager.Instance;
        int finalScore = manager != null && manager.PlayerData != null
            ? manager.PlayerData.pontos
            : 0;
        if (manager != null && manager.PlayerData != null)
        {
            manager.EndGame();
            manager.enabled = false;
        }

        finalScoreLabel.text = $"PONTUAÇÃO FINAL: {finalScore}";
        hudRoot.pickingMode = PickingMode.Position;
        battleHud.style.display = DisplayStyle.None;
        defeatScreen.style.display = DisplayStyle.Flex;
    }

    private void ReturnToMenu()
    {
        defeatScreen.style.display = DisplayStyle.None;
        battleOverlay.style.display = DisplayStyle.None;
        hudRoot.pickingMode = PickingMode.Ignore;
        if (mainMenu != null)
        {
            mainMenu.style.display = DisplayStyle.Flex;
        }
    }

    private void RefreshHud()
    {
        healthLabel.text = $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        float healthPercent = playerHealth.MaxHealth > 0
            ? 100f * playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 0f;
        healthFill.style.width = new Length(healthPercent, LengthUnit.Percent);

        PlayerData data = GameManager.Instance != null ? GameManager.Instance.PlayerData : null;
        scoreLabel.text = data != null ? data.pontos.ToString() : "0";
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
