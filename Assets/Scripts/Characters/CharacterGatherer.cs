using GameResources;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Character))]
[AddComponentMenu("Room Horror/Characters/Character Gatherer")]
public class CharacterGatherer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] [Min(0.05f)] private float _slotArrivalThreshold = 0.35f;
    [SerializeField] [Min(0.05f)] private float _cornerReachThreshold = 0.4f;
    [SerializeField] private bool _faceResourceWhileGathering = false;

    [Header("Pathfinding")]
    [SerializeField] [Min(0.1f)] private float _pathValidationSampleDistance = 2f;
    [SerializeField] [Min(0.1f)] private float _repathInterval = 0.75f;

    [Header("Debug")]
    [SerializeField] private bool _verboseLogs;
    [MMInspectorButton("DebugStartNearestResourceTask")] public bool DebugStartNearestResourceTaskButton;
    [MMInspectorButton("DebugCheckNearestResourcePaths")] public bool DebugCheckNearestResourcePathsButton;

    private Character _character;
    private CharacterPathfinder3D _pathfinder;
    private CharacterMovement _movement;
    private AIBrain _brain;

    private GatherTask _currentTask;
    private GatherTask _lastTask;
    private bool _brainWasEnabledBeforeTask;
    private bool _brainWasActiveBeforeTask;
    private bool _currentTaskOwnsBrain;
    private bool _brainSuppressedWithoutTask;

    private NavMeshPath _activePath;
    private int _currentCornerIndex;
    private float _lastPathRebuildTime;
    private Vector3 _currentPathTarget;

    public GatherTask CurrentTask => _currentTask;
    public GatherTask LastTask => _lastTask;
    public ResourceNode CurrentResourceNode => _currentTask?.ResourceNode;
    public bool HasActiveTask => _currentTask != null && !_currentTask.IsTerminal;
    public bool IsGathering => _currentTask != null && _currentTask.State == GatherTaskState.Gathering;

    private void Awake()
    {
        RefreshCachedComponents();
        _activePath = new NavMeshPath();
    }

    private void Update()
    {
        if (!HasActiveTask)
        {
            return;
        }

        if (_character.CharacterType == Character.CharacterTypes.Player)
        {
            CancelCurrentTask();
            return;
        }

        if (_currentTask.ResourceNode == null)
        {
            FailCurrentTask("Resource node reference was lost.");
            return;
        }

        switch (_currentTask.State)
        {
            case GatherTaskState.MovingToSlot:
                UpdateMoveToSlot();
                break;

            case GatherTaskState.Gathering:
                UpdateGathering();
                break;
        }
    }

    private void OnDisable()
    {
        CancelCurrentTask();
    }

    public void RefreshCachedComponents()
    {
        _character = GetComponent<Character>();
        _brain = GetComponent<AIBrain>();
        _pathfinder = _character != null ? _character.FindAbility<CharacterPathfinder3D>() : null;
        _movement = _character != null ? _character.FindAbility<CharacterMovement>() : null;
    }

    public bool TryStartWorkOnNearestNode(bool pauseBrainWhileTaskActive = true)
    {
        ReleaseIdleBrainSuppression();

        ResourceNode[] resourceNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        if (resourceNodes == null || resourceNodes.Length == 0)
        {
            if (pauseBrainWhileTaskActive)
            {
                SuppressIdleBrain();
            }

            Debug.LogWarning($"[CharacterGatherer] {name} could not find any resource nodes in the scene.", this);
            return false;
        }

        System.Array.Sort(resourceNodes, (left, right) =>
        {
            float leftDistance = (left.transform.position - transform.position).sqrMagnitude;
            float rightDistance = (right.transform.position - transform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        for (int i = 0; i < resourceNodes.Length; i++)
        {
            ResourceNode resourceNode = resourceNodes[i];
            if (resourceNode == null || !resourceNode.CanBeGathered)
            {
                continue;
            }

            if (TryStartGatherTask(resourceNode, pauseBrainWhileTaskActive))
            {
                return true;
            }
        }

        StopMovement();
        if (pauseBrainWhileTaskActive)
        {
            SuppressIdleBrain();
        }

        Debug.LogWarning($"[CharacterGatherer] {name} could not find a reachable resource node.", this);
        return false;
    }

    public bool TryStartGatherTask(ResourceNode resourceNode, bool pauseBrainWhileTaskActive = true)
    {
        if (resourceNode == null)
        {
            return false;
        }

        RefreshCachedComponents();
        ReleaseIdleBrainSuppression();

        if (_movement == null)
        {
            Debug.LogWarning($"[CharacterGatherer] {name} requires CharacterMovement to gather resources.", this);
            return false;
        }

        if (_character == null)
        {
            Debug.LogWarning($"[CharacterGatherer] {name} requires Character component references to be initialized.", this);
            return false;
        }

        if (_character.CharacterType == Character.CharacterTypes.Player)
        {
            Debug.LogWarning($"[CharacterGatherer] {name} cannot start a gather task while under player control.", this);
            return false;
        }

        if (HasActiveTask)
        {
            if (_currentTask.ResourceNode == resourceNode)
            {
                return true;
            }

            CancelCurrentTask();
        }

        if (!resourceNode.TryReserveClosestSlot(transform, transform.position, out ResourceApproachSlot reservedSlot))
        {
            LogVerbose($"No free gather slot on {resourceNode.name} for {name}.");
            return false;
        }

        GatherTask newTask = new GatherTask(resourceNode, reservedSlot, transform);
        Vector3 slotPosition = newTask.SlotWorldPosition;

        if (!TryBuildPathTo(slotPosition))
        {
            resourceNode.ReleaseSlot(transform);
            newTask.Fail("No NavMesh path to reserved gather slot.");
            _lastTask = newTask;
            LogVerbose($"No valid path from {name} to gather slot on {resourceNode.name}.");
            return false;
        }

        _currentTask = newTask;
        _currentTaskOwnsBrain = pauseBrainWhileTaskActive;

        if (_currentTaskOwnsBrain)
        {
            PauseBrain();
        }

        if (_movement != null)
        {
            _movement.ScriptDrivenInput = true;
            _movement.InputAuthorized = false;
        }

        _currentTask.MarkMovingToSlot();
        NotifyTaskStarted(_currentTask);

        LogVerbose($"{name} started moving to resource {resourceNode.name}.");
        return true;
    }

    public void CancelCurrentTask()
    {
        if (_currentTask == null)
        {
            return;
        }

        _currentTask.Cancel();
        FinishTaskCleanup();
    }

    public bool HasTaskFor(ResourceNode resourceNode)
    {
        return HasActiveTask && _currentTask.ResourceNode == resourceNode;
    }

    private void UpdateMoveToSlot()
    {
        if (!_currentTask.ResourceNode.TryGetReservedSlotPosition(transform, out Vector3 slotPosition))
        {
            FailCurrentTask("Reserved gather slot was released unexpectedly.");
            return;
        }

        if (!_currentTask.ResourceNode.CanBeGathered)
        {
            FailCurrentTask("Resource is depleted before gathering started.");
            return;
        }

        float distanceToSlot = GetPlanarDistance(transform.position, slotPosition);
        if (distanceToSlot <= _slotArrivalThreshold)
        {
            StopMovement();
            _currentTask.BeginGathering();
            LogVerbose($"{name} arrived at resource {_currentTask.ResourceNode.name} and started gathering.");
            return;
        }

        if (Time.time - _lastPathRebuildTime >= _repathInterval)
        {
            if (!TryBuildPathTo(slotPosition))
            {
                FailCurrentTask("Path to gather slot became invalid.");
                return;
            }
        }

        if (_activePath == null || _activePath.corners == null || _activePath.corners.Length == 0)
        {
            FailCurrentTask("No active path available for gathering.");
            return;
        }

        AdvanceCornerIndexIfNeeded();

        Vector3 targetCorner = GetCurrentCornerOrSlot(slotPosition);
        Vector3 direction = targetCorner - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            StopMovement();
            return;
        }

        direction.Normalize();
        _movement.SetMovement(new Vector2(direction.x, direction.z));
    }

    private void UpdateGathering()
    {
        ResourceNode resourceNode = _currentTask.ResourceNode;
        if (resourceNode == null)
        {
            FailCurrentTask("Resource node reference was lost during gathering.");
            return;
        }

        StopMovement();

        if (_faceResourceWhileGathering)
        {
            FaceResource(resourceNode.transform.position);
        }

        if (_currentTask.TimeInCurrentState < resourceNode.GatherDuration)
        {
            return;
        }

        if (!resourceNode.TryGatherCycle(out int gatheredAmount))
        {
            FailCurrentTask("Gather cycle finished but resource had no amount left.");
            return;
        }

        if (CollectedResourcesManager.HasInstance)
        {
            CollectedResourcesManager.Instance.AddResource(resourceNode.Type, gatheredAmount);
        }

        _currentTask.Complete(gatheredAmount);
        _lastTask = _currentTask;
        NotifyTaskCompleted(_currentTask);
        LogVerbose($"{name} gathered {gatheredAmount} {resourceNode.Type} from {resourceNode.name}.");
        FinishTaskCleanup();
    }

    private void FailCurrentTask(string reason)
    {
        if (_currentTask == null)
        {
            return;
        }

        bool suppressBrainAfterFailure = _currentTaskOwnsBrain;
        _currentTask.Fail(reason);
        _lastTask = _currentTask;
        NotifyTaskFailed(_currentTask);
        Debug.LogWarning($"[CharacterGatherer] {name} failed gather task: {reason}", this);
        FinishTaskCleanup();

        if (suppressBrainAfterFailure)
        {
            SuppressIdleBrain();
        }
    }

    private void FinishTaskCleanup()
    {
        if (_currentTask == null)
        {
            return;
        }

        ResourceNode resourceNode = _currentTask.ResourceNode;
        if (resourceNode != null)
        {
            resourceNode.ReleaseSlot(transform);
        }

        StopMovement();
        ClearActivePath();

        if (_currentTaskOwnsBrain)
        {
            RestoreBrain();
        }

        if (_currentTask.State == GatherTaskState.Cancelled)
        {
            _lastTask = _currentTask;
            NotifyTaskCancelled(_currentTask);
            LogVerbose($"{name} cancelled gather task.");
        }

        _currentTask = null;
        _currentTaskOwnsBrain = false;
    }

    private bool TryBuildPathTo(Vector3 targetPosition)
    {
        int areaMask = _pathfinder != null ? _pathfinder.AreaMask : NavMesh.AllAreas;
        float sampleDistance = _pathfinder != null ? Mathf.Max(_pathfinder.ClosestPointThreshold, _pathValidationSampleDistance) : _pathValidationSampleDistance;

        bool startValid = NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, sampleDistance, areaMask);
        bool targetValid = NavMesh.SamplePosition(targetPosition, out NavMeshHit targetHit, sampleDistance, areaMask);

        if (!startValid || !targetValid)
        {
            return false;
        }

        if (_activePath == null)
        {
            _activePath = new NavMeshPath();
        }

        bool pathCalculated = NavMesh.CalculatePath(startHit.position, targetHit.position, areaMask, _activePath);
        if (!pathCalculated || _activePath.status != NavMeshPathStatus.PathComplete || _activePath.corners == null || _activePath.corners.Length == 0)
        {
            return false;
        }

        _currentPathTarget = targetHit.position;
        _currentCornerIndex = _activePath.corners.Length > 1 ? 1 : 0;
        _lastPathRebuildTime = Time.time;
        return true;
    }

    private void AdvanceCornerIndexIfNeeded()
    {
        if (_activePath == null || _activePath.corners == null)
        {
            return;
        }

        while (_currentCornerIndex < _activePath.corners.Length)
        {
            Vector3 currentCorner = _activePath.corners[_currentCornerIndex];
            if (GetPlanarDistance(transform.position, currentCorner) > _cornerReachThreshold)
            {
                break;
            }

            _currentCornerIndex++;
        }
    }

    private Vector3 GetCurrentCornerOrSlot(Vector3 fallbackSlotPosition)
    {
        if (_activePath == null || _activePath.corners == null || _activePath.corners.Length == 0)
        {
            return fallbackSlotPosition;
        }

        if (_currentCornerIndex >= _activePath.corners.Length)
        {
            return _currentPathTarget != Vector3.zero ? _currentPathTarget : fallbackSlotPosition;
        }

        return _activePath.corners[_currentCornerIndex];
    }

    private void ClearActivePath()
    {
        _currentCornerIndex = 0;
        _lastPathRebuildTime = 0f;
        _currentPathTarget = Vector3.zero;
        _activePath = new NavMeshPath();
    }

    private void StopMovement()
    {
        if (_movement != null)
        {
            _movement.SetMovement(Vector2.zero);
            _movement.StopMovementInstantly();
        }

        if (_pathfinder != null)
        {
            _pathfinder.StopPathfinding();
            _pathfinder.CleanTarget();
        }
    }

    private void PauseBrain()
    {
        if (_brain == null)
        {
            return;
        }

        _brainSuppressedWithoutTask = false;
        _brainWasEnabledBeforeTask = _brain.enabled;
        _brainWasActiveBeforeTask = _brain.BrainActive;
        _brain.Target = null;
        _brain.BrainActive = false;
    }

    private void RestoreBrain()
    {
        if (_brain == null)
        {
            return;
        }

        _brain.enabled = _brainWasEnabledBeforeTask;

        if (_brainWasEnabledBeforeTask && _brainWasActiveBeforeTask)
        {
            _brain.ResetBrain();
            return;
        }

        _brain.BrainActive = _brainWasActiveBeforeTask;
    }

    private void SuppressIdleBrain()
    {
        if (_brain == null || _brainSuppressedWithoutTask)
        {
            return;
        }

        _brain.Target = null;
        _brain.BrainActive = false;
        _brainSuppressedWithoutTask = true;
    }

    private void ReleaseIdleBrainSuppression()
    {
        if (_brain == null || !_brainSuppressedWithoutTask)
        {
            return;
        }

        _brain.BrainActive = true;
        _brain.ResetBrain();
        _brainSuppressedWithoutTask = false;
    }

    private void NotifyTaskStarted(GatherTask task)
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.NotifyGatherTaskStarted(this, task);
        }
    }

    private void NotifyTaskCompleted(GatherTask task)
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.NotifyGatherTaskCompleted(this, task);
        }
    }

    private void NotifyTaskFailed(GatherTask task)
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.NotifyGatherTaskFailed(this, task);
        }
    }

    private void NotifyTaskCancelled(GatherTask task)
    {
        if (CharacterTaskManager.HasInstance)
        {
            CharacterTaskManager.Instance.NotifyGatherTaskCancelled(this, task);
        }
    }

    private void FaceResource(Vector3 worldPosition)
    {
        Vector3 lookDirection = worldPosition - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }

    private float GetPlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void DebugStartNearestResourceTask()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[CharacterGatherer] Debug gather button works in Play Mode.", this);
            return;
        }

        TryStartWorkOnNearestNode();
    }

    private void DebugCheckNearestResourcePaths()
    {
        RefreshCachedComponents();

        int areaMask = _pathfinder != null ? _pathfinder.AreaMask : NavMesh.AllAreas;
        float sampleDistance = _pathfinder != null ? Mathf.Max(_pathfinder.ClosestPointThreshold, _pathValidationSampleDistance) : _pathValidationSampleDistance;

        bool startValid = NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, sampleDistance, areaMask);
        Debug.Log(
            $"[CharacterGatherer] Path debug for {name}. " +
            $"Start world={transform.position}, startOnNavMesh={startValid}, sampledStart={(startValid ? startHit.position.ToString() : "n/a")}, " +
            $"areaMask={areaMask}, sampleDistance={sampleDistance}.",
            this);

        ResourceNode[] resourceNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);
        if (resourceNodes == null || resourceNodes.Length == 0)
        {
            Debug.LogWarning($"[CharacterGatherer] {name} could not find any resource nodes to debug.", this);
            return;
        }

        System.Array.Sort(resourceNodes, (left, right) =>
        {
            float leftDistance = (left.transform.position - transform.position).sqrMagnitude;
            float rightDistance = (right.transform.position - transform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        for (int i = 0; i < resourceNodes.Length; i++)
        {
            ResourceNode resourceNode = resourceNodes[i];
            if (resourceNode == null)
            {
                continue;
            }

            bool reserved = resourceNode.TryReserveClosestSlot(transform, transform.position, out ResourceApproachSlot reservedSlot);
            if (!reserved || reservedSlot == null)
            {
                Debug.LogWarning($"[CharacterGatherer] {name} -> {resourceNode.name}: no free slot to debug.", resourceNode);
                continue;
            }

            Vector3 slotPosition = reservedSlot.GetWorldPosition(resourceNode.transform);
            bool targetValid = NavMesh.SamplePosition(slotPosition, out NavMeshHit targetHit, sampleDistance, areaMask);

            NavMeshPath debugPath = new NavMeshPath();
            bool pathCalculated = startValid && targetValid && NavMesh.CalculatePath(startHit.position, targetHit.position, areaMask, debugPath);
            int cornerCount = debugPath.corners != null ? debugPath.corners.Length : 0;

            Debug.Log(
                $"[CharacterGatherer] {name} -> {resourceNode.name}: " +
                $"slotWorld={slotPosition}, targetOnNavMesh={targetValid}, sampledTarget={(targetValid ? targetHit.position.ToString() : "n/a")}, " +
                $"pathCalculated={pathCalculated}, pathStatus={debugPath.status}, corners={cornerCount}.",
                resourceNode);

            if (pathCalculated && debugPath.corners != null && debugPath.corners.Length > 1)
            {
                for (int cornerIndex = 1; cornerIndex < debugPath.corners.Length; cornerIndex++)
                {
                    Debug.DrawLine(debugPath.corners[cornerIndex - 1], debugPath.corners[cornerIndex], Color.green, 5f);
                }
            }

            resourceNode.ReleaseSlot(transform);
        }
    }

    private void LogVerbose(string message)
    {
        if (_verboseLogs)
        {
            Debug.Log($"[CharacterGatherer] {message}", this);
        }
    }
}
