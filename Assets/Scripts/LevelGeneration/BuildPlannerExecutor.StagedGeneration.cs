using System;
using System.Collections.Generic;
using FIMSpace.Generating.Planning;
using UnityEngine;

namespace FIMSpace.Generating
{
    /// <summary>
    /// Project-side staged spawning API for BuildPlannerExecutor.
    /// Planner objects can be spawned in stages and the unbuilt expansion can be replanned.
    /// </summary>
    public partial class BuildPlannerExecutor
    {
        private readonly HashSet<int> _stagedGeneratedPlannerIndices = new HashSet<int>();

        public bool IsStagedPlanReady =>
            generatingInstance != null &&
            generatingInstance.IsGeneratingDone &&
            GeneratedPreview != null &&
            !IsGenerating;

        public int StagedPlannerCount => generatingInstance != null
            ? generatingInstance.BasePlanners.Count
            : 0;

        public bool BeginStagedGenerationPreview()
        {
            if (BuildPlannerPreset == null)
            {
                Debug.LogError("[BuildPlannerExecutor] A BuildPlannerPreset is required for staged generation.", this);
                return false;
            }

            EnsureFieldsInitialization();
            ClearGenerated();
            _stagedGeneratedPlannerIndices.Clear();
            GeneratePreview();
            return true;
        }

        public bool BeginStagedReplanPreviewPreservingGenerated()
        {
            if (BuildPlannerPreset == null)
            {
                Debug.LogError("[BuildPlannerExecutor] A BuildPlannerPreset is required for staged replanning.", this);
                return false;
            }

            if (IsGenerating)
            {
                Debug.LogWarning("[BuildPlannerExecutor] Cannot replan while another layout is generating.", this);
                return false;
            }

            EnsureFieldsInitialization();

            List<BuildPlannerReference> preservedReferences = BuildPlannerReferences != null
                ? new List<BuildPlannerReference>(BuildPlannerReferences)
                : new List<BuildPlannerReference>();

            if (generatingInstance != null) generatingInstance.ClearGeneration();

            // GeneratePreview normally clears all spawned objects when a preview already
            // exists. Reset only the preview state so the dungeon master room stays alive.
            GeneratedPreview = null;
            latestGeneratedPreviewSeed = null;
            _stagedGeneratedPlannerIndices.Clear();

            bool randomSeedWasEnabled = RandomSeed;
            RandomSeed = false;

            try
            {
                GeneratePreview();
            }
            finally
            {
                RandomSeed = randomSeedWasEnabled;
            }

            if (BuildPlannerReferences == null) BuildPlannerReferences = new List<BuildPlannerReference>();
            for (int i = 0; i < preservedReferences.Count; i++)
            {
                BuildPlannerReference plannerReference = preservedReferences[i];
                if (plannerReference == null || BuildPlannerReferences.Contains(plannerReference)) continue;
                BuildPlannerReferences.Add(plannerReference);
                _stagedGeneratedPlannerIndices.Add(plannerReference.BuildPlannerIndex);
            }

            return true;
        }

        public void ClearStagedGeneration()
        {
            ClearGenerated();
            _stagedGeneratedPlannerIndices.Clear();
        }

