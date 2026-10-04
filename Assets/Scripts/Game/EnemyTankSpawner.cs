using UnityEngine;

/// <summary>
/// Reuses the scene's enemy actors for survival rounds. Spawn poses are saved scene markers,
/// checked against the group's own grid/A*, physical hulls and the gameplay camera.
/// No navigation package, runtime enemy prefab copies or score/backend changes are needed.
/// </summary>
public sealed class EnemyTankSpawner : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private TankGrid grid;
    [SerializeField] private TankHealth playerHealth;
    [SerializeField] private TankHealth[] enemyPool;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Population and timing")]
    [Tooltip("The assignment requires four enemies. The pool also limits the active population.")]
    [SerializeField, Min(4)] private int maximumActiveEnemies = 4;
    [SerializeField, Min(0.1f)] private float replacementDelay = 4f;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1f;
    [SerializeField, Min(0.1f)] private float retryInterval = 0.75f;

    [Header("Spawn safety")]
    [SerializeField, Min(1f)] private float minimumPlayerDistance = 14f;
    [SerializeField, Min(0f)] private float hullClearance = 0.2f;
    [SerializeField, Range(0f, 0.5f)] private float viewportPadding = 0.08f;
    [Tooltip("Conservative height of a tank and character, used when excluding visible poses.")]
    [SerializeField, Min(1f)] private float visibilityHeight = 4f;
    [SerializeField] private LayerMask blockedSpawnMask = (1 << 6) | (1 << 8);

    private sealed class EnemySlot
    {
        public TankHealth Health;
        public BoxCollider Hull;
        public Rigidbody Body;
        public AITank Attack;
        public EnemyTankMovement Movement;
        public EnemyTankStateMachine StateMachine;
        public bool Pending;
        public float DelayRemaining;
        public float GroundHeight;
    }

    private EnemySlot[] slots;
    private readonly Collider[] overlaps = new Collider[64];
    private readonly AStarPathfinder pathfinder = new();
    private float attemptRemaining;
    private int nextSlotIndex;
    private bool prepared;

    public bool IsSpawning { get; private set; }
    public int ActiveEnemyCount
    {
        get
        {
            int count = 0;
            if (slots == null) return count;
            foreach (EnemySlot slot in slots)
                if (slot.Health != null && slot.Health.gameObject.activeInHierarchy && !slot.Health.IsDead) count++;
            return count;
        }
    }
    public int ActiveLimit => slots == null ? 0 : Mathf.Min(Mathf.Max(4, maximumActiveEnemies), slots.Length);

    private void Awake()
    {
        if (grid == null || playerHealth == null || gameplayCamera == null ||
            enemyPool == null || enemyPool.Length < 4 || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("EnemyTankSpawner needs a grid, player, camera, at least four pooled enemies and spawn points.", this);
            enabled = false;
            return;
        }

        slots = new EnemySlot[enemyPool.Length];
        for (int i = 0; i < enemyPool.Length; i++)
        {
            TankHealth health = enemyPool[i];
            if (health == null || !health.DeactivatesOnDeath || !health.TryGetComponent(out BoxCollider hull) ||
                !health.TryGetComponent(out Rigidbody body) || !health.TryGetComponent(out AITank attack) ||
                !health.TryGetComponent(out EnemyTankMovement movement) || !health.TryGetComponent(out EnemyTankStateMachine stateMachine))
            {
                Debug.LogError("Pooled enemies need reusable TankHealth, a hull, Rigidbody and the group's AI components.", this);
                slots = null;
                enabled = false;
                return;
            }
            for (int previous = 0; previous < i; previous++)
                if (slots[previous].Health == health)
                {
                    Debug.LogError("EnemyTankSpawner cannot reference the same actor twice.", this);
                    slots = null;
                    enabled = false;
                    return;
                }
            slots[i] = new EnemySlot
            {
                Health = health, Hull = hull, Body = body, Attack = attack,
                Movement = movement, StateMachine = stateMachine, GroundHeight = health.transform.position.y
            };
        }
        foreach (EnemySlot slot in slots) slot.Health.Died += OnEnemyDied;
    }

    public void PrepareForRound()
    {
        StopSpawning(true);
        if (slots == null || !isActiveAndEnabled) return;
        foreach (EnemySlot slot in slots)
        {
            slot.Health.RestoreFullHealth();
            slot.Movement.Stop();
            slot.Pending = true;
            slot.DelayRemaining = 0f;
        }
        prepared = true;
        nextSlotIndex = 0;
    }

    public void StartSpawning()
    {
        if (!prepared || slots == null || !isActiveAndEnabled || playerHealth.IsDead) return;
        IsSpawning = true;
        // Initial population is filled only after the fade/countdown and camera blend finish.
        for (int i = 0; i < ActiveLimit; i++)
            if (!TrySpawnNext()) break;
        attemptRemaining = retryInterval;
    }

    public void StopSpawning(bool hideEnemies = false)
    {
        IsSpawning = false;
        prepared = false;
        attemptRemaining = 0f;
        if (slots == null) return;
        foreach (EnemySlot slot in slots)
        {
            slot.Pending = false;
            slot.DelayRemaining = 0f;
            if (hideEnemies && slot.Health != null) slot.Health.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsSpawning || playerHealth == null || playerHealth.IsDead ||
            PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        foreach (EnemySlot slot in slots)
            if (slot.Pending) slot.DelayRemaining = Mathf.Max(0f, slot.DelayRemaining - Time.deltaTime);
        attemptRemaining = Mathf.Max(0f, attemptRemaining - Time.deltaTime);
        if (attemptRemaining > 0f || ActiveEnemyCount >= ActiveLimit) return;
        attemptRemaining = TrySpawnNext() ? spawnInterval : retryInterval;
    }

    private void OnEnemyDied(TankHealth enemy, GameObject source)
    {
        if (!IsSpawning) return;
        foreach (EnemySlot slot in slots)
        {
            if (slot.Health != enemy || slot.Pending) continue;
            slot.Pending = true;
            slot.DelayRemaining = replacementDelay;
            return;
        }
    }

    private bool TrySpawnNext()
    {
        if (!IsSpawning || ActiveEnemyCount >= ActiveLimit) return false;
        for (int offset = 0; offset < slots.Length; offset++)
        {
            int index = (nextSlotIndex + offset) % slots.Length;
            EnemySlot slot = slots[index];
            // Never interrupt death animation or spawn a second copy of the actor.
            if (!slot.Pending || slot.DelayRemaining > 0f || slot.Health == null || slot.Health.gameObject.activeSelf) continue;
            if (!TryFindSpawnPose(slot, out Vector3 position, out Quaternion rotation)) continue;

            slot.Health.transform.SetPositionAndRotation(position, rotation);
            slot.Body.position = position;
            slot.Body.rotation = rotation;
            if (!slot.Body.isKinematic)
            {
                slot.Body.linearVelocity = Vector3.zero;
                slot.Body.angularVelocity = Vector3.zero;
            }
            slot.Movement.Stop();
            slot.Health.gameObject.SetActive(true);
            // Reset Animator parameters after activation; inactive animators keep their death state.
            slot.Health.RestoreFullHealth();
            Physics.SyncTransforms();
            slot.Attack.ResetForNewRound();
            slot.StateMachine.ResetForNewRound();
            slot.Pending = false;
            nextSlotIndex = (index + 1) % slots.Length;
            return true;
        }
        return false;
    }

    private bool TryFindSpawnPose(EnemySlot slot, out Vector3 position, out Quaternion rotation)
    {
        position = default;
        rotation = Quaternion.Euler(0f, 90f * Random.Range(0, 4), 0f);
        if (grid == null || !grid.IsBuilt || gameplayCamera == null || spawnPoints == null || spawnPoints.Length == 0) return false;
        GridNode playerNode = grid.GetNearestWalkableNode(playerHealth.transform.position);
        if (playerNode == null) return false;
        Physics.SyncTransforms();
        int start = Random.Range(0, spawnPoints.Length);
        for (int offset = 0; offset < spawnPoints.Length; offset++)
        {
            Transform point = spawnPoints[(start + offset) % spawnPoints.Length];
            if (point == null || !point.gameObject.activeInHierarchy) continue;
            GridNode node = grid.GetNodeFromWorldPosition(point.position);
            // GetNodeFromWorldPosition clamps indices: reject markers outside the actual grid.
            if (node == null || !node.IsWalkable || Mathf.Abs(point.position.x - node.WorldPosition.x) > grid.CellSize * .5f + .001f ||
                Mathf.Abs(point.position.z - node.WorldPosition.z) > grid.CellSize * .5f + .001f) continue;
            Vector3 candidate = new(node.WorldPosition.x, slot.GroundHeight, node.WorldPosition.z);
            Vector3 toPlayer = Vector3.ProjectOnPlane(candidate - playerHealth.transform.position, Vector3.up);
            if (toPlayer.sqrMagnitude < minimumPlayerDistance * minimumPlayerDistance ||
                IsVisiblePose(slot.Hull, candidate) || !IsFreePose(slot, candidate, rotation)) continue;
            // A* runs on a bounded spawn attempt, never as a search on every frame.
            if (!pathfinder.TryFindPath(grid, node, playerNode, out _)) continue;
            position = candidate;
            return true;
        }
        return false;
    }

    private bool IsFreePose(EnemySlot slot, Vector3 position, Quaternion rotation)
    {
        Vector3 scale = slot.Hull.transform.lossyScale;
        Vector3 size = Vector3.Scale(slot.Hull.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        Vector3 center = position + rotation * Vector3.Scale(slot.Hull.center, scale);
        int count = Physics.OverlapBoxNonAlloc(center, size * .5f + Vector3.one * hullClearance,
            overlaps, rotation, Physics.AllLayers, QueryTriggerInteraction.Collide);
        if (count >= overlaps.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Collider other = overlaps[i];
            if (other.transform.IsChildOf(slot.Health.transform)) continue;
            if ((blockedSpawnMask.value & (1 << other.gameObject.layer)) != 0 ||
                other.GetComponentInParent<TankHealth>() != null) return false;
        }
        return true;
    }

    private bool IsVisiblePose(BoxCollider hull, Vector3 position)
    {
        Vector3 size = Vector3.Scale(hull.size, hull.transform.lossyScale);
        float radius = Mathf.Max(Mathf.Abs(size.x), Mathf.Abs(size.z)) * .5f + .5f;
        Bounds visibleBounds = new(position + Vector3.up * (visibilityHeight * .5f),
            new Vector3(radius * 2f, visibilityHeight, radius * 2f));
        Vector2 min = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 max = new(float.NegativeInfinity, float.NegativeInfinity);
        bool inFront = false;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = visibleBounds.center + Vector3.Scale(visibleBounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 viewport = gameplayCamera.WorldToViewportPoint(corner);
            inFront |= viewport.z > 0f;
            min = Vector2.Min(min, viewport);
            max = Vector2.Max(max, viewport);
        }
        return inFront && max.x >= -viewportPadding && min.x <= 1f + viewportPadding &&
               max.y >= -viewportPadding && min.y <= 1f + viewportPadding;
    }

    private void OnDisable() => StopSpawning();

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(0.2f, 0.9f, 0.7f);
        foreach (Transform point in spawnPoints)
            if (point != null) Gizmos.DrawWireSphere(point.position + Vector3.up * .5f, 1f);
    }

    private void OnDestroy()
    {
        if (slots == null) return;
        foreach (EnemySlot slot in slots)
            if (slot.Health != null) slot.Health.Died -= OnEnemyDied;
    }
}
