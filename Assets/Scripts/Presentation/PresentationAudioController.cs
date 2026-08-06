using System.Collections;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>Small, leak-free audio layer for presentation clips. Core remains audio-agnostic.</summary>
    public sealed class PresentationAudioController : MonoBehaviour
    {
        private AudioSource _motif;
        private AudioSource _worldMusic;
        private AudioSource _ambience;
        private AudioSource _sfx;
        private AudioSetAsset _set;
        private MainCompositionRoot _main;
        private string _pendingStageId;
        private bool _menuMode = true;
        private bool _interactionUnlocked;
        private float _lastUiMoveAt = -10f;
        private float _lastUiSubmitAt = -10f;

        public void Bind(MainCompositionRoot main)
        {
            _main = main;
            EnsureSources();
            ApplyPreferences();
        }

        public void PlayMenuMusic()
        {
            EnsureSources();
            _set = PresentationAssetLibrary.Catalog?.audio;
            _menuMode = true;
            _pendingStageId = null;
            if (_set == null) return;
            if (_interactionUnlocked) StartMenuLoop();
            ApplyPreferences();
        }

        public void BeginStage(string stageId)
        {
            EnsureSources();
            _set = PresentationAssetLibrary.Catalog?.audio;
            _pendingStageId = stageId;
            _menuMode = false;
            if (_set == null) return;
            if (_interactionUnlocked) StartStageLoops();
            ApplyPreferences();
        }

        public void ApplyPreferences()
        {
            EnsureSources();
            var preferences = _main?.Save?.Preferences;
            var bgm = preferences != null ? Mathf.Clamp01(preferences.bgmVolume) : 0.7f;
            var sfx = preferences != null ? Mathf.Clamp01(preferences.sfxVolume) : 0.8f;
            // Full replacement BGMs play on the world channel; motif stays silent to avoid stacking.
            _motif.volume = 0f;
            _worldMusic.volume = bgm * 0.55f;
            _ambience.volume = _menuMode ? 0f : bgm * 0.16f;
            _sfx.volume = sfx * 0.82f;
        }

        public void Play(AudioClip clip, float scale = 1f)
        {
            if (clip == null) return;
            EnsureSources();
            UnlockFromUserInteraction();
            _sfx.PlayOneShot(clip, Mathf.Clamp01(scale));
        }

        public void PlayUiMove()
        {
            if (Time.unscaledTime - _lastUiMoveAt < 0.06f) return;
            _lastUiMoveAt = Time.unscaledTime;
            Play(Clips?.uiMove, 0.58f);
        }

        public void PlayUiSubmit()
        {
            if (Time.unscaledTime - _lastUiSubmitAt < 0.06f) return;
            _lastUiSubmitAt = Time.unscaledTime;
            Play(Clips?.uiSubmit, 0.68f);
        }

        public void PlayShadowCellChimes(int count)
        {
            var clip = Clips?.shadowCell;
            if (clip == null) return;
            UnlockFromUserInteraction();
            StartCoroutine(ShadowCellChimeRoutine(clip, Mathf.Clamp(count, 1, 4)));
        }

        public AudioSetAsset Clips => _set ??= PresentationAssetLibrary.Catalog?.audio;

        public void UnlockFromUserInteraction()
        {
            if (_interactionUnlocked) return;
            _interactionUnlocked = true;
            if (_menuMode || string.IsNullOrWhiteSpace(_pendingStageId))
            {
                StartMenuLoop();
            }
            else
            {
                StartStageLoops();
            }
        }

        private void StartMenuLoop()
        {
            if (!_interactionUnlocked || _set == null) return;
            StopSource(_motif);
            StopSource(_ambience);
            PlayLoop(_worldMusic, _set.commonMotif);
        }

        private void StartStageLoops()
        {
            if (!_interactionUnlocked || _set == null || string.IsNullOrWhiteSpace(_pendingStageId)) return;
            var world = PresentationAssetLibrary.ParseWorld(_pendingStageId);
            StopSource(_motif);
            PlayLoop(_worldMusic, _set.GetWorldMusic(world));
            PlayLoop(_ambience, _set.GetWorldAmbience(world));
        }

        private IEnumerator ShadowCellChimeRoutine(AudioClip clip, int count)
        {
            for (var index = 0; index < count; index++)
            {
                _sfx.PlayOneShot(clip, 0.48f + index * 0.08f);
                if (index + 1 < count) yield return new WaitForSecondsRealtime(0.055f);
            }
        }

        private void EnsureSources()
        {
            if (_motif != null) return;
            _motif = CreateSource("BGM_Motif", true);
            _worldMusic = CreateSource("BGM_World", true);
            _ambience = CreateSource("Ambience", true);
            _sfx = CreateSource("SFX", false);
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }

        private static void PlayLoop(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null) return;
            if (source.clip == clip && source.isPlaying) return;
            source.Stop();
            source.clip = clip;
            source.Play();
        }

        private static void StopSource(AudioSource source)
        {
            if (source == null) return;
            source.Stop();
            source.clip = null;
        }
    }
}
