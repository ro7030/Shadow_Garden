using System.Collections;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Cloaked Moa silhouette with move / pass / fall / sink / vacuum presentation tweens.
    /// Does not recompute Core rules — only follows positions supplied by the host.
    /// </summary>
    public sealed class PlayerPresenter : MonoBehaviour
    {
        [SerializeField] private Transform playerVisual;

        private Coroutine _motion;

        public Transform Visual => playerVisual;
        public bool IsMotionPlaying => _motion != null;

        public void EnsureVisual()
        {
            if (playerVisual != null)
            {
                return;
            }

            var root = new GameObject("Moa");
            root.transform.SetParent(transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.46f, 0.34f, 0.46f);
            body.transform.localPosition = new Vector3(0f, 0.08f, -0.28f);
            Object.Destroy(body.GetComponent<Collider>());
            ApplyColor(body, MockupPalette.PlayerCloak);

            var cloak = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cloak.name = "Cloak";
            cloak.transform.SetParent(root.transform, false);
            cloak.transform.localScale = new Vector3(0.52f, 0.22f, 0.18f);
            cloak.transform.localPosition = new Vector3(0f, -0.06f, -0.22f);
            cloak.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Object.Destroy(cloak.GetComponent<Collider>());
            ApplyColor(cloak, MockupPalette.PlayerCloak);

            var hood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hood.name = "Hood";
            hood.transform.SetParent(root.transform, false);
            hood.transform.localScale = new Vector3(0.34f, 0.28f, 0.34f);
            hood.transform.localPosition = new Vector3(0f, 0.34f, -0.32f);
            Object.Destroy(hood.GetComponent<Collider>());
            ApplyColor(hood, MockupPalette.PlayerHood);

            playerVisual = root.transform;
        }

        public void Snap(ShadowGarden.Core.GridPosition position)
        {
            EnsureVisual();
            StopMotion();
            playerVisual.position = GridWorld.ToWorld(position, -0.38f);
            playerVisual.localScale = Vector3.one;
        }

        public void Render(ShadowGarden.Core.GridPosition position) => Snap(position);

        public Coroutine AnimateMove(ShadowGarden.Core.GridPosition from, ShadowGarden.Core.GridPosition to, float seconds)
        {
            EnsureVisual();
            StopMotion();
            _motion = StartCoroutine(LerpWorld(
                GridWorld.ToWorld(from, -0.38f),
                GridWorld.ToWorld(to, -0.38f),
                Mathf.Max(0.01f, seconds)));
            return _motion;
        }

        public Coroutine AnimatePassThroughDoor(ShadowGarden.Core.GridPosition goal, float seconds)
        {
            EnsureVisual();
            StopMotion();
            var start = GridWorld.ToWorld(goal, -0.38f);
            var end = start + new Vector3(0.65f, 0f, -0.35f);
            _motion = StartCoroutine(LerpWorld(start, end, Mathf.Max(0.01f, seconds), shrink: true));
            return _motion;
        }

        public Coroutine AnimateCliffFall(ShadowGarden.Core.GridPosition cell, float seconds)
        {
            EnsureVisual();
            StopMotion();
            _motion = StartCoroutine(CliffFallRoutine(cell, seconds));
            return _motion;
        }

        public Coroutine AnimateOverlapSink(ShadowGarden.Core.GridPosition cell, float seconds)
        {
            EnsureVisual();
            StopMotion();
            var start = GridWorld.ToWorld(cell, -0.38f);
            var end = start + new Vector3(0f, -0.85f, 0.1f);
            _motion = StartCoroutine(LerpWorld(start, end, Mathf.Max(0.01f, seconds), shrink: true));
            return _motion;
        }

        public Coroutine AnimateTimeVacuum(ShadowGarden.Core.GridPosition cell, float seconds)
        {
            EnsureVisual();
            StopMotion();
            _motion = StartCoroutine(VacuumRoutine(cell, seconds));
            return _motion;
        }

        private IEnumerator CliffFallRoutine(ShadowGarden.Core.GridPosition cell, float fallSeconds)
        {
            var start = GridWorld.ToWorld(cell, -0.38f);
            var approach = start + new Vector3(0f, -PresentationTiming.CliffApproachCells * GridWorld.CellSize, 0f);
            yield return AnimateLerp(start, approach, 0.12f, shrink: false);
            var end = approach + new Vector3(0f, -1.4f, 0.2f);
            yield return AnimateLerp(approach, end, Mathf.Max(0.01f, fallSeconds), shrink: true);
            _motion = null;
        }

        private IEnumerator VacuumRoutine(ShadowGarden.Core.GridPosition cell, float seconds)
        {
            var start = GridWorld.ToWorld(cell, -0.38f);
            playerVisual.position = start;
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var spin = t * 360f * 1.5f;
                playerVisual.localRotation = Quaternion.Euler(0f, 0f, spin);
                playerVisual.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.05f, t * t);
                playerVisual.position = start + new Vector3(Mathf.Sin(t * 12f) * 0.08f, -t * 0.4f, 0f);
                yield return null;
            }

            playerVisual.localScale = Vector3.one * 0.05f;
            _motion = null;
        }

        private IEnumerator LerpWorld(Vector3 from, Vector3 to, float seconds, bool shrink = false)
        {
            yield return AnimateLerp(from, to, seconds, shrink);
            _motion = null;
        }

        private IEnumerator AnimateLerp(Vector3 from, Vector3 to, float seconds, bool shrink)
        {
            var elapsed = 0f;
            playerVisual.position = from;
            var startScale = Vector3.one;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / seconds);
                var eased = t * t * (3f - 2f * t);
                playerVisual.position = Vector3.LerpUnclamped(from, to, eased);
                if (shrink)
                {
                    playerVisual.localScale = Vector3.Lerp(startScale, Vector3.one * 0.15f, eased);
                }

                yield return null;
            }

            playerVisual.position = to;
            if (shrink)
            {
                playerVisual.localScale = Vector3.one * 0.15f;
            }
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
        }

        private static void ApplyColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color")
                         ?? Shader.Find("Sprites/Default");
            renderer.sharedMaterial = new Material(shader) { color = color };
        }
    }
}
