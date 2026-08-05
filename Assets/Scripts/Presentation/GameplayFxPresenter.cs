using System.Collections;
using System.Collections.Generic;
using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>Small reusable SpriteRenderer pool for documented gameplay beats.</summary>
    public sealed class GameplayFxPresenter : MonoBehaviour
    {
        private readonly List<SpriteRenderer> _pool = new();
        private GameplayFxSetAsset _set;
        private bool _reduceMotion;

        public void Bind(bool reduceMotion)
        {
            _set = PresentationAssetLibrary.Catalog?.gameplayFx;
            _reduceMotion = reduceMotion;
        }

        public void PlayRotate(GridPosition position, Color color, float duration)
        {
            Play(_set?.rotateSweep, position, color, duration, 0.65f, 1.25f, 75f);
        }

        public void PlayDeath(GameOverCause cause, GridPosition position, float duration)
        {
            var sprite = cause switch
            {
                GameOverCause.OverlappingShadows => _set?.dangerPulse,
                GameOverCause.CliffFall => _set?.fallDust,
                GameOverCause.TimeExpired => _set?.vacuumSwirl,
                _ => _set?.dangerPulse
            };
            var color = cause == GameOverCause.OverlappingShadows ? UiTheme.Coral : Color.white;
            Play(sprite, position, color, duration, 0.55f, 1.35f, cause == GameOverCause.TimeExpired ? 220f : 35f);
        }

        public void PlayComplete(GridPosition position, float duration)
        {
            Play(_set?.completionGlow, position, UiTheme.Mint, duration, 0.55f, 1.5f, 24f);
        }

        public void PlayDoorGlow(GridPosition position, float duration)
        {
            Play(_set?.doorGlow, position, Color.white, duration, 0.42f, 0.82f, 0f);
        }

        public void PlayFlowerBloom(GridPosition position, float duration)
        {
            Play(_set?.flowerPetal, position, Color.white, duration, 0.48f, 1.0f, 18f);
        }

        private void Play(
            Sprite sprite,
            GridPosition position,
            Color color,
            float duration,
            float fromScale,
            float toScale,
            float rotation)
        {
            if (sprite == null) return;
            var renderer = Acquire();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.transform.position = GridWorld.ToWorld(position, -0.6f);
            renderer.gameObject.SetActive(true);
            StartCoroutine(Animate(renderer, Mathf.Max(0.05f, duration), fromScale, toScale, rotation));
        }

        private SpriteRenderer Acquire()
        {
            foreach (var renderer in _pool)
                if (renderer != null && !renderer.gameObject.activeSelf) return renderer;
            var go = new GameObject($"GameplayFx_{_pool.Count + 1}");
            go.transform.SetParent(transform, false);
            var created = go.AddComponent<SpriteRenderer>();
            created.sortingOrder = 90;
            go.SetActive(false);
            _pool.Add(created);
            return created;
        }

        private IEnumerator Animate(SpriteRenderer renderer, float duration, float fromScale, float toScale, float rotation)
        {
            var elapsed = 0f;
            var baseColor = renderer.color;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = t * t * (3f - 2f * t);
                renderer.transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, eased);
                renderer.transform.localRotation = _reduceMotion
                    ? Quaternion.identity
                    : Quaternion.Euler(0f, 0f, rotation * eased);
                var color = baseColor;
                color.a = 1f - eased;
                renderer.color = color;
                yield return null;
            }
            renderer.gameObject.SetActive(false);
            renderer.transform.localScale = Vector3.one;
            renderer.transform.localRotation = Quaternion.identity;
        }
    }
}
