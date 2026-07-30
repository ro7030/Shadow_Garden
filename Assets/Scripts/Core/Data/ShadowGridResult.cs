using System;
using System.Collections.Generic;

namespace ShadowGarden.Core
{
    public sealed class ShadowGridResult
    {
        private readonly int[] _shadowCountByCell;
        private readonly GridSize _boardSize;
        private readonly List<GridPosition> _singleShadowCells;
        private readonly List<GridPosition> _overlapHazardCells;

        public IReadOnlyList<int> ShadowCountByCell => _shadowCountByCell;
        public IReadOnlyList<GridPosition> SingleShadowCells => _singleShadowCells;
        public IReadOnlyList<GridPosition> OverlapHazardCells => _overlapHazardCells;

        public ShadowGridResult(
            GridSize boardSize,
            int[] shadowCountByCell,
            List<GridPosition> singleShadowCells,
            List<GridPosition> overlapHazardCells)
        {
            _boardSize = boardSize;
            _shadowCountByCell = shadowCountByCell ?? throw new ArgumentNullException(nameof(shadowCountByCell));
            _singleShadowCells = singleShadowCells ?? throw new ArgumentNullException(nameof(singleShadowCells));
            _overlapHazardCells = overlapHazardCells ?? throw new ArgumentNullException(nameof(overlapHazardCells));
        }

        public int GetShadowCount(GridPosition position)
        {
            if (!_boardSize.Contains(position))
            {
                return 0;
            }

            return _shadowCountByCell[_boardSize.ToIndex(position)];
        }
    }
}
