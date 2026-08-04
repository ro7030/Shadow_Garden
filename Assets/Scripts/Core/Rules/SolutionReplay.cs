using System.Collections.Generic;
using System.Text;

namespace ShadowGarden.Core
{
    public sealed class SolutionReplayFailure
    {
        public string StageId { get; }
        public string Reason { get; }
        public GridPosition? Position { get; }
        public string CommandTrace { get; }

        public SolutionReplayFailure(string stageId, string reason, GridPosition? position, string commandTrace)
        {
            StageId = stageId;
            Reason = reason;
            Position = position;
            CommandTrace = commandTrace;
        }

        public override string ToString()
        {
            var pos = Position.HasValue ? Position.Value.ToString() : "-";
            return $"[{StageId}] {Reason} at {pos} | cmds={CommandTrace}";
        }
    }

    public static class SolutionReplay
    {
        public static bool TryReplay(
            StageDefinition stage,
            RecordedSolution solution,
            out SolutionReplayFailure failure)
        {
            failure = null;
            if (stage == null || solution == null || solution.PathCells == null || solution.PathCells.Count == 0)
            {
                failure = new SolutionReplayFailure(stage?.StageId ?? "?", "Missing solution path.", null, "");
                return false;
            }

            var trace = new StringBuilder();
            var state = StageCommands.Start(stage).NextState;
            if (state.PlayerPosition != solution.PathCells[0])
            {
                failure = new SolutionReplayFailure(
                    stage.StageId,
                    $"Path start {solution.PathCells[0]} != player start {state.PlayerPosition}.",
                    state.PlayerPosition,
                    trace.ToString());
                return false;
            }

            var rotateIndex = 0;
            for (var i = 1; i < solution.PathCells.Count; i++)
            {
                var from = solution.PathCells[i - 1];
                var to = solution.PathCells[i];
                if (from == to)
                {
                    if (rotateIndex >= solution.Rotates.Count)
                    {
                        failure = new SolutionReplayFailure(
                            stage.StageId,
                            "Path stay without remaining rotate.",
                            from,
                            trace.ToString());
                        return false;
                    }

                    var rot = solution.Rotates[rotateIndex++];
                    if (!stage.TryGetLampAt(state.PlayerPosition, out var lamp) || lamp.Channel != rot.Channel)
                    {
                        failure = new SolutionReplayFailure(
                            stage.StageId,
                            $"Rotate expected channel {rot.Channel} at lamp, standing={state.PlayerPosition}.",
                            state.PlayerPosition,
                            trace.ToString());
                        return false;
                    }

                    trace.Append($"R({rot.Channel}:{rot.QuarterTurnsClockwise}) ");
                    var rotated = StageCommands.TryRotate(stage, state, rot.QuarterTurnsClockwise);
                    if (rotated.Events.Length == 0 ||
                        !System.Array.Exists(rotated.Events, e => e.Type == StageEventType.LampRotated))
                    {
                        failure = new SolutionReplayFailure(
                            stage.StageId,
                            "Rotate command failed.",
                            state.PlayerPosition,
                            trace.ToString());
                        return false;
                    }

                    state = rotated.NextState;
                    continue;
                }

                if (!TryDirection(from, to, out var dir))
                {
                    failure = new SolutionReplayFailure(
                        stage.StageId,
                        $"Non-adjacent path step {from}->{to}.",
                        from,
                        trace.ToString());
                    return false;
                }

                if (state.PlayerPosition != from)
                {
                    failure = new SolutionReplayFailure(
                        stage.StageId,
                        $"Desync before move: state={state.PlayerPosition} expected={from}.",
                        state.PlayerPosition,
                        trace.ToString());
                    return false;
                }

                trace.Append($"M({dir}) ");
                var moved = StageCommands.TryMove(stage, state, dir);
                if (!moved.Move.HasValue)
                {
                    failure = new SolutionReplayFailure(
                        stage.StageId,
                        "Move returned no resolution.",
                        state.PlayerPosition,
                        trace.ToString());
                    return false;
                }

                var outcome = moved.Move.Value.Outcome;
                if (outcome != MoveOutcome.Moved &&
                    outcome != MoveOutcome.ExitReached &&
                    outcome != MoveOutcome.NightFlowerReached)
                {
                    failure = new SolutionReplayFailure(
                        stage.StageId,
                        $"Unsafe/blocked move outcome {outcome} toward {to}.",
                        to,
                        trace.ToString());
                    return false;
                }

                state = moved.NextState;
                if (state.PlayerPosition != to &&
                    outcome != MoveOutcome.ExitReached &&
                    outcome != MoveOutcome.NightFlowerReached)
                {
                    failure = new SolutionReplayFailure(
                        stage.StageId,
                        $"Move landed on {state.PlayerPosition}, expected {to}.",
                        state.PlayerPosition,
                        trace.ToString());
                    return false;
                }
            }

            if (rotateIndex != solution.Rotates.Count)
            {
                failure = new SolutionReplayFailure(
                    stage.StageId,
                    $"Unused rotates remain ({solution.Rotates.Count - rotateIndex}).",
                    state.PlayerPosition,
                    trace.ToString());
                return false;
            }

            if (state.Phase != StagePhase.ResolvingClear)
            {
                failure = new SolutionReplayFailure(
                    stage.StageId,
                    $"Path finished without clear (phase={state.Phase}).",
                    state.PlayerPosition,
                    trace.ToString());
                return false;
            }

            return true;
        }

        private static bool TryDirection(GridPosition from, GridPosition to, out CardinalDirection direction)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            if (dx == 1 && dy == 0)
            {
                direction = CardinalDirection.East;
                return true;
            }

            if (dx == -1 && dy == 0)
            {
                direction = CardinalDirection.West;
                return true;
            }

            if (dx == 0 && dy == 1)
            {
                direction = CardinalDirection.South;
                return true;
            }

            if (dx == 0 && dy == -1)
            {
                direction = CardinalDirection.North;
                return true;
            }

            direction = default;
            return false;
        }
    }
}
