using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public sealed class ValidationIssue
    {
        public string Code { get; }
        public string Message { get; }

        public ValidationIssue(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public override string ToString() => $"{Code}: {Message}";
    }

    public static class StageValidator
    {
        public static IReadOnlyList<ValidationIssue> Validate(StageDefinition stage)
        {
            var issues = new List<ValidationIssue>();
            if (stage == null)
            {
                issues.Add(new ValidationIssue("null", "StageDefinition is null."));
                return issues;
            }

            if (string.IsNullOrWhiteSpace(stage.StageId))
            {
                issues.Add(new ValidationIssue("stageId", "StageId is required."));
            }

            if (stage.BoardSize.Width != 12 || stage.BoardSize.Height != 6)
            {
                issues.Add(new ValidationIssue("boardSize", "Board size must be 12x6."));
            }

            if (!stage.IsInBounds(stage.PlayerStart) || stage.IsPillar(stage.PlayerStart))
            {
                issues.Add(new ValidationIssue("playerStart", "Player start must be in-bounds and not a pillar."));
            }

            if (!stage.IsInBounds(stage.GoalPosition) || stage.IsPillar(stage.GoalPosition))
            {
                issues.Add(new ValidationIssue("goal", "Goal must be in-bounds and not a pillar."));
            }

            var expectedTime = stage.ClearGoalType == ClearGoalType.ExitDoor ? 120 : 150;
            if (stage.TimeLimitSeconds != expectedTime)
            {
                issues.Add(new ValidationIssue(
                    "timeLimit",
                    $"Time limit must be {expectedTime} for {stage.ClearGoalType}."));
            }

            if (stage.Lamps.Count > 4)
            {
                issues.Add(new ValidationIssue("lamps", "Maximum 4 lamps allowed."));
            }

            if (stage.Pillars.Count > 10)
            {
                issues.Add(new ValidationIssue("pillars", "Maximum 10 pillars allowed."));
            }

            var lampChannels = new HashSet<ChannelId>();
            var lampPositions = new HashSet<GridPosition>();
            foreach (var lamp in stage.Lamps)
            {
                if (!stage.IsInBounds(lamp.Position))
                {
                    issues.Add(new ValidationIssue("lampBounds", $"Lamp {lamp.Channel} is out of bounds."));
                }

                if (!lampChannels.Add(lamp.Channel))
                {
                    issues.Add(new ValidationIssue("lampChannel", $"Duplicate lamp channel {lamp.Channel}."));
                }

                if (!lampPositions.Add(lamp.Position))
                {
                    issues.Add(new ValidationIssue("lampPosition", $"Duplicate lamp position {lamp.Position}."));
                }
            }

            var pillarPositions = new HashSet<GridPosition>();
            var pillarChannels = new HashSet<ChannelId>();
            foreach (var pillar in stage.Pillars)
            {
                if (!stage.IsInBounds(pillar.Position))
                {
                    issues.Add(new ValidationIssue("pillarBounds", $"Pillar at {pillar.Position} is out of bounds."));
                }

                if (!pillarPositions.Add(pillar.Position))
                {
                    issues.Add(new ValidationIssue("pillarPosition", $"Duplicate pillar position {pillar.Position}."));
                }

                pillarChannels.Add(pillar.Channel);
                if (!lampChannels.Contains(pillar.Channel))
                {
                    issues.Add(new ValidationIssue(
                        "pillarChannel",
                        $"Pillar channel {pillar.Channel} has no lamp."));
                }
            }

            if (pillarPositions.Contains(stage.GoalPosition))
            {
                issues.Add(new ValidationIssue("goalPillar", "Goal overlaps a pillar."));
            }

            var safeSeen = new HashSet<GridPosition>();
            foreach (var cell in stage.SafeCells)
            {
                if (!stage.IsInBounds(cell))
                {
                    issues.Add(new ValidationIssue("safeBounds", $"Safe cell {cell} is out of bounds."));
                }

                if (!safeSeen.Add(cell))
                {
                    issues.Add(new ValidationIssue("safeDuplicate", $"Duplicate safe cell {cell}."));
                }
            }

            return issues;
        }

        public static bool IsValid(StageDefinition stage) => Validate(stage).Count == 0;
    }
}
