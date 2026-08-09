using FIMSpace.Generating.Checker;
using FIMSpace.Graph;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating.Planning.PlannerNodes.Field.Special
{
    public class PR_QuickAlign : PlannerRuleBase
    {
        public override string GetDisplayName(float maxWidth = 120) { return wasCreated ? "Quick Align" : "Quick Align"; }
        public override string GetNodeTooltipDescription { get { return "Trying to find a valid placement on target field edge while avoiding collisions."; } }
        public override EPlannerNodeType NodeType { get { return EPlannerNodeType.WholeFieldPlacement; } }
        public override Vector2 NodeSize { get { return new Vector2(222, 145); } }
        public override bool IsFoldable { get { return false; } }
        public override Color GetNodeColor() { return new Color(0.1f, 0.7f, 1f, 0.95f); }

        [Port(EPortPinType.Input, 1)] public PGGPlannerPort ToPush;
        [Tooltip("If inputing multiple planners, random candidate will be checked first")]
        [Port(EPortPinType.Input)] public PGGPlannerPort AlignTo;
        [Space(3)]
        [Tooltip("If 'Collision With' left empty or -1 then colliding with every field in the current plan stage")]
        [Port(EPortPinType.Input)] public PGGPlannerPort CollisionWith;
        [Tooltip("Additional push-out in cell units. A positive value is also reserved as horizontal clearance around the whole field.")]
        [Port(EPortPinType.Input, 1)] public FloatPort ExtraPushOut;

        public override void Execute(PlanGenerationPrint print, PlannerResult newResult)
        {
            FieldPlanner planner = GetPlannerFromPort(ToPush);
            if (planner == null) return;
            if (planner.LatestChecker == null) return;

            planner.LatestChecker._IsCollidingWith_MyFirstCollisionCell = null;

            CheckerField3D checker = planner.LatestChecker;
            CheckerField3D checkerBackup = checker.Copy();
            Vector3 initialPosition = checkerBackup.RootPosition;
            Quaternion initialRotation = checkerBackup.RootRotation;

            List<FieldPlanner> alignCandidates = CollectAlignCandidates(planner);
            if (alignCandidates.Count == 0)
            {
                checker.RootPosition = initialPosition;
                checker.RootRotation = initialRotation;
                return;
            }

            List<CheckerField3D> collisionMasks = CollectCollisionMasks(planner);
            Bounds printBounds = print.GetFullBounds();
            float bestBoundsSize = float.MaxValue;
            int bestAligns = -1;
            Vector3 bestRootPos = initialPosition;
            Quaternion bestRootRot = initialRotation;
            bool found = false;

            float pushOutDistance = ExtraPushOut.GetInputValue;

            for (int a = 0; a < alignCandidates.Count; a++)
            {
                FieldPlanner alignPlanner = alignCandidates[a];
                if (alignPlanner == null) continue;

                CheckerField3D alignChecker = alignPlanner.LatestChecker;
                if (alignChecker == null) continue;
                if (alignChecker.AllCells.Count == 0) continue;

                CheckerField3D alignCheckerEdges = alignChecker.GetInlineChecker(false, true, true, false);
                if (alignCheckerEdges == null) continue;
                if (alignCheckerEdges.AllCells.Count == 0) continue;

                List<FieldCell> edgeCells = IGeneration.GetRandomizedCells(alignCheckerEdges.Grid);
                int rotations = ParentPlanner.AllowRotateBy90 ? 2 : 1;

                for (int i = 0; i < edgeCells.Count; i++)
                {
                    var dirs = edgeCells[i].GetAvailableOutDirs();
                    Shuffle(dirs);

                    for (int d = 0; d < dirs.Length; d++)
                    {
                        for (int r = 0; r < rotations; r++)
                        {
                            if (r == 0)
                            {
                                checker.RootPosition = initialPosition;
                                checker.RootRotation = initialRotation;
                            }
                            else
                            {
                                checker.RootPosition = checkerBackup.RootPosition;
                                checker.RootRotation = checkerBackup.RootRotation * Quaternion.Euler(0f, 90f, 0f);
                            }

                            Bounds predictedBounds = GetBoundsPlacement(checker, alignCheckerEdges, edgeCells[i], alignChecker, collisionMasks, dirs[d], true, pushOutDistance, printBounds);
                            if (predictedBounds.size.x == float.MaxValue) continue;

                            FieldCell contactMyCell = null;
                            FieldCell contactOtherCell = null;
                            if (!PR_TightPlacement.GetContactCells(checker, alignCheckerEdges, dirs[d], pushOutDistance, ref contactMyCell, ref contactOtherCell))
                                continue;

                            int aligns = checker.CountAlignmentsWith(alignCheckerEdges);
                            if (aligns <= 0) continue;

                            float newBndsSize = GetBoundsSize(predictedBounds);
                            if (!found || aligns > bestAligns || (aligns == bestAligns && newBndsSize < bestBoundsSize))
                            {
                                found = true;
                                bestAligns = aligns;
                                bestBoundsSize = newBndsSize;
                                bestRootPos = checker.RootPosition;
                                bestRootRot = checker.RootRotation;
                            }
                        }
                    }
                }
            }

            if (found)
            {
                checker.RootPosition = bestRootPos;
                checker.RootRotation = bestRootRot;
            }
            else
            {
                checker.RootPosition = initialPosition;
                checker.RootRotation = initialRotation;
            }
        }

        List<FieldPlanner> CollectAlignCandidates(FieldPlanner planner)
        {
            List<FieldPlanner> result = new List<FieldPlanner>();
            List<FieldPlanner> aligns = GetPlannersFromPort(AlignTo, true, true);

            for (int i = 0; i < aligns.Count; i++)
            {
                AppendPlannerCandidate(result, aligns[i], planner);

                if (aligns[i] == null) continue;
                var duplicates = aligns[i].GetDuplicatesPlannersList();
                if (duplicates == null) continue;

                for (int d = 0; d < duplicates.Count; d++)
                    AppendPlannerCandidate(result, duplicates[d], planner);
            }

            Shuffle(result);
            return result;
        }

        void AppendPlannerCandidate(List<FieldPlanner> result, FieldPlanner candidate, FieldPlanner planner)
        {
            if (candidate == null) return;
            if (candidate == planner) return;
            if (candidate.Available == false) return;
            if (result.Contains(candidate)) return;
            result.Add(candidate);
        }

        List<CheckerField3D> CollectCollisionMasks(FieldPlanner planner)
        {
            List<CheckerField3D> result = new List<CheckerField3D>();
            bool collideWithAll = false;

            if (CollisionWith.PortState() != EPortPinState.Connected)
                if (CollisionWith.UniquePlannerID < 0)
                    collideWithAll = true;

            if (collideWithAll)
            {
                var collisionCheckers = ParentPlanner.ParentBuildPlanner.CollectAllAvailablePlannersCheckerRefs(true, true, true, true);
                for (int i = 0; i < collisionCheckers.Count; i++)
                    AppendCheckerMask(result, collisionCheckers[i] == null ? null : collisionCheckers[i].CheckerReference, planner);
            }
            else
            {
                var collisionCheckers = CollisionWith.Get_GetMultipleCheckers;
                for (int i = 0; i < collisionCheckers.Count; i++)
                    AppendCheckerMask(result, collisionCheckers[i] == null ? null : collisionCheckers[i].CheckerReference, planner);
            }

            return result;
        }

        void AppendCheckerMask(List<CheckerField3D> result, CheckerField3D candidate, FieldPlanner planner)
        {
            if (candidate == null) return;
            if (candidate == planner.LatestChecker) return;
            if (result.Contains(candidate)) return;
            result.Add(candidate);
        }

        Bounds GetBoundsPlacement(CheckerField3D checker, CheckerField3D inline, FieldCell cell, CheckerField3D alignToChecker, List<CheckerField3D> fullMask, Vector3Int dir, bool breakOnAnyCollWithMask, float pushOutDistance, Bounds printBounds)
        {
            CheckerField3D.DebugHelper = false;

            checker.SetRootPositionInWorldPosCentered(inline.GetWorldPos(cell), false);

            if (checker.RootScale.x % 1 != 0)
                checker.RoundRootPositionToScale();
            else
                checker.RoundRootPositionAccordingly(inline);

            checker.RootPosition += checker.ScaleV3(dir);
            checker.StepPushOutOfCollision(alignToChecker, dir);

            if (pushOutDistance != 0f)
                checker.RootPosition += checker.ScaleV3(dir.V3IntToV3()) * pushOutDistance;

            if (breakOnAnyCollWithMask && checker.IsCollidingWith(fullMask))
                return new Bounds(Vector3.zero, Vector3.one * float.MaxValue);

            // ExtraPushOut was originally checked only in the alignment direction. For rooms
            // this could leave their side or corner directly touching another field. Reserve
            // the same world-space gap around the complete horizontal perimeter instead.
            float horizontalClearance = Mathf.Max(0f, pushOutDistance) * checker.ScaleV3(dir.V3IntToV3()).magnitude;
            if (breakOnAnyCollWithMask && horizontalClearance > 0f)
                if (!HasHorizontalClearance(checker, fullMask, horizontalClearance))
                    return new Bounds(Vector3.zero, Vector3.one * float.MaxValue);

            Bounds r = new Bounds(printBounds.center, printBounds.size);
            r.Encapsulate(checker.GetFullBoundsWorldSpace());
            return r;
        }

        bool HasHorizontalClearance(CheckerField3D checker, List<CheckerField3D> collisionMasks, float minimumClearance)
        {
            const float tolerance = 0.001f;
            float minimumGap = Mathf.Max(0f, minimumClearance - tolerance);
            Vector3 checkerCellSize = GetWorldCellSize(checker);
            Bounds checkerBounds = checker.GetFullBoundsWorldSpace();
            checkerBounds.Expand(new Vector3(minimumClearance * 2f, 0f, minimumClearance * 2f));

            for (int maskIndex = 0; maskIndex < collisionMasks.Count; maskIndex++)
            {
                CheckerField3D other = collisionMasks[maskIndex];
                if (other == null || other == checker || other.ChildPositionsCount == 0) continue;

                Bounds otherBounds = other.GetFullBoundsWorldSpace();
                if (!checkerBounds.Intersects(otherBounds)) continue;

                Vector3 otherCellSize = GetWorldCellSize(other);

                for (int myCellIndex = 0; myCellIndex < checker.ChildPositionsCount; myCellIndex++)
                {
                    Vector3 myPosition = checker.GetWorldPos(myCellIndex);

                    for (int otherCellIndex = 0; otherCellIndex < other.ChildPositionsCount; otherCellIndex++)
                    {
                        Vector3 otherPosition = other.GetWorldPos(otherCellIndex);

                        float halfHeight = (checkerCellSize.y + otherCellSize.y) * 0.5f;
                        if (Mathf.Abs(myPosition.y - otherPosition.y) >= halfHeight - tolerance) continue;

                        float gapX = Mathf.Abs(myPosition.x - otherPosition.x) - (checkerCellSize.x + otherCellSize.x) * 0.5f;
                        float gapZ = Mathf.Abs(myPosition.z - otherPosition.z) - (checkerCellSize.z + otherCellSize.z) * 0.5f;

                        // Both axes inside the expanded cell footprint means there is not
                        // enough room for the wall, including at room corners.
                        if (gapX < minimumGap && gapZ < minimumGap)
                            return false;
                    }
                }
            }

            return true;
        }

        Vector3 GetWorldCellSize(CheckerField3D checker)
        {
            Vector3 xAxis = checker.RootRotation * new Vector3(checker.RootScale.x, 0f, 0f);
            Vector3 yAxis = checker.RootRotation * new Vector3(0f, checker.RootScale.y, 0f);
            Vector3 zAxis = checker.RootRotation * new Vector3(0f, 0f, checker.RootScale.z);

            return new Vector3(
                Mathf.Abs(xAxis.x) + Mathf.Abs(yAxis.x) + Mathf.Abs(zAxis.x),
                Mathf.Abs(xAxis.y) + Mathf.Abs(yAxis.y) + Mathf.Abs(zAxis.y),
                Mathf.Abs(xAxis.z) + Mathf.Abs(yAxis.z) + Mathf.Abs(zAxis.z));
        }

        float GetBoundsSize(Bounds b)
        {
            return (b.max - b.min).magnitude;
        }

        void Shuffle(List<FieldPlanner> planners)
        {
            for (int i = planners.Count - 1; i > 0; i--)
            {
                int swapWith = FGenerators.GetRandom(0, i + 1);
                FieldPlanner temp = planners[i];
                planners[i] = planners[swapWith];
                planners[swapWith] = temp;
            }
        }

        void Shuffle(Vector3Int[] dirs)
        {
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int swapWith = FGenerators.GetRandom(0, i + 1);
                Vector3Int temp = dirs[i];
                dirs[i] = dirs[swapWith];
                dirs[swapWith] = temp;
            }
        }


        #region Editor Related

#if UNITY_EDITOR
        public override void Editor_OnNodeBodyGUI(ScriptableObject setup)
        {
            CollisionWith.Editor_DefaultValueInfo = "(All)";
            base.Editor_OnNodeBodyGUI(setup);
        }
#endif

        #endregion
    }
}
