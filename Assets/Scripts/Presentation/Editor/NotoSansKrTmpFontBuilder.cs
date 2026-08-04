#if UNITY_EDITOR
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ShadowGarden.Presentation.EditorTools
{
    /// <summary>
    /// Builds Noto Sans KR TMP SDF assets from the game string corpus (UI/UX §40).
    /// </summary>
    public static class NotoSansKrTmpFontBuilder
    {
        private const string RegularOtf = UiTypography.RegularOtfPath;
        private const string BoldOtf = UiTypography.BoldOtfPath;
        private const string OutDir = "Assets/Resources/Fonts";

        [MenuItem("ShadowGarden/Fonts/Rebuild Noto Sans KR TMP Assets")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);

            // Runtime IMGUI fallback (TestField) loads these via Resources.
            EnsureResourcesFontCopy(RegularOtf, "Assets/Resources/Fonts/NotoSansKR-Regular.otf");
            EnsureResourcesFontCopy(BoldOtf, "Assets/Resources/Fonts/NotoSansKR-Bold.otf");

            var corpus = BuildCorpus();
            BuildOne(RegularOtf, UiTypography.RegularAssetPath, corpus);
            BuildOne(BoldOtf, UiTypography.BoldAssetPath, corpus);

            var settings = AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
            if (settings != null)
            {
                var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTypography.RegularAssetPath);
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = regular;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(settings);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ShadowGarden] Rebuilt Noto Sans KR TMP fonts. Corpus length={corpus.Length}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureResourcesFontCopy(string sourcePath, string destPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Font>(destPath) != null)
            {
                return;
            }

            AssetDatabase.CopyAsset(sourcePath, destPath);
        }

        private static void BuildOne(string sourceFontPath, string outputPath, string corpus)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (font == null)
            {
                throw new FileNotFoundException("Missing source font", sourceFontPath);
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                AssetDatabase.DeleteAsset(outputPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                72,
                6,
                GlyphRenderMode.SDFAA,
                4096,
                4096,
                AtlasPopulationMode.Dynamic);
            fontAsset.name = Path.GetFileNameWithoutExtension(outputPath);
            AssetDatabase.CreateAsset(fontAsset, outputPath);

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            }

            fontAsset.TryAddCharacters(corpus, out var missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[ShadowGarden] Missing glyphs in {outputPath}: count={missing.Length}");
            }

            // Bake corpus then lock for WebGL-friendly static atlases.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fontAsset);
        }

        public static string BuildCorpus()
        {
            var sb = new StringBuilder();
            sb.Append("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789");
            sb.Append(" ·×/!:?.,-'\"[]()↑↓←→⌂❀★●▲◆");
            sb.Append("그림자정원시작계속월드맵게임오버완료다시도전레벨선택첫목표출구문밤꽃");
            sb.Append("노을과수원바람종협곡별뿌리온실시험의도테스트필드");
            sb.Append("원삼각형별마름모열린문");
            sb.Append("겹친그림자의힘에끌려심연으로빠졌습니다절벽아래로떨어졌습니다");
            sb.Append("시간안에방을빠져나가지못해어둠속으로빨려들어갔습니다");
            sb.Append("밤꽃에도달하지못한채어둠속으로빨려들어갔습니다");
            sb.Append("태양등위에서방향을바꾼뒤남색길로출구에가세요");
            sb.Append("두채널을맞춰를피하고밤꽃에도달하세요");
            sb.Append("세구간을순서대로이으세요");
            sb.Append("네채널을조율해온실의밤꽃까지가세요는심연입니다");
            sb.Append("그림자가바뀌었습니다남색길만건너세요보드를초기화했습니다");
            sb.Append("남은시간초막힌칸입니다스테이지완료다음선택확인이동회전");
            sb.Append("QEWASDR");
            sb.Append("을를이가은는와과에도만부터까지보다처럼같이");
            return sb.ToString();
        }
    }
}
#endif
