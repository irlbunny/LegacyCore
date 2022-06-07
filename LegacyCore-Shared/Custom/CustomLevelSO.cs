using LegacyCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LegacyCore.Custom
{
    public class CustomLevelSO : StandardLevelSO, IResetableSO
    {
        private static readonly List<SceneInfo> kSceneInfos = new List<SceneInfo>();

        public enum Characteristic
        {
            Standard,
            NoArrows,
            OneSaber
        }

        private static readonly Dictionary<string, AudioClip> _cachedAudioClips = new();
        private static readonly Dictionary<string, Sprite> _cachedCoverImages = new();

        public CustomLevelInfo levelInfo { get; private set; }
        public Characteristic characteristic { get; private set; }

        public bool isAudioClipLoading { get; set; }

        public void Init(CustomLevelInfo levelInfo, Characteristic characteristic = Characteristic.Standard)
        {
            this.levelInfo = levelInfo;
            this.characteristic = characteristic;

            _levelID = levelInfo.GetLevelData();
            _songName = levelInfo.songName;
            _songSubName = levelInfo.songSubName;
            _songAuthorName = $"{levelInfo.songAuthorName} [{levelInfo.levelAuthorName}]";
            _beatsPerMinute = levelInfo.beatsPerMinute;
            _songTimeOffset = levelInfo.songTimeOffset;
            _shuffle = levelInfo.shuffle;
            _shufflePeriod = levelInfo.shufflePeriod;
            _previewStartTime = levelInfo.previewStartTime;
            _previewDuration = levelInfo.previewDuration;
            _environmentSceneInfo = LoadSceneInfo(levelInfo.environmentName);
        }

        public SceneInfo GetSceneInfo(string environmentName)
        {
            var sceneInfo = kSceneInfos.FirstOrDefault(x => x.sceneName == environmentName);
            if (sceneInfo != null)
                return sceneInfo;

            sceneInfo = Resources.FindObjectsOfTypeAll<SceneInfo>().FirstOrDefault(x => x.sceneName == environmentName);
            if (sceneInfo == null)
                return GetSceneInfo("DefaultEnvironment");

            kSceneInfos.Add(sceneInfo);
            return sceneInfo;
        }

        private SceneInfo LoadSceneInfo(string environmentName)
        {
            return GetSceneInfo(environmentName);
        }

        private string GetEscapedUrl(string path)
            => $"file:///{WWW.EscapeURL(path)}";

        public IEnumerator LoadAudioClip(Action callback)
        {
            var audioClipPath = Path.Combine(levelInfo.levelPath, levelInfo.songFilename);

            AudioClip audioClip;
            if (!_cachedAudioClips.ContainsKey(audioClipPath))
            {
                using (var www = new WWW(GetEscapedUrl(audioClipPath)))
                {
                    isAudioClipLoading = true;
                    yield return www;

                    audioClip = www.GetAudioClip(true, true, levelInfo.songFilename.Contains(".egg") ? AudioType.OGGVORBIS : AudioType.UNKNOWN);

                    var timeout = Time.realtimeSinceStartup + 5;
                    while (audioClip.length == 0)
                    {
                        if (Time.realtimeSinceStartup > timeout)
                        {
                            LogUtil.Warning($"Loading audio clip \"{audioClip.name}\" timed out.");
                            break;
                        }

                        yield return null;
                    }

                    _cachedAudioClips.Add(audioClipPath, audioClip);
                }
            }
            else
                audioClip = _cachedAudioClips[audioClipPath];

            _audioClip = audioClip;

            callback.Invoke();

            isAudioClipLoading = false;
        }

        public void SetAudioClip(AudioClip audioClip)
            => _audioClip = audioClip;

        public void LoadCoverImage()
        {
            var coverImagePath = Path.Combine(levelInfo.levelPath, levelInfo.coverImageFilename);

            Sprite coverImage;
            if (!_cachedCoverImages.ContainsKey(coverImagePath))
            {
                if (!File.Exists(coverImagePath))
                    return;

                var texture = new Texture2D(1, 1);
                if (!texture.LoadImage(File.ReadAllBytes(coverImagePath), true))
                {
                    LogUtil.Warning($"Failed to load cover image \"{coverImagePath}\".");
                    return;
                }

                coverImage = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f);
                _cachedCoverImages.Add(coverImagePath, coverImage);
            }
            else
                coverImage = _cachedCoverImages[coverImagePath];

            _coverImage = coverImage;
        }

        public void SetDifficultyBeatmaps(DifficultyBeatmap[] difficultyBeatmaps)
            => _difficultyBeatmaps = difficultyBeatmaps;

        public void Reset()
            => _audioClip = null;
    }
}
