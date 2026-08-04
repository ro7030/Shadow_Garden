using ShadowGarden.Core;
using TMPro;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Icon-only onboarding (no sentence tutorial popups).
    /// WASD near Moa until first move; Q/E on lamp until first rotate; R after first death-risk action.
    /// </summary>
    public sealed class OnboardingHintsPresenter : MonoBehaviour
    {
        [SerializeField] private Transform wasdRoot;
        [SerializeField] private Transform qeRoot;
        [SerializeField] private Transform rRoot;

        private bool _wasdDismissed;
        private bool _qeDismissed;
        private bool _rDismissed;
        private bool _rArmed;

        public bool WasdVisible => wasdRoot != null && wasdRoot.gameObject.activeSelf;
        public bool QeVisible => qeRoot != null && qeRoot.gameObject.activeSelf;
        public bool RVisible => rRoot != null && rRoot.gameObject.activeSelf;

        public void ResetProgress()
        {
            _wasdDismissed = false;
            _qeDismissed = false;
            _rDismissed = false;
            _rArmed = false;
            EnsureVisuals();
            SetWasd(true);
            SetQe(false);
            SetR(false);
        }

        public void NotifyMoved()
        {
            _wasdDismissed = true;
            SetWasd(false);
            _rArmed = true;
        }

        public void NotifyRotated()
        {
            _qeDismissed = true;
            SetQe(false);
            _rArmed = true;
        }

        public void NotifyResetUsed()
        {
            _rDismissed = true;
            SetR(false);
        }

        public void Tick(StageDefinition stage, StageRuntimeState state, Transform playerVisual)
        {
            EnsureVisuals();
            if (playerVisual == null || stage == null || state == null)
            {
                return;
            }

            // UI/UX §25–26: 상황형 안내는 1월드만. 2월드 이후는 소멸.
            if (!IsWorldOne(stage.StageId))
            {
                SetWasd(false);
                SetQe(false);
                SetR(false);
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

            var onLamp = stage.TryGetLampAt(state.PlayerPosition, out var lamp);
            if (!_qeDismissed && onLamp)
            {
                SetQe(true);
                if (qeRoot != null)
                {
                    var label = qeRoot.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null)
                    {
                        var glyph = MockupPalette.ChannelGlyph(lamp.Channel);
                        label.text = $"Q / E  {glyph}";
                        UiTypography.Apply(label, bold: true);
                    }
                }

                qeRoot.position = playerVisual.position + new Vector3(0f, 1.15f, -0.2f);
            }
            else
            {
                SetQe(false);
            }

            if (_rArmed && !_rDismissed && _wasdDismissed)
            {
                SetR(true);
                rRoot.position = playerVisual.position + new Vector3(0f, 1.45f, -0.2f);
            }
            else
            {
                SetR(false);
            }
        }

        private static bool IsWorldOne(string stageId)
        {
            return !string.IsNullOrWhiteSpace(stageId) && stageId.StartsWith("1-");
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

            if (rRoot == null)
            {
                rRoot = CreateWorldLabel("RHint", "R");
            }
        }

        private Transform CreateWorldLabel(string name, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var label = go.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 5.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 1f, 1f, 0.92f);
            label.rectTransform.sizeDelta = new Vector2(3f, 1f);
            UiTypography.Apply(label, bold: true);
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

        private void SetR(bool on)
        {
            if (rRoot != null)
            {
                rRoot.gameObject.SetActive(on);
            }
        }
    }
}
