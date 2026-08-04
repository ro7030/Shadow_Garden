using System;
using UnityEngine;

namespace ShadowGarden.Infrastructure
{
    /// <summary>
    /// Loads/saves progress and UI prefs. Failures never block boot — defaults are used.
    /// </summary>
    public sealed class SaveService
    {
        private readonly IProgressSaveRepository _progress;
        private readonly IUiPreferencesRepository _uiPrefs;

        public SaveData Progress { get; private set; }
        public UiPreferencesData Preferences { get; private set; }
        public string LastProgressError { get; private set; }
        public string LastPreferencesError { get; private set; }

        public SaveService(
            IProgressSaveRepository progressRepository = null,
            IUiPreferencesRepository uiPreferencesRepository = null)
        {
            var shared = progressRepository as PlayerPrefsSaveRepository
                         ?? uiPreferencesRepository as PlayerPrefsSaveRepository;
            if (shared != null)
            {
                _progress = shared;
                _uiPrefs = shared;
            }
            else
            {
                _progress = progressRepository ?? new PlayerPrefsSaveRepository();
                _uiPrefs = uiPreferencesRepository ?? new PlayerPrefsSaveRepository();
            }

            Progress = SaveData.CreateDefault();
            Preferences = UiPreferencesData.CreateDefault();
        }

        public void LoadAll()
        {
            var progressResult = _progress.Load();
            Progress = progressResult.Data ?? SaveData.CreateDefault();
            LastProgressError = progressResult.UsedFallback ? progressResult.Error : null;

            var prefsResult = _uiPrefs.Load();
            Preferences = prefsResult.Data ?? UiPreferencesData.CreateDefault();
            LastPreferencesError = prefsResult.UsedFallback ? prefsResult.Error : null;
        }

        public bool TrySaveProgress()
        {
            try
            {
                return _progress.Save(Progress ?? SaveData.CreateDefault());
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TrySavePreferences()
        {
            try
            {
                return _uiPrefs.Save(Preferences ?? UiPreferencesData.CreateDefault());
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void MarkOpeningSeen()
        {
            Preferences ??= UiPreferencesData.CreateDefault();
            Preferences.openingSeen = true;
            TrySavePreferences();
        }

        public void RecordStageSelected(string stageId)
        {
            Progress ??= SaveData.CreateDefault();
            if (!string.IsNullOrWhiteSpace(stageId))
            {
                Progress.lastStageId = stageId.Trim();
            }

            TrySaveProgress();
        }

        public void RecordStageCleared(string stageId, long elapsedMilliseconds)
        {
            Progress ??= SaveData.CreateDefault();
            if (string.IsNullOrWhiteSpace(stageId))
            {
                return;
            }

            var id = stageId.Trim();
            if (!Progress.completedStageIds.Contains(id))
            {
                Progress.completedStageIds.Add(id);
            }

            Progress.lastStageId = id;
            var best = Progress.ToBestClearDictionary();
            if (!best.TryGetValue(id, out var previous) || elapsedMilliseconds < previous)
            {
                best[id] = Math.Max(0, elapsedMilliseconds);
            }

            Progress.SetBestClearDictionary(best);
            TrySaveProgress();
        }

        /// <summary>Persist last played stage after a failure so WorldMap can restore focus.</summary>
        public void RecordStageFailed(string stageId)
        {
            Progress ??= SaveData.CreateDefault();
            if (!string.IsNullOrWhiteSpace(stageId))
            {
                Progress.lastStageId = stageId.Trim();
            }

            TrySaveProgress();
        }

        public bool CanContinue()
        {
            Progress ??= SaveData.CreateDefault();
            Preferences ??= UiPreferencesData.CreateDefault();
            if (Preferences.openingSeen)
            {
                return true;
            }

            if (Progress.completedStageIds != null && Progress.completedStageIds.Count > 0)
            {
                return true;
            }

            if (Progress.bestClearMillisecondsByStage != null &&
                Progress.bestClearMillisecondsByStage.Count > 0)
            {
                return true;
            }

            return false;
        }

        public void ResetProgressForNewGame()
        {
            Progress = SaveData.CreateDefault();
            TrySaveProgress();
            Preferences ??= UiPreferencesData.CreateDefault();
            Preferences.openingSeen = false;
            TrySavePreferences();
        }

        public long? TryGetBestClearMilliseconds(string stageId)
        {
            if (Progress == null || string.IsNullOrWhiteSpace(stageId))
            {
                return null;
            }

            var map = Progress.ToBestClearDictionary();
            return map.TryGetValue(stageId.Trim(), out var ms) ? ms : (long?)null;
        }

        public string ResolveNextStageId(StageCatalogAsset catalog, string currentStageId)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(currentStageId))
            {
                return null;
            }

            var ids = catalog.GetOrderedStageIds();
            for (var i = 0; i < ids.Count - 1; i++)
            {
                if (ids[i] == currentStageId)
                {
                    var next = ids[i + 1];
                    return catalog.IsUnlocked(next, Progress) ? next : null;
                }
            }

            return null;
        }
    }
}
