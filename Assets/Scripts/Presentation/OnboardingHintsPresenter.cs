using ShadowGarden.Core;
using TMPro;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>Small world-space key caps for the first world; never blocks the board.</summary>
    public sealed class OnboardingHintsPresenter : MonoBehaviour
    {
        [SerializeField] private Transform wasdRoot;
        [SerializeField] private Transform qeRoot;
        [SerializeField] private Transform rRoot;

        private bool _wasdDismissed;
        private bool _qeDismissed;
        private bool _rDismissed;
        private bool _rArmed;
        private SpriteRenderer _qeChannel;

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
            _rArmed = true;
            SetWasd(false);
        }

        public void NotifyRotated()
        {
            _qeDismissed = true;
            _rArmed = true;
            SetQe(false);
        }

        public void NotifyResetUsed()
        {
            _rDismissed = true;
            SetR(false);
        }

        public void Tick(StageDefinition stage, StageRuntimeState state, Transform playerVisual)
        {
            EnsureVisuals();
            if (playerVisual == null || stage == null || state == null) return;
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
                wasdRoot.position = playerVisual.position + new Vector3(0f, 1.55f, -0.2f);
            }
            else SetWasd(false);

            var onLamp = stage.TryGetLampAt(state.PlayerPosition, out var lamp);
            if (!_qeDismissed && onLamp)
            {
                SetQe(true);
                if (_qeChannel != null)
                {
                    _qeChannel.sprite = PresentationAssetLibrary.Catalog?.GetChannelIcon(lamp.Channel);
                    _qeChannel.color = MockupPalette.ChannelColor(lamp.Channel);
                }
                qeRoot.position = playerVisual.position + new Vector3(0f, 0.84f, -0.2f);
            }
            else SetQe(false);

            if (_rArmed && !_rDismissed && _wasdDismissed)
            {
                SetR(true);
                rRoot.position = playerVisual.position + new Vector3(0f, 0.98f, -0.2f);
            }
            else SetR(false);
        }

        private void EnsureVisuals()
        {
            if (wasdRoot == null) wasdRoot = CreateWasdHint();
            if (qeRoot == null)
            {
                qeRoot = CreateQeHint();
                var icon = new GameObject("ChannelIcon");
                icon.transform.SetParent(qeRoot, false);
                icon.transform.localPosition = new Vector3(0f, -0.01f, -0.02f);
                icon.transform.localScale = Vector3.one * 0.18f;
                _qeChannel = icon.AddComponent<SpriteRenderer>();
                _qeChannel.sortingOrder = 101;
            }
            if (rRoot == null) rRoot = CreateWorldKey("RHint", "R", 0.34f);
        }

        private Transform CreateWasdHint()
        {
            var root = CreateHintRoot("WasdHint");
            AddKeyCap(root, "W", new Vector2(0f, 0.14f));
            AddKeyCap(root, "A", new Vector2(-0.19f, -0.10f));
            AddKeyCap(root, "S", new Vector2(0f, -0.10f));
            AddKeyCap(root, "D", new Vector2(0.19f, -0.10f));
            return root;
        }

        private Transform CreateQeHint()
        {
            var root = CreateHintRoot("QeHint");
            AddKeyCap(root, "Q", new Vector2(-0.27f, 0f));
            AddKeyCap(root, "E", new Vector2(0.27f, 0f));
            return root;
        }

        private Transform CreateHintRoot(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            return go.transform;
        }

        private void AddKeyCap(Transform root, string text, Vector2 position)
        {
            var key = new GameObject($"Key_{text}");
            key.transform.SetParent(root, false);
            key.transform.localPosition = new Vector3(position.x, position.y, 0f);

            var renderer = key.AddComponent<SpriteRenderer>();
            renderer.sprite = PresentationAssetLibrary.Catalog?.keyCap;
            renderer.sortingOrder = 100;
            renderer.color = new Color(1f, 1f, 1f, 0.97f);
            var bounds = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
            key.transform.localScale = new Vector3(
                bounds.x > 0f ? 0.18f / bounds.x : 0.18f,
                bounds.y > 0f ? 0.18f / bounds.y : 0.18f,
                1f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(root, false);
            textGo.transform.localPosition = new Vector3(position.x, position.y + 0.008f, -0.03f);
            var label = textGo.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 1.9f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UiTheme.NavyDeep;
            label.rectTransform.sizeDelta = new Vector2(0.25f, 0.22f);
            label.sortingOrder = 102;
            UiTypography.Apply(label, bold: true);
        }

        private Transform CreateWorldKey(string name, string text, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var background = new GameObject("KeyCap");
            background.transform.SetParent(go.transform, false);
            var renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = PresentationAssetLibrary.Catalog?.keyCap;
            renderer.sortingOrder = 100;
            renderer.color = new Color(1f, 1f, 1f, 0.96f);
            var bounds = renderer.sprite != null ? renderer.sprite.bounds.size : Vector3.one;
            background.transform.localScale = new Vector3(
                bounds.x > 0f ? width / bounds.x : width,
                bounds.y > 0f ? 0.28f / bounds.y : 0.28f,
                1f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.015f, -0.03f);
            var label = textGo.AddComponent<TextMeshPro>();
            label.text = text;
            label.fontSize = 2.35f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = UiTheme.NavyDeep;
            label.rectTransform.sizeDelta = new Vector2(width * 1.4f, 0.5f);
            label.sortingOrder = 102;
            UiTypography.Apply(label, bold: true);
            go.SetActive(false);
            return go.transform;
        }

        private static bool IsWorldOne(string stageId) =>
            !string.IsNullOrWhiteSpace(stageId) && stageId.StartsWith("1-");

        private void SetWasd(bool value) { if (wasdRoot != null) wasdRoot.gameObject.SetActive(value); }
        private void SetQe(bool value) { if (qeRoot != null) qeRoot.gameObject.SetActive(value); }
        private void SetR(bool value) { if (rRoot != null) rRoot.gameObject.SetActive(value); }
    }
}
