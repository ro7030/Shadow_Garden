using ShadowGarden.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Idempotent final layout for the production flow screens. Navigation and copy stay in
    /// MainFlowScreens; this component owns hierarchy, anchoring, and visual layering only.
    /// </summary>
    public sealed class MainScreenArtDecorator : MonoBehaviour
    {
        private MainCompositionRoot _main;
        private AppScreenRouter _router;

        public void Bind(MainCompositionRoot main, AppScreenRouter router)
        {
            _main = main;
            _router = router;
            DecorateBackgrounds();
            ApplyLayout(_main != null ? _main.CurrentState : AppState.Title);
        }

        public void ApplyLayout(AppState state)
        {
            if (_router == null)
            {
                return;
            }

            DecorateBackgrounds();
            switch (state)
            {
                case AppState.Title:
                    LayoutTitle();
                    break;
                case AppState.Opening:
                    LayoutOpening();
                    break;
                case AppState.WorldMap:
                    LayoutWorldMap();
                    break;
                case AppState.GameOver:
                    LayoutModal(_router.GameOverRoot, danger: true);
                    break;
                case AppState.Cleared:
                    LayoutModal(_router.ClearedRoot, danger: false);
                    break;
                case AppState.Ending:
                    LayoutEnding();
                    break;
            }

            Canvas.ForceUpdateCanvases();
        }

        private void DecorateBackgrounds()
        {
            var worldOne = PresentationAssetLibrary.ForStage("1-1");
            DecorateBackground(_router?.TitleRoot, worldOne?.background, 0.28f);
            DecorateBackground(_router?.OpeningRoot, worldOne?.background, 0.48f);
            DecorateBackground(_router?.WorldMapRoot, worldOne?.background, 0.56f);
            DecorateBackground(_router?.EndingRoot, PresentationAssetLibrary.ForStage("3-4")?.background, 0.38f);
        }

        private void LayoutTitle()
        {
            var root = _router?.TitleRoot;
            if (root == null) return;

            var artLayer = EnsureFullLayer(root.transform, "CharacterArtLayer", 2);
            var portrait = EnsureImage(artLayer, "MoaTitleArt", PresentationAssetLibrary.Catalog?.moa?.holdSeed,
                new Vector2(620f, 760f), new Vector2(430f, -20f));
            portrait.preserveAspect = true;

            var panel = EnsurePanel(root.transform, "TitleNotePanel", new Vector2(650f, 870f), new Vector2(-430f, 0f));
            panel.color = new Color(1f, 1f, 1f, 0.96f);
            var header = EnsureFullLayer(panel.transform, "HeaderRoot", 0);
            var content = EnsureFullLayer(panel.transform, "ContentRoot", 1);
            var buttons = EnsureButtonGroup(panel.transform, "ButtonGroup", new Vector2(360f, 300f), new Vector2(0f, -45f));

            MoveInto(root.transform, "TitleLabel", "Label", header, new Vector2(0f, 280f), new Vector2(540f, 92f), 64f);
            var concept = FindDescendant(root.transform, "ConceptLabel");
            if (concept != null)
            {
                concept.gameObject.SetActive(false);
            }

            MoveInto(root.transform, "ProgressLabel", null, content, new Vector2(0f, -245f), new Vector2(520f, 52f), 18f);
            ArrangeButtons(root.transform, buttons, "ContinueButton", "NewGameButton", "ReplayOpeningButton", "SettingsButton");
            panel.transform.SetAsLastSibling();
        }

        private void LayoutOpening()
        {
            var root = _router?.OpeningRoot;
            if (root == null) return;

            var artLayer = EnsureFullLayer(root.transform, "CharacterArtLayer", 2);
            var portrait = EnsureImage(artLayer, "OpeningMoa", PresentationAssetLibrary.Catalog?.moa?.observe,
                new Vector2(520f, 650f), new Vector2(-520f, -40f));
            portrait.preserveAspect = true;

            var panel = EnsurePanel(root.transform, "OpeningNotePanel", new Vector2(900f, 760f), new Vector2(250f, 0f));
            var header = EnsureFullLayer(panel.transform, "HeaderRoot", 0);
            var content = EnsureFullLayer(panel.transform, "ContentRoot", 1);
            var buttons = EnsureButtonGroup(panel.transform, "ButtonGroup", new Vector2(380f, 144f), new Vector2(0f, -205f));

            MoveInto(root.transform, "TitleLabel", "Label", header, new Vector2(0f, 285f), new Vector2(720f, 70f), 44f);
            MoveInto(root.transform, "OpeningPageLabel", null, content, new Vector2(0f, 205f), new Vector2(640f, 40f), 18f);
            MoveInto(root.transform, "OpeningBody", null, content, new Vector2(0f, 65f), new Vector2(720f, 220f), 24f);
            MoveInto(root.transform, "SkipHoldGauge", null, content, new Vector2(0f, -305f), new Vector2(360f, 52f), null);
            ArrangeButtons(root.transform, buttons, "ContinueButton", "SkipButton");
            panel.transform.SetAsLastSibling();
        }

        private void LayoutWorldMap()
        {
            var root = _router?.WorldMapRoot;
            if (root == null) return;
            var header = EnsurePanel(root.transform, "WorldMapHeader", new Vector2(520f, 86f), new Vector2(0f, 430f));
            header.color = new Color(1f, 1f, 1f, 0.92f);
            Move(root.transform, "TitleLabel", "Label", new Vector2(0f, 430f), new Vector2(460f, 64f), 40f);
        }

        private void LayoutEnding()
        {
            var root = _router?.EndingRoot;
            if (root == null) return;

            var beatLayer = EnsureFullLayer(root.transform, "EndingBeatLayer", 1);
            var worldA = EnsureImage(beatLayer, "EndingWorldA", PresentationAssetLibrary.ForStage("1-1")?.background,
                Vector2.zero, Vector2.zero);
            Stretch(worldA.rectTransform);
            worldA.preserveAspect = false;
            var worldB = EnsureImage(beatLayer, "EndingWorldB", PresentationAssetLibrary.ForStage("2-1")?.background,
                Vector2.zero, Vector2.zero);
            Stretch(worldB.rectTransform);
            worldB.color = new Color(1f, 1f, 1f, 0f);
            var flower = EnsureImage(beatLayer, "EndingFlower", PresentationAssetLibrary.ForStage("1-1")?.flowerBloom,
                new Vector2(220f, 220f), new Vector2(-420f, -40f));
            flower.preserveAspect = true;
            flower.color = new Color(1f, 1f, 1f, 0f);

            var artLayer = EnsureFullLayer(root.transform, "CharacterArtLayer", 2);
            var portrait = EnsureImage(artLayer, "EndingMoa", PresentationAssetLibrary.Catalog?.moa?.relieved,
                new Vector2(410f, 500f), new Vector2(470f, -105f));
            portrait.preserveAspect = true;
            var vfx = EnsureImage(artLayer, "EndingVfx", PresentationAssetLibrary.Catalog?.gameplayFx?.completionGlow,
                new Vector2(520f, 520f), new Vector2(470f, -40f));
            vfx.preserveAspect = true;
            vfx.color = new Color(1f, 1f, 1f, 0f);

            var panel = EnsurePanel(root.transform, "EndingNotePanel", new Vector2(850f, 760f), new Vector2(-230f, 0f));
            var header = EnsureFullLayer(panel.transform, "HeaderRoot", 0);
            var content = EnsureFullLayer(panel.transform, "ContentRoot", 1);
            var buttons = EnsureButtonGroup(panel.transform, "ButtonGroup", new Vector2(360f, 144f), new Vector2(0f, -225f));

            MoveInto(root.transform, "TitleLabel", "Label", header, new Vector2(0f, 285f), new Vector2(700f, 70f), 48f);
            MoveInto(root.transform, "EndingBody", null, content, new Vector2(0f, 155f), new Vector2(700f, 90f), 24f);
            var credits = FindDescendant(root.transform, "CreditsLabel");
            if (credits != null)
            {
                credits.gameObject.SetActive(false);
            }

            ArrangeButtons(root.transform, buttons, "WorldMapButton", "TitleButton");
            panel.transform.SetAsLastSibling();
        }

        private void LayoutModal(GameObject root, bool danger)
        {
            if (root == null) return;
            var world = PresentationAssetLibrary.ForStage(_main?.PendingStageId ?? "1-1");
            DecorateBackground(root, world?.background, danger ? 0.72f : 0.58f);

            var panel = EnsurePanel(root.transform, danger ? "GameOverNotePanel" : "ClearedNotePanel",
                new Vector2(820f, 650f), Vector2.zero);
            panel.color = danger
                ? new Color(0.95f, 0.72f, 0.76f, 0.96f)
                : new Color(1f, 1f, 1f, 0.98f);
            var header = EnsureFullLayer(panel.transform, "HeaderRoot", 0);
            var content = EnsureFullLayer(panel.transform, "ContentRoot", 1);
            var buttons = EnsureButtonGroup(panel.transform, "ButtonGroup", new Vector2(360f, 150f), new Vector2(0f, -135f));

            MoveInto(root.transform, "TitleLabel", "Label", header, new Vector2(0f, 235f), new Vector2(700f, 70f), 46f);
            MoveInto(root.transform, danger ? "ReasonLabel" : "DetailLabel", null, content,
                new Vector2(0f, 35f), new Vector2(700f, 110f), 22f);
            var icon = EnsureImage(content, "ModalIcon",
                danger ? PresentationAssetLibrary.Catalog?.iconDanger : PresentationAssetLibrary.Catalog?.iconCheck,
                new Vector2(76f, 76f), new Vector2(0f, 145f));
            icon.color = danger ? UiTheme.Coral : UiTheme.Mint;
            icon.preserveAspect = true;
            ArrangeAllModalButtons(root.transform, buttons);
            panel.transform.SetAsLastSibling();
        }

        private static void ArrangeButtons(Transform screenRoot, RectTransform group, params string[] names)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var found = FindDescendant(screenRoot, names[i]);
                if (found == null) continue;
                found.SetParent(group, false);
                found.SetSiblingIndex(i);
                var rt = found as RectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(Mathf.Max(288f, rt.sizeDelta.x), Mathf.Max(56f, rt.sizeDelta.y));
            }
        }

        private static void ArrangeAllModalButtons(Transform screenRoot, RectTransform group)
        {
            var order = new[] { "RetryButton", "NextButton", "EndingButton", "WorldMapButton" };
            ArrangeButtons(screenRoot, group, order);
        }

        private static RectTransform EnsureButtonGroup(Transform panel, string name, Vector2 size, Vector2 position)
        {
            var rt = EnsureRect(panel, name);
            Center(rt, size, position);
            var layout = rt.GetComponent<VerticalLayoutGroup>() ?? rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 16f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return rt;
        }

        private static RectTransform EnsureFullLayer(Transform parent, string name, int siblingIndex)
        {
            var rt = EnsureRect(parent, name);
            Stretch(rt);
            rt.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            return rt;
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var found = FindDescendant(parent, name);
            if (found != null)
            {
                if (found.parent != parent) found.SetParent(parent, false);
                return found as RectTransform ?? found.gameObject.AddComponent<RectTransform>();
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void DecorateBackground(GameObject root, Sprite sprite, float shadeAlpha)
        {
            if (root == null) return;
            var background = EnsureImage(root.transform, "FinalBackground", sprite, Vector2.zero, Vector2.zero);
            Stretch(background.rectTransform);
            background.color = Color.white;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();
            var shade = EnsureImage(root.transform, "FinalShade", null, Vector2.zero, Vector2.zero);
            Stretch(shade.rectTransform);
            shade.color = new Color(UiTheme.NavyDeep.r, UiTheme.NavyDeep.g, UiTheme.NavyDeep.b, shadeAlpha);
            shade.raycastTarget = false;
            shade.transform.SetSiblingIndex(Mathf.Min(1, root.transform.childCount - 1));
        }

        private static Image EnsurePanel(Transform root, string name, Vector2 size, Vector2 position)
        {
            var existing = FindDescendant(root, name);
            Image image;
            if (existing != null)
            {
                if (existing.parent != root) existing.SetParent(root, false);
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                image = go.GetComponent<Image>();
            }

            image.sprite = PresentationAssetLibrary.Catalog?.panel;
            image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;
            Center(image.rectTransform, size, position);
            return image;
        }

        private static Image EnsureImage(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 position)
        {
            var existing = FindDescendant(parent, name);
            Image image;
            if (existing != null)
            {
                if (existing.parent != parent) existing.SetParent(parent, false);
                image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            }
            else
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(parent, false);
                image = go.GetComponent<Image>();
            }

            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            Center(image.rectTransform, size, position);
            return image;
        }

        private static void MoveInto(
            Transform root,
            string primaryName,
            string fallbackName,
            Transform parent,
            Vector2 position,
            Vector2 size,
            float? fontSize)
        {
            var found = FindDescendant(root, primaryName) ??
                        (!string.IsNullOrEmpty(fallbackName) ? FindDescendant(root, fallbackName) : null);
            if (found == null) return;
            found.SetParent(parent, false);
            var rt = found as RectTransform;
            if (rt != null) Center(rt, size, position);
            var text = found.GetComponent<TextMeshProUGUI>();
            if (text != null && fontSize.HasValue) text.fontSize = fontSize.Value;
        }

        private static void Move(
            Transform root,
            string primaryName,
            string fallbackName,
            Vector2 position,
            Vector2 size,
            float fontSize)
        {
            var found = FindDescendant(root, primaryName) ?? FindDescendant(root, fallbackName);
            if (found == null) return;
            var rt = found as RectTransform;
            if (rt != null) Center(rt, size, position);
            var text = found.GetComponent<TextMeshProUGUI>();
            if (text != null) text.fontSize = fontSize;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void Center(RectTransform rt, Vector2 size, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            rt.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
