using UnityEngine;
using NPC;

namespace NPC.Decisions
{
    /// <summary>
    /// Work decision - NPC performs work at a specific location
    /// </summary>
    [CreateAssetMenu(fileName = "WorkDecision", menuName = "NPC/Decisions/Work", order = 1)]
    public class WorkDecision : NPCDecision
    {
        [Header("Work Settings")]
        [Tooltip("Work duration in seconds")]
        public float workDuration = 5f;
        
        [Tooltip("Work location (can be set at runtime)")]
        public Vector3 workLocation;

        private float _workStartTime;

        public override void Execute(NPCController npc)
        {
            _workStartTime = Time.time;
            Debug.Log($"[WorkDecision] {npc.npcName} started working at {workLocation}");
            
            // TODO: Move NPC to work location
            // TODO: Play work animation
        }

        public override void Update(NPCController npc)
        {
            // TODO: Update work progress
        }

        public override bool IsComplete(NPCController npc)
        {
            return Time.time - _workStartTime >= workDuration;
        }

        public override void OnCancel(NPCController npc)
        {
            Debug.Log($"[WorkDecision] {npc.npcName} stopped working");
        }
    }

    /// <summary>
    /// Guard decision - NPC patrols or guards a location
    /// </summary>
    [CreateAssetMenu(fileName = "GuardDecision", menuName = "NPC/Decisions/Guard", order = 2)]
    public class GuardDecision : NPCDecision
    {
        [Header("Guard Settings")]
        [Tooltip("Guard position")]
        public Vector3 guardPosition;
        
        [Tooltip("Patrol radius")]
        public float patrolRadius = 5f;
        
        [Tooltip("Is this an infinite guard task?")]
        public bool isInfinite = true;

        public override void Execute(NPCController npc)
        {
            Debug.Log($"[GuardDecision] {npc.npcName} started guarding at {guardPosition}");
            
            // TODO: Move NPC to guard position
            // TODO: Set up patrol behavior
        }

        public override void Update(NPCController npc)
        {
            // TODO: Patrol logic
        }

        public override bool IsComplete(NPCController npc)
        {
            return !isInfinite; // Never complete if infinite
        }

        public override void OnCancel(NPCController npc)
        {
            Debug.Log($"[GuardDecision] {npc.npcName} stopped guarding");
        }
    }

    /// <summary>
    /// Build decision - NPC builds a structure
    /// </summary>
    [CreateAssetMenu(fileName = "BuildDecision", menuName = "NPC/Decisions/Build", order = 3)]
    public class BuildDecision : NPCDecision
    {
        [Header("Build Settings")]
        [Tooltip("Building to construct")]
        public GameObject buildingPrefab;
        
        [Tooltip("Build location")]
        public Vector3 buildLocation;
        
        [Tooltip("Build time in seconds")]
        public float buildTime = 10f;

        private float _buildStartTime;

        public override void Execute(NPCController npc)
        {
            _buildStartTime = Time.time;
            Debug.Log($"[BuildDecision] {npc.npcName} started building at {buildLocation}");
            
            // TODO: Move NPC to build location
            // TODO: Spawn construction site visual
        }

        public override void Update(NPCController npc)
        {
            // TODO: Update build progress visual
        }

        public override bool IsComplete(NPCController npc)
        {
            if (Time.time - _buildStartTime >= buildTime)
            {
                // Spawn the actual building
                if (buildingPrefab != null)
                {
                    GameObject.Instantiate(buildingPrefab, buildLocation, Quaternion.identity);
                    Debug.Log($"[BuildDecision] Building completed by {npc.npcName}");
                }
                return true;
            }
            return false;
        }

        public override void OnCancel(NPCController npc)
        {
            Debug.Log($"[BuildDecision] {npc.npcName} stopped building");
            // TODO: Remove construction site visual
        }
    }

    /// <summary>
    /// Gather decision - NPC gathers resources
    /// </summary>
    [CreateAssetMenu(fileName = "GatherDecision", menuName = "NPC/Decisions/Gather", order = 4)]
    public class GatherDecision : NPCDecision
    {
        [Header("Gather Settings")]
        [Tooltip("Resource type to gather")]
        public string resourceType = "Wood";
        
        [Tooltip("Gather location")]
        public Vector3 gatherLocation;
        
        [Tooltip("Gather duration")]
        public float gatherDuration = 5f;
        
        [Tooltip("Amount to gather")]
        public int gatherAmount = 10;

        private float _gatherStartTime;

        public override void Execute(NPCController npc)
        {
            _gatherStartTime = Time.time;
            Debug.Log($"[GatherDecision] {npc.npcName} started gathering {resourceType} at {gatherLocation}");
            
            // TODO: Move NPC to gather location
            // TODO: Play gather animation
        }

        public override void Update(NPCController npc)
        {
            // TODO: Update gather progress
        }

        public override bool IsComplete(NPCController npc)
        {
            if (Time.time - _gatherStartTime >= gatherDuration)
            {
                Debug.Log($"[GatherDecision] {npc.npcName} gathered {gatherAmount} {resourceType}");
                // TODO: Add resources to inventory system
                return true;
            }
            return false;
        }

        public override void OnCancel(NPCController npc)
        {
            Debug.Log($"[GatherDecision] {npc.npcName} stopped gathering");
        }
    }

    /// <summary>
    /// Move decision - NPC moves to a location
    /// </summary>
    [CreateAssetMenu(fileName = "MoveDecision", menuName = "NPC/Decisions/Move", order = 5)]
    public class MoveDecision : NPCDecision
    {
        [Header("Move Settings")]
        [Tooltip("Target position")]
        public Vector3 targetPosition;
        
        [Tooltip("Acceptable distance from target")]
        public float arrivalThreshold = 0.5f;

        public override void Execute(NPCController npc)
        {
            Debug.Log($"[MoveDecision] {npc.npcName} moving to {targetPosition}");
            
            // TODO: Set navigation target
            // TODO: Start movement
        }

        public override void Update(NPCController npc)
        {
            // TODO: Update movement
        }

        public override bool IsComplete(NPCController npc)
        {
            float distance = Vector3.Distance(npc.transform.position, targetPosition);
            return distance <= arrivalThreshold;
        }

        public override void OnCancel(NPCController npc)
        {
            Debug.Log($"[MoveDecision] {npc.npcName} movement cancelled");
            // TODO: Stop movement
        }
    }
}

