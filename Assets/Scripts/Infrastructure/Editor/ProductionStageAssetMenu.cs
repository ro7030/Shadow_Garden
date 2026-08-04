#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using ShadowGarden.Core;
using UnityEditor;
using UnityEngine;

namespace ShadowGarden.Infrastructure.Editor
{
    public static class ProductionStageAssetMenu
    {
        private static readonly string[] WorldFolders =
        {
            "Assets/Content/Stages/World01",
            "Assets/Content/Stages/World02",
            "Assets/Content/Stages/World03"
        };

        [MenuItem("ShadowGarden/Stages/Generate Production Stage Assets (12)")]
        public static void GenerateProductionAssets()
        {
            EnsureFolders();
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalogAsset>("Assets/Stages/StageCatalog.asset");
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StageCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, "Assets/Stages/StageCatalog.asset");
            }

            var assets = new List<StageDefinitionAsset>();
            foreach (var bundle in MainStages.AllBundles())
            {
                var path = ContentPathFor(bundle.Definition.StageId);
                var asset = AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<StageDefinitionAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                StageDefinitionFactory.ApplyProduction(asset, bundle);
                EditorUtility.SetDirty(asset);
                assets.Add(asset);
            }

            var so = new SerializedObject(catalog);
            var stagesProp = so.FindProperty("stages");
            if (stagesProp != null && stagesProp.isArray)
            {
                stagesProp.arraySize = assets.Count;
                for (var i = 0; i < assets.Count; i++)
                {
                    stagesProp.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ShadowGarden] Generated {assets.Count} production stage assets + updated StageCatalog.");
        }

        [MenuItem("ShadowGarden/Stages/Validate All Production Stages")]
        public static void ValidateAllProductionStages()
        {
            var report = new StringBuilder();
            var failures = 0;
            var shorterReports = new List<string>();

            foreach (var bundle in MainStages.AllBundles())
            {
                var id = bundle.Definition.StageId;
                var issues = StageValidator.Validate(bundle.Definition);
                if (issues.Count > 0)
                {
                    failures++;
                    report.AppendLine($"FAIL {id} validator:");
                    foreach (var issue in issues)
                    {
                        report.AppendLine($"  - {issue}");
                    }

                    continue;
                }

                if (!SolutionReplay.TryReplay(bundle.Definition, bundle.Solution, out var replayFail))
                {
                    failures++;
                    report.AppendLine($"FAIL {id} replay: {replayFail}");
                    continue;
                }

                if (!SafetyPathFinder.TryFindMinimalSolution(bundle.Definition, out var found))
                {
                    failures++;
                    report.AppendLine($"FAIL {id}: SafetyPathFinder found no safe solution.");
                    continue;
                }

                if (found.RotateCount < bundle.Solution.DocumentedMinRotates)
                {
                    var msg =
                        $"SHORTER {id}: documentedMinRotates={bundle.Solution.DocumentedMinRotates} " +
                        $"foundRotates={found.RotateCount} moves={found.MoveCount} cmds=[{string.Join(" ", found.Commands)}]";
                    shorterReports.Add(msg);
                    report.AppendLine(msg);
                }

                report.AppendLine(
                    $"OK {id} size={bundle.Definition.BoardSize} lamps={bundle.Definition.Lamps.Count} " +
                    $"pathFinderRotates={found.RotateCount} explored={found.ExploredStates}");
            }

            if (shorterReports.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("=== Shorter solutions that may bypass learning gates (reported, not hidden) ===");
                foreach (var s in shorterReports)
                {
                    report.AppendLine(s);
                }
            }

            report.AppendLine();
            report.AppendLine(failures == 0
                ? $"PASS all {MainStages.AllBundles().Count} production stages."
                : $"DONE with {failures} failure(s).");
            if (failures == 0)
            {
                Debug.Log(report.ToString());
            }
            else
            {
                Debug.LogError(report.ToString());
            }
        }

        private static string ContentPathFor(string stageId)
        {
            var world = stageId[0] switch
            {
                '1' => "World01",
                '2' => "World02",
                '3' => "World03",
                _ => "World01"
            };
            var file = "Stage_" + stageId.Replace('-', '_') + ".asset";
            return $"Assets/Content/Stages/{world}/{file}";
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Content"))
            {
                AssetDatabase.CreateFolder("Assets", "Content");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Content/Stages"))
            {
                AssetDatabase.CreateFolder("Assets/Content", "Stages");
            }

            foreach (var folder in WorldFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                var name = System.IO.Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
