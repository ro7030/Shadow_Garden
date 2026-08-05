using UnityEngine;

namespace ShadowGarden.Presentation
{
    [CreateAssetMenu(menuName = "Shadow Garden/Presentation/Stage Binding", fileName = "StagePresentationBinding")]
    public sealed class StagePresentationBindingAsset : ScriptableObject
    {
        public string stageId;
        public WorldArtSetAsset worldArt;
    }
}
