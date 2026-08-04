using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowGarden.Infrastructure
{
    /// <summary>
    /// PlayerPrefs JSON storage for progress and UI preferences on separate keys.
    /// Save failure never throws — callers always receive a playable default.
    /// </summary>
    public sealed class PlayerPrefsSaveRepository : IProgressSaveRepository, IUiPreferencesRepository
    {
        public const string ProgressKey = "ShadowGarden.Save.v1";
        public const string UiPrefsKey = "ShadowGarden.UiPrefs.v1";

        private readonly string _progressKey;
        private readonly string _uiPrefsKey;

        public PlayerPrefsSaveRepository(
            string progressKey = ProgressKey,
            string uiPrefsKey = UiPrefsKey)
        {
            _progressKey = progressKey;
            _uiPrefsKey = uiPrefsKey;
        }

        SaveLoadResult<SaveData> IProgressSaveRepository.Load() => LoadProgress();

        bool IProgressSaveRepository.Save(SaveData data) => SaveProgress(data);

        SaveLoadResult<UiPreferencesData> IUiPreferencesRepository.Load() => LoadUiPreferences();

        bool IUiPreferencesRepository.Save(UiPreferencesData data) => SaveUiPreferences(data);

        public SaveLoadResult<SaveData> LoadProgress()
        {
            try
            {
                if (!PlayerPrefs.HasKey(_progressKey))
                {
                    return SaveLoadResult<SaveData>.Recovered(SaveData.CreateDefault(), "missing");
                }

                var json = PlayerPrefs.GetString(_progressKey, string.Empty);
                return ParseProgress(json);
            }
            catch (Exception ex)
            {
                return SaveLoadResult<SaveData>.Recovered(SaveData.CreateDefault(), ex.Message);
            }
        }

        public bool SaveProgress(SaveData data)
        {
            try
            {
                var normalized = SaveDataNormalizer.Normalize(data ?? SaveData.CreateDefault());
                var json = JsonUtility.ToJson(normalized);
                PlayerPrefs.SetString(_progressKey, json);
                PlayerPrefs.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public SaveLoadResult<UiPreferencesData> LoadUiPreferences()
        {
            try
            {
                if (!PlayerPrefs.HasKey(_uiPrefsKey))
                {
                    return SaveLoadResult<UiPreferencesData>.Recovered(
                        UiPreferencesData.CreateDefault(),
                        "missing");
                }

                var json = PlayerPrefs.GetString(_uiPrefsKey, string.Empty);
                return ParseUiPreferences(json);
            }
            catch (Exception ex)
            {
                return SaveLoadResult<UiPreferencesData>.Recovered(
                    UiPreferencesData.CreateDefault(),
                    ex.Message);
            }
        }

        public bool SaveUiPreferences(UiPreferencesData data)
        {
            try
            {
                var normalized = UiPreferencesNormalizer.Normalize(data ?? UiPreferencesData.CreateDefault());
                var json = JsonUtility.ToJson(normalized);
                PlayerPrefs.SetString(_uiPrefsKey, json);
                PlayerPrefs.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static SaveLoadResult<SaveData> ParseProgress(string json) =>
            SaveDataNormalizer.Parse(json);

        public static SaveLoadResult<UiPreferencesData> ParseUiPreferences(string json) =>
            UiPreferencesNormalizer.Parse(json);
    }

    public static class SaveDataNormalizer
    {
        public static SaveLoadResult<SaveData> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return SaveLoadResult<SaveData>.Recovered(SaveData.CreateDefault(), "empty");
            }

            try
            {
                // Accept legacy clearedStageIds by rewriting key before deserialize when needed.
                var sanitized = json.Replace("\"clearedStageIds\"", "\"completedStageIds\"");
                var raw = JsonUtility.FromJson<SaveData>(sanitized);
                if (raw == null)
                {
                    return SaveLoadResult<SaveData>.Recovered(SaveData.CreateDefault(), "null_json");
                }

                var normalized = Normalize(raw);
                var recovered = raw.version != SaveData.CurrentVersion ||
                                raw.completedStageIds == null ||
                                raw.bestClearMillisecondsByStage == null ||
                                string.IsNullOrWhiteSpace(raw.lastStageId);
                return recovered
                    ? SaveLoadResult<SaveData>.Recovered(normalized, "normalized")
                    : SaveLoadResult<SaveData>.Ok(normalized);
            }
            catch (Exception ex)
            {
                return SaveLoadResult<SaveData>.Recovered(SaveData.CreateDefault(), ex.Message);
            }
        }

        public static SaveData Normalize(SaveData source)
        {
            var data = SaveData.CreateDefault();
            if (source == null)
            {
                return data;
            }

            data.version = SaveData.CurrentVersion;
            data.lastStageId = string.IsNullOrWhiteSpace(source.lastStageId) ? "1-1" : source.lastStageId.Trim();
            data.completedStageIds = new List<string>();
            if (source.completedStageIds != null)
            {
                var seen = new HashSet<string>();
                foreach (var id in source.completedStageIds)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    var trimmed = id.Trim();
                    if (seen.Add(trimmed))
                    {
                        data.completedStageIds.Add(trimmed);
                    }
                }
            }

            var best = source.ToBestClearDictionary();
            data.SetBestClearDictionary(best);
            return data;
        }
    }

    public static class UiPreferencesNormalizer
    {
        public static SaveLoadResult<UiPreferencesData> Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return SaveLoadResult<UiPreferencesData>.Recovered(
                    UiPreferencesData.CreateDefault(),
                    "empty");
            }

            try
            {
                var raw = JsonUtility.FromJson<UiPreferencesData>(json);
                if (raw == null)
                {
                    return SaveLoadResult<UiPreferencesData>.Recovered(
                        UiPreferencesData.CreateDefault(),
                        "null_json");
                }

                var normalized = Normalize(raw);
                var recovered = raw.version != UiPreferencesData.CurrentVersion;
                return recovered
                    ? SaveLoadResult<UiPreferencesData>.Recovered(normalized, "normalized")
                    : SaveLoadResult<UiPreferencesData>.Ok(normalized);
            }
            catch (Exception ex)
            {
                return SaveLoadResult<UiPreferencesData>.Recovered(
                    UiPreferencesData.CreateDefault(),
                    ex.Message);
            }
        }

        public static UiPreferencesData Normalize(UiPreferencesData source)
        {
            var data = UiPreferencesData.CreateDefault();
            if (source == null)
            {
                return data;
            }

            data.version = UiPreferencesData.CurrentVersion;
            data.bgmVolume = Mathf.Clamp01(source.bgmVolume);
            data.sfxVolume = Mathf.Clamp01(source.sfxVolume);
            data.reduceMotion = source.reduceMotion;
            data.openingSeen = source.openingSeen;
            return data;
        }
    }

    /// <summary>
    /// In-memory progress store for EditMode tests (no PlayerPrefs).
    /// </summary>
    public sealed class MemoryProgressSaveRepository : IProgressSaveRepository
    {
        private SaveData _data = SaveData.CreateDefault();

        public SaveLoadResult<SaveData> Load() =>
            SaveLoadResult<SaveData>.Ok(SaveDataNormalizer.Normalize(_data));

        public bool Save(SaveData data)
        {
            _data = SaveDataNormalizer.Normalize(data ?? SaveData.CreateDefault());
            return true;
        }
    }

    public sealed class MemoryUiPreferencesRepository : IUiPreferencesRepository
    {
        private UiPreferencesData _data = UiPreferencesData.CreateDefault();

        public SaveLoadResult<UiPreferencesData> Load() =>
            SaveLoadResult<UiPreferencesData>.Ok(UiPreferencesNormalizer.Normalize(_data));

        public bool Save(UiPreferencesData data)
        {
            _data = UiPreferencesNormalizer.Normalize(data ?? UiPreferencesData.CreateDefault());
            return true;
        }
    }
}
