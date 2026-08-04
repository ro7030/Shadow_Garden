using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Shared uGUI builders for production WebGL UI (44px buttons, 3px mint focus).
    /// </summary>
    public static class UiFactory
    {
        public static CanvasScaler ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(UiTheme.ReferenceWidth, UiTheme.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UiTheme.Match;
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            return scaler;
        }

        public static RectTransform StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image CreatePanel(Transform parent, string name, Color color, Vector2 size, Vector2 anchored)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static TextMeshProUGUI CreateLabel(
            Transform parent,
            string name,
            string text,
            Vector2 anchored,
            Vector2 size,
            float fontSize,
            bool bold,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var existing = parent.Find(name);
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                tmp = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
            }

            var rt = tmp.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            tmp.text = text;
            tmp.fontSize = Mathf.Max(fontSize, UiTheme.BodyFontMin);
            tmp.alignment = align;
            tmp.color = UiTheme.TextPrimary;
            tmp.raycastTarget = false;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            UiTypography.Apply(tmp, bold);
            return tmp;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchored,
            UnityAction onClick,
            float width = UiTheme.ButtonWidth,
            float height = UiTheme.ButtonMinHeight)
        {
            height = Mathf.Max(height, UiTheme.ButtonMinHeight);
            var existing = parent.Find(name);
            Button button;
            Image image;
            if (existing != null)
            {
                button = existing.GetComponent<Button>() ?? existing.gameObject.AddComponent<Button>();
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
                var rt = existing.GetComponent<RectTransform>();
                rt.anchoredPosition = anchored;
                rt.sizeDelta = new Vector2(width, height);
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(width, height);
                rt.anchoredPosition = anchored;
                image = go.GetComponent<Image>();
                button = go.GetComponent<Button>();
                var outline = go.GetComponent<Outline>();
                outline.effectColor = UiTheme.Mint;
                outline.effectDistance = new Vector2(UiTheme.FocusOutline, UiTheme.FocusOutline);
                outline.enabled = false;
                go.AddComponent<UiFocusOutline>();
            }

            image.color = UiTheme.Navy;
            var labelTmp = EnsureButtonLabel(button.transform, label);
            labelTmp.fontSize = UiTheme.ButtonFont;
            UiTypography.Apply(labelTmp, bold: true);

            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.7f);
            button.colors = colors;

            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            button.gameObject.SetActive(true);
            return button;
        }

        public static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = interactable ? UiTheme.Navy : UiTheme.Disabled;
            }
        }

        public static void Select(Button button)
        {
            if (button == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private static TextMeshProUGUI EnsureButtonLabel(Transform button, string label)
        {
            var existing = button.Find("Label");
            TextMeshProUGUI tmp;
            if (existing != null)
            {
                tmp = existing.GetComponent<TextMeshProUGUI>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                var go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(button, false);
                tmp = go.AddComponent<TextMeshProUGUI>();
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = UiTheme.Ivory;
            tmp.raycastTarget = false;
            return tmp;
        }
    }

    /// <summary>Shows mint Outline while EventSystem selection points at this button.</summary>
    public sealed class UiFocusOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private Outline _outline;

        private void Awake()
        {
            _outline = GetComponent<Outline>();
        }

        private void OnEnable()
        {
            Sync();
        }

        public void OnSelect(BaseEventData eventData) => Set(true);

        public void OnDeselect(BaseEventData eventData) => Set(false);

        private void Sync()
        {
            var selected = EventSystem.current != null &&
                           EventSystem.current.currentSelectedGameObject == gameObject;
            Set(selected);
        }

        private void Set(bool on)
        {
            if (_outline == null)
            {
                _outline = GetComponent<Outline>();
            }

            if (_outline != null)
            {
                _outline.enabled = on;
            }
        }
    }
}