        public int FindStagedPlannerIndex(string plannerName)
        {
            if (string.IsNullOrWhiteSpace(plannerName)) return -1;

            BuildPlannerPreset plan = generatingInstance != null ? generatingInstance : BuildPlannerPreset;
            if (plan == null || plan.BasePlanners == null) return -1;

            for (int i = 0; i < plan.BasePlanners.Count; i++)
            {
                FieldPlanner planner = plan.BasePlanners[i];
                if (planner == null) continue;

                string candidateName = planner.name.Replace("(Clone)", string.Empty).Trim();
                if (string.Equals(candidateName, plannerName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        public bool TrySetStagedPlannerVariable(string plannerName, string variableName, Vector3 value)
        {
            if (BuildPlannerPreset == null || string.IsNullOrWhiteSpace(variableName)) return false;

            EnsureFieldsInitialization();
            int plannerIndex = FindStagedPlannerIndex(plannerName);
            if (plannerIndex < 0 || plannerIndex >= PlannerPrepare.FieldSetupCompositions.Count) return false;

            FieldSetupComposition composition = PlannerPrepare.FieldSetupCompositions[plannerIndex];
            if (composition == null || composition.PlannerVariablesOverrides == null) return false;

            for (int i = 0; i < composition.PlannerVariablesOverrides.Count; i++)
            {
                FieldVariable variable = composition.PlannerVariablesOverrides[i];
                if (variable == null) continue;
                if (!string.Equals(variable.Name, variableName, StringComparison.OrdinalIgnoreCase)) continue;

                variable.SetValue(value);
                return true;
            }

            return false;
        }

        public bool EnsureFourCenteredEdgeInstructions(string plannerName, int instructionId)
        {
            if (!IsStagedPlanReady) return false;

            int plannerIndex = FindStagedPlannerIndex(plannerName);
            if (plannerIndex < 0 || plannerIndex >= generatingInstance.BasePlanners.Count) return false;

            FieldPlanner planner = generatingInstance.BasePlanners[plannerIndex];
            if (planner == null || planner.LatestResult == null || planner.LatestChecker == null) return false;
            if (planner.LatestChecker.ChildPositionsCount == 0) return false;

            Vector3Int[] directions =
            {
                Vector3Int.forward,
                Vector3Int.right,
                Vector3Int.back,
                Vector3Int.left
            };

            FieldCell[] centeredEdgeCells = new FieldCell[directions.Length];

            for (int i = 0; i < directions.Length; i++)
            {
                centeredEdgeCells[i] = FindCenteredEdgeCell(planner.LatestChecker, directions[i]);
                if (centeredEdgeCells[i] == null) return false;
            }

            // The corridor planner adds its own door instruction to the nearest main-room
            // cell while creating the preview. Keeping it and then adding four centered
            // doors produces a fifth, usually off-center passage. For the main room this
            // method owns the complete set of door instructions, so replace that set
            // atomically while preserving instructions with other IDs.
            RemoveInstructionsWithId(planner.LatestResult, instructionId);

            for (int i = 0; i < directions.Length; i++)
            {
                AddInstructionIfMissing(
                    planner.LatestResult,
                    centeredEdgeCells[i],
                    directions[i],
                    instructionId);
            }

            return true;
        }

        public bool AlignStagedExpansionToCenteredEdge(
            string mainRoomPlannerName,
            string corridorPlannerName,
            Vector3Int direction,
            int corridorClearInstructionId,
            params string[] attachedPlannerNames)
        {
            if (!IsStagedPlanReady) return false;
            if (direction.y != 0 || Mathf.Abs(direction.x) + Mathf.Abs(direction.z) != 1) return false;

            int mainRoomIndex = FindStagedPlannerIndex(mainRoomPlannerName);
            int corridorIndex = FindStagedPlannerIndex(corridorPlannerName);
            if (mainRoomIndex < 0 || mainRoomIndex >= generatingInstance.BasePlanners.Count) return false;
            if (corridorIndex < 0 || corridorIndex >= generatingInstance.BasePlanners.Count) return false;

            FieldPlanner mainRoom = generatingInstance.BasePlanners[mainRoomIndex];
            FieldPlanner corridor = generatingInstance.BasePlanners[corridorIndex];
            if (mainRoom == null || corridor == null) return false;
            if (mainRoom.LatestChecker == null || corridor.LatestChecker == null) return false;

            FieldCell centeredRoomCell = FindCenteredEdgeCell(mainRoom.LatestChecker, direction);
            if (centeredRoomCell == null) return false;

            Vector3 desiredConnectionPosition = mainRoom.LatestChecker.LocalToWorld(centeredRoomCell.Pos + direction);
            Vector3 mainToCorridorWorldDirection =
                mainRoom.LatestChecker.RootRotation * (Vector3)direction;
            FieldCell previousConnectionCell = corridor.LatestChecker.GetNearestCellTo(mainRoom.LatestChecker, false);
            FieldCell corridorConnectionCell = FindInwardEdgeConnectionCell(
                corridor.LatestChecker,
                mainToCorridorWorldDirection,
                desiredConnectionPosition);

            if (previousConnectionCell == null || corridorConnectionCell == null) return false;

            // Build the wall-removal direction from the actual final seam positions.
            // Checker positions are in executor space; store a world-space rotation so
            // GridPainter can convert it through the complete generated transform.
            Vector3 mainConnectionPosition =
                mainRoom.LatestChecker.LocalToWorld(centeredRoomCell.Pos);
            Vector3 corridorToRoomDirection = mainConnectionPosition - desiredConnectionPosition;
            if (corridorToRoomDirection.sqrMagnitude < 0.0001f) return false;

            Vector3 corridorToRoomWorldDirection =
                transform.TransformDirection(corridorToRoomDirection.normalized);
            Quaternion clearWallWorldRotation = Quaternion.LookRotation(
                corridorToRoomWorldDirection,
                transform.up);

            RelocateConnectionInstruction(
                corridor.LatestResult,
                previousConnectionCell,
                corridorConnectionCell,
                clearWallWorldRotation,
                corridorClearInstructionId);

            Vector3 actualConnectionPosition = corridor.LatestChecker.LocalToWorld(corridorConnectionCell.Pos);
            Vector3 offset = desiredConnectionPosition - actualConnectionPosition;

            HashSet<Checker.CheckerField3D> shiftedCheckers = new HashSet<Checker.CheckerField3D>();
            HashSet<FieldPlanner> shiftedPlanners = new HashSet<FieldPlanner>();
            ShiftStagedPlannerHierarchy(corridor, offset, shiftedCheckers, shiftedPlanners);

            if (attachedPlannerNames != null)
            {
                for (int i = 0; i < attachedPlannerNames.Length; i++)
                {
                    int attachedPlannerIndex = FindStagedPlannerIndex(attachedPlannerNames[i]);
                    if (attachedPlannerIndex < 0 || attachedPlannerIndex >= generatingInstance.BasePlanners.Count)
                        return false;

                    ShiftStagedPlannerHierarchy(
                        generatingInstance.BasePlanners[attachedPlannerIndex],
                        offset,
                        shiftedCheckers,
                        shiftedPlanners);
                }
            }

            return true;
        }

        public bool IsStagedPlannerGenerated(int plannerIndex)
        {
            if (_stagedGeneratedPlannerIndices.Contains(plannerIndex)) return true;

            if (BuildPlannerReferences == null) return false;
            for (int i = 0; i < BuildPlannerReferences.Count; i++)
            {
                BuildPlannerReference plannerReference = BuildPlannerReferences[i];
                if (plannerReference == null) continue;
                if (plannerReference.BuildPlannerIndex != plannerIndex) continue;

                _stagedGeneratedPlannerIndices.Add(plannerIndex);
                return true;
            }

            return false;
        }

        public bool TryGenerateStagedPlanner(string plannerName)
        {
            int plannerIndex = FindStagedPlannerIndex(plannerName);
            if (plannerIndex < 0)
            {
                Debug.LogError($"[BuildPlannerExecutor] Planner '{plannerName}' was not found in the generated layout.", this);
                return false;
            }

            return TryGenerateStagedPlanner(plannerIndex);
        }

        public bool TryGenerateStagedPlanner(int plannerIndex)
        {
            if (!IsStagedPlanReady)
            {
                Debug.LogWarning("[BuildPlannerExecutor] The staged layout is not ready yet.", this);
                return false;
            }

            if (generatingInstance == null || plannerIndex < 0 || plannerIndex >= generatingInstance.BasePlanners.Count)
            {
                Debug.LogError($"[BuildPlannerExecutor] Invalid staged planner index: {plannerIndex}.", this);
                return false;
            }

            if (IsStagedPlannerGenerated(plannerIndex))
            {
                Debug.LogWarning($"[BuildPlannerExecutor] Planner stage {plannerIndex} is already generated.", this);
                return true;
            }

            if (FlexibleGen)
            {
                Debug.LogError("[BuildPlannerExecutor] Staged generation currently requires FlexibleGen to be disabled.", this);
                return false;
            }

            FieldPlanner planner = generatingInstance.BasePlanners[plannerIndex];
            if (planner == null || planner.DontGenerateIt)
            {
                Debug.LogError($"[BuildPlannerExecutor] Planner stage {plannerIndex} is unavailable or disabled.", this);
                return false;
            }

            seedIteration = CalculateStagedSeedOffset(plannerIndex);
            GenerateStagedPlannerObjects(planner);
            _stagedGeneratedPlannerIndices.Add(plannerIndex);
            return true;
        }

        private void GenerateStagedPlannerObjects(FieldPlanner planner)
        {
            GenerateSingleStagedPlannerObject(planner);

            List<FieldPlanner> duplicates = planner.GetDuplicatesPlannersList();
            if (duplicates != null)
            {
                for (int i = 0; i < duplicates.Count; i++)
                    GenerateSingleStagedPlannerObject(duplicates[i]);
            }

            for (int i = 0; i < planner.GetSubFieldsCount; i++)
            {
                FieldPlanner subField = planner.GetSubField(i);
                if (subField == null || subField.DontGenerateIt) continue;
                if (subField.DefaultFieldSetup == null) subField.DefaultFieldSetup = planner.DefaultFieldSetup;
                GenerateSingleStagedPlannerObject(subField);
            }
        }

        private void GenerateSingleStagedPlannerObject(FieldPlanner planner)
        {
            if (planner == null || planner.DontGenerateIt) return;

            if (planner.FieldType == FieldPlanner.EFieldType.Prefab)
                InstantiatePrefabField(planner);
            else
                GenerateGridPainterWithPlanner(planner);
        }

        private int CalculateStagedSeedOffset(int targetPlannerIndex)
        {
            int offset = 0;

            for (int plannerIndex = 0; plannerIndex < targetPlannerIndex; plannerIndex++)
            {
                FieldPlanner planner = generatingInstance.BasePlanners[plannerIndex];
                if (planner == null || planner.DontGenerateIt) continue;

                offset += CountGridPaintersForStagedPlanner(planner);
            }

            return offset;
        }

        private int CountGridPaintersForStagedPlanner(FieldPlanner planner)
        {
            int count = planner.FieldType == FieldPlanner.EFieldType.Prefab ? 0 : 1;

            List<FieldPlanner> duplicates = planner.GetDuplicatesPlannersList();
            if (duplicates != null)
            {
                for (int i = 0; i < duplicates.Count; i++)
                {
                    FieldPlanner duplicate = duplicates[i];
                    if (duplicate == null || duplicate.DontGenerateIt) continue;
                    if (duplicate.FieldType != FieldPlanner.EFieldType.Prefab) count++;
                }
            }

            for (int i = 0; i < planner.GetSubFieldsCount; i++)
            {
                FieldPlanner subField = planner.GetSubField(i);
                if (subField == null || subField.DontGenerateIt) continue;
                if (subField.FieldType != FieldPlanner.EFieldType.Prefab) count++;
            }

            return count;
        }

        private void ShiftStagedPlannerHierarchy(
            FieldPlanner planner,
            Vector3 offset,
            HashSet<Checker.CheckerField3D> shiftedCheckers,
            HashSet<FieldPlanner> shiftedPlanners)
        {
            if (planner == null || !shiftedPlanners.Add(planner)) return;

            Checker.CheckerField3D checker = planner.LatestChecker;
            if (checker != null && shiftedCheckers.Add(checker))
                checker.RootPosition += offset;

            List<FieldPlanner> duplicates = planner.GetDuplicatesPlannersList();
            if (duplicates != null)
            {
                for (int i = 0; i < duplicates.Count; i++)
                    ShiftStagedPlannerHierarchy(duplicates[i], offset, shiftedCheckers, shiftedPlanners);
            }

            for (int i = 0; i < planner.GetSubFieldsCount; i++)
                ShiftStagedPlannerHierarchy(planner.GetSubField(i), offset, shiftedCheckers, shiftedPlanners);
        }

        private FieldCell FindInwardEdgeConnectionCell(
            Checker.CheckerField3D checker,
            Vector3 worldDirection,
            Vector3 desiredConnectionPosition)
        {
            worldDirection.Normalize();

            FieldCell bestCell = null;
            float bestProjection = float.MaxValue;
            float bestPerpendicularDistance = float.MaxValue;

            for (int i = 0; i < checker.AllCells.Count; i++)
            {
                FieldCell candidate = checker.AllCells[i];
                Vector3 candidatePosition = checker.LocalToWorld(candidate.Pos);
                float projection = Vector3.Dot(candidatePosition, worldDirection);

                Vector3 toDesired = candidatePosition - desiredConnectionPosition;
                Vector3 perpendicular = toDesired - Vector3.Project(toDesired, worldDirection);
                float perpendicularDistance = perpendicular.sqrMagnitude;

                if (projection > bestProjection + 0.001f) continue;
                if (Mathf.Abs(projection - bestProjection) <= 0.001f &&
                    perpendicularDistance >= bestPerpendicularDistance)
                    continue;

                bestProjection = projection;
                bestPerpendicularDistance = perpendicularDistance;
                bestCell = candidate;
            }

            return bestCell;
        }

        private void RelocateConnectionInstruction(
            PlannerResult result,
            FieldCell previousCell,
            FieldCell targetCell,
            Quaternion worldRotation,
            int instructionId)
        {
            if (result == null || result.CellsInstructions == null) return;

            SpawnInstructionGuide connectionInstruction = null;

            for (int i = 0; i < result.CellsInstructions.Count; i++)
            {
                SpawnInstructionGuide instruction = result.CellsInstructions[i];
                if (instruction == null || instruction.Id != instructionId) continue;

                // Prefer the instruction which the planner originally placed on its
                // nearest connection cell, but still recover if PGG selected that cell
                // with slightly different nearest-cell rules.
                if (connectionInstruction == null || instruction.pos == previousCell.Pos)
                    connectionInstruction = instruction;

                if (instruction.pos == previousCell.Pos)
                    break;
            }

            if (connectionInstruction == null)
            {
                connectionInstruction = new SpawnInstructionGuide
                {
                    Id = instructionId
                };
                result.CellsInstructions.Add(connectionInstruction);
            }

            // WorldRot lets GridPainter account for both the executor rotation and the
            // corridor checker's own rotation when converting to desiredDirection.
            connectionInstruction.pos = targetCell.Pos;
            connectionInstruction.HelperCellRef = targetCell;
            connectionInstruction.rot = worldRotation;
            connectionInstruction.WorldRot = true;
            connectionInstruction.UseDirection = true;

            // The connection owns this instruction ID in the corridor. Remove stale
            // duplicates so an old preview cell cannot clear an additional wall.
            for (int i = result.CellsInstructions.Count - 1; i >= 0; i--)
            {
                SpawnInstructionGuide instruction = result.CellsInstructions[i];
                if (instruction == null || instruction == connectionInstruction) continue;
                if (instruction.Id == instructionId)
                    result.CellsInstructions.RemoveAt(i);
            }
        }

        private FieldCell FindCenteredEdgeCell(Checker.CheckerField3D checker, Vector3Int direction)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            int minZ = int.MaxValue;
            int maxZ = int.MinValue;

            for (int i = 0; i < checker.AllCells.Count; i++)
            {
                Vector3Int position = checker.AllCells[i].Pos;
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
                minZ = Mathf.Min(minZ, position.z);
                maxZ = Mathf.Max(maxZ, position.z);
            }

            float middleX = (minX + maxX) * 0.5f;
            float middleY = (minY + maxY) * 0.5f;
            float middleZ = (minZ + maxZ) * 0.5f;
            FieldCell bestCell = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < checker.AllCells.Count; i++)
            {
                FieldCell candidate = checker.AllCells[i];
                Vector3Int position = candidate.Pos;

                bool onEdge =
                    direction.x > 0 ? position.x == maxX :
                    direction.x < 0 ? position.x == minX :
                    direction.z > 0 ? position.z == maxZ :
                    position.z == minZ;

                if (!onEdge) continue;

                float perpendicularDistance = direction.x != 0
                    ? Mathf.Abs(position.z - middleZ)
                    : Mathf.Abs(position.x - middleX);
                float distance = perpendicularDistance + Mathf.Abs(position.y - middleY);

                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestCell = candidate;
            }

            return bestCell;
        }

        private void AddInstructionIfMissing(PlannerResult result, FieldCell cell, Vector3Int direction, int instructionId)
        {
            for (int i = 0; i < result.CellsInstructions.Count; i++)
            {
                SpawnInstructionGuide existing = result.CellsInstructions[i];
                if (existing == null) continue;
                if (existing.Id == instructionId && existing.pos == cell.Pos) return;
            }

            SpawnInstructionGuide instruction = new SpawnInstructionGuide
            {
                pos = cell.Pos,
                HelperCellRef = cell,
                rot = Quaternion.LookRotation((Vector3)direction),
                WorldRot = false,
                Id = instructionId,
                UseDirection = true
            };

            result.CellsInstructions.Add(instruction);
        }

        private void RemoveInstructionsWithId(PlannerResult result, int instructionId)
        {
            if (result == null || result.CellsInstructions == null) return;

            for (int i = result.CellsInstructions.Count - 1; i >= 0; i--)
            {
                SpawnInstructionGuide instruction = result.CellsInstructions[i];
                if (instruction != null && instruction.Id == instructionId)
                    result.CellsInstructions.RemoveAt(i);
            }
        }
    }
}
