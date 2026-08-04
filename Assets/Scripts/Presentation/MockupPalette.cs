using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// TestField "시험의 정원" mockup palette — cream sky, navy shadows, abyss void.
    /// </summary>
    public static class MockupPalette
    {
        public static readonly Color SoftSky = new Color(0.93f, 0.90f, 0.84f, 1f);
        public static readonly Color SoftSkyDeep = new Color(0.86f, 0.84f, 0.80f, 1f);
        public static readonly Color BoardFrame = new Color(0.42f, 0.38f, 0.34f, 0.92f);
        public static readonly Color BoardVoid = new Color(0.08f, 0.09f, 0.16f, 1f);
        public static readonly Color SafeTerrain = new Color(0.94f, 0.90f, 0.82f, 1f);
        public static readonly Color SingleShadow = new Color(0.12f, 0.18f, 0.42f, 0.98f);
        public static readonly Color OverlapHazard = new Color(0.48f, 0.12f, 0.34f, 1f);
        public static readonly Color OverlapCoral = new Color(0.95f, 0.52f, 0.46f, 1f);
        public static readonly Color Cliff = new Color(0.04f, 0.05f, 0.10f, 1f);
        public static readonly Color CliffRim = new Color(0.16f, 0.18f, 0.32f, 0.85f);
        public static readonly Color LampGold = new Color(0.88f, 0.68f, 0.28f, 1f);
        public static readonly Color LampBrass = new Color(0.62f, 0.48f, 0.22f, 1f);
        public static readonly Color PillarStone = new Color(0.48f, 0.46f, 0.42f, 1f);
        public static readonly Color ExitCyan = new Color(0.42f, 0.74f, 0.88f, 1f);
        public static readonly Color NightFlower = new Color(0.72f, 0.48f, 0.92f, 1f);
        public static readonly Color PlayerCloak = new Color(0.10f, 0.16f, 0.40f, 1f);
        public static readonly Color PlayerHood = new Color(0.08f, 0.12f, 0.32f, 1f);
        public static readonly Color HudNavy = new Color(0.14f, 0.20f, 0.38f, 0.92f);
        public static readonly Color WarningAmber = new Color(0.85f, 0.62f, 0.22f, 1f);
        public static readonly Color WarningCoral = new Color(0.95f, 0.55f, 0.50f, 1f);

        public static Color ChannelColor(ChannelId channel) => channel switch
        {
            ChannelId.Circle => new Color(0.35f, 0.65f, 0.95f, 1f),
            ChannelId.Triangle => new Color(0.95f, 0.62f, 0.25f, 1f),
            ChannelId.Star => new Color(0.72f, 0.45f, 0.95f, 1f),
            ChannelId.Diamond => new Color(0.95f, 0.35f, 0.42f, 1f),
            _ => Color.white
        };

        public static string ChannelLabel(ChannelId channel) => channel switch
        {
            ChannelId.Circle => "원",
            ChannelId.Triangle => "삼각형",
            ChannelId.Star => "별",
            ChannelId.Diamond => "마름모",
            _ => "?"
        };

        public static string ChannelGlyph(ChannelId channel) => channel switch
        {
            ChannelId.Circle => "●",
            ChannelId.Triangle => "▲",
            ChannelId.Star => "★",
            ChannelId.Diamond => "◆",
            _ => "?"
        };

        public static string DirectionArrow(CardinalDirection direction) => direction switch
        {
            CardinalDirection.North => "↑",
            CardinalDirection.East => "→",
            CardinalDirection.South => "↓",
            CardinalDirection.West => "←",
            _ => "?"
        };

        public static string WorldName(string stageId) => stageId switch
        {
            "TF-1" => "시험의 정원",
            "1-1" or "1-2" or "1-3" or "1-4" => "노을 과수원",
            "2-1" or "2-2" or "2-3" or "2-4" => "바람종 협곡",
            "3-1" or "3-2" or "3-3" or "3-4" => "별뿌리 온실",
            _ => "테스트 필드"
        };

        public static string GoalLabel(ClearGoalType goal) =>
            goal == ClearGoalType.ExitDoor ? "출구 문" : "밤꽃";

        public static string GameOverReason(GameOverCause cause, ClearGoalType goal) => cause switch
        {
            GameOverCause.OverlappingShadows => "겹친 그림자의 힘에 끌려 심연으로 빠졌습니다.",
            GameOverCause.CliffFall => "절벽 아래로 떨어졌습니다!",
            GameOverCause.TimeExpired when goal == ClearGoalType.ExitDoor =>
                "시간 안에 방을 빠져나가지 못해 어둠 속으로 빨려 들어갔습니다.",
            GameOverCause.TimeExpired =>
                "밤꽃에 도달하지 못한 채 어둠 속으로 빨려 들어갔습니다.",
            _ => "게임 오버"
        };
    }
}
