using System;
using System.Collections.Generic;
using System.Globalization;
using ShadowGarden.Core;
using ShadowGarden.Infrastructure;

namespace ShadowGarden.Presentation
{
    public static class ProgressTimeFormat
    {
        public const string Incomplete = "--:--.-";

        public static string FormatBestClear(long? milliseconds)
        {
            if (!milliseconds.HasValue || milliseconds.Value < 0)
            {
                return Incomplete;
            }

            var totalTenths = milliseconds.Value / 100L;
            var minutes = totalTenths / 600L;
            var seconds = (totalTenths / 10L) % 60L;
            var tenths = totalTenths % 10L;
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2}", minutes, seconds, tenths);
        }

        public static string FormatBestClear(SaveData progress, string stageId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(stageId))
            {
                return Incomplete;
            }

            var map = progress.ToBestClearDictionary();
            return map.TryGetValue(stageId.Trim(), out var ms)
                ? FormatBestClear(ms)
                : Incomplete;
        }
    }

    public sealed class StageNodeViewModel
    {
        public string StageId { get; set; }
        public string Title { get; set; }
        public bool Unlocked { get; set; }
        public bool Completed { get; set; }
        public ClearGoalType GoalType { get; set; }
        public string CompletionIcon { get; set; }
        public string TimeLabel { get; set; }
        public bool IsFocused { get; set; }
    }

    public sealed class WorldCardViewModel
    {
        public int WorldNumber { get; set; }
        public string WorldTitle { get; set; }
        public bool Unlocked { get; set; }
        public IReadOnlyList<StageNodeViewModel> Nodes { get; set; }
    }

    /// <summary>
    /// Presentation ViewModel for the 3×4 world map. Pure data — no Core rule recomputation.
    /// </summary>
    public sealed class WorldMapViewModel
    {
        public IReadOnlyList<WorldCardViewModel> Worlds { get; private set; }
        public string FocusedStageId { get; private set; }
        public IReadOnlyList<StageNodeViewModel> FlatNodes { get; private set; }

        public static WorldMapViewModel Build(
            StageCatalogAsset catalog,
            SaveData progress,
            string focusStageId = null)
        {
            var vm = new WorldMapViewModel();
            var worlds = new List<WorldCardViewModel>();
            var flat = new List<StageNodeViewModel>();
            progress ??= SaveData.CreateDefault();

            var focus = string.IsNullOrWhiteSpace(focusStageId) ? progress.lastStageId : focusStageId.Trim();
            if (string.IsNullOrWhiteSpace(focus))
            {
                focus = "1-1";
            }

            if (catalog == null || catalog.Count == 0)
            {
                vm.Worlds = worlds;
                vm.FlatNodes = flat;
                vm.FocusedStageId = focus;
                return vm;
            }

            // Ensure focus lands on an unlocked node when possible.
            if (!catalog.IsUnlocked(focus, progress))
            {
                focus = catalog.GetAt(0)?.stageId ?? "1-1";
            }

            for (var world = 1; world <= 3; world++)
            {
                var nodes = new List<StageNodeViewModel>();
                for (var slot = 1; slot <= 4; slot++)
                {
                    var id = $"{world}-{slot}";
                    StageDefinitionAsset asset = null;
                    catalog.TryGetById(id, out asset);
                    var unlocked = catalog.IsUnlocked(id, progress);
                    var completed = progress.completedStageIds != null &&
                                    progress.completedStageIds.Contains(id);
                    var goal = asset != null ? asset.clearGoalType
                        : (slot == 4 ? ClearGoalType.NightFlower : ClearGoalType.ExitDoor);
                    var icon = string.Empty;
                    if (completed)
                    {
                        icon = goal == ClearGoalType.NightFlower ? "꽃" : "문";
                    }

                    var hasBest = progress.ToBestClearDictionary().ContainsKey(id);
                    var node = new StageNodeViewModel
                    {
                        StageId = id,
                        Title = asset != null ? $"{id}" : id,
                        Unlocked = unlocked,
                        Completed = completed,
                        GoalType = goal,
                        CompletionIcon = icon,
                        TimeLabel = completed && hasBest
                            ? "BEST " + ProgressTimeFormat.FormatBestClear(progress, id)
                            : ProgressTimeFormat.Incomplete,
                        IsFocused = id == focus
                    };
                    nodes.Add(node);
                    flat.Add(node);
                }

                var firstId = $"{world}-1";
                worlds.Add(new WorldCardViewModel
                {
                    WorldNumber = world,
                    WorldTitle = MockupPalette.WorldName(firstId),
                    Unlocked = catalog.IsUnlocked(firstId, progress),
                    Nodes = nodes
                });
            }

            vm.Worlds = worlds;
            vm.FlatNodes = flat;
            vm.FocusedStageId = focus;
            return vm;
        }

        public StageNodeViewModel FindFocused()
        {
            foreach (var node in FlatNodes)
            {
                if (node.IsFocused)
                {
                    return node;
                }
            }

            return FlatNodes.Count > 0 ? FlatNodes[0] : null;
        }

        public void MoveFocus(int delta)
        {
            if (FlatNodes == null || FlatNodes.Count == 0)
            {
                return;
            }

            var unlocked = new List<int>();
            for (var i = 0; i < FlatNodes.Count; i++)
            {
                if (FlatNodes[i].Unlocked)
                {
                    unlocked.Add(i);
                }
            }

            if (unlocked.Count == 0)
            {
                return;
            }

            var current = 0;
            for (var i = 0; i < unlocked.Count; i++)
            {
                if (FlatNodes[unlocked[i]].StageId == FocusedStageId)
                {
                    current = i;
                    break;
                }
            }

            current = (current + delta + unlocked.Count * 8) % unlocked.Count;
            SetFocus(FlatNodes[unlocked[current]].StageId);
        }

        /// <summary>Grid navigation across 3 worlds × 4 slots among unlocked nodes only.</summary>
        public void MoveFocusGrid(int dx, int dy)
        {
            if (FlatNodes == null || FlatNodes.Count == 0 || (dx == 0 && dy == 0))
            {
                return;
            }

            if (!TryParseStageId(FocusedStageId, out var world, out var slot))
            {
                world = 1;
                slot = 1;
            }

            var candidates = new List<StageNodeViewModel>();
            foreach (var node in FlatNodes)
            {
                if (node.Unlocked)
                {
                    candidates.Add(node);
                }
            }

            if (candidates.Count == 0)
            {
                return;
            }

            StageNodeViewModel best = null;
            var bestScore = int.MaxValue;
            foreach (var node in candidates)
            {
                if (!TryParseStageId(node.StageId, out var nw, out var ns))
                {
                    continue;
                }

                var dw = nw - world;
                var ds = ns - slot;
                if (dx != 0 && Math.Sign(dw) != Math.Sign(dx) && dw != 0)
                {
                    continue;
                }

                if (dy != 0 && Math.Sign(ds) != Math.Sign(dy) && ds != 0)
                {
                    continue;
                }

                if (dx != 0 && dw == 0)
                {
                    continue;
                }

                if (dy != 0 && ds == 0)
                {
                    continue;
                }

                var score = Math.Abs(dw) * 10 + Math.Abs(ds);
                if (score > 0 && score < bestScore)
                {
                    bestScore = score;
                    best = node;
                }
            }

            if (best == null)
            {
                // Wrap within unlocked linear order when grid step has no neighbor.
                MoveFocus(dx != 0 ? dx : dy);
                return;
            }

            SetFocus(best.StageId);
        }

        public void SetFocus(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId) || FlatNodes == null)
            {
                return;
            }

            FocusedStageId = stageId.Trim();
            foreach (var node in FlatNodes)
            {
                node.IsFocused = node.StageId == FocusedStageId;
            }
        }

        private static bool TryParseStageId(string stageId, out int world, out int slot)
        {
            world = 0;
            slot = 0;
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            var parts = stageId.Split('-');
            return parts.Length == 2 &&
                   int.TryParse(parts[0], out world) &&
                   int.TryParse(parts[1], out slot);
        }
    }

    public sealed class ModalOption
    {
        public string Id { get; }
        public string Label { get; }

        public ModalOption(string id, string label)
        {
            Id = id;
            Label = label;
        }
    }

    public enum ModalScreenKind
    {
        GameOver = 0,
        Cleared = 1
    }

    /// <summary>
    /// GameOver / Cleared modal selection ViewModel (keyboard + mouse parity).
    /// </summary>
    public sealed class ModalViewModel
    {
        public ModalScreenKind Kind { get; private set; }
        public IReadOnlyList<ModalOption> Options { get; private set; }
        public int SelectedIndex { get; private set; }

        public ModalOption Selected =>
            Options != null && Options.Count > 0
                ? Options[Math.Clamp(SelectedIndex, 0, Options.Count - 1)]
                : null;

        public static ModalViewModel CreateGameOver()
        {
            return new ModalViewModel
            {
                Kind = ModalScreenKind.GameOver,
                Options = new[]
                {
                    new ModalOption("retry", "다시 도전"),
                    new ModalOption("worldmap", "레벨 선택")
                },
                SelectedIndex = 0
            };
        }

        public static ModalViewModel CreateCleared(bool hasNextStage, bool isFinalStage, bool nextIsNewWorld = false)
        {
            if (isFinalStage)
            {
                return new ModalViewModel
                {
                    Kind = ModalScreenKind.Cleared,
                    Options = new[]
                    {
                        new ModalOption("ending", "엔딩 보기"),
                        new ModalOption("worldmap", "레벨 선택")
                    },
                    SelectedIndex = 0
                };
            }

            if (hasNextStage)
            {
                return new ModalViewModel
                {
                    Kind = ModalScreenKind.Cleared,
                    Options = new[]
                    {
                        new ModalOption("next", nextIsNewWorld ? "다음 월드" : "다음 스테이지"),
                        new ModalOption("worldmap", "레벨 선택")
                    },
                    SelectedIndex = 0
                };
            }

            return new ModalViewModel
            {
                Kind = ModalScreenKind.Cleared,
                Options = new[]
                {
                    new ModalOption("worldmap", "레벨 선택"),
                    new ModalOption("retry", "다시 도전")
                },
                SelectedIndex = 0
            };
        }

        public void MoveSelection(int delta)
        {
            if (Options == null || Options.Count == 0)
            {
                return;
            }

            SelectedIndex = (SelectedIndex + delta + Options.Count * 8) % Options.Count;
        }

        public void SetSelectedIndex(int index)
        {
            if (Options == null || Options.Count == 0)
            {
                SelectedIndex = 0;
                return;
            }

            SelectedIndex = Math.Clamp(index, 0, Options.Count - 1);
        }
    }
}
