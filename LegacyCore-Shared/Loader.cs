using LegacyCore.Custom;
using LegacyCore.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LegacyCore
{
    public class Loader : MonoBehaviour
    {
        public const string kMenuSceneName = "Menu";

        public static Loader instance;

        public static event Action<Loader> loadingStartedEvent;
        public static event Action<Loader, List<CustomLevelSO>> levelsLoadedEvent;

        public static bool isLoading { get; private set; }
        public static bool isLoaded { get; private set; }

        public static float loadingProgress { get; private set; }

        public static List<CustomLevelSO> customLevels = new();

        private MainFlowCoordinator _mainFlowCoordinator;

        private Dictionary<GameplayMode, CustomLevelCollectionSO> _levelCollectionsForGameplayModes = new();
        private static CustomLevelCollectionsForGameplayModes _customLevelCollectionsForGameplayModes;

        private static readonly AudioClip _emptyAudioClip = AudioClip.Create("Empty", 1, 2, 1000, true);

        private readonly PoolSO<CustomLevelSO> _customLevelPool = new();
        private readonly PoolSO<CustomBeatmapDataSO> _beatmapDataPool = new();

        private ProgressBar _progressBar;

        private HMTask _loadingTask;
        private bool _loadingCanceled;

        public static void OnLoad()
        {
            if (instance == null)
                new GameObject("LegacyCore Loader").AddComponent<Loader>();
        }

        private void Awake()
        {
            if (instance != null)
            {
                LogUtil.Warning($"Instance of {GetType().Name} already exists, destroying.");
                DestroyImmediate(this);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this);

            CreateCustomCollections();

            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

            _progressBar = ProgressBar.Create();

            ReloadLevels();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // To prevent crashing, we must cancel loading (if we are currently loading)
            // and we're loading a new scene.
            if (isLoading && _loadingTask != null)
            {
                _loadingTask.Cancel();
                _loadingCanceled = true;

                isLoading = false;

                loadingProgress = 0;

                StopAllCoroutines();

                _progressBar.ShowMessage("Loading canceled.\n<size=80%>Press Ctrl+R to refresh.</size>");

                LogUtil.Info("Loading has been canceled. Reason: A new scene was loaded.");
            }

            if (scene.name == kMenuSceneName)
            {
                _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().FirstOrDefault();
                _mainFlowCoordinator.SetPrivateField("_levelCollectionsForGameplayModes", _customLevelCollectionsForGameplayModes);

                var standardLevelListViewController = Resources.FindObjectsOfTypeAll<StandardLevelListViewController>().FirstOrDefault();
                if (standardLevelListViewController != null)
                    standardLevelListViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
            }
        }

        private void OnDidSelectLevelEvent(StandardLevelListViewController standardLevelDetailViewController, IStandardLevel level)
        {
            if (level is CustomLevelSO customLevel)
            {
                if (customLevel.audioClip == _emptyAudioClip && !customLevel.isAudioClipLoading)
                {
                    var levels = standardLevelDetailViewController.GetPrivateField<IStandardLevel[]>("_levels").ToList();

                    StartCoroutine(customLevel.LoadAudioClip(delegate
                    {
                        standardLevelDetailViewController.SetPrivateField("_selectedLevel", null);
                        standardLevelDetailViewController.HandleLevelListTableViewDidSelectRow(null, levels.IndexOf(customLevel));
                    }));
                }
            }
        }

        private void CreateCustomCollections()
        {
            var levelCollectionsForGameplayModes = Resources.FindObjectsOfTypeAll<LevelCollectionsForGameplayModes>().FirstOrDefault();

            (_levelCollectionsForGameplayModes[GameplayMode.SoloStandard] = ScriptableObject.CreateInstance<CustomLevelCollectionSO>())
                .Init(levelCollectionsForGameplayModes.GetLevels(GameplayMode.SoloStandard));
            (_levelCollectionsForGameplayModes[GameplayMode.SoloOneSaber] = ScriptableObject.CreateInstance<CustomLevelCollectionSO>())
                .Init(levelCollectionsForGameplayModes.GetLevels(GameplayMode.SoloOneSaber));
            (_levelCollectionsForGameplayModes[GameplayMode.SoloNoArrows] = ScriptableObject.CreateInstance<CustomLevelCollectionSO>())
                .Init(levelCollectionsForGameplayModes.GetLevels(GameplayMode.SoloNoArrows));
            (_levelCollectionsForGameplayModes[GameplayMode.PartyStandard] = ScriptableObject.CreateInstance<CustomLevelCollectionSO>())
                .Init(levelCollectionsForGameplayModes.GetLevels(GameplayMode.PartyStandard));

            _customLevelCollectionsForGameplayModes = ScriptableObject.CreateInstance<CustomLevelCollectionsForGameplayModes>();
            _customLevelCollectionsForGameplayModes.SetCollections(new[]
            {
                new CustomLevelCollectionForGameplayMode(GameplayMode.SoloStandard, _levelCollectionsForGameplayModes[GameplayMode.SoloStandard]),
                new CustomLevelCollectionForGameplayMode(GameplayMode.SoloOneSaber, _levelCollectionsForGameplayModes[GameplayMode.SoloOneSaber]),
                new CustomLevelCollectionForGameplayMode(GameplayMode.SoloNoArrows, _levelCollectionsForGameplayModes[GameplayMode.SoloNoArrows]),
                new CustomLevelCollectionForGameplayMode(GameplayMode.PartyStandard, _levelCollectionsForGameplayModes[GameplayMode.PartyStandard])
            });
        }

        private CustomLevelInfo GetCustomLevelInfo(string customLevelPath)
        {
            CustomLevelInfo customLevelInfo;
            try
            {
                var infoJsonData = File.ReadAllText(Path.Combine(customLevelPath, "info.dat"));
                customLevelInfo = JsonConvert.DeserializeObject<CustomLevelInfo>(infoJsonData);
                customLevelInfo.levelPath = customLevelPath;
            }
            catch (Exception ex)
            {
                LogUtil.Error($"An exception occured while parsing song \"{customLevelPath}\": {ex}");
                return null;
            }

            return customLevelInfo;
        }

        private List<CustomLevelSO> LoadSong(CustomLevelInfo customLevelInfo)
        {
            try
            {
                var customLevels = new List<CustomLevelSO>();
                var characteristicCustomLevels = new Dictionary<CustomLevelSO.Characteristic, CustomLevelSO>();
                var characteristicDifficultyBeatmaps = new Dictionary<CustomLevelSO.Characteristic, List<StandardLevelSO.DifficultyBeatmap>>();

                foreach (var difficultyBeatmapSet in customLevelInfo.difficultyBeatmapSets)
                {
                    if (!Enum.TryParse<CustomLevelSO.Characteristic>(difficultyBeatmapSet.beatmapCharacteristicName, out var characteristic))
                        continue;

                    CustomLevelSO customLevel;
                    if (!characteristicCustomLevels.ContainsKey(characteristic))
                    {
                        customLevel = _customLevelPool.Get();
                        customLevel.Init(customLevelInfo, characteristic);
                        customLevel.SetAudioClip(_emptyAudioClip);
                        characteristicCustomLevels[characteristic] = customLevel;
                    }
                    else
                        customLevel = characteristicCustomLevels[characteristic];

                    List<StandardLevelSO.DifficultyBeatmap> difficultyBeatmaps;
                    if (!characteristicDifficultyBeatmaps.ContainsKey(characteristic))
                    {
                        difficultyBeatmaps = new List<StandardLevelSO.DifficultyBeatmap>();
                        characteristicDifficultyBeatmaps[characteristic] = difficultyBeatmaps;
                    }
                    else
                        difficultyBeatmaps = characteristicDifficultyBeatmaps[characteristic];

                    foreach (var difficultyBeatmap in difficultyBeatmapSet.difficultyBeatmaps)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(difficultyBeatmap.jsonData))
                            {
                                LogUtil.Warning($"Couldn't find or parse difficulty beatmap JSON for \"{difficultyBeatmap.beatmapFilename}\" in \"{customLevelInfo.levelPath}\".");
                                continue;
                            }

                            var beatmapData = _beatmapDataPool.Get();
                            beatmapData.SetJsonData(difficultyBeatmap.jsonData);

                            if (!Enum.TryParse<LevelDifficulty>(difficultyBeatmap.difficulty, out var difficulty))
                            {
                                LogUtil.Warning($"Couldn't parse difficulty text for \"{difficultyBeatmap.beatmapFilename}\" in \"{customLevelInfo.levelPath}\".");
                                continue;
                            }

                            difficultyBeatmaps.Add(new StandardLevelSO.DifficultyBeatmap(customLevel, difficulty, difficultyBeatmap.difficultyRank, difficultyBeatmap.noteJumpMovementSpeed, beatmapData));
                        }
                        catch (Exception ex)
                        {
                            LogUtil.Error($"An exception occured while parsing difficulty beatmap JSON for \"{difficultyBeatmap.beatmapFilename}\" in \"{customLevelInfo.levelPath}\": {ex}");
                        }
                    }
                }

                foreach (var characteristicCustomLevel in characteristicCustomLevels)
                {
                    var customLevel = characteristicCustomLevel.Value;
                    var difficultyBeatmaps = characteristicDifficultyBeatmaps[characteristicCustomLevel.Key];
                    if (difficultyBeatmaps != null && difficultyBeatmaps.Count > 0)
                    {
                        customLevel.SetDifficultyBeatmaps(difficultyBeatmaps.ToArray());
                        customLevel.LoadCoverImage();
                        customLevel.InitData();
                        customLevels.Add(customLevel);
                    }
                }

                return customLevels;
            }
            catch (Exception ex)
            {
                LogUtil.Error($"An exception occured while loading song \"{customLevelInfo.levelPath}\": {ex}");
            }

            return null;
        }

        private void RetrieveLevels(bool fullReload)
        {
            var levels = new List<CustomLevelSO>();

            if (fullReload)
            {
                _customLevelPool.ReturnAll();
                _beatmapDataPool.ReturnAll();

                customLevels.Clear();
            }

            _loadingTask = new HMTask(delegate
            {
                try
                {
                    var songFolders = Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data\\CustomLevels")).ToList();
                    var loadedIds = new List<string>();

                    var index = 0f;
                    foreach (var songFolder in songFolders)
                    {
                        index++;

                        var results = Directory.GetFiles(songFolder, "info.dat", SearchOption.AllDirectories);
                        if (results.Length == 0)
                        {
                            LogUtil.Warning($"Custom level \"{songFolder}\" is missing info.dat file(s)!");
                            continue;
                        }

                        foreach (var result in results)
                        {
                            try
                            {
                                var customLevelPath = Path.GetDirectoryName(result);
                                if (!fullReload)
                                {
                                    if (customLevels.Any(x => x.levelInfo.levelPath == customLevelPath))
                                        continue;
                                }

                                var customLevelInfo = GetCustomLevelInfo(customLevelPath);
                                if (customLevelInfo == null)
                                    continue;

                                var id = customLevelInfo.GetLevelData();
                                if (loadedIds.Any(x => x == id))
                                {
                                    LogUtil.Warning($"Duplicate level found at \"{songFolder}\".");
                                    continue;
                                }

                                loadedIds.Add(id);

                                var tempIndex = index;
                                HMMainThreadDispatcher.instance.Enqueue(delegate
                                {
                                    if (_loadingCanceled)
                                        return;

                                    var _levels = LoadSong(customLevelInfo);
                                    if (_levels != null)
                                        levels.AddRange(_levels);

                                    loadingProgress = tempIndex / songFolders.Count;
                                });
                            }
                            catch (Exception ex)
                            {
                                LogUtil.Error($"An exception occured while loading song folder \"{songFolder}\": {ex}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.Error($"An exception occured while retrieving songs: {ex}");
                }
            }, delegate
            {
                customLevels.AddRange(levels);
                customLevels = customLevels.OrderBy(x => x.songName).ToList();

                foreach (var customLevel in customLevels)
                {
                    switch (customLevel.characteristic)
                    {
                        case CustomLevelSO.Characteristic.Standard:
                            _levelCollectionsForGameplayModes[GameplayMode.SoloStandard].levelsList.Add(customLevel);
                            _levelCollectionsForGameplayModes[GameplayMode.PartyStandard].levelsList.Add(customLevel);
                            break;

                        case CustomLevelSO.Characteristic.NoArrows:
                            _levelCollectionsForGameplayModes[GameplayMode.SoloNoArrows].levelsList.Add(customLevel);
                            break;

                        case CustomLevelSO.Characteristic.OneSaber:
                            _levelCollectionsForGameplayModes[GameplayMode.SoloOneSaber].levelsList.Add(customLevel);
                            break;
                    }
                }

                isLoaded = true;
                isLoading = false;

                loadingProgress = 1;

                _loadingTask = null;

                if (levelsLoadedEvent != null)
                    levelsLoadedEvent(this, customLevels);
            });

            _loadingTask.Run();
        }

        public void ReloadLevels(bool fullReload = true)
        {
            if (SceneManager.GetActiveScene().name == kMenuSceneName && !isLoading)
            {
                LogUtil.Info(fullReload ? "Starting full song reload..." : "Starting song reload...");

                isLoaded = false;
                isLoading = true;

                loadingProgress = 0;

                _loadingCanceled = false;

                if (loadingStartedEvent != null)
                {
                    try
                    {
                        loadingStartedEvent(this);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.Error($"A plugin is causing \"loadingStartedEvent\" to throw an exception: {ex}");
                    }
                }

                foreach (var level in customLevels)
                {
                    _levelCollectionsForGameplayModes[GameplayMode.SoloStandard].levelsList.Remove(level);
                    _levelCollectionsForGameplayModes[GameplayMode.SoloOneSaber].levelsList.Remove(level);
                    _levelCollectionsForGameplayModes[GameplayMode.SoloNoArrows].levelsList.Remove(level);
                    _levelCollectionsForGameplayModes[GameplayMode.PartyStandard].levelsList.Remove(level);
                }

                RetrieveLevels(fullReload);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
                ReloadLevels(Input.GetKey(KeyCode.LeftControl));
        }
    }
}
