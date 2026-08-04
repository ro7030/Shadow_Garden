using System;
using System.Collections.Generic;

namespace ShadowGarden.Infrastructure
{
    /// <summary>
    /// Progress save payload. Field name completedStageIds follows Stage-1 command
    /// (architecture v1.1 used clearedStageIds — command takes priority).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public List<string> completedStageIds = new List<string>();
        public string lastStageId = "1-1";
        public List<StageBestClearEntry> bestClearMillisecondsByStage = new List<StageBestClearEntry>();

        public static SaveData CreateDefault() => new SaveData();

        public Dictionary<string, long> ToBestClearDictionary()
        {
            var map = new Dictionary<string, long>();
            if (bestClearMillisecondsByStage == null)
            {
                return map;
            }

            foreach (var entry in bestClearMillisecondsByStage)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.stageId))
                {
                    continue;
                }

                if (!map.ContainsKey(entry.stageId) || entry.milliseconds < map[entry.stageId])
                {
                    map[entry.stageId] = entry.milliseconds;
                }
            }

            return map;
        }

        public void SetBestClearDictionary(Dictionary<string, long> source)
        {
            bestClearMillisecondsByStage = new List<StageBestClearEntry>();
            if (source == null)
            {
                return;
            }

            foreach (var pair in source)
            {
                bestClearMillisecondsByStage.Add(new StageBestClearEntry
                {
                    stageId = pair.Key,
                    milliseconds = pair.Value
                });
            }
        }
    }

    [Serializable]
    public sealed class StageBestClearEntry
    {
        public string stageId;
        public long milliseconds;
    }

    [Serializable]
    public sealed class UiPreferencesData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public float bgmVolume = 1f;
        public float sfxVolume = 1f;
        public bool reduceMotion;
        public bool openingSeen;

        public static UiPreferencesData CreateDefault() => new UiPreferencesData();
    }

    public readonly struct SaveLoadResult<T>
    {
        public bool Success { get; }
        public T Data { get; }
        public string Error { get; }
        public bool UsedFallback { get; }

        public SaveLoadResult(bool success, T data, string error = null, bool usedFallback = false)
        {
            Success = success;
            Data = data;
            Error = error;
            UsedFallback = usedFallback;
        }

        public static SaveLoadResult<T> Ok(T data) => new SaveLoadResult<T>(true, data);
        public static SaveLoadResult<T> Recovered(T data, string error) =>
            new SaveLoadResult<T>(true, data, error, true);
    }

    public interface IProgressSaveRepository
    {
        SaveLoadResult<SaveData> Load();
        bool Save(SaveData data);
    }

    public interface IUiPreferencesRepository
    {
        SaveLoadResult<UiPreferencesData> Load();
        bool Save(UiPreferencesData data);
    }
}
