using System.Collections;
using System.Collections.Generic;
using ShadowGarden.Core;
using TMPro;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    /// <summary>
    /// Pure presentation of a solved board. It consumes Core state and shared authored sprites;
    /// it never creates textures/materials or recomputes shadow rules.
    /// </summary>
    public sealed class BoardPresenter : MonoBehaviour
    {
        private const int BackgroundOrder = -100;
        private const int FrameOrder = -40;
        private const int BoardOrder = 0;
        private const int ShadowOrder = 20;
        private const int ObjectOrder = 40;
        private const int MarkOrder = 60;
        private const int FrontDecorOrder = 82;
        private static readonly Vector3[] GlyphBoldOffsets =
        {
            new(-0.055f, 0f, 0f), new(0.055f, 0f, 0f),
            new(0f, -0.055f, 0f), new(0f, 0.055f, 0f)
        };

        [SerializeField] private Transform cellRoot;
        [SerializeField] private bool showDebugCounts;

        private SpriteRenderer[,] _cells;
        private SpriteRenderer[,] _overlays;
        private SpriteRenderer[,] _objects;
        private SpriteRenderer[,] _channelMarks;
        private SpriteRenderer[,] _directionMarks;
        private TextMeshPro[,] _debugLabels;
        private Transform[,] _pillars;
        private Vector3[,] _pillarBaseScales;
        private GridSize _size;
        private InGameAssetCatalogAsset _catalog;
        private WorldArtSetAsset _world;
        private Sprite _fallbackSprite;
        private SpriteRenderer _goalRenderer;
        private SpriteRenderer _environmentReactionRenderer;
        private Coroutine _environmentReactionRoutine;
        private readonly List<(Transform target, Vector3 baseScale)> _pulseTargets = new();
        private bool _reduceMotion;

        public void SetReduceMotion(bool enabled) => _reduceMotion = enabled;
        public void SetShowDebugCounts(bool enabled) => showDebugCounts = enabled;

        public void Build(StageDefinition stage)
        {
            if (stage == null) return;
            _size = stage.BoardSize;
            _catalog = PresentationAssetLibrary.Catalog;
            _world = PresentationAssetLibrary.ForStage(stage.StageId);
            _fallbackSprite = ResolveFallbackSprite();
            Clear();
            EnsureRoot();

            _cells = new SpriteRenderer[_size.Width, _size.Height];
            _overlays = new SpriteRenderer[_size.Width, _size.Height];
            _objects = new SpriteRenderer[_size.Width, _size.Height];
            _channelMarks = new SpriteRenderer[_size.Width, _size.Height];
            _directionMarks = new SpriteRenderer[_size.Width, _size.Height];
            _debugLabels = new TextMeshPro[_size.Width, _size.Height];
            _pillars = new Transform[_size.Width, _size.Height];
            _pillarBaseScales = new Vector3[_size.Width, _size.Height];
            _pulseTargets.Clear();
            _goalRenderer = null;
            _environmentReactionRenderer = null;

            BuildEnvironment();
            for (var y = 0; y < _size.Height; y++)
            {
                for (var x = 0; x < _size.Width; x++)
                {
                    BuildCell(stage, new GridPosition(x, y));
                }
            }
        }

        public void Render(StageDefinition stage, ShadowGridResult shadows, StageRuntimeState state)
        {
            if (stage == null || shadows == null || state == null) return;
            if (_cells == null || _size.Width != stage.BoardSize.Width || _size.Height != stage.BoardSize.Height)
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
                    var baseRenderer = _cells[x, y];
                    var overlay = _overlays[x, y];
                    var objectRenderer = _objects[x, y];
                    var channel = _channelMarks[x, y];
                    var arrow = _directionMarks[x, y];
                    var debug = _debugLabels[x, y];

                    baseRenderer.sprite = BaseSprite(stage, pos);
                    baseRenderer.color = _world != null ? _world.safeTint : Color.white;
                    overlay.enabled = false;
                    overlay.color = Color.white;
                    if (debug != null)
                    {
                        debug.text = string.Empty;
                        debug.gameObject.SetActive(false);
                    }

                    if (stage.IsPillar(pos))
                    {
                        continue;
                    }

                    if (stage.IsLamp(pos) && stage.TryGetLampAt(pos, out var lamp))
                    {
                        if (objectRenderer != null)
                        {
                            objectRenderer.sprite = _catalog != null ? _catalog.lampBody : _fallbackSprite;
                            objectRenderer.color = Color.white;
                        }

                        if (channel != null)
                        {
                            ApplyBoldChannelGlyph(channel, ChannelSprite(lamp.Channel),
                                MockupPalette.ChannelColor(lamp.Channel));
                            channel.transform.localPosition = LampGlyphPosition;
                            channel.transform.localScale = Vector3.one * 0.38f;
                        }

                        if (arrow != null)
                        {
                            arrow.sprite = _catalog != null ? _catalog.lampArrow : null;
                            arrow.color = MockupPalette.ChannelColor(lamp.Channel);
                            arrow.transform.localRotation = DirectionRotation(state.DirectionByChannel[lamp.Channel]);
                            arrow.transform.localPosition = LampArrowPosition(state.DirectionByChannel[lamp.Channel]);
                            arrow.transform.localScale = Vector3.one * 0.46f;
                            arrow.enabled = arrow.sprite != null;
                        }

                        continue;
                    }

                    if (stage.IsGoal(pos))
                    {
                        if (objectRenderer != null)
                        {
                            objectRenderer.sprite = stage.ClearGoalType == ClearGoalType.ExitDoor
                                ? _world?.doorClosed ?? _fallbackSprite
                                : _world?.flowerClosed ?? _fallbackSprite;
                            objectRenderer.color = Color.white;
                        }

                        continue;
                    }

                    switch (kind)
                    {
                        case CellKind.SingleShadow:
                            overlay.enabled = true;
                            overlay.sprite = _catalog?.gameplayFx?.singleShadow ?? _fallbackSprite;
                            overlay.color = _world != null ? _world.shadowTint : MockupPalette.SingleShadow;
                            ShowCount(debug, "1", new Color(1f, 1f, 1f, 0.58f));
                            break;
                        case CellKind.OverlapHazard:
                            overlay.enabled = true;
                            overlay.sprite = _catalog?.gameplayFx?.overlapHazard ?? _fallbackSprite;
                            overlay.color = Color.white;
                            _pulseTargets.Add((overlay.transform, Vector3.one * 0.94f));
                            ShowCount(debug, "2+", UiTheme.Coral);
                            break;
                        case CellKind.Cliff:
                            overlay.enabled = true;
                            overlay.sprite = _world?.cliffTile ?? _catalog?.gameplayFx?.cliffRim ?? _fallbackSprite;
                            overlay.color = Color.white;
                            break;
                    }
                }
            }
        }

        public void PulseChannelPillars(StageDefinition stage, ChannelId channel, float durationSeconds = 0.3f)
        {
            if (stage == null || _pillars == null) return;
            StopCoroutine(nameof(PulseChannelRoutine));
            StartCoroutine(PulseChannelRoutine(stage, channel, durationSeconds));
        }

        public void PlayEnvironmentReaction(float durationSeconds = 0.48f)
        {
            if (_environmentReactionRenderer == null || _world?.environmentReaction == null) return;
            if (_environmentReactionRoutine != null) StopCoroutine(_environmentReactionRoutine);
            _environmentReactionRoutine = StartCoroutine(EnvironmentReactionRoutine(durationSeconds));
        }

        public Coroutine PlayDoorOpen(StageDefinition stage, float durationSeconds)
        {
            if (stage == null || stage.ClearGoalType != ClearGoalType.ExitDoor || _goalRenderer == null) return null;
            return StartCoroutine(DoorOpenRoutine(durationSeconds));
        }

        public Coroutine PlayFlowerBloom(StageDefinition stage, float durationSeconds)
        {
            if (stage == null || stage.ClearGoalType != ClearGoalType.NightFlower || _goalRenderer == null) return null;
            return StartCoroutine(FlowerBloomRoutine(durationSeconds));
        }

        private void LateUpdate()
        {
            if (_pulseTargets.Count == 0) return;
            var pulse = _reduceMotion ? 1f : 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.045f;
            foreach (var (target, baseScale) in _pulseTargets)
            {
                if (target != null) target.localScale = baseScale * pulse;
            }
        }

        private void BuildCell(StageDefinition stage, GridPosition pos)
        {
            var x = pos.X;
            var y = pos.Y;
            var cell = new GameObject($"Cell_{x}_{y}");
            cell.transform.SetParent(cellRoot, false);
            cell.transform.localPosition = GridWorld.ToWorld(pos);

            var baseRenderer = CreateRenderer(cell.transform, "Base", BoardOrder, Vector3.one * 0.94f);
            _cells[x, y] = baseRenderer;
            var overlay = CreateRenderer(cell.transform, "ShadowState", ShadowOrder, Vector3.one * 0.94f);
            overlay.enabled = false;
            _overlays[x, y] = overlay;

            var objectRenderer = CreateRenderer(cell.transform, "GameplayObject", ObjectOrder + y * 2, Vector3.one);
            _objects[x, y] = objectRenderer;
            var channel = CreateRenderer(cell.transform, "ChannelMark", MarkOrder + y * 2, Vector3.one * 0.30f);
            channel.enabled = false;
            _channelMarks[x, y] = channel;
            var direction = CreateRenderer(cell.transform, "DirectionMark", MarkOrder + y * 2 + 1, Vector3.one * 0.46f);
            direction.enabled = false;
            _directionMarks[x, y] = direction;
            _debugLabels[x, y] = CreateDebugLabel(cell.transform);

            if (stage.IsPillar(pos) && TryFindPillar(stage, pos, out var pillar))
            {
                objectRenderer.sprite = _catalog?.GetPillar(pillar.Height) ?? _fallbackSprite;
                objectRenderer.color = Color.white;
                ApplyBoldChannelGlyph(channel, ChannelSprite(pillar.Channel),
                    MockupPalette.ChannelColor(pillar.Channel));
                channel.transform.SetParent(objectRenderer.transform, false);
                channel.transform.localPosition = PillarGlyphPosition(pillar.Height);
                channel.transform.localScale = Vector3.one * 0.34f;
                var pillarScale = Vector3.one;
                objectRenderer.transform.localScale = pillarScale;
                _pillars[x, y] = objectRenderer.transform;
                _pillarBaseScales[x, y] = pillarScale;
            }
            else if (stage.IsLamp(pos))
            {
                objectRenderer.sprite = _catalog?.lampBody ?? _fallbackSprite;
                channel.transform.SetParent(objectRenderer.transform, false);
                direction.transform.SetParent(objectRenderer.transform, false);
            }
            else if (stage.IsGoal(pos))
            {
                objectRenderer.sprite = stage.ClearGoalType == ClearGoalType.ExitDoor
                    ? _world?.doorClosed ?? _fallbackSprite
                    : _world?.flowerClosed ?? _fallbackSprite;
                _goalRenderer = objectRenderer;
            }
            else
            {
                objectRenderer.enabled = false;
            }
        }

        private void BuildEnvironment()
        {
            var background = CreateRenderer(cellRoot, "WorldBackground", BackgroundOrder, Vector3.one);
            background.sprite = _world?.background ?? _fallbackSprite;
            background.color = _world != null ? _world.ambientTint : Color.white;
            background.transform.localPosition = GridWorld.BoardCenter(_size) + new Vector3(0f, 0.15f, 0f);
            // The expanded-board camera reserves extra HUD/object headroom. Overscan the
            // authored 16:9 background so the camera clear color never appears as side bars.
            FitSprite(background, _size.Width + 10f, _size.Height + 8f, cover: true);

            var frame = CreateRenderer(cellRoot, "BoardFrame", FrameOrder, Vector3.one);
            frame.sprite = _world?.boardFrame ?? _fallbackSprite;
            frame.color = new Color(1f, 1f, 1f, 0.92f);
            frame.transform.localPosition = GridWorld.BoardCenter(_size);
            FitSprite(frame, _size.Width + 1.05f, _size.Height + 1.05f, cover: true);

            var voidPad = CreateRenderer(cellRoot, "BoardVoid", FrameOrder + 1, Vector3.one);
            voidPad.sprite = _world?.boardVoid ?? _fallbackSprite;
            voidPad.color = new Color(0.18f, 0.18f, 0.23f, 0.96f);
            voidPad.transform.localPosition = GridWorld.BoardCenter(_size);
            FitSprite(voidPad, _size.Width + 0.38f, _size.Height + 0.38f, cover: true);
            BuildDecor();
        }

        private void BuildDecor()
        {
            if (_world == null) return;
            var center = GridWorld.BoardCenter(_size);
            var positions = new[]
            {
                center + new Vector3(-_size.Width * 0.48f, _size.Height * 0.5f + 0.4f, 0f),
                center + new Vector3(_size.Width * 0.48f, _size.Height * 0.5f + 0.2f, 0f),
                center + new Vector3(-_size.Width * 0.48f, -_size.Height * 0.5f - 0.25f, 0f)
            };
            for (var i = 0; i < positions.Length; i++)
            {
                var sprite = _world.backDecor != null && i < _world.backDecor.Length ? _world.backDecor[i] : null;
                if (sprite == null) continue;
                var decor = CreateRenderer(cellRoot, $"BackDecor_{i}", FrameOrder + 2, Vector3.one * 0.75f);
                decor.sprite = sprite;
                decor.transform.localPosition = positions[i];
            }

            var frontPositions = new[]
            {
                center + new Vector3(-_size.Width * 0.43f, -_size.Height * 0.5f - 0.42f, 0f),
                center + new Vector3(_size.Width * 0.43f, -_size.Height * 0.5f - 0.38f, 0f),
                center + new Vector3(0f, -_size.Height * 0.5f - 0.58f, 0f)
            };
            for (var i = 0; i < frontPositions.Length; i++)
            {
                var sprite = _world.frontDecor != null && i < _world.frontDecor.Length ? _world.frontDecor[i] : null;
                if (sprite == null) continue;
                var decor = CreateRenderer(cellRoot, $"FrontDecor_{i}", FrontDecorOrder + i, Vector3.one * 0.72f);
                decor.sprite = sprite;
                decor.transform.localPosition = frontPositions[i];
            }

            if (_world.environmentReaction != null)
            {
                _environmentReactionRenderer = CreateRenderer(
                    cellRoot,
                    "EnvironmentReaction",
                    FrontDecorOrder + 4,
                    Vector3.one * 0.8f);
                _environmentReactionRenderer.sprite = _world.environmentReaction;
                _environmentReactionRenderer.color = new Color(
                    _world.reactionTint.r,
                    _world.reactionTint.g,
                    _world.reactionTint.b,
                    0f);
                _environmentReactionRenderer.transform.localPosition =
                    center + new Vector3(0f, -_size.Height * 0.5f - 0.12f, 0f);
                _environmentReactionRenderer.gameObject.SetActive(false);
            }
        }

        private IEnumerator EnvironmentReactionRoutine(float durationSeconds)
        {
            var renderer = _environmentReactionRenderer;
            if (renderer == null) yield break;
            renderer.gameObject.SetActive(true);
            var duration = Mathf.Max(0.08f, durationSeconds);
            var elapsed = 0f;
            var basePosition = renderer.transform.localPosition;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var wave = Mathf.Sin(t * Mathf.PI);
                var tint = _world != null ? _world.reactionTint : Color.white;
                tint.a *= _reduceMotion ? 0.72f : wave;
                renderer.color = tint;
                if (!_reduceMotion)
                {
                    var world = _world != null ? _world.worldNumber : 1;
                    renderer.transform.localScale = world switch
                    {
                        2 => new Vector3(Mathf.Lerp(0.72f, 1.1f, t), 0.82f, 1f),
                        3 => Vector3.one * (0.78f + wave * 0.16f),
                        _ => Vector3.one * (0.78f + t * 0.18f)
                    };
                    renderer.transform.localPosition = world == 2
                        ? basePosition + Vector3.right * Mathf.Lerp(-0.2f, 0.2f, t)
                        : basePosition;
                }
                yield return null;
            }
            renderer.gameObject.SetActive(false);
            renderer.transform.localPosition = basePosition;
            renderer.transform.localScale = Vector3.one * 0.8f;
            _environmentReactionRoutine = null;
        }

        private IEnumerator PulseChannelRoutine(StageDefinition stage, ChannelId channel, float duration)
        {
            var targets = new List<Transform>();
            var baseScales = new List<Vector3>();
            foreach (var pillar in stage.Pillars)
            {
                if (pillar.Channel != channel) continue;
                var target = _pillars[pillar.Position.X, pillar.Position.Y];
                if (target == null) continue;
                targets.Add(target);
                baseScales.Add(_pillarBaseScales[pillar.Position.X, pillar.Position.Y]);
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var scale = _reduceMotion ? 1f : 1f + Mathf.Sin(elapsed * 18f) * 0.07f;
                for (var i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null) targets[i].localScale = baseScales[i] * scale;
                }
                yield return null;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null) targets[i].localScale = baseScales[i];
            }
        }

        private IEnumerator DoorOpenRoutine(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                _goalRenderer.color = Color.Lerp(Color.white, new Color(1f, 0.93f, 0.68f, 1f), Mathf.Sin(t * Mathf.PI));
                _goalRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, t);
                if (t >= 0.45f && _world?.doorOpen != null) _goalRenderer.sprite = _world.doorOpen;
                yield return null;
            }

            _goalRenderer.color = Color.white;
            _goalRenderer.transform.localScale = Vector3.one;
            if (_world?.doorOpen != null) _goalRenderer.sprite = _world.doorOpen;
        }

        private IEnumerator FlowerBloomRoutine(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                if (t >= 0.35f && _world?.flowerBloom != null) _goalRenderer.sprite = _world.flowerBloom;
                var eased = t * t * (3f - 2f * t);
                _goalRenderer.transform.localScale = Vector3.one * Mathf.Lerp(0.88f, 1.12f, eased);
                _goalRenderer.color = Color.Lerp(new Color(0.75f, 0.8f, 1f, 0.85f), Color.white, eased);
                yield return null;
            }

            _goalRenderer.transform.localScale = Vector3.one;
            _goalRenderer.color = Color.white;
            if (_world?.flowerBloom != null) _goalRenderer.sprite = _world.flowerBloom;
        }

        private Sprite BaseSprite(StageDefinition stage, GridPosition pos)
        {
            if (stage.IsAlwaysSafe(pos) || stage.IsPillar(pos))
            {
                return _world?.PickSafeTile(pos.X, pos.Y) ?? _fallbackSprite;
            }

            return _world?.boardVoid ?? _fallbackSprite;
        }

        private Sprite ChannelSprite(ChannelId channel) => _catalog?.GetChannelIcon(channel) ?? _fallbackSprite;

        private Sprite ResolveFallbackSprite()
        {
            if (_world?.safeTile != null) return _world.safeTile;
            if (_catalog?.panelLight != null) return _catalog.panelLight;
            return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static SpriteRenderer CreateRenderer(
            Transform parent,
            string name,
            int sortingOrder,
            Vector3 scale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static TextMeshPro CreateDebugLabel(Transform parent)
        {
            var go = new GameObject("DebugCount");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 4.8f;
            text.color = Color.white;
            text.rectTransform.sizeDelta = new Vector2(1.2f, 0.5f);
            text.transform.localPosition = new Vector3(0f, -0.28f, 0f);
            text.sortingOrder = MarkOrder + 10;
            UiTypography.Apply(text, bold: true);
            go.SetActive(false);
            return text;
        }

        private void ShowCount(TextMeshPro label, string value, Color color)
        {
            if (!showDebugCounts || label == null) return;
            label.text = value;
            label.color = color;
            label.gameObject.SetActive(true);
        }

        private static Quaternion DirectionRotation(CardinalDirection direction) => direction switch
        {
            CardinalDirection.East => Quaternion.Euler(0f, 0f, -90f),
            CardinalDirection.South => Quaternion.Euler(0f, 0f, 180f),
            CardinalDirection.West => Quaternion.Euler(0f, 0f, 90f),
            _ => Quaternion.identity
        };

        private static readonly Vector3 LampGlyphPosition = new(0f, 1.37f, 0f);

        private static Vector3 PillarGlyphPosition(PillarHeight height) => height switch
        {
            PillarHeight.Low => new Vector3(0f, 0.82f, 0f),
            PillarHeight.Medium => new Vector3(0f, 1.22f, 0f),
            _ => new Vector3(0f, 1.69f, 0f)
        };

        private static void ApplyBoldChannelGlyph(SpriteRenderer renderer, Sprite sprite, Color color)
        {
            if (renderer == null) return;
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.enabled = sprite != null;
            for (var i = 0; i < GlyphBoldOffsets.Length; i++)
            {
                var child = renderer.transform.Find($"Bold_{i}");
                if (child == null)
                {
                    var go = new GameObject($"Bold_{i}");
                    go.transform.SetParent(renderer.transform, false);
                    child = go.transform;
                    go.AddComponent<SpriteRenderer>();
                }

                child.localPosition = GlyphBoldOffsets[i];
                child.localScale = Vector3.one;
                var bold = child.GetComponent<SpriteRenderer>();
                bold.sprite = sprite;
                bold.color = color;
                bold.sortingOrder = renderer.sortingOrder;
                bold.enabled = sprite != null;
            }
        }

        private static Vector3 LampArrowPosition(CardinalDirection direction)
        {
            const float centerY = 1.48f;
            const float radius = 0.58f;
            return direction switch
            {
                CardinalDirection.East => new Vector3(radius, centerY, 0f),
                CardinalDirection.South => new Vector3(0f, centerY - radius, 0f),
                CardinalDirection.West => new Vector3(-radius, centerY, 0f),
                _ => new Vector3(0f, centerY + radius, 0f)
            };
        }

        private static void FitSprite(SpriteRenderer renderer, float width, float height, bool cover)
        {
            if (renderer?.sprite == null) return;
            var bounds = renderer.sprite.bounds.size;
            if (bounds.x <= 0f || bounds.y <= 0f) return;
            var sx = width / bounds.x;
            var sy = height / bounds.y;
            var uniform = cover ? Mathf.Max(sx, sy) : Mathf.Min(sx, sy);
            renderer.transform.localScale = new Vector3(uniform, uniform, 1f);
        }

        private void EnsureRoot()
        {
            if (cellRoot != null) return;
            var root = new GameObject("Cells");
            root.transform.SetParent(transform, false);
            cellRoot = root.transform;
        }

        private void Clear()
        {
            _pulseTargets.Clear();
            if (cellRoot == null) return;
            for (var i = cellRoot.childCount - 1; i >= 0; i--)
            {
                var child = cellRoot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
        }

        private static bool TryFindPillar(StageDefinition stage, GridPosition pos, out PillarDefinition pillar)
        {
            foreach (var candidate in stage.Pillars)
            {
                if (candidate.Position != pos) continue;
                pillar = candidate;
                return true;
            }

            pillar = null;
            return false;
        }
    }
}
