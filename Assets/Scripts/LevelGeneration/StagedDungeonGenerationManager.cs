using System.Collections;
using FIMSpace.Generating;
using MoreMountains.Tools;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[AddComponentMenu("Room Horror/Level Generation/Staged Dungeon Generation Manager")]
public class StagedDungeonGenerationManager : MonoBehaviour
{
    public enum CorridorSide
    {
        North,
        East,
        South,
        West
    }

    public enum GenerationState
    {
        Idle,
        Planning,
        MainRoomReady,
        BuildingExpansion,
        Completed,
        Failed
    }

    [Header("Generator")]
    [SerializeField] private BuildPlannerExecutor _executor;
    [SerializeField] private bool _generateMainRoomOnStart = true;

    [Header("Planner Names")]
    [SerializeField] private string _mainRoomPlannerName = "DungeonMasterRoom";
    [SerializeField] private string _corridorPlannerName = "SideCorridor";
    [SerializeField] private string _resourceRoomPlannerName = "ResourceRoomWareHouse";

    [Header("Passages and Expansion Direction")]
    [SerializeField] private string _corridorSideVariableName = "Corridor Side";
    [SerializeField, Tooltip("Direction is relative to the BuildPlannerExecutor transform: North = +Z, East = +X.")]
    private CorridorSide _selectedCorridorSide = CorridorSide.North;
    [SerializeField, Min(0)] private int _doorInstructionId = 1;
    [SerializeField, Min(0)] private int _corridorClearInstructionId;

    [Header("Runtime NavMesh")]
    [SerializeField] private bool _rebuildNavMeshAfterEachStage = true;
    [SerializeField, Min(0f)] private float _navMeshRebuildDelay = 0.15f;
    [SerializeField] private NavMeshSurface[] _navMeshSurfaces;

    [Header("Events")]
    [SerializeField] private UnityEvent _mainRoomGenerated;
    [SerializeField] private UnityEvent _expansionGenerated;
    [SerializeField] private UnityEvent _generationFailed;

    [Header("Runtime State")]
    [SerializeField] private GenerationState _state = GenerationState.Idle;
    [SerializeField] private bool _verboseLogs;

    [Header("Play Mode Controls")]
    [MMInspectorButton(nameof(GenerateMainRoomFromScratch))]
    public bool GenerateMainRoomButton;
    [MMInspectorButton(nameof(BuildCorridorAndResourceRooms))]
    public bool BuildExpansionButton;
    [MMInspectorButton(nameof(ClearGeneratedDungeon))]
    public bool ClearGeneratedButton;

    private Coroutine _generationRoutine;
    private bool _buildExpansionWhenReady;

    public GenerationState State => _state;
    public bool MainRoomIsReady => _state == GenerationState.MainRoomReady || _state == GenerationState.Completed;
    public bool ExpansionIsReady => _state == GenerationState.Completed;
    public CorridorSide SelectedCorridorSide
    {
        get => _selectedCorridorSide;
        set => _selectedCorridorSide = value;
    }

    private void Reset()
    {
        _executor = GetComponent<BuildPlannerExecutor>();
    }

    private void Awake()
    {
        if (_executor == null) _executor = GetComponent<BuildPlannerExecutor>();

        // This manager owns the executor lifecycle. Letting both components generate on
        // Start would run two layouts and clear objects behind each other's back.
        if (_executor != null) _executor.GenerateOnStart = false;
    }

    private void Start()
    {
        if (_generateMainRoomOnStart)
            StartInitialGeneration();
    }

    public void GenerateMainRoomFromScratch()
    {
        if (!EnsurePlayMode()) return;
        StartInitialGeneration();
    }

    public void BuildCorridorAndResourceRooms()
    {
        if (!EnsurePlayMode()) return;

        if (_state == GenerationState.Completed)
        {
            Log("The corridor and resource rooms are already generated.");
            return;
        }

        if (_state == GenerationState.Planning)
        {
            _buildExpansionWhenReady = true;
            Log("Expansion was queued until the main room is ready.");
            return;
        }

        if (_state != GenerationState.MainRoomReady)
        {
            Debug.LogWarning($"[StagedDungeonGenerationManager] Expansion cannot be built while state is {_state}.", this);
            return;
        }

        StartExpansionGeneration();
    }

