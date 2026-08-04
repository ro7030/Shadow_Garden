using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Icon-only onboarding (no sentence tutorial popups).
    /// WASD near Moa until first move; Q/E only while standing on a lamp until first rotate.
    /// </summary>
    public sealed class OnboardingHintsPresenter : MonoBehaviour
    {
        [SerializeField] private Transform wasdRoot;
        [SerializeField] private Transform qeRoot;

        private bool _wasdDismissed;
        private bool _qeDismissed;

        public bool WasdVisible => wasdRoot != null && wasdRoot.gameObject.activeSelf;
        public bool QeVisible => qeRoot != null && qeRoot.gameObject.activeSelf;

        public void ResetProgress()
        {
            _wasdDismissed = false;
            _qeDismissed = false;
            EnsureVisuals();
            SetWasd(true);
            SetQe(false);
        }

        public void NotifyMoved()
        {
            _wasdDismissed = true;
            SetWasd(false);
        }

        public void NotifyRotated()
        {
            _qeDismissed = true;
            SetQe(false);
        }

        public void Tick(StageDefinition stage, StageRuntimeState state, Transform playerVisual)
        {
            EnsureVisuals();
            if (playerVisual == null || stage == null || state == null)
            {
                return;
            }

            if (!_wasdDismissed)
            {
                SetWasd(true);
                wasdRoot.position = playerVisual.position + new Vector3(0f, 0.85f, -0.2f);
            }
            else
            {
                SetWasd(false);
            }

            var onLamp = stage.TryGetLampAt(state.PlayerPosition, out _);
            if (!_qeDismissed && onLamp)
            {
                SetQe(true);
                qeRoot.position = playerVisual.position + new Vector3(0f, 1.15f, -0.2f);
            }
            else
            {
                SetQe(false);
            }
        }

        private void EnsureVisuals()
        {
            if (wasdRoot == null)
            {
                wasdRoot = CreateWorldLabel("WasdHint", "WASD");
            }

            if (qeRoot == null)
            {
                qeRoot = CreateWorldLabel("QeHint", "Q / E");
            }
        }

        private Transform CreateWorldLabel(string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var label = go.AddComponent<TextMesh>();
            label.text = text;
            label.characterSize = 0.12f;
            label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = new Color(1f, 1f, 1f, 0.92f);
            go.SetActive(false);
            return go.transform;
        }

        private void SetWasd(bool on)
        {
            if (wasdRoot != null)
            {
                wasdRoot.gameObject.SetActive(on);
            }
        }

        private void SetQe(bool on)
        {
            if (qeRoot != null)
            {
                qeRoot.gameObject.SetActive(on);
            }
        }
    }
}
