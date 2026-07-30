#if UNITY_EDITOR
using ShadowGarden.Core;
using UnityEditor;
using UnityEngine;

namespace ShadowGarden.Infrastructure.Editor
{
    public static class GrayboxStageAssetMenu
    {
        [MenuItem("ShadowGarden/Generate Graybox Stage Assets")]
        public static void Generate()
        {
            Ensure("Assets/Stages/Stage_TF_1.asset", GrayboxStages.CreateTF_1());
            Ensure("Assets/Stages/Stage_1_1.asset", GrayboxStages.Create1_1());
            Ensure("Assets/Stages/Stage_1_2.asset", GrayboxStages.Create1_2());
            Ensure("Assets/Stages/Stage_1_4.asset", GrayboxStages.Create1_4());
            Ensure("Assets/Stages/Stage_3_4.asset", GrayboxStages.Create3_4());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Graybox stage assets generated.");
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
