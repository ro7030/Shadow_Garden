using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public static class ShadowGridSolver
    {
        public static ShadowGridResult Calculate(
            StageDefinition stage,
            IReadOnlyDictionary<ChannelId, CardinalDirection> directions)
        {
            var counts = new int[stage.BoardSize.CellCount];
            foreach (var pillar in stage.Pillars)
            {
                if (!directions.TryGetValue(pillar.Channel, out var direction))
                {
                    continue;
                }

                var cursor = pillar.Position;
                for (var step = 0; step < pillar.ShadowLength; step++)
                {
                    cursor = cursor.Step(direction);
                    if (!stage.IsInBounds(cursor))
                    {
                        break;
                    }

                    if (stage.IsPillar(cursor))
                    {
                        break;
                    }

                    counts[stage.BoardSize.ToIndex(cursor)]++;
                }
            }

            var singles = new List<GridPosition>();
            var overlaps = new List<GridPosition>();
            for (var index = 0; index < counts.Length; index++)
            {
                var position = stage.BoardSize.FromIndex(index);
                if (stage.IsAlwaysSafe(position) || stage.IsPillar(position))
                {
                    continue;
                }

                var count = counts[index];
                if (count == 1)
                {
                    singles.Add(position);
                }
                else if (count >= 2)
                {
                    overlaps.Add(position);
                }
            }

            return new ShadowGridResult(stage.BoardSize, counts, singles, overlaps);
        }
    }
}
