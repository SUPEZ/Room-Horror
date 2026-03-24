using System.Collections;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[AddComponentMenu("Room Horror/Spawners/Character Spawner")]
public class CharacterSpawner : MonoBehaviour
{
    [System.Serializable]
    public class RoleSpawnDefinition
    {
        public CharacterRoleType Role;
        public GameObject Prefab;
    }

    [Header("Base Prefab")]
    [Tooltip("Default character prefab used when a role-specific prefab is not set. For example: AICrateCharacter.")]
    [SerializeField] private GameObject _defaultCharacterPrefab;

    [Header("Spawn Point")]
    [SerializeField] private Transform _spawnPoint;
    [SerializeField] private Transform _spawnParent;

    [Header("Role Overrides")]
    [SerializeField] private RoleSpawnDefinition[] _rolePrefabs;

    [Header("Worker Setup")]
    [SerializeField] private bool _ensureNavMeshPathfinder = true;
    [SerializeField] private bool _ensureWorkerGatherComponents = true;

    [Header("Runtime NavMesh")]
    [SerializeField] private bool _buildRuntimeNavMeshOnStart = true;
    [SerializeField] [Min(0f)] private float _runtimeNavMeshBuildDelay = 0.1f;
    [SerializeField] private LayerMask _runtimeNavMeshLayerMask = ~0;

    [Header("Spawn Placement")]
    [SerializeField] [Min(0.25f)] private float _spawnNavMeshSampleDistance = 4f;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogs;
    [MMInspectorButton("DebugSpawnWorker")] public bool DebugSpawnWorkerButton;
    [MMInspectorButton("DebugSpawnWarrior")] public bool DebugSpawnWarriorButton;
    [MMInspectorButton("DebugSpawnScout")] public bool DebugSpawnScoutButton;
    [MMInspectorButton("DebugSpawnScientist")] public bool DebugSpawnScientistButton;
    [MMInspectorButton("DebugRebuildRuntimeNavMesh")] public bool DebugRebuildRuntimeNavMeshButton;

    private NavMeshSurface _fallbackRuntimeNavMeshSurface;
    private Coroutine _runtimeNavMeshBuildRoutine;

    public GameObject DefaultCharacterPrefab => _defaultCharacterPrefab;
    public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

