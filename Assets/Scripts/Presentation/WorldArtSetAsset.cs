using UnityEngine;

namespace ShadowGarden.Presentation
{
    [CreateAssetMenu(menuName = "Shadow Garden/Presentation/World Art Set", fileName = "WorldArtSet")]
    public sealed class WorldArtSetAsset : ScriptableObject
    {
        public int worldNumber = 1;
        public string worldName;

        [Header("Environment")]
        public Sprite background;
        public Sprite boardFrame;
        public Sprite boardVoid;
        public Sprite safeTile;
        public Sprite safeTileVariant;
        public Sprite safeTileFlora;
        public Sprite safeTileFeature;
        public Sprite cliffTile;
        public Sprite[] backDecor;
        public Sprite[] frontDecor;
        public Sprite environmentReaction;

        [Header("Goals")]
        public Sprite doorClosed;
        public Sprite doorOpen;
        public Sprite flowerClosed;
        public Sprite flowerBloom;

        [Header("Art direction")]
        public Color ambientTint = Color.white;
        public Color safeTint = Color.white;
        public Color shadowTint = new Color(0.08f, 0.14f, 0.25f, 1f);
        public Color reactionTint = Color.white;
        public AudioClip ambienceLoop;

        public Sprite PickSafeTile(int x, int y)
        {
            var selector = Mathf.Abs(x * 31 + y * 17) % 11;
            if (selector == 0 && safeTileFeature != null) return safeTileFeature;
            if (selector <= 2 && safeTileFlora != null) return safeTileFlora;
            if (selector <= 5 && safeTileVariant != null) return safeTileVariant;
            return safeTile;
        }
    }
}
