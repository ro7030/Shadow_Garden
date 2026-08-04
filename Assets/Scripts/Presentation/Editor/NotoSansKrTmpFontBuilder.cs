#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ShadowGarden.Presentation.EditorTools
{
    /// <summary>
    /// Builds Noto Sans KR TMP SDF assets and Liberation/ornament symbol fallbacks.
    /// </summary>
    public static class NotoSansKrTmpFontBuilder
    {
        private const string RegularOtf = UiTypography.RegularOtfPath;
        private const string BoldOtf = UiTypography.BoldOtfPath;
        private const string OutDir = "Assets/Resources/Fonts";

        private static readonly string[] OrnamentFontCandidates =
        {
            "/System/Library/Fonts/ZapfDingbats.ttf",
            "/System/Library/Fonts/Apple Symbols.ttf",
            "Assets/Fonts/Symbols/ZapfDingbats.ttf",
            "Assets/Fonts/Symbols/AppleSymbols.ttf"
        };

        [MenuItem("ShadowGarden/Fonts/Rebuild Noto Sans KR TMP Assets")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);

            // Runtime IMGUI fallback (TestField) loads these via Resources.
            EnsureResourcesFontCopy(RegularOtf, "Assets/Resources/Fonts/NotoSansKR-Regular.otf");
            EnsureResourcesFontCopy(BoldOtf, "Assets/Resources/Fonts/NotoSansKR-Bold.otf");

            var koreanCorpus = BuildCorpus();
            BuildOne(RegularOtf, UiTypography.RegularAssetPath, koreanCorpus);
            BuildOne(BoldOtf, UiTypography.BoldAssetPath, koreanCorpus);

            RebuildSymbolFallbacks();
            WireNotoFallbacks();

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
            Debug.Log($"[ShadowGarden] Rebuilt Noto + symbol fallback TMP fonts. Korean corpus={koreanCorpus.Length}");
        }

        [MenuItem("ShadowGarden/Fonts/Rebuild Symbol Fallback Fonts")]
        public static void RebuildSymbolFallbacksMenu()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(OutDir);
            RebuildSymbolFallbacks();
            WireNotoFallbacks();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ShadowGarden] Rebuilt symbol fallback TMP fonts.");
        }

        private static void RebuildSymbolFallbacks()
        {
            // LiberationSans covers ⌂ ● ▲ arrows etc.
            BuildOne(
                UiTypography.LiberationSansTtfPath,
                UiTypography.SymbolFallbackAssetPath,
                UiTypography.SymbolCorpus);

            var ornamentSource = FindExistingFontPath(OrnamentFontCandidates);
            if (!string.IsNullOrEmpty(ornamentSource))
            {
                // ZapfDingbats / Apple Symbols cover ❀ ★ ◆ missing from LiberationSans.
                BuildOneFromAbsoluteOrAsset(
                    ornamentSource,
                    UiTypography.OrnamentFallbackAssetPath,
                    UiTypography.SymbolCorpus);
            }
            else
            {
                Debug.LogWarning(
                    "[ShadowGarden] No ornament source font found (ZapfDingbats/Apple Symbols). " +
                    "❀★◆ may still be missing until a symbols TTF is added under Assets/Fonts/Symbols/.");
            }
        }

        private static void WireNotoFallbacks()
        {
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTypography.RegularAssetPath);
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTypography.BoldAssetPath);
            var symbols = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTypography.SymbolFallbackAssetPath);
            var ornaments = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiTypography.OrnamentFallbackAssetPath);

            Wire(regular, symbols, ornaments);
            Wire(bold, symbols, ornaments);
        }

        private static void Wire(TMP_FontAsset primary, TMP_FontAsset symbols, TMP_FontAsset ornaments)
        {
            if (primary == null)
            {
                return;
            }

            primary.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (symbols != null)
            {
                primary.fallbackFontAssetTable.Add(symbols);
            }

            if (ornaments != null)
            {
                primary.fallbackFontAssetTable.Add(ornaments);
            }

            EditorUtility.SetDirty(primary);
        }

        private static string FindExistingFontPath(IEnumerable<string> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                if (candidate.StartsWith("Assets/"))
                {
                    if (AssetDatabase.LoadAssetAtPath<Font>(candidate) != null)
                    {
                        return candidate;
                    }
                }
                else if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
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

            CreateBakedFontAsset(font, outputPath, corpus);
        }

        private static void BuildOneFromAbsoluteOrAsset(string sourcePath, string outputPath, string corpus)
        {
            string importedPath = null;
            Font font;
            if (sourcePath.StartsWith("Assets/"))
            {
                font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            }
            else
            {
                importedPath = OutDir + "/_TempOrnamentSource" + Path.GetExtension(sourcePath);
                var absoluteDest = Path.GetFullPath(Path.Combine(Application.dataPath, "..", importedPath));
                File.Copy(sourcePath, absoluteDest, true);
                AssetDatabase.ImportAsset(importedPath);
                font = AssetDatabase.LoadAssetAtPath<Font>(importedPath);
            }

            if (font == null)
            {
                if (!string.IsNullOrEmpty(importedPath))
                {
                    AssetDatabase.DeleteAsset(importedPath);
                }

                throw new FileNotFoundException("Missing source font", sourcePath);
            }

            try
            {
                CreateBakedFontAsset(font, outputPath, corpus);
            }
            finally
            {
                if (!string.IsNullOrEmpty(importedPath))
                {
                    AssetDatabase.DeleteAsset(importedPath);
                }
            }
        }

        private static void CreateBakedFontAsset(Font font, string outputPath, string corpus)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                AssetDatabase.DeleteAsset(outputPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                72,
                6,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
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
                Debug.Log($"[ShadowGarden] Partial glyphs in {outputPath}: missing='{missing}'");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fontAsset);
        }

        public static string BuildCorpus()
        {
            // Exact UI phrases (Stage 1–4). Deduped below so every displayed syllable is baked.
            string[] phrases =
            {
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789",
                " ·×/!:?.,-'\"[]()↑↓←→",
                "그림자 정원", "시작", "계속", "월드 맵", "게임 오버", "완료",
                "다시 도전", "레벨 선택", "목표", "출구 문", "밤꽃",
                "노을 과수원", "바람종 협곡", "별뿌리 온실", "시험의 정원", "테스트 필드",
                "원", "삼각형", "별", "마름모", "열린 문",
                "겹친 그림자의 힘에 끌려 심연으로 빠졌습니다.",
                "절벽 아래로 떨어졌습니다!",
                "시간 안에 방을 빠져나가지 못해 어둠 속으로 빨려 들어갔습니다.",
                "밤꽃에 도달하지 못한 채 어둠 속으로 빨려 들어갔습니다.",
                "태양등 위에서 Q/E로 방향을 바꾼 뒤, 남색 그림자 길로 출구에 가세요.",
                "두 채널을 맞춰 ×2를 피하고 밤꽃에 도달하세요.",
                "원·삼각·별 세 구간을 순서대로 이으세요.",
                "네 채널을 조율해 18×8 온실의 밤꽃까지 가세요. ×2는 심연입니다.",
                "그림자가 바뀌었습니다. 남색 길만 건너세요.",
                "보드를 초기화했습니다.",
                "남은 시간 30초", "남은 시간 10초!", "막힌 칸입니다.",
                "스테이지 완료", "다음 선택", "확인", "이동", "회전",
                "WASD 이동", "Q/E 90° 회전", "R 다시 도전", "스테이지",
                "클리어 시간", "방향키", "Enter", "Space",
                "정원으로 돌아가기", "새로 시작", "마지막", "완료",
                "잠김", "이전 월드의 밤꽃을 피워 주세요",
                "엔딩", "엔딩 보기", "타이틀", "다음 스테이지", "다음 월드",
                "세 정원이 다시 숨을 쉬기 시작했습니다.",
                "제작 Shadow Garden",
                "Font Noto Sans KR SIL OFL",
                "브라우저 포커스가 잠시 떠났습니다",
                "클릭하면 안전하게 이어서 플레이합니다",
                "클릭하여 복귀",
                "일시정지", "계속하기", "설정", "효과음", "모션 완화", "점멸 맥동 제거",
                "오프닝 다시 보기", "닫기", "전체화면",
                "빛과 그림자로 잠든 정원을 되살린다",
                "정오의 정원이 멈췄다", "해가 제자리에 굳어 그림자도 숨이 멎었다",
                "모아가 밤씨앗을 품고 도착한다", "잠든 길을 다시 열어야 한다",
                "태양등을 돌리면 그림자가 길을 연다", "남색 길만이 발이 닿는 땅이다",
                "겹친 그림자와 빈 절벽은 위험하다", "한 칸씩 천천히 읽으며 걷자",
                "출구 문으로 방을 지나고", "밤꽃을 피워 월드를 깨운다",
                "첫 그림자를 따라 걸어 보자", "정원이 다시 숨을 쉴 때까지",
                "정원으로", "다음", "건너뛰기", "오프닝",
                "세 정원이 모두 되살아났습니다", "다음 월드가 해금되었습니다",
                "출구 완료", "밤꽃 완료", "남은 시간",
                "을를이가은는와과에도만부터까지보다처럼같이요",
                "QEWASDR"
            };

            var seen = new HashSet<char>();
            var sb = new StringBuilder();
            foreach (var phrase in phrases)
            {
                foreach (var ch in phrase)
                {
                    if (seen.Add(ch))
                    {
                        sb.Append(ch);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
#endif
