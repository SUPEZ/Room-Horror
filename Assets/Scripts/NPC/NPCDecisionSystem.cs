using System.Collections.Generic;
using UnityEngine;

namespace NPC
{
    /// <summary>
    /// Base class for NPC decisions - defines work types and behaviors
    /// Extend this class to create specific decision types
    /// </summary>
    public abstract class NPCDecision : ScriptableObject
    {
        [Header("Decision Info")]
        [Tooltip("Display name of this decision")]
        public string decisionName;
        
        [Tooltip("Description of what this decision does")]
        [TextArea(2, 4)]
        public string description;
        
        [Tooltip("Icon for UI display")]
        public Sprite icon;

        /// <summary>
        /// Execute this decision on the target NPC
        /// </summary>
        public abstract void Execute(NPCController npc);

        /// <summary>
        /// Check if this decision can be executed
        /// </summary>
        public virtual bool CanExecute(NPCController npc)
        {
            return true;
        }

        /// <summary>
        /// Called when decision is cancelled
        /// </summary>
        public virtual void OnCancel(NPCController npc)
        {
        }

        /// <summary>
        /// Update decision (called per frame while active)
        /// </summary>
        public virtual void Update(NPCController npc)
        {
        }

        /// <summary>
        /// Check if decision is complete
        /// </summary>
        public virtual bool IsComplete(NPCController npc)
        {
            return false;
        }
    }

    /// <summary>
    /// Manages decision queue for a single NPC
    /// </summary>
    public class NPCDecisionSystem : MonoBehaviour
    {
        [Header("Decision Queue")]
        [Tooltip("Maximum number of queued decisions")]
        [SerializeField] private int _maxQueueSize = 10;

        // Decision queue
        private Queue<NPCDecision> _decisionQueue = new Queue<NPCDecision>();
        private NPCDecision _currentDecision;
        private NPCController _npcController;

        public NPCDecision CurrentDecision => _currentDecision;
        public int QueueCount => _decisionQueue.Count;

        private void Awake()
        {
            _npcController = GetComponent<NPCController>();
            if (_npcController == null)
            {
                Debug.LogError("[NPCDecisionSystem] NPCController component not found!", this);
            }
        }

        private void Update()
        {
            ProcessDecisions();
        }

        /// <summary>
        /// Process current decision and queue
        /// </summary>
        private void ProcessDecisions()
        {
            if (_currentDecision != null)
            {
                // Update current decision
                _currentDecision.Update(_npcController);

                // Check if complete
                if (_currentDecision.IsComplete(_npcController))
                {
                    CompleteCurrentDecision();
                }
            }
            else if (_decisionQueue.Count > 0)
            {
                // Start next decision in queue
                StartNextDecision();
            }
        }

        /// <summary>
        /// Add decision to queue
        /// </summary>
        public bool EnqueueDecision(NPCDecision decision)
        {
            if (_decisionQueue.Count >= _maxQueueSize)
            {
                Debug.LogWarning($"[NPCDecisionSystem] Queue full for {gameObject.name}");
                return false;
            }

            _decisionQueue.Enqueue(decision);
            Debug.Log($"[NPCDecisionSystem] Queued decision: {decision.decisionName} for {gameObject.name}");
            return true;
        }

        /// <summary>
        /// Set decision immediately, clearing queue
        /// </summary>
        public void SetDecisionImmediate(NPCDecision decision)
        {
            CancelCurrentDecision();
            ClearQueue();
            _decisionQueue.Enqueue(decision);
            StartNextDecision();
        }

        /// <summary>
        /// Start next decision from queue
        /// </summary>
        private void StartNextDecision()
        {
            if (_decisionQueue.Count == 0) return;

            NPCDecision nextDecision = _decisionQueue.Dequeue();

            if (nextDecision.CanExecute(_npcController))
            {
                _currentDecision = nextDecision;
                _currentDecision.Execute(_npcController);
                Debug.Log($"[NPCDecisionSystem] Started decision: {_currentDecision.decisionName}");
            }
            else
            {
                Debug.LogWarning($"[NPCDecisionSystem] Cannot execute decision: {nextDecision.decisionName}");
            }
        }

        /// <summary>
        /// Complete current decision
        /// </summary>
        private void CompleteCurrentDecision()
        {
            if (_currentDecision != null)
            {
                Debug.Log($"[NPCDecisionSystem] Completed decision: {_currentDecision.decisionName}");
                _currentDecision = null;
            }
        }

        /// <summary>
        /// Cancel current decision
        /// </summary>
        public void CancelCurrentDecision()
        {
            if (_currentDecision != null)
            {
                _currentDecision.OnCancel(_npcController);
                Debug.Log($"[NPCDecisionSystem] Cancelled decision: {_currentDecision.decisionName}");
                _currentDecision = null;
            }
        }

        /// <summary>
        /// Clear all queued decisions
        /// </summary>
        public void ClearQueue()
        {
            _decisionQueue.Clear();
        }

        /// <summary>
        /// Get all queued decision names
        /// </summary>
        public List<string> GetQueuedDecisionNames()
        {
            List<string> names = new List<string>();
            foreach (NPCDecision decision in _decisionQueue)
            {
                names.Add(decision.decisionName);
            }
            return names;
        }
    }

    /// <summary>
    /// NPC Controller - manages NPC state and behavior
    /// Attach this to NPC GameObjects
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        [Header("NPC Info")]
        [Tooltip("NPC display name")]
        public string npcName = "NPC";
        
        [Tooltip("NPC type/role")]
        public string npcType = "Worker";

        [Header("Components")]
        private NPCDecisionSystem _decisionSystem;

        public NPCDecisionSystem DecisionSystem => _decisionSystem;

        private void Awake()
        {
            _decisionSystem = GetComponent<NPCDecisionSystem>();
            if (_decisionSystem == null)
            {
                _decisionSystem = gameObject.AddComponent<NPCDecisionSystem>();
            }

            // Ensure NPC tag is set
            if (!gameObject.CompareTag("NPC"))
            {
                gameObject.tag = "NPC";
            }
        }

        /// <summary>
        /// Assign decision to this NPC
        /// </summary>
        public void AssignDecision(NPCDecision decision, bool immediate = false)
        {
            if (immediate)
            {
                _decisionSystem.SetDecisionImmediate(decision);
            }
            else
            {
                _decisionSystem.EnqueueDecision(decision);
            }
        }

        /// <summary>
        /// Get current task description for UI
        /// </summary>
        public string GetCurrentTask()
        {
            if (_decisionSystem.CurrentDecision != null)
            {
                return _decisionSystem.CurrentDecision.decisionName;
            }
            return "Idle";
        }
    }
}

