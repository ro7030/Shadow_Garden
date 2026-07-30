using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// 시험의 정원 board: cream tiles, deep navy shadows, pulsing ×2 abyss, brass lamps.
    /// </summary>
    public sealed class BoardPresenter : MonoBehaviour
    {
        [SerializeField] private Transform cellRoot;
        [SerializeField] private bool showDebugCounts;

        private SpriteRenderer[,] _cells;
        private SpriteRenderer[,] _overlays;
        private TextMesh[,] _labels;
        private TextMesh[,] _arrows;
        private Transform[,] _pillars;
        private GridSize _size;
        private Sprite _quad;
        private Sprite _softQuad;
        private readonly System.Collections.Generic.List<(Transform t, Vector3 baseScale)> _pulseTargets =
            new System.Collections.Generic.List<(Transform, Vector3)>();

        public void Build(StageDefinition stage)
        {
            _size = stage.BoardSize;
            Clear();
            EnsureRoot();
            _quad = CreateRoundedSprite(0.18f);
            _softQuad = CreateRoundedSprite(0.28f);
            _cells = new SpriteRenderer[_size.Width, _size.Height];
            _overlays = new SpriteRenderer[_size.Width, _size.Height];
            _labels = new TextMesh[_size.Width, _size.Height];
            _arrows = new TextMesh[_size.Width, _size.Height];
            _pillars = new Transform[_size.Width, _size.Height];
            _pulseTargets.Clear();

            BuildSkyBackdrop();
            BuildBoardFrame();

            for (var y = 0; y < _size.Height; y++)
            {
                for (var x = 0; x < _size.Width; x++)
                {
                    var pos = new GridPosition(x, y);
                    var cell = new GameObject($"Cell_{x}_{y}");
                    cell.transform.SetParent(cellRoot, false);
                    cell.transform.localPosition = GridWorld.ToWorld(pos);
                    cell.transform.localScale = Vector3.one * 0.90f;

                    var baseRenderer = cell.AddComponent<SpriteRenderer>();
                    baseRenderer.sprite = _quad;
                    baseRenderer.sortingOrder = 0;
                    _cells[x, y] = baseRenderer;

                    var overlayGo = new GameObject("Overlay");
                    overlayGo.transform.SetParent(cell.transform, false);
                    overlayGo.transform.localScale = Vector3.one * 0.96f;
                    var overlay = overlayGo.AddComponent<SpriteRenderer>();
                    overlay.sprite = _softQuad;
                    overlay.sortingOrder = 1;
                    overlay.enabled = false;
                    _overlays[x, y] = overlay;

                    var label = CreateLabel(cell.transform, "Label", 0.16f, new Vector3(0f, -0.26f, -0.05f));
                    _labels[x, y] = label;

                    var arrow = CreateLabel(cell.transform, "Arrow", 0.22f, new Vector3(0f, 0.20f, -0.05f));
                    arrow.fontSize = 48;
                    _arrows[x, y] = arrow;

                    if (stage.IsPillar(pos))
                    {
                        _pillars[x, y] = CreatePillar(cell.transform, FindPillar(stage, pos));
                    }
                }
            }
        }

        public void Render(StageDefinition stage, ShadowGridResult shadows, StageRuntimeState state)
        {
            if (_cells == null)
            {
                Build(stage);
            }

            _pulseTargets.Clear();

            for (var y = 0; y < _size.Height; y++)
            {
                for (var x = 0; x < _size.Width; x++)
                {
                    var pos = new GridPosition(x, y);
                    var kind = CellClassifier.Classify(stage, shadows, pos);
                    var cell = _cells[x, y];
                    var overlay = _overlays[x, y];
                    var label = _labels[x, y];
                    var arrow = _arrows[x, y];

                    cell.transform.localScale = Vector3.one * 0.90f;
                    cell.color = BaseColor(stage, pos, kind);
                    overlay.enabled = false;
                    label.text = string.Empty;
                    arrow.text = string.Empty;

                    if (stage.IsPillar(pos))
                    {
                        if (TryFindPillar(stage, pos, out var pillar))
                        {
                            label.text = MockupPalette.ChannelGlyph(pillar.Channel);
                            label.color = MockupPalette.ChannelColor(pillar.Channel);
                            label.transform.localPosition = new Vector3(0f, -0.38f, -0.05f);
                        }

                        continue;
                    }

                    if (stage.IsLamp(pos) && stage.TryGetLampAt(pos, out var lamp))
                    {
                        cell.color = MockupPalette.LampGold;
                        overlay.enabled = true;
                        overlay.color = new Color(
                            MockupPalette.LampBrass.r,
                            MockupPalette.LampBrass.g,
                            MockupPalette.LampBrass.b,
                            0.35f);
                        var direction = state.DirectionByChannel[lamp.Channel];
                        arrow.text = MockupPalette.DirectionArrow(direction);
                        arrow.color = MockupPalette.ChannelColor(lamp.Channel);
                        label.text = MockupPalette.ChannelGlyph(lamp.Channel);
                        label.color = MockupPalette.ChannelColor(lamp.Channel);
                        continue;
                    }

                    if (stage.IsGoal(pos))
                    {
                        cell.color = stage.ClearGoalType == ClearGoalType.ExitDoor
                            ? MockupPalette.ExitCyan
                            : MockupPalette.NightFlower;
                        label.text = stage.ClearGoalType == ClearGoalType.ExitDoor ? "⌂" : "❀";
                        label.color = Color.white;
                        label.characterSize = 0.22f;
                        label.transform.localPosition = new Vector3(0f, 0.02f, -0.05f);
                        continue;
                    }

                    if (kind == CellKind.SingleShadow)
                    {
                        cell.color = MockupPalette.SingleShadow;
                        if (showDebugCounts)
                        {
                            label.text = "1";
                            label.color = new Color(1f, 1f, 1f, 0.55f);
                        }
                    }
                    else if (kind == CellKind.OverlapHazard)
                    {
                        cell.color = MockupPalette.OverlapHazard;
                        overlay.enabled = true;
                        overlay.color = new Color(
                            MockupPalette.OverlapCoral.r,
                            MockupPalette.OverlapCoral.g,
                            MockupPalette.OverlapCoral.b,
                            0.42f);
                        label.text = "×2";
                        label.color = new Color(1f, 0.78f, 0.72f, 1f);
                        label.characterSize = 0.18f;
                        _pulseTargets.Add((cell.transform, Vector3.one * 0.90f));
                    }
                    else if (kind == CellKind.Cliff)
                    {
                        cell.color = MockupPalette.Cliff;
                        overlay.enabled = true;
                        overlay.color = MockupPalette.CliffRim;
                        overlay.transform.localScale = Vector3.one * 1.05f;
                    }
                    else if (stage.IsAlwaysSafe(pos))
                    {
                        cell.color = MockupPalette.SafeTerrain;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (_pulseTargets.Count == 0)
            {
                return;
            }

            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.045f;
            var alphaPulse = 0.38f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.10f;
            for (var i = 0; i < _pulseTargets.Count; i++)
            {
                var (t, baseScale) = _pulseTargets[i];
                if (t == null)
                {
                    continue;
                }

                t.localScale = baseScale * pulse;
                var overlay = t.Find("Overlay");
                if (overlay != null)
                {
                    var sr = overlay.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.enabled)
                    {
                        var c = sr.color;
                        c.a = alphaPulse;
                        sr.color = c;
                    }
                }
            }
        }

        public void SetShowDebugCounts(bool enabled)
        {
            showDebugCounts = enabled;
        }

        private void BuildSkyBackdrop()
        {
            var sky = new GameObject("SkyBackdrop");
            sky.transform.SetParent(cellRoot, false);
            sky.transform.localPosition = GridWorld.BoardCenter(_size) + new Vector3(0f, 0.2f, 1.2f);
            sky.transform.localScale = new Vector3(_size.Width + 6.5f, _size.Height + 5.5f, 1f);
            var renderer = sky.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateGradientSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = -5;
        }

        private void BuildBoardFrame()
        {
            var voidPad = new GameObject("BoardVoidPad");
            voidPad.transform.SetParent(cellRoot, false);
            voidPad.transform.localPosition = GridWorld.BoardCenter(_size) + new Vector3(0f, 0f, 0.7f);
            voidPad.transform.localScale = new Vector3(_size.Width + 0.55f, _size.Height + 0.55f, 1f);
            var voidRenderer = voidPad.AddComponent<SpriteRenderer>();
            voidRenderer.sprite = _quad;
            voidRenderer.color = MockupPalette.BoardVoid;
            voidRenderer.sortingOrder = -3;

            var frame = new GameObject("BoardFrame");
            frame.transform.SetParent(cellRoot, false);
            frame.transform.localPosition = GridWorld.BoardCenter(_size) + new Vector3(0f, 0f, 0.85f);
            frame.transform.localScale = new Vector3(_size.Width + 0.85f, _size.Height + 0.85f, 1f);
            var renderer = frame.AddComponent<SpriteRenderer>();
            renderer.sprite = _softQuad;
            renderer.color = MockupPalette.BoardFrame;
            renderer.sortingOrder = -4;
        }

        private Transform CreatePillar(Transform parent, PillarDefinition pillar)
        {
            var heightScale = pillar.Height switch
            {
                PillarHeight.Low => 0.48f,
                PillarHeight.Medium => 0.82f,
                PillarHeight.High => 1.22f,
                _ => 0.82f
            };

            var root = new GameObject("Pillar");
            root.transform.SetParent(parent, false);

            var basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlate.name = "Base";
            basePlate.transform.SetParent(root.transform, false);
            basePlate.transform.localPosition = new Vector3(0f, -0.12f, -0.15f);
            basePlate.transform.localScale = new Vector3(0.42f, 0.06f, 0.42f);
            Object.Destroy(basePlate.GetComponent<Collider>());
            ApplyMat(basePlate, MockupPalette.LampBrass);

            var shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(root.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.12f * heightScale, -0.2f);
            shaft.transform.localScale = new Vector3(0.28f, 0.38f * heightScale, 0.28f);
            Object.Destroy(shaft.GetComponent<Collider>());
            ApplyMat(shaft, MockupPalette.PillarStone);

            var cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Cap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.12f * heightScale + 0.22f * heightScale, -0.22f);
            cap.transform.localScale = new Vector3(0.22f, 0.14f, 0.22f);
            Object.Destroy(cap.GetComponent<Collider>());
            ApplyMat(cap, MockupPalette.ChannelColor(pillar.Channel) * 0.85f);

            return root.transform;
        }

        private static void ApplyMat(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            var mat = new Material(FindUnlit()) { color = color };
            renderer.sharedMaterial = mat;
        }

        private static TextMesh CreateLabel(Transform parent, string name, float characterSize, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var text = go.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = characterSize;
            text.fontSize = 36;
            text.color = Color.white;
            return text;
        }

        private void EnsureRoot()
        {
            if (cellRoot != null)
            {
                return;
            }

            var root = new GameObject("Cells");
            root.transform.SetParent(transform, false);
            cellRoot = root.transform;
        }

        private void Clear()
        {
            _pulseTargets.Clear();
            if (cellRoot == null)
            {
                return;
            }

            for (var i = cellRoot.childCount - 1; i >= 0; i--)
            {
                var child = cellRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }

        private static Color BaseColor(StageDefinition stage, GridPosition pos, CellKind kind)
        {
            if (stage.IsAlwaysSafe(pos))
            {
                return MockupPalette.SafeTerrain;
            }

            return kind switch
            {
                CellKind.Cliff => MockupPalette.Cliff,
                _ => MockupPalette.BoardVoid
            };
        }

        private static PillarDefinition FindPillar(StageDefinition stage, GridPosition pos)
        {
            TryFindPillar(stage, pos, out var pillar);
            return pillar;
        }

        private static bool TryFindPillar(StageDefinition stage, GridPosition pos, out PillarDefinition pillar)
        {
            foreach (var candidate in stage.Pillars)
            {
                if (candidate.Position == pos)
                {
                    pillar = candidate;
                    return true;
                }
            }

            pillar = null;
            return false;
        }

        private static Sprite CreateRoundedSprite(float cornerRatio)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var corner = size * cornerRatio;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Abs(x - center);
                    var dy = Mathf.Abs(y - center);
                    var inside =
                        (dx <= center - corner || dy <= center - corner) ||
                        ((dx - (center - corner)) * (dx - (center - corner)) +
                         (dy - (center - corner)) * (dy - (center - corner)) <= corner * corner);
                    texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreateGradientSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                var t = y / (float)(size - 1);
                var c = Color.Lerp(MockupPalette.SoftSkyDeep, MockupPalette.SoftSky, t);
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, c);
                }
            }

            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Shader FindUnlit()
        {
            return Shader.Find("Universal Render Pipeline/Unlit")
                   ?? Shader.Find("Unlit/Color")
                   ?? Shader.Find("Sprites/Default");
        }
    }
}