    protected virtual void Awake()
    {
        if (_buildRuntimeNavMeshOnStart && FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None).Length == 0)
        {
            EnsureFallbackRuntimeNavMeshSurface();
        }
    }

    protected virtual void Start()
    {
        if (_buildRuntimeNavMeshOnStart && Application.isPlaying)
        {
            QueueRuntimeNavMeshBuild();
        }
    }

    public GameObject SpawnWorker()
    {
        return SpawnCharacter(CharacterRoleType.Worker);
    }

    public GameObject SpawnWarrior()
    {
        return SpawnCharacter(CharacterRoleType.Warrior);
    }

    public GameObject SpawnScout()
    {
        return SpawnCharacter(CharacterRoleType.Scout);
    }

    public GameObject SpawnScientist()
    {
        return SpawnCharacter(CharacterRoleType.Scientist);
    }

    public GameObject SpawnCharacter(CharacterRoleType role)
    {
        GameObject prefabToSpawn = ResolvePrefab(role);
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[CharacterSpawner] No prefab configured for role {role} on {name}.", this);
            return null;
        }

        Transform point = SpawnPoint;
        Vector3 spawnPosition = ResolveSpawnPosition(point);
        GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, point.rotation, _spawnParent);
        ConfigureSpawnedCharacter(spawnedObject, role);
        return spawnedObject;
    }

    public GameObject SpawnCharacter(GameObject prefabOverride, CharacterRoleType role, Transform spawnOverride)
    {
        if (prefabOverride == null)
        {
            return SpawnCharacter(role);
        }

        Transform point = spawnOverride != null ? spawnOverride : SpawnPoint;
        Vector3 spawnPosition = ResolveSpawnPosition(point);
        GameObject spawnedObject = Instantiate(prefabOverride, spawnPosition, point.rotation, _spawnParent);
        ConfigureSpawnedCharacter(spawnedObject, role);
        return spawnedObject;
    }

    protected virtual void ConfigureSpawnedCharacter(GameObject spawnedObject, CharacterRoleType role)
    {
        if (spawnedObject == null)
        {
            return;
        }

        spawnedObject.name = role + "_" + spawnedObject.name;

        Character character = spawnedObject.GetComponent<Character>();
        CharacterRoleProfile roleProfile = GetOrAddComponent<CharacterRoleProfile>(spawnedObject);
        roleProfile.SetRole(role);

        if (_ensureNavMeshPathfinder)
        {
            EnsureNavMeshPathfinder(spawnedObject, character);
        }

        if (_ensureWorkerGatherComponents && role == CharacterRoleType.Worker)
        {
            EnsureWorkerComponents(spawnedObject, character);
            roleProfile.RefreshCachedComponents();
        }

        if (character != null)
        {
            character.CacheAbilities();
            character.ForceAbilitiesInitialization();
        }

        CharacterProvider provider = spawnedObject.GetComponent<CharacterProvider>();
        if (provider != null)
        {
            provider.SetCharacterType(Character.CharacterTypes.AI);
        }
    }

    protected virtual void EnsureWorkerComponents(GameObject spawnedObject, Character character)
    {
        if (character == null)
        {
            Debug.LogWarning($"[CharacterSpawner] {spawnedObject.name} has no Character component. Worker setup skipped.", this);
            return;
        }

        EnsureNavMeshPathfinder(spawnedObject, character);

        CharacterGatherer gatherer = spawnedObject.GetComponent<CharacterGatherer>();
        if (gatherer == null)
        {
            gatherer = spawnedObject.AddComponent<CharacterGatherer>();
        }

        character.CacheAbilities();
        character.ForceAbilitiesInitialization();
        gatherer.RefreshCachedComponents();
    }

    protected virtual void EnsureNavMeshPathfinder(GameObject spawnedObject, Character character)
    {
        if (character == null)
        {
            return;
        }

        CharacterPathfinder3D pathfinder = character.FindAbility<CharacterPathfinder3D>();
        if (pathfinder == null)
        {
            spawnedObject.AddComponent<CharacterPathfinder3D>();
            character.CacheAbilities();
            character.ForceAbilitiesInitialization();
        }
    }

    protected virtual T GetOrAddComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    protected virtual GameObject ResolvePrefab(CharacterRoleType role)
    {
        if (_rolePrefabs != null)
        {
            for (int i = 0; i < _rolePrefabs.Length; i++)
            {
                RoleSpawnDefinition definition = _rolePrefabs[i];
                if (definition != null && definition.Role == role && definition.Prefab != null)
                {
                    return definition.Prefab;
                }
            }
        }

        return _defaultCharacterPrefab;
    }

    protected virtual Vector3 ResolveSpawnPosition(Transform point)
    {
        Vector3 requestedPosition = point != null ? point.position : transform.position;
        if (!Application.isPlaying)
        {
            return requestedPosition;
        }

        if (NavMesh.SamplePosition(requestedPosition, out NavMeshHit navMeshHit, _spawnNavMeshSampleDistance, NavMesh.AllAreas))
        {
            return navMeshHit.position;
        }

        if (_buildRuntimeNavMeshOnStart)
        {
            BuildRuntimeNavMeshNow();
            if (NavMesh.SamplePosition(requestedPosition, out navMeshHit, _spawnNavMeshSampleDistance, NavMesh.AllAreas))
            {
                return navMeshHit.position;
            }
        }

        LogVerbose($"Spawn point for {name} was not on NavMesh. Using raw position {requestedPosition}.");
        return requestedPosition;
    }

    protected virtual void EnsureFallbackRuntimeNavMeshSurface()
    {
        _fallbackRuntimeNavMeshSurface = GetComponent<NavMeshSurface>();
        if (_fallbackRuntimeNavMeshSurface == null)
        {
            _fallbackRuntimeNavMeshSurface = gameObject.AddComponent<NavMeshSurface>();
        }

        _fallbackRuntimeNavMeshSurface.collectObjects = CollectObjects.All;
        _fallbackRuntimeNavMeshSurface.layerMask = _runtimeNavMeshLayerMask;
        _fallbackRuntimeNavMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        _fallbackRuntimeNavMeshSurface.defaultArea = 0;
        _fallbackRuntimeNavMeshSurface.ignoreNavMeshAgent = true;
        _fallbackRuntimeNavMeshSurface.ignoreNavMeshObstacle = true;
    }

    protected virtual void QueueRuntimeNavMeshBuild()
    {
        if (_runtimeNavMeshBuildRoutine != null)
        {
            StopCoroutine(_runtimeNavMeshBuildRoutine);
        }

        _runtimeNavMeshBuildRoutine = StartCoroutine(BuildRuntimeNavMeshAfterDelay());
    }

    protected virtual IEnumerator BuildRuntimeNavMeshAfterDelay()
    {
        if (_runtimeNavMeshBuildDelay > 0f)
        {
            yield return new WaitForSeconds(_runtimeNavMeshBuildDelay);
        }
        else
        {
            yield return null;
        }

        BuildRuntimeNavMeshNow();
        _runtimeNavMeshBuildRoutine = null;
    }

    protected virtual void BuildRuntimeNavMeshNow()
    {
        NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
        if (surfaces == null || surfaces.Length == 0)
        {
            EnsureFallbackRuntimeNavMeshSurface();
            surfaces = new[] { _fallbackRuntimeNavMeshSurface };
        }

        for (int i = 0; i < surfaces.Length; i++)
        {
            if (surfaces[i] != null && surfaces[i].isActiveAndEnabled)
            {
                surfaces[i].BuildNavMesh();
            }
        }

        LogVerbose($"Runtime NavMesh rebuilt by {name}. Surfaces: {surfaces.Length}.");
    }

    protected virtual void DebugSpawnWorker()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CharacterSpawner] Debug spawn buttons work in Play Mode.", this);
            return;
        }

        SpawnWorker();
    }

    protected virtual void DebugSpawnWarrior()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CharacterSpawner] Debug spawn buttons work in Play Mode.", this);
            return;
        }

        SpawnWarrior();
    }

    protected virtual void DebugSpawnScout()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CharacterSpawner] Debug spawn buttons work in Play Mode.", this);
            return;
        }

        SpawnScout();
    }

    protected virtual void DebugSpawnScientist()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CharacterSpawner] Debug spawn buttons work in Play Mode.", this);
            return;
        }

        SpawnScientist();
    }

    protected virtual void DebugRebuildRuntimeNavMesh()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CharacterSpawner] Runtime NavMesh rebuild works in Play Mode.", this);
            return;
        }

        BuildRuntimeNavMeshNow();
    }

    protected virtual void LogVerbose(string message)
    {
        if (_verboseLogs)
        {
            Debug.Log($"[CharacterSpawner] {message}", this);
        }
    }
}