    public void SetCorridorSide(CorridorSide side)
    {
        _selectedCorridorSide = side;
    }

    public void ClearGeneratedDungeon()
    {
        if (!EnsurePlayMode()) return;

        StopActiveGenerationRoutine();
        _buildExpansionWhenReady = false;

        if (_executor != null) _executor.ClearStagedGeneration();
        SetState(GenerationState.Idle);
        Log("Generated dungeon was cleared.");
    }

    private void StartInitialGeneration()
    {
        if (!ValidateConfiguration()) return;

        StopActiveGenerationRoutine();
        _buildExpansionWhenReady = false;
        SetState(GenerationState.Planning);
        _generationRoutine = StartCoroutine(GenerateMainRoomRoutine());
    }

    private IEnumerator GenerateMainRoomRoutine()
    {
        // Let StartCoroutine return first so _generationRoutine always references the
        // currently running operation, including when an early validation error occurs.
        yield return null;

        if (!ApplySelectedCorridorSide())
        {
            FailGeneration($"Could not set planner variable '{_corridorSideVariableName}'.");
            yield break;
        }

        if (!_executor.BeginStagedGenerationPreview())
        {
            FailGeneration("Could not start the staged layout preview.");
            yield break;
        }

        yield return null;
        while (_executor.IsGenerating)
            yield return null;

        if (!_executor.IsStagedPlanReady)
        {
            FailGeneration("The staged layout did not finish correctly.");
            yield break;
        }

        if (!_executor.EnsureFourCenteredEdgeInstructions(_mainRoomPlannerName, _doorInstructionId))
        {
            FailGeneration("Could not create the four centered dungeon master room passages.");
            yield break;
        }

        if (!_executor.TryGenerateStagedPlanner(_mainRoomPlannerName))
        {
            FailGeneration($"Could not generate planner '{_mainRoomPlannerName}'.");
            yield break;
        }

        yield return RebuildRuntimeNavMesh();

        SetState(GenerationState.MainRoomReady);
        _mainRoomGenerated?.Invoke();
        Log("Dungeon master room generated.");

        _generationRoutine = null;

        if (_buildExpansionWhenReady)
        {
            _buildExpansionWhenReady = false;
            StartExpansionGeneration();
        }
    }

    private void StartExpansionGeneration()
    {
        SetState(GenerationState.BuildingExpansion);
        _generationRoutine = StartCoroutine(BuildExpansionRoutine());
    }

    private IEnumerator BuildExpansionRoutine()
    {
        yield return null;

        if (!ApplySelectedCorridorSide())
        {
            FailGeneration($"Could not set planner variable '{_corridorSideVariableName}'.");
            yield break;
        }

        if (!_executor.BeginStagedReplanPreviewPreservingGenerated())
        {
            FailGeneration("Could not replan the selected expansion side.");
            yield break;
        }

        yield return null;
        while (_executor.IsGenerating)
            yield return null;

        if (!_executor.IsStagedPlanReady)
        {
            FailGeneration("The selected expansion layout did not finish correctly.");
            yield break;
        }

        if (!_executor.AlignStagedExpansionToCenteredEdge(
                _mainRoomPlannerName,
                _corridorPlannerName,
                GetSelectedCorridorDirection(),
                _corridorClearInstructionId,
                _resourceRoomPlannerName))
        {
            FailGeneration("Could not align the corridor with the centered room passage.");
            yield break;
        }

        if (!_executor.TryGenerateStagedPlanner(_corridorPlannerName))
        {
            FailGeneration($"Could not generate planner '{_corridorPlannerName}'.");
            yield break;
        }

        // Allow generated corridor renderers and colliders to become visible to dependent systems.
        yield return null;

        if (!_executor.TryGenerateStagedPlanner(_resourceRoomPlannerName))
        {
            FailGeneration($"Could not generate planner '{_resourceRoomPlannerName}'.");
            yield break;
        }

        yield return RebuildRuntimeNavMesh();

        SetState(GenerationState.Completed);
        _expansionGenerated?.Invoke();
        Log($"Corridor and resource rooms generated on the {_selectedCorridorSide} side.");
        _generationRoutine = null;
    }

