using System;
using System.Collections.Generic;

namespace ShadowGarden.Infrastructure
{
    public interface ISaveRepository
    {
        SaveLoadResult Load();
        void Save(MemorySaveData data);
    }

    public sealed class MemorySaveData
    {
        public int Version { get; set; } = 1;
        public List<string> ClearedStageIds { get; set; } = new List<string>();
        public string LastStageId { get; set; } = "1-1";
        public Dictionary<string, long> BestClearMillisecondsByStage { get; set; } =
            new Dictionary<string, long>();
    }

    public readonly struct SaveLoadResult
    {
        public bool Success { get; }
        public MemorySaveData Data { get; }
        public string Error { get; }

        public SaveLoadResult(bool success, MemorySaveData data, string error = null)
        {
            Success = success;
            Data = data;
            Error = error;
        }

        public static SaveLoadResult Ok(MemorySaveData data) => new SaveLoadResult(true, data);
        public static SaveLoadResult Fail(string error) => new SaveLoadResult(false, new MemorySaveData(), error);
    }

    /// <summary>
    /// TestField uses in-memory save only. Final PlayerPrefs wiring is out of scope.
    /// </summary>
    public sealed class MemorySaveRepository : ISaveRepository
    {
        private MemorySaveData _data = new MemorySaveData();

        public SaveLoadResult Load() => SaveLoadResult.Ok(Clone(_data));

        public void Save(MemorySaveData data)
        {
            _data = Clone(data ?? throw new ArgumentNullException(nameof(data)));
        }

        private static MemorySaveData Clone(MemorySaveData source)
        {
            var copy = new MemorySaveData
            {
                Version = source.Version,
                LastStageId = source.LastStageId,
                ClearedStageIds = new List<string>(source.ClearedStageIds),
                BestClearMillisecondsByStage = new Dictionary<string, long>(source.BestClearMillisecondsByStage)
            };
            return copy;
        }
    }
}
