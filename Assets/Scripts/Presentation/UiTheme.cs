using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Production UI/UX design tokens (UI/UX §11). Hazard colors only for danger states.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Navy = Hex(0x14243B);
        public static readonly Color NavyDeep = Hex(0x0E1828);
        public static readonly Color Ivory = Hex(0xF6F0E4);
        public static readonly Color IvoryMuted = new Color(0.92f, 0.88f, 0.82f, 0.92f);
        public static readonly Color Mint = Hex(0x78CDB8);
        public static readonly Color Brass = Hex(0xD5A63F);
        public static readonly Color Hazard = Hex(0x6B214D);
        public static readonly Color Coral = Hex(0xF28C7F);
        public static readonly Color Disabled = new Color(0.45f, 0.48f, 0.52f, 0.85f);
        public static readonly Color Panel = new Color(0.08f, 0.12f, 0.2f, 0.94f);
        public static readonly Color PanelSoft = new Color(0.12f, 0.16f, 0.26f, 0.9f);
        public static readonly Color TextPrimary = Ivory;
        public static readonly Color TextMuted = new Color(0.86f, 0.84f, 0.8f, 0.92f);

        public const float Space = 8f;
        public const float SafeMargin = 32f;
        public const float ButtonMinHeight = 44f;
        public const float ButtonWidth = 320f;
        public const float FocusOutline = 3f;
        public const int BodyFontMin = 16;
        public const int TitleFont = 48;
        public const int SubtitleFont = 24;
        public const int ButtonFont = 26;
        public const int HudFont = 28;
        public const int TimerFont = 36;

        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float Match = 0.5f;

        public static Color Hex(int rgb)
        {
            var r = ((rgb >> 16) & 0xFF) / 255f;
            var g = ((rgb >> 8) & 0xFF) / 255f;
            var b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
