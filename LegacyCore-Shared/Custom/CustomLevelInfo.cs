using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace LegacyCore.Custom
{
    [JsonObject(MemberSerialization.OptIn)]
    public class CustomLevelInfo
    {
        public string levelPath { get; set; }

        [JsonProperty("_songName")] public string songName { get; set; }
        [JsonProperty("_songSubName")] public string songSubName { get; set; }
        [JsonProperty("_songAuthorName")] public string songAuthorName { get; set; }

        [JsonProperty("_levelAuthorName")] public string levelAuthorName { get; set; }

        [JsonProperty("_beatsPerMinute")] public float beatsPerMinute { get; set; }

        [JsonProperty("_songTimeOffset")] public float songTimeOffset { get; set; }

        [JsonProperty("_shuffle")] public float shuffle { get; set; }
        [JsonProperty("_shufflePeriod")] public float shufflePeriod { get; set; }

        [JsonProperty("_previewStartTime")] public float previewStartTime { get; set; }
        [JsonProperty("_previewDuration")] public float previewDuration { get; set; }

        [JsonProperty("_songFilename")] public string songFilename { get; set; }

        [JsonProperty("_coverImageFilename")] public string coverImageFilename { get; set; }

        [JsonProperty("_environmentName")] public string environmentName { get; set; }

        [JsonProperty("_allDirectionsEnvironmentName")] public string allDirectionsEnvironmentName { get; set; }

        [JsonProperty("_difficultyBeatmapSets")] public DifficultyBeatmapSet[] difficultyBeatmapSets { get; set; }

        [JsonObject(MemberSerialization.OptIn)]
        public class DifficultyBeatmapSet
        {
            [JsonProperty("_beatmapCharacteristicName")] public string beatmapCharacteristicName { get; set; }

            [JsonProperty("_difficultyBeatmaps")] public DifficultyBeatmap[] difficultyBeatmaps { get; set; }
        }

        [JsonObject(MemberSerialization.OptIn)]
        public class DifficultyBeatmap
        {
            public string jsonData { get; set; }

            [JsonProperty("_difficulty")] public string difficulty { get; set; }
            [JsonProperty("_difficultyRank")] public int difficultyRank { get; set; }

            [JsonProperty("_beatmapFilename")] public string beatmapFilename { get; set; }

            [JsonProperty("_noteJumpMovementSpeed")] public float noteJumpMovementSpeed { get; set; }
            [JsonProperty("_noteJumpStartBeatOffset")] public float noteJumpStartBeatOffset { get; set; }
        }

        private string CreateHash(byte[] input)
            => BitConverter.ToString(SHA1.Create().ComputeHash(input)).Replace("-", string.Empty);

        public string GetLevelData()
        {
            var combined = new List<byte>();
            combined.AddRange(File.ReadAllBytes(Path.Combine(levelPath, "info.dat")));

            for (var i = 0; i < difficultyBeatmapSets.Length; i++)
            {
                for (var j = 0; j < difficultyBeatmapSets[i].difficultyBeatmaps.Length; j++)
                {
                    var beatmapPath = Path.Combine(levelPath, difficultyBeatmapSets[i].difficultyBeatmaps[j].beatmapFilename);
                    if (File.Exists(beatmapPath))
                    {
                        combined.AddRange(File.ReadAllBytes(beatmapPath));

                        difficultyBeatmapSets[i].difficultyBeatmaps[j].jsonData = File.ReadAllText(beatmapPath);
                    }
                }
            }

            return $"custom_level_{CreateHash(combined.ToArray())}";
        }
    }
}
