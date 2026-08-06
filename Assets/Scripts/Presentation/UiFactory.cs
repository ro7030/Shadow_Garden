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
            var prefab = Resources.Load<GameObject>("Presentation/Prefabs/FinalPanel");
            var go = prefab != null
                ? Object.Instantiate(prefab)
                : new GameObject(name, typeof(RectTransform), typeof(Image));
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            var image = go.GetComponent<Image>();
            image.color = color;
            var skin = PresentationAssetLibrary.Catalog;
            if (skin?.panel != null)
            {
                image.sprite = skin.panel;
                image.type = Image.Type.Sliced;
            }
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
            tmp.textWrappingMode = TextWrappingModes.Normal;
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
            float height = UiTheme.ButtonMinHeight,
            bool forceSecondary = false)
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
                var outline = existing.GetComponent<Outline>() ?? existing.gameObject.AddComponent<Outline>();
                outline.effectColor = UiTheme.Mint;
                outline.effectDistance = new Vector2(UiTheme.FocusOutline, UiTheme.FocusOutline);
                outline.enabled = false;
                if (existing.GetComponent<UiFocusOutline>() == null)
                {
                    existing.gameObject.AddComponent<UiFocusOutline>();
                }
            }
            else
            {
                var prefab = Resources.Load<GameObject>("Presentation/Prefabs/FinalButton");
                var go = prefab != null
                    ? Object.Instantiate(prefab)
                    : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
                go.name = name;
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
                if (go.GetComponent<UiFocusOutline>() == null)
                {
                    go.AddComponent<UiFocusOutline>();
                }
            }

            var catalog = PresentationAssetLibrary.Catalog;
            var secondary = forceSecondary || IsSecondaryAction(name);
            image.sprite = secondary ? catalog?.buttonSecondary : catalog?.buttonPrimary;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = image.sprite != null ? Color.white : (secondary ? UiTheme.Ivory : UiTheme.Navy);
            var labelTmp = EnsureButtonLabel(button.transform, label);
            labelTmp.fontSize = UiTheme.ButtonFont;
            labelTmp.color = secondary ? UiTheme.NavyDeep : UiTheme.Ivory;
            UiTypography.Apply(labelTmp, bold: true);

            ApplyButtonColorTint(button, secondary);

            if (onClick != null)
            {
                // Refreshing an existing production screen is allowed to restyle its
                // controls without destroying callbacks wired by MainFlowScreens.
                // Only replace listeners when this factory call owns a callback.
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(onClick);
            }

            button.gameObject.SetActive(true);
            EnsureFocusFrame(button.transform, catalog?.buttonFocus);
            return button;
        }

        /// <summary>
        /// ColorTint multiplies the graphic color. White/secondary sprites clamp above 1,
        /// so highlight/selected must darken or mint-tint instead of brightening.
        /// </summary>
        public static void ApplyButtonColorTint(Button button, bool secondary)
        {
            if (button == null)
            {
                return;
            }

            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            if (secondary)
            {
                colors.highlightedColor = new Color(0.82f, 0.96f, 0.90f, 1f);
                colors.selectedColor = new Color(0.74f, 0.93f, 0.86f, 1f);
                colors.pressedColor = new Color(0.72f, 0.84f, 0.80f, 1f);
            }
            else
            {
                colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
                colors.selectedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
                colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            }

            colors.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.7f);
            button.colors = colors;
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
                image.color = interactable
                    ? (image.sprite != null ? Color.white : UiTheme.Navy)
                    : UiTheme.Disabled;
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

        public static Image EnsureIcon(
            Transform parent,
            string name,
            Sprite sprite,
            Vector2 size,
            Vector2 anchored = default)
        {
            var existing = parent.Find(name);
            var image = existing != null
                ? existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>()
                : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            if (existing == null) image.transform.SetParent(parent, false);
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchored;
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static bool IsSecondaryAction(string name)
        {
            return name.Contains("Settings") || name.Contains("Replay") || name.Contains("NewGame") ||
                   name.Contains("LevelSelect") || name.Contains("WorldMap") || name.Contains("Back") ||
                   name.Contains("Credits") || name.Contains("Cancel") || name.Contains("Title");
        }

        private static void EnsureFocusFrame(Transform button, Sprite sprite)
        {
            if (sprite == null) return;
            var image = EnsureIcon(button, "FocusFrame", sprite, Vector2.zero);
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-5f, -5f);
            rt.offsetMax = new Vector2(5f, 5f);
            image.type = Image.Type.Sliced;
            image.gameObject.SetActive(false);
            image.transform.SetAsFirstSibling();
        }
    }

}
