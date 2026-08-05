using UnityEngine;

namespace ShadowGarden.Presentation
{
    [CreateAssetMenu(menuName = "Shadow Garden/Presentation/In-Game Asset Catalog", fileName = "InGameAssetCatalog")]
    public sealed class InGameAssetCatalogAsset : ScriptableObject
    {
        [Header("Primary sets")]
        public MoaAnimationSetAsset moa;
        public GameplayFxSetAsset gameplayFx;
        public AudioSetAsset audio;

        [Header("Common gameplay")]
        public Sprite lampBody;
        public Sprite lampArrow;
        public Sprite pillarLow;
        public Sprite pillarMedium;
        public Sprite pillarHigh;
        public Sprite channelCircle;
        public Sprite channelTriangle;
        public Sprite channelStar;
        public Sprite channelDiamond;

        [Header("UI skin")]
        public Sprite panel;
        public Sprite panelLight;
        public Sprite buttonPrimary;
        public Sprite buttonSecondary;
        public Sprite buttonFocus;
        public Sprite worldCardFrame;
        public Sprite keyCap;
        public Sprite iconPause;
        public Sprite iconDoor;
        public Sprite iconFlower;
        public Sprite iconRetry;
        public Sprite iconWorldMap;
        public Sprite iconLock;
        public Sprite iconCheck;
        public Sprite iconDanger;

        public Sprite GetChannelIcon(ShadowGarden.Core.ChannelId channel) => channel switch
        {
            ShadowGarden.Core.ChannelId.Circle => channelCircle,
            ShadowGarden.Core.ChannelId.Triangle => channelTriangle,
            ShadowGarden.Core.ChannelId.Star => channelStar,
            ShadowGarden.Core.ChannelId.Diamond => channelDiamond,
            _ => channelCircle
        };

        public Sprite GetPillar(ShadowGarden.Core.PillarHeight height) => height switch
        {
            ShadowGarden.Core.PillarHeight.Low => pillarLow,
            ShadowGarden.Core.PillarHeight.Medium => pillarMedium,
            ShadowGarden.Core.PillarHeight.High => pillarHigh,
            _ => pillarMedium
        };
    }
}
