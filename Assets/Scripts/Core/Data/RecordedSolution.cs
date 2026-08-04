using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public readonly struct RecordedRotate
    {
        public ChannelId Channel { get; }
        public int QuarterTurnsClockwise { get; }

        public RecordedRotate(ChannelId channel, int quarterTurnsClockwise)
        {
            Channel = channel;
            QuarterTurnsClockwise = quarterTurnsClockwise;
        }
    }

    /// <summary>
    /// Documented primary solution: path cells (duplicate cell = rotate in place) + rotate sequence.
    /// </summary>
    public sealed class RecordedSolution
    {
        public IReadOnlyList<GridPosition> PathCells { get; }
        public IReadOnlyList<RecordedRotate> Rotates { get; }
        public int DocumentedMinRotates { get; }
        public IReadOnlyList<RecordedSolution> AllowedVariants { get; }

        public RecordedSolution(
            IReadOnlyList<GridPosition> pathCells,
            IReadOnlyList<RecordedRotate> rotates,
            int documentedMinRotates,
            IReadOnlyList<RecordedSolution> allowedVariants)
        {
            PathCells = pathCells;
            Rotates = rotates;
            DocumentedMinRotates = documentedMinRotates;
            AllowedVariants = allowedVariants ?? System.Array.Empty<RecordedSolution>();
        }
    }
}
