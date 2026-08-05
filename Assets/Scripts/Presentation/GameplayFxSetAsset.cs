using UnityEngine;

namespace ShadowGarden.Presentation
{
    [CreateAssetMenu(menuName = "Shadow Garden/Presentation/Gameplay FX Set", fileName = "GameplayFxSet")]
    public sealed class GameplayFxSetAsset : ScriptableObject
    {
        public Sprite singleShadow;
        public Sprite overlapHazard;
        public Sprite cliffRim;
        public Sprite rotateSweep;
        public Sprite dangerPulse;
        public Sprite doorGlow;
        public Sprite flowerPetal;
        public Sprite fallDust;
        public Sprite vacuumSwirl;
        public Sprite completionGlow;
    }
}
