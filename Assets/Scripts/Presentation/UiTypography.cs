using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Game typography contract (UI/UX §11 / §40): Noto Sans KR Regular/Bold TMP SDF, SIL OFL 1.1.
    /// Symbol glyphs (⌂❀★…) are served by LiberationSans / ornament fallback atlases.
    /// </summary>
    public static class UiTypography
    {
        public const string RegularAssetPath = "Assets/Resources/Fonts/NotoSansKR-Regular SDF.asset";
        public const string BoldAssetPath = "Assets/Resources/Fonts/NotoSansKR-Bold SDF.asset";
        public const string SymbolFallbackAssetPath = "Assets/Resources/Fonts/ShadowGardenSymbols SDF.asset";
        public const string OrnamentFallbackAssetPath = "Assets/Resources/Fonts/ShadowGardenOrnaments SDF.asset";
        public const string RegularOtfPath = "Assets/Fonts/NotoSansKR/NotoSansKR-Regular.otf";
        public const string BoldOtfPath = "Assets/Fonts/NotoSansKR/NotoSansKR-Bold.otf";
        public const string LiberationSansTtfPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";

        private const string RegularResource = "Fonts/NotoSansKR-Regular SDF";
        private const string BoldResource = "Fonts/NotoSansKR-Bold SDF";
        private const string SymbolResource = "Fonts/ShadowGardenSymbols SDF";
        private const string OrnamentResource = "Fonts/ShadowGardenOrnaments SDF";

        /// <summary>Door / flower / channel ornaments used by board + world map.</summary>
        public const string SymbolCorpus = "⌂❀★●▲◆↑↓←→×·";

        private static TMP_FontAsset _regular;
        private static TMP_FontAsset _bold;
        private static TMP_FontAsset _symbols;
        private static TMP_FontAsset _ornaments;
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

        public static TMP_FontAsset Symbols
        {
            get
            {
                if (_symbols == null)
                {
                    _symbols = LoadTmp(SymbolFallbackAssetPath, SymbolResource);
                }

                return _symbols;
            }
        }

        public static TMP_FontAsset Ornaments
        {
            get
            {
                if (_ornaments == null)
                {
                    _ornaments = LoadTmp(OrnamentFallbackAssetPath, OrnamentResource);
                }

                return _ornaments;
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

            EnsureFallbacks();
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
            EnsureFallbacks();
            if (Regular != null && TMP_Settings.instance != null)
            {
                TMP_Settings.defaultFontAsset = Regular;
            }
        }

        public static void EnsureFallbacks()
        {
            WireFallbackChain(Regular);
            WireFallbackChain(Bold);
        }

        public static void WireFallbackChain(TMP_FontAsset primary)
        {
            if (primary == null)
            {
                return;
            }

            primary.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            AddFallbackUnique(primary.fallbackFontAssetTable, Symbols);
            AddFallbackUnique(primary.fallbackFontAssetTable, Ornaments);
        }

        private static void AddFallbackUnique(List<TMP_FontAsset> table, TMP_FontAsset fallback)
        {
            if (fallback == null)
            {
                return;
            }

            for (var i = 0; i < table.Count; i++)
            {
                if (table[i] == fallback)
                {
                    return;
                }
            }

            table.Add(fallback);
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
