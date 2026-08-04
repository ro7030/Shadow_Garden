using System;
using System.Collections.Generic;
using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Infrastructure
{
    [Serializable]
    public struct GridPositionAuthoring
    {
        public int x;
        public int y;

        public GridPosition ToCore() => new GridPosition(x, y);
    }

    [Serializable]
    public class LampAuthoring
    {
        public GridPositionAuthoring position;
        public ChannelId channel = ChannelId.Circle;
        public CardinalDirection initialDirection = CardinalDirection.East;
    }

    [Serializable]
    public class PillarAuthoring
    {
        public GridPositionAuthoring position;
        public ChannelId channel = ChannelId.Circle;
        public PillarHeight height = PillarHeight.Medium;
    }

    [Serializable]
    public class RecordedRotateAuthoring
    {
        public ChannelId channel = ChannelId.Circle;
        public int quarterTurnsClockwise = 1;
    }

    [CreateAssetMenu(menuName = "ShadowGarden/Stage Definition", fileName = "Stage_")]
    public sealed class StageDefinitionAsset : ScriptableObject
    {
        public string stageId = "1-1";
        public int boardWidth = 12;
        public int boardHeight = 6;
        public GridPositionAuthoring playerStart;
        public List<GridPositionAuthoring> safeCells = new List<GridPositionAuthoring>();
        public List<LampAuthoring> lamps = new List<LampAuthoring>();
        public List<PillarAuthoring> pillars = new List<PillarAuthoring>();
        public ClearGoalType clearGoalType = ClearGoalType.ExitDoor;
        public GridPositionAuthoring goalPosition;
        public int timeLimitSeconds = 120;

        [Header("Recorded primary solution (level design)")]
        public List<GridPositionAuthoring> recordedSolutionPath = new List<GridPositionAuthoring>();
        public List<RecordedRotateAuthoring> recordedRotates = new List<RecordedRotateAuthoring>();
        public int documentedMinRotates;

        public StageDefinition ToDefinition()
        {
            var safe = new GridPosition[safeCells.Count];
            for (var i = 0; i < safeCells.Count; i++)
            {
                safe[i] = safeCells[i].ToCore();
            }

            var lampDefs = new LampDefinition[lamps.Count];
            for (var i = 0; i < lamps.Count; i++)
            {
                var lamp = lamps[i];
                lampDefs[i] = new LampDefinition(lamp.position.ToCore(), lamp.channel, lamp.initialDirection);
            }

            var pillarDefs = new PillarDefinition[pillars.Count];
            for (var i = 0; i < pillars.Count; i++)
            {
                var pillar = pillars[i];
                pillarDefs[i] = new PillarDefinition(pillar.position.ToCore(), pillar.channel, pillar.height);
            }

            return new StageDefinition(
                stageId,
                new GridSize(boardWidth, boardHeight),
                playerStart.ToCore(),
                safe,
                lampDefs,
                pillarDefs,
                clearGoalType,
                goalPosition.ToCore(),
                timeLimitSeconds);
        }

        public RecordedSolution ToRecordedSolution()
        {
            var path = new GridPosition[recordedSolutionPath.Count];
            for (var i = 0; i < recordedSolutionPath.Count; i++)
            {
                path[i] = recordedSolutionPath[i].ToCore();
            }

            var rotates = new RecordedRotate[recordedRotates.Count];
            for (var i = 0; i < recordedRotates.Count; i++)
            {
                var r = recordedRotates[i];
                rotates[i] = new RecordedRotate(r.channel, r.quarterTurnsClockwise);
            }

            return new RecordedSolution(path, rotates, documentedMinRotates, Array.Empty<RecordedSolution>());
        }

        private void OnValidate()
        {
            if (clearGoalType == ClearGoalType.ExitDoor)
            {
                timeLimitSeconds = 120;
            }
            else
            {
                timeLimitSeconds = 150;
            }
        }
    }
}
