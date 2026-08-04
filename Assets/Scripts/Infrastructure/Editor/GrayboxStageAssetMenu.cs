#if UNITY_EDITOR
using ShadowGarden.Core;
using UnityEditor;
using UnityEngine;

namespace ShadowGarden.Infrastructure.Editor
{
    public static class GrayboxStageAssetMenu
    {
        [MenuItem("ShadowGarden/Generate Canonical Stage Assets")]
        public static void Generate()
        {
            Ensure("Assets/Stages/Stage_1_1.asset", GrayboxStages.Create1_1());
            Ensure("Assets/Stages/Stage_1_4.asset", GrayboxStages.Create1_4());
            Ensure("Assets/Stages/Stage_2_2.asset", GrayboxStages.Create2_2());
            Ensure("Assets/Stages/Stage_3_4.asset", GrayboxStages.Create3_4());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Canonical validation stage assets generated (1-1, 1-4, 2-2, 3-4).");
        }

        private static void Ensure(string path, StageDefinition definition)
        {
            var asset = AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<StageDefinitionAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            StageDefinitionFactory.ApplyGraybox(asset, definition);
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
