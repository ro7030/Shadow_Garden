using UnityEngine;

namespace ShadowGarden.Presentation
{
    [CreateAssetMenu(menuName = "Shadow Garden/Presentation/Audio Set", fileName = "AudioSet")]
    public sealed class AudioSetAsset : ScriptableObject
    {
        [Header("Music")]
        public AudioClip commonMotif;
        public AudioClip orchardLayer;
        public AudioClip canyonLayer;
        public AudioClip greenhouseLayer;

        [Header("Ambience")]
        public AudioClip orchardAmbience;
        public AudioClip canyonAmbience;
        public AudioClip greenhouseAmbience;

        [Header("Gameplay SFX")]
        public AudioClip move;
        public AudioClip rotate;
        public AudioClip shadowCell;
        public AudioClip warning30;
        public AudioClip warning10;
        public AudioClip blocked;
        public AudioClip overlapDeath;
        public AudioClip cliffDeath;
        public AudioClip timeDeath;
        public AudioClip doorOpen;
        public AudioClip doorPass;
        public AudioClip flowerBloom;
        public AudioClip complete;
        public AudioClip uiMove;
        public AudioClip uiSubmit;

        public AudioClip GetWorldMusic(int world) => world switch
        {
            2 => canyonLayer,
            3 => greenhouseLayer,
            _ => orchardLayer
        };

        public AudioClip GetWorldAmbience(int world) => world switch
        {
            2 => canyonAmbience,
            3 => greenhouseAmbience,
            _ => orchardAmbience
        };
    }
}
