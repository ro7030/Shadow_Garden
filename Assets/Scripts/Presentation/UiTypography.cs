using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Game typography contract (UI/UX §11 / §40): Noto Sans KR Regular/Bold TMP SDF, SIL OFL 1.1.
    /// </summary>
    public static class UiTypography
    {
        public const string RegularAssetPath = "Assets/Resources/Fonts/NotoSansKR-Regular SDF.asset";
        public const string BoldAssetPath = "Assets/Resources/Fonts/NotoSansKR-Bold SDF.asset";
        public const string RegularOtfPath = "Assets/Fonts/NotoSansKR/NotoSansKR-Regular.otf";
        public const string BoldOtfPath = "Assets/Fonts/NotoSansKR/NotoSansKR-Bold.otf";

        private const string RegularResource = "Fonts/NotoSansKR-Regular SDF";
        private const string BoldResource = "Fonts/NotoSansKR-Bold SDF";

        private static TMP_FontAsset _regular;
        private static TMP_FontAsset _bold;
        private static Font _regularLegacy;
        private static Font _boldLegacy;

        public static TMP_FontAsset Regular
        {
            get
            {
                if (_regular == null)
                {
                    _regular = LoadTmp(RegularAssetPath, RegularResource);
                }

                return _regular;
            }
        }

        public static TMP_FontAsset Bold
        {
            get
            {
                if (_bold == null)
                {
                    _bold = LoadTmp(BoldAssetPath, BoldResource) ?? Regular;
                }

                return _bold;
            }
        }

        /// <summary>Legacy Font for IMGUI (TestField OnGUI). Prefer TMP elsewhere.</summary>
        public static Font RegularLegacy
        {
            get
            {
                if (_regularLegacy == null)
                {
                    _regularLegacy = LoadLegacy(RegularOtfPath, "Fonts/NotoSansKR-Regular");
                }

                return _regularLegacy;
            }
        }

        public static Font BoldLegacy
        {
            get
            {
                if (_boldLegacy == null)
                {
                    _boldLegacy = LoadLegacy(BoldOtfPath, "Fonts/NotoSansKR-Bold") ?? RegularLegacy;
                }

                return _boldLegacy;
            }
        }

        public static void Apply(TMP_Text text, bool bold = false)
        {
            if (text == null)
            {
                return;
            }

            var font = bold ? Bold : Regular;
            if (font != null)
            {
                text.font = font;
            }
        }

        public static void ApplyToGuiStyle(GUIStyle style, bool bold = false)
        {
            if (style == null)
            {
                return;
            }

            var font = bold ? BoldLegacy : RegularLegacy;
            if (font != null)
            {
                style.font = font;
            }
        }

        public static void ApplyDefaultSettings()
        {
            if (Regular != null && TMP_Settings.instance != null)
            {
                TMP_Settings.defaultFontAsset = Regular;
            }
        }

        private static TMP_FontAsset LoadTmp(string editorPath, string resourcesPath)
        {
#if UNITY_EDITOR
            var fromEditor = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(editorPath);
            if (fromEditor != null)
            {
                return fromEditor;
            }
#endif
            return Resources.Load<TMP_FontAsset>(resourcesPath);
        }

        private static Font LoadLegacy(string editorPath, string resourcesPath)
        {
#if UNITY_EDITOR
            var fromEditor = AssetDatabase.LoadAssetAtPath<Font>(editorPath);
            if (fromEditor != null)
            {
                return fromEditor;
            }
#endif
            return Resources.Load<Font>(resourcesPath);
        }
    }
}
