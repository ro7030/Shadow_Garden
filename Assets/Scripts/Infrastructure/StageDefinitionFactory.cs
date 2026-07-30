using System.Collections.Generic;
using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Infrastructure
{
    public static class StageDefinitionFactory
    {
        public static StageDefinition CreateFromAsset(StageDefinitionAsset asset)
        {
            if (asset == null)
            {
                throw new System.ArgumentNullException(nameof(asset));
            }

            var definition = asset.ToDefinition();
            var issues = StageValidator.Validate(definition);
            if (issues.Count > 0)
            {
                Debug.LogError($"Stage '{asset.stageId}' validation failed: {string.Join(" | ", issues)}");
            }

            return definition;
        }

        public static void ApplyGraybox(StageDefinitionAsset asset, StageDefinition definition)
        {
            asset.stageId = definition.StageId;
            asset.playerStart = new GridPositionAuthoring
            {
                x = definition.PlayerStart.X,
                y = definition.PlayerStart.Y
            };
            asset.clearGoalType = definition.ClearGoalType;
            asset.goalPosition = new GridPositionAuthoring
            {
                x = definition.GoalPosition.X,
                y = definition.GoalPosition.Y
            };
            asset.timeLimitSeconds = definition.TimeLimitSeconds;

            asset.safeCells = new List<GridPositionAuthoring>();
            foreach (var cell in definition.SafeCells)
            {
                asset.safeCells.Add(new GridPositionAuthoring { x = cell.X, y = cell.Y });
            }

            asset.lamps = new List<LampAuthoring>();
            foreach (var lamp in definition.Lamps)
            {
                asset.lamps.Add(new LampAuthoring
                {
                    position = new GridPositionAuthoring { x = lamp.Position.X, y = lamp.Position.Y },
                    channel = lamp.Channel,
                    initialDirection = lamp.InitialDirection
                });
            }

            asset.pillars = new List<PillarAuthoring>();
            foreach (var pillar in definition.Pillars)
            {
                asset.pillars.Add(new PillarAuthoring
                {
                    position = new GridPositionAuthoring { x = pillar.Position.X, y = pillar.Position.Y },
                    channel = pillar.Channel,
                    height = pillar.Height
                });
            }
        }
    }
}
