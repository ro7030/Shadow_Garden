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
