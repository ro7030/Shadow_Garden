using System;
using ShadowGarden.Core;

namespace ShadowGarden.Runtime
{
    public sealed class StageSession
    {
        public Guid SessionId { get; private set; } = Guid.NewGuid();
        public StageDefinition Definition { get; }
        public StageRuntimeState State { get; private set; }
        public ShadowGridResult Shadows { get; private set; }

        public event Action<StageCommandResult> CommandApplied;

        public StageSession(StageDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            var started = StageCommands.Start(definition);
            State = started.NextState;
            Shadows = started.Shadows;
        }

        public StageCommandResult Apply(StageCommandResult result)
        {
            State = result.NextState;
            Shadows = result.Shadows;
            CommandApplied?.Invoke(result);
            return result;
        }

        public StageCommandResult Move(CardinalDirection direction) =>
            Apply(StageCommands.TryMove(Definition, State, direction));

        public StageCommandResult Rotate(int quarterTurnsClockwise) =>
            Apply(StageCommands.TryRotate(Definition, State, quarterTurnsClockwise));

        public StageCommandResult Tick(long deltaMilliseconds) =>
            Apply(StageCommands.TickTimer(Definition, State, deltaMilliseconds));

        public StageCommandResult SetFocus(bool hasFocus) =>
            Apply(StageCommands.SetFocus(Definition, State, hasFocus));

        public StageCommandResult Restart()
        {
            SessionId = Guid.NewGuid();
            return Apply(StageCommands.Restart(Definition));
        }
    }
}
