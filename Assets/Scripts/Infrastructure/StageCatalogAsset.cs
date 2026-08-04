using System;
using System.Collections.Generic;
using ShadowGarden.Core;
using UnityEngine;

namespace ShadowGarden.Infrastructure
{
    /// <summary>
    /// Ordered catalog of main-story StageDefinitionAsset references.
    /// Prototype / TestField assets must not be listed here.
    /// </summary>
    [CreateAssetMenu(menuName = "ShadowGarden/Stage Catalog", fileName = "StageCatalog")]
    public sealed class StageCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<StageDefinitionAsset> stages = new List<StageDefinitionAsset>();

        public IReadOnlyList<StageDefinitionAsset> Stages => stages;

        public int Count => stages?.Count ?? 0;

        public bool TryGetById(string stageId, out StageDefinitionAsset asset)
        {
            asset = null;
            if (stages == null || string.IsNullOrWhiteSpace(stageId))
            {
                return false;
            }

            foreach (var candidate in stages)
            {
                if (candidate != null && candidate.stageId == stageId)
                {
                    asset = candidate;
                    return true;
                }
            }

            return false;
        }

        public StageDefinitionAsset GetAt(int index)
        {
            if (stages == null || index < 0 || index >= stages.Count)
            {
                return null;
            }

            return stages[index];
        }

        public IReadOnlyList<string> GetOrderedStageIds()
        {
            var ids = new List<string>();
            if (stages == null)
            {
                return ids;
            }

            foreach (var stage in stages)
            {
                if (stage != null && !string.IsNullOrWhiteSpace(stage.stageId))
                {
                    ids.Add(stage.stageId);
                }
            }

            return ids;
        }

        public bool IsUnlocked(string stageId, SaveData progress)
        {
            if (string.IsNullOrWhiteSpace(stageId) || stages == null || stages.Count == 0)
            {
                return false;
            }

            if (stages[0] != null && stages[0].stageId == stageId)
            {
                return true;
            }

            var completed = progress?.completedStageIds ?? new List<string>();
            for (var i = 1; i < stages.Count; i++)
            {
                var stage = stages[i];
                if (stage == null || stage.stageId != stageId)
                {
                    continue;
                }

                var previous = stages[i - 1];
                return previous != null && completed.Contains(previous.stageId);
            }

            return false;
        }

        public StageDefinition CreateDefinition(string stageId)
        {
            if (!TryGetById(stageId, out var asset) || asset == null)
            {
                throw new InvalidOperationException($"Stage '{stageId}' is not in the catalog.");
            }

            return StageDefinitionFactory.CreateFromAsset(asset);
        }
    }
}
