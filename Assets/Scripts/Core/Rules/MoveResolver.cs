using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public static class CellClassifier
    {
        public static CellKind Classify(StageDefinition stage, ShadowGridResult shadows, GridPosition position)
        {
            if (!stage.IsInBounds(position) || stage.IsPillar(position))
            {
                return CellKind.Blocked;
            }

            if (stage.IsAlwaysSafe(position))
            {
                return CellKind.Safe;
            }

            var count = shadows.GetShadowCount(position);
            if (count >= 2)
            {
                return CellKind.OverlapHazard;
            }

            if (count == 1)
            {
                return CellKind.SingleShadow;
            }

            return CellKind.Cliff;
        }
    }

    public static class MoveResolver
    {
        public static MoveResolution ResolveMove(
            StageDefinition stage,
            StageRuntimeState state,
            ShadowGridResult shadows,
            CardinalDirection direction)
        {
            var target = state.PlayerPosition.Step(direction);

            if (!stage.IsInBounds(target) || stage.IsPillar(target))
            {
                return MoveResolution.Blocked(state.PlayerPosition);
            }

            if (stage.IsGoal(target))
            {
                return stage.ClearGoalType == ClearGoalType.ExitDoor
                    ? MoveResolution.ExitReached(target)
                    : MoveResolution.NightFlowerReached(target);
            }

            if (stage.IsAlwaysSafe(target))
            {
                return MoveResolution.Moved(target);
            }

            var count = shadows.GetShadowCount(target);
            if (count >= 2)
            {
                return MoveResolution.OverlapDeath(target);
            }

            if (count == 1)
            {
                return MoveResolution.Moved(target);
            }

            return MoveResolution.CliffDeath(target);
        }
    }

    public static class StageCommands
    {
        public static ShadowGridResult CurrentShadows(StageDefinition stage, StageRuntimeState state) =>
            ShadowGridSolver.Calculate(stage, state.DirectionByChannel);

        public static StageCommandResult Start(StageDefinition stage)
        {
            var state = stage.CreateInitialRuntimeState().WithPhase(StagePhase.Playing);
            var shadows = CurrentShadows(stage, state);
            return new StageCommandResult(
                state,
                shadows,
                null,
                new[]
                {
                    new StageEvent(StageEventType.StageStarted, remainingMilliseconds: state.RemainingMilliseconds),
                    new StageEvent(StageEventType.ShadowGridChanged),
                    new StageEvent(StageEventType.TimerChanged, remainingMilliseconds: state.RemainingMilliseconds)
                });
        }

        public static StageCommandResult Restart(StageDefinition stage) => Start(stage);

        public static StageCommandResult TryMove(
            StageDefinition stage,
            StageRuntimeState state,
            CardinalDirection direction)
        {
            if (state.Phase != StagePhase.Playing && state.Phase != StagePhase.ResolvingAction)
            {
                return NoOp(stage, state);
            }

            var shadows = CurrentShadows(stage, state);
            var move = MoveResolver.ResolveMove(stage, state, shadows, direction);
            var events = new List<StageEvent>();

            switch (move.Outcome)
            {
                case MoveOutcome.Blocked:
                    events.Add(new StageEvent(StageEventType.MoveBlocked, position: state.PlayerPosition));
                    return new StageCommandResult(state.WithPhase(StagePhase.Playing), shadows, move, events.ToArray());

                case MoveOutcome.Moved:
                {
                    var next = state.WithPlayer(move.TargetPosition, StagePhase.Playing);
                    events.Add(new StageEvent(StageEventType.PlayerMoved, position: move.TargetPosition));
                    return new StageCommandResult(next, shadows, move, events.ToArray());
                }

                case MoveOutcome.OverlapDeath:
                case MoveOutcome.CliffDeath:
                {
                    var stopped = StageTimer.Stop(state.WithPlayer(move.TargetPosition, StagePhase.ResolvingDeath));
                    events.Add(new StageEvent(StageEventType.PlayerMoved, position: move.TargetPosition));
                    events.Add(new StageEvent(
                        StageEventType.GameOverStarted,
                        gameOverCause: move.DeathCause));
                    return new StageCommandResult(stopped, shadows, move, events.ToArray());
                }

                case MoveOutcome.ExitReached:
                case MoveOutcome.NightFlowerReached:
                {
                    var cleared = StageTimer.Stop(state.WithPlayer(move.TargetPosition, StagePhase.ResolvingClear));
                    events.Add(new StageEvent(StageEventType.PlayerMoved, position: move.TargetPosition));
                    events.Add(new StageEvent(
                        StageEventType.ClearStarted,
                        position: move.TargetPosition,
                        clearGoalType: stage.ClearGoalType));
                    events.Add(new StageEvent(
                        StageEventType.Cleared,
                        position: move.TargetPosition,
                        clearGoalType: stage.ClearGoalType,
                        remainingMilliseconds: cleared.RemainingMilliseconds));
                    return new StageCommandResult(cleared, shadows, move, events.ToArray());
                }

                default:
                    return NoOp(stage, state);
            }
        }

        public static StageCommandResult TryRotate(
            StageDefinition stage,
            StageRuntimeState state,
            int quarterTurnsClockwise)
        {
            if (state.Phase != StagePhase.Playing && state.Phase != StagePhase.ResolvingAction)
            {
                return NoOp(stage, state);
            }

            if (!stage.TryGetLampAt(state.PlayerPosition, out var lamp))
            {
                return NoOp(stage, state);
            }

            var nextDirection = DirectionUtility.Rotate(state.GetDirection(lamp.Channel), quarterTurnsClockwise);
            var nextState = state.WithDirection(lamp.Channel, nextDirection, StagePhase.Playing);
            var shadows = CurrentShadows(stage, nextState);
            return new StageCommandResult(
                nextState,
                shadows,
                null,
                new[]
                {
                    new StageEvent(
                        StageEventType.LampRotated,
                        channel: lamp.Channel,
                        direction: nextDirection,
                        position: lamp.Position),
                    new StageEvent(StageEventType.ShadowGridChanged),
                    new StageEvent(StageEventType.ActionReady)
                });
        }

        public static StageCommandResult TickTimer(
            StageDefinition stage,
            StageRuntimeState state,
            long deltaMilliseconds)
        {
            var tick = StageTimer.Tick(state, deltaMilliseconds);
            if (ReferenceEquals(tick.NextState, state) && !tick.FiredWarning30 && !tick.FiredWarning10 && !tick.Expired)
            {
                return NoOp(stage, state);
            }

            var shadows = CurrentShadows(stage, tick.NextState);
            var events = new List<StageEvent>
            {
                new StageEvent(StageEventType.TimerChanged, remainingMilliseconds: tick.NextState.RemainingMilliseconds)
            };

            if (tick.FiredWarning30)
            {
                events.Add(new StageEvent(
                    StageEventType.TimerWarning30,
                    remainingMilliseconds: tick.NextState.RemainingMilliseconds));
            }

            if (tick.FiredWarning10)
            {
                events.Add(new StageEvent(
                    StageEventType.TimerWarning10,
                    remainingMilliseconds: tick.NextState.RemainingMilliseconds));
            }

            if (tick.Expired)
            {
                var stopped = StageTimer.Stop(tick.NextState);
                events.Add(new StageEvent(StageEventType.TimeExpired));
                events.Add(new StageEvent(
                    StageEventType.GameOverStarted,
                    gameOverCause: GameOverCause.TimeExpired));
                return new StageCommandResult(stopped, shadows, null, events.ToArray());
            }

            return new StageCommandResult(tick.NextState, shadows, null, events.ToArray());
        }

        public static StageCommandResult SetFocus(
            StageDefinition stage,
            StageRuntimeState state,
            bool hasFocus)
        {
            if (state.Phase != StagePhase.Playing && state.Phase != StagePhase.ResolvingAction)
            {
                return NoOp(stage, state);
            }

            var next = hasFocus
                ? StageTimer.Resume(state)
                : StageTimer.Pause(state, TimerPauseReason.FocusLost);

            return new StageCommandResult(
                next,
                CurrentShadows(stage, next),
                null,
                new[]
                {
                    new StageEvent(StageEventType.TimerChanged, remainingMilliseconds: next.RemainingMilliseconds)
                });
        }

        private static StageCommandResult NoOp(StageDefinition stage, StageRuntimeState state) =>
            new StageCommandResult(state, CurrentShadows(stage, state), null, System.Array.Empty<StageEvent>());
    }

    public static class SafetyPathFinder
    {
        private readonly struct Key
        {
            public readonly int PlayerIndex;
            public readonly int DirectionMask;

            public Key(int playerIndex, int directionMask)
            {
                PlayerIndex = playerIndex;
                DirectionMask = directionMask;
            }
        }

        public static int MaxStatesFor(StageDefinition stage)
        {
            var lampCount = stage.Lamps.Count;
            var limit = stage.BoardSize.CellCount;
            for (var i = 0; i < lampCount; i++)
            {
                limit *= 4;
            }

            const int hardCap = 36864;
            return limit > hardCap ? hardCap : limit;
        }

        public static bool HasSafeSolution(StageDefinition stage, int maxStates = -1)
        {
            if (maxStates < 0)
            {
                maxStates = MaxStatesFor(stage);
            }

            var initial = stage.CreateInitialRuntimeState();
            var queue = new Queue<StageRuntimeState>();
            var visited = new HashSet<Key>();
            queue.Enqueue(initial);
            visited.Add(MakeKey(stage, initial));

            var dirs = new[]
            {
                CardinalDirection.North,
                CardinalDirection.East,
                CardinalDirection.South,
                CardinalDirection.West
            };

            var explored = 0;
            while (queue.Count > 0 && explored < maxStates)
            {
                explored++;
                var state = queue.Dequeue();
                var shadows = StageCommands.CurrentShadows(stage, state);

                foreach (var direction in dirs)
                {
                    var move = MoveResolver.ResolveMove(stage, state, shadows, direction);
                    if (move.Outcome == MoveOutcome.ExitReached ||
                        move.Outcome == MoveOutcome.NightFlowerReached)
                    {
                        return true;
                    }

                    if (move.Outcome != MoveOutcome.Moved)
                    {
                        continue;
                    }

                    var next = state.WithPlayer(move.TargetPosition, StagePhase.Playing);
                    var key = MakeKey(stage, next);
                    if (visited.Add(key))
                    {
                        queue.Enqueue(next);
                    }
                }

                if (stage.TryGetLampAt(state.PlayerPosition, out var lamp))
                {
                    foreach (var turn in new[] { -1, 1 })
                    {
                        var rotated = DirectionUtility.Rotate(state.GetDirection(lamp.Channel), turn);
                        var next = state.WithDirection(lamp.Channel, rotated, StagePhase.Playing);
                        var key = MakeKey(stage, next);
                        if (visited.Add(key))
                        {
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return false;
        }

        private static Key MakeKey(StageDefinition stage, StageRuntimeState state)
        {
            var mask = 0;
            foreach (var pair in state.DirectionByChannel)
            {
                mask |= ((int)pair.Value & 0x3) << ((int)pair.Key * 2);
            }

            return new Key(stage.BoardSize.ToIndex(state.PlayerPosition), mask);
        }
    }
}
