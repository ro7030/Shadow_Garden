using System.Collections;
using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>Authored Moa sprite presentation driven only by positions supplied by Core.</summary>
    public sealed class PlayerPresenter : MonoBehaviour
    {
        [SerializeField] private Transform playerVisual;

        private SpriteRenderer _renderer;
        private MoaAnimationSetAsset _moa;
        private Coroutine _motion;
        private bool _alternateFrame;
        private CardinalDirection _lastDirection = CardinalDirection.South;

        public Transform Visual => playerVisual;
        public bool IsMotionPlaying => _motion != null;

        public void EnsureVisual()
        {
            _moa = PresentationAssetLibrary.Catalog?.moa;
            if (playerVisual == null)
            {
                var root = new GameObject("Moa");
                root.transform.SetParent(transform, false);
                playerVisual = root.transform;
            }

            _renderer = playerVisual.GetComponentInChildren<SpriteRenderer>();
            if (_renderer == null)
            {
                var visual = new GameObject("MoaSprite");
                visual.transform.SetParent(playerVisual, false);
                _renderer = visual.AddComponent<SpriteRenderer>();
            }

            _renderer.sortingOrder = 72;
            _renderer.sprite = _moa?.GetMoveFrame(CardinalDirection.South, false);
            _renderer.color = Color.white;
            playerVisual.localScale = Vector3.one;
        }

        public void Snap(GridPosition position)
        {
            EnsureVisual();
            StopMotion();
            playerVisual.position = GridWorld.ToWorld(position, -0.38f);
            SetFrame(_lastDirection, false);
        }

        public void Render(GridPosition position) => Snap(position);

        public Coroutine AnimateMove(GridPosition from, GridPosition to, float seconds)
        {
            EnsureVisual();
            StopMotion();
            _lastDirection = ResolveDirection(from, to, _lastDirection);
            _alternateFrame = !_alternateFrame;
            SetFrame(_lastDirection, _alternateFrame);
            _motion = StartCoroutine(MoveRoutine(
                GridWorld.ToWorld(from, -0.38f),
                GridWorld.ToWorld(to, -0.38f),
                Mathf.Max(0.01f, seconds)));
            return _motion;
        }

        public Coroutine AnimatePassThroughDoor(GridPosition goal, float seconds)
        {
            EnsureVisual();
            StopMotion();
            var start = GridWorld.ToWorld(goal, -0.38f);
            var end = start + DirectionVector(_lastDirection) * 0.65f;
            _motion = StartCoroutine(LerpWorld(start, end, Mathf.Max(0.01f, seconds), shrink: true));
            return _motion;
        }

        public Coroutine AnimateCliffFall(
            GridPosition from,
            CardinalDirection direction,
            float fallSeconds)
        {
            EnsureVisual();
            StopMotion();
            _lastDirection = direction;
            SetFrame(direction, false);
            _motion = StartCoroutine(CliffFallRoutine(from, direction, fallSeconds));
            return _motion;
        }

        public Coroutine AnimateOverlapSink(
            GridPosition from,
            GridPosition target,
            CardinalDirection direction,
            float moveSeconds,
            float sinkSeconds)
        {
            EnsureVisual();
            StopMotion();
            _lastDirection = direction;
            SetFrame(direction, true);
            _motion = StartCoroutine(OverlapSinkRoutine(from, target, moveSeconds, sinkSeconds));
            return _motion;
        }

        public Coroutine AnimateTimeVacuum(GridPosition cell, float seconds)
        {
            EnsureVisual();
            StopMotion();
            _motion = StartCoroutine(VacuumRoutine(cell, seconds));
            return _motion;
        }

        private IEnumerator MoveRoutine(Vector3 from, Vector3 to, float seconds)
        {
            var elapsed = 0f;
            playerVisual.position = from;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = t * t * (3f - 2f * t);
                playerVisual.position = Vector3.LerpUnclamped(from, to, eased) +
                                        new Vector3(0f, Mathf.Sin(t * Mathf.PI) * 0.07f, 0f);
                yield return null;
            }

            playerVisual.position = to;
            SetFrame(_lastDirection, false);
            _motion = null;
        }

        private IEnumerator CliffFallRoutine(
            GridPosition from,
            CardinalDirection direction,
            float fallSeconds)
        {
            var start = GridWorld.ToWorld(from, -0.38f);
            var approach = start + DirectionVector(direction) * PresentationTiming.CliffApproachCells;
            yield return AnimateLerp(start, approach, PresentationTiming.MoveSeconds, false, 0f);
            var end = approach + new Vector3(0f, -1.25f, 0f);
            yield return AnimateLerp(approach, end, Mathf.Max(0.01f, fallSeconds), true, 95f);
            _motion = null;
        }

        private IEnumerator OverlapSinkRoutine(
            GridPosition from,
            GridPosition target,
            float moveSeconds,
            float sinkSeconds)
        {
            var start = GridWorld.ToWorld(from, -0.38f);
            var hazard = GridWorld.ToWorld(target, -0.38f);
            yield return AnimateLerp(start, hazard, Mathf.Max(0.01f, moveSeconds), false, 0f);
            SetFrame(_lastDirection, false);
            var end = hazard + new Vector3(0f, -0.62f, 0f);
            yield return AnimateLerp(hazard, end, Mathf.Max(0.01f, sinkSeconds), true, 0f);
            _motion = null;
        }

        private IEnumerator VacuumRoutine(GridPosition cell, float seconds)
        {
            var start = GridWorld.ToWorld(cell, -0.38f);
            playerVisual.position = start;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, seconds));
                playerVisual.localRotation = Quaternion.Euler(0f, 0f, t * 540f);
                playerVisual.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.05f, t * t);
                playerVisual.position = start + new Vector3(Mathf.Sin(t * 12f) * 0.08f, -t * 0.38f, 0f);
                SetAlpha(1f - t);
                yield return null;
            }

            _motion = null;
        }

        private IEnumerator LerpWorld(Vector3 from, Vector3 to, float seconds, bool shrink, float rotate = 0f)
        {
            yield return AnimateLerp(from, to, seconds, shrink, rotate);
            _motion = null;
        }

        private IEnumerator AnimateLerp(Vector3 from, Vector3 to, float seconds, bool shrink, float rotate)
        {
            var elapsed = 0f;
            playerVisual.position = from;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, seconds));
                var eased = t * t * (3f - 2f * t);
                playerVisual.position = Vector3.LerpUnclamped(from, to, eased);
                if (shrink)
                {
                    playerVisual.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.14f, eased);
                    SetAlpha(1f - eased);
                }

                if (Mathf.Abs(rotate) > 0.01f)
                {
                    playerVisual.localRotation = Quaternion.Euler(0f, 0f, rotate * eased);
                }

                yield return null;
            }

            playerVisual.position = to;
        }

        private void SetFrame(CardinalDirection direction, bool alternate)
        {
            if (_renderer == null) return;
            _renderer.sprite = _moa?.GetMoveFrame(direction, alternate);
            _renderer.flipX = direction == CardinalDirection.West;
            _renderer.color = Color.white;
        }

        private void SetAlpha(float alpha)
        {
            if (_renderer == null) return;
            var color = _renderer.color;
            color.a = Mathf.Clamp01(alpha);
            _renderer.color = color;
        }

        private void StopMotion()
        {
            if (_motion != null)
            {
                StopCoroutine(_motion);
                _motion = null;
            }

            if (playerVisual != null)
            {
                playerVisual.localScale = Vector3.one;
                playerVisual.localRotation = Quaternion.identity;
            }

            SetAlpha(1f);
        }

        private static CardinalDirection ResolveDirection(
            GridPosition from,
            GridPosition to,
            CardinalDirection fallback)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            if (dx > 0) return CardinalDirection.East;
            if (dx < 0) return CardinalDirection.West;
            if (dy > 0) return CardinalDirection.South;
            if (dy < 0) return CardinalDirection.North;
            return fallback;
        }

        private static Vector3 DirectionVector(CardinalDirection direction) => direction switch
        {
            CardinalDirection.North => Vector3.up,
            CardinalDirection.East => Vector3.right,
            CardinalDirection.South => Vector3.down,
            CardinalDirection.West => Vector3.left,
            _ => Vector3.right
        };
    }
}
