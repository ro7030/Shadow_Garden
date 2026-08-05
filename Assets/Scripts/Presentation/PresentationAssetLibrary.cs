using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowGarden.Presentation
{
    public static class PresentationAssetLibrary
    {
        private const string CatalogResource = "Presentation/InGameAssetCatalog";
        private static InGameAssetCatalogAsset _catalog;
        private static Dictionary<string, WorldArtSetAsset> _byStage;
        private static readonly HashSet<string> WarnedStages = new HashSet<string>(StringComparer.Ordinal);

        public static InGameAssetCatalogAsset Catalog =>
            _catalog != null ? _catalog : _catalog = Resources.Load<InGameAssetCatalogAsset>(CatalogResource);

        public static WorldArtSetAsset ForStage(string stageId)
        {
            EnsureBindings();
            if (!string.IsNullOrWhiteSpace(stageId) && _byStage.TryGetValue(stageId, out var art) && art != null)
            {
                return art;
            }

            if (!string.IsNullOrWhiteSpace(stageId) && WarnedStages.Add(stageId))
            {
                Debug.LogWarning($"Presentation binding missing for '{stageId}'. A readable fallback will be used.");
            }

            var world = ParseWorld(stageId);
            foreach (var candidate in Resources.LoadAll<WorldArtSetAsset>("Presentation/Worlds"))
            {
                if (candidate != null && candidate.worldNumber == world)
                {
                    return candidate;
                }
            }

            return null;
        }

        public static int ParseWorld(string stageId)
        {
            if (!string.IsNullOrEmpty(stageId) && stageId.Length > 0 && char.IsDigit(stageId[0]))
            {
                return stageId[0] - '0';
            }

            return 1;
        }

        public static void ResetCache()
        {
            _catalog = null;
            _byStage = null;
            WarnedStages.Clear();
        }

        private static void EnsureBindings()
        {
            if (_byStage != null)
            {
                return;
            }

            _byStage = new Dictionary<string, WorldArtSetAsset>(StringComparer.Ordinal);
            foreach (var binding in Resources.LoadAll<StagePresentationBindingAsset>("Presentation/Bindings"))
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.stageId) || binding.worldArt == null)
                {
                    continue;
                }

                _byStage[binding.stageId] = binding.worldArt;
            }
        }
    }
}