    private bool ApplySelectedCorridorSide()
    {
        return _executor.TrySetStagedPlannerVariable(
            _corridorPlannerName,
            _corridorSideVariableName,
            (Vector3)GetSelectedCorridorDirection());
    }

    private Vector3Int GetSelectedCorridorDirection()
    {
        Vector3Int direction;

        switch (_selectedCorridorSide)
        {
            case CorridorSide.North:
                direction = Vector3Int.forward;
                break;
            case CorridorSide.East:
                direction = Vector3Int.right;
                break;
            case CorridorSide.South:
                direction = Vector3Int.back;
                break;
            case CorridorSide.West:
                direction = Vector3Int.left;
                break;
            default:
                direction = Vector3Int.forward;
                break;
        }

        return direction;
    }

    private IEnumerator RebuildRuntimeNavMesh()
    {
        if (!_rebuildNavMeshAfterEachStage) yield break;

        if (_navMeshRebuildDelay > 0f)
            yield return new WaitForSecondsRealtime(_navMeshRebuildDelay);
        else
            yield return null;

        NavMeshSurface[] surfaces = ResolveNavMeshSurfaces();
        if (surfaces == null || surfaces.Length == 0)
        {
            Debug.LogWarning("[StagedDungeonGenerationManager] No NavMeshSurface was found for runtime rebuild.", this);
            yield break;
        }

        for (int i = 0; i < surfaces.Length; i++)
        {
            NavMeshSurface surface = surfaces[i];
            if (surface == null || !surface.isActiveAndEnabled) continue;

            if (surface.navMeshData == null)
            {
                surface.BuildNavMesh();
                continue;
            }

            AsyncOperation update = surface.UpdateNavMesh(surface.navMeshData);
            while (update != null && !update.isDone)
                yield return null;
        }
    }

    private NavMeshSurface[] ResolveNavMeshSurfaces()
    {
        if (_navMeshSurfaces != null && _navMeshSurfaces.Length > 0)
            return _navMeshSurfaces;

        return FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
    }

    private bool ValidateConfiguration()
    {
        if (_executor == null) _executor = GetComponent<BuildPlannerExecutor>();

        if (_executor == null)
        {
            FailGeneration("BuildPlannerExecutor reference is missing.");
            return false;
        }

        if (_executor.BuildPlannerPreset == null)
        {
            FailGeneration("BuildPlannerPreset is missing on the executor.");
            return false;
        }

        if (_executor.FlexibleGen)
        {
            FailGeneration("Disable FlexibleGen on BuildPlannerExecutor before using staged generation.");
            return false;
        }

        if (_executor.FindStagedPlannerIndex(_mainRoomPlannerName) < 0 ||
            _executor.FindStagedPlannerIndex(_corridorPlannerName) < 0 ||
            _executor.FindStagedPlannerIndex(_resourceRoomPlannerName) < 0)
        {
            FailGeneration("One or more configured planner names were not found in DungeonBuildPlanner.");
            return false;
        }

        return true;
    }

    private void FailGeneration(string message)
    {
        SetState(GenerationState.Failed);
        _generationRoutine = null;
        Debug.LogError($"[StagedDungeonGenerationManager] {message}", this);
        _generationFailed?.Invoke();
    }

    private void StopActiveGenerationRoutine()
    {
        if (_generationRoutine == null) return;
        StopCoroutine(_generationRoutine);
        _generationRoutine = null;
    }

    private bool EnsurePlayMode()
    {
        if (Application.isPlaying) return true;

        Debug.LogWarning("[StagedDungeonGenerationManager] Inspector generation controls work in Play Mode.", this);
        return false;
    }

    private void SetState(GenerationState state)
    {
        _state = state;
    }

    private void Log(string message)
    {
        if (_verboseLogs)
            Debug.Log($"[StagedDungeonGenerationManager] {message}", this);
    }
}
