using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    [CreateAssetMenu(menuName = "Shadow Garden/Presentation/Moa Animation Set", fileName = "MoaAnimationSet")]
    public sealed class MoaAnimationSetAsset : ScriptableObject
    {
        [Header("Six gameplay frames")]
        public Sprite frontA;
        public Sprite frontB;
        public Sprite backA;
        public Sprite backB;
        public Sprite sideA;
        public Sprite sideB;

        [Header("Portrait expressions")]
        public Sprite neutral;
        public Sprite curious;
        public Sprite surprised;
        public Sprite worried;
        public Sprite determined;
        public Sprite relieved;

        [Header("Story poses")]
        public Sprite holdSeed;
        public Sprite adjustCloak;
        public Sprite observe;
        public Sprite rotateLamp;
        public Sprite stepForward;
        public Sprite celebrateQuietly;

        public Sprite GetMoveFrame(CardinalDirection direction, bool alternate)
        {
            return direction switch
            {
                CardinalDirection.North => alternate ? backB : backA,
                CardinalDirection.South => alternate ? frontB : frontA,
                CardinalDirection.East or CardinalDirection.West => alternate ? sideB : sideA,
                _ => frontA
            };
        }
    }
}
