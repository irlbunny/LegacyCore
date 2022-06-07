using IllusionPlugin;
using System;
using System.IO;
using UnityEngine.SceneManagement;

namespace LegacyCore
{
    public class Plugin : IPlugin
    {
        public const string VersionString = "1.0.0";

        public string Name => "LegacyCore";
        public string Version => VersionString;

        public void OnApplicationStart()
        {
            CosturaUtility.Initialize();

            var userData = Path.Combine(Environment.CurrentDirectory, "UserData");

            Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Beat Saber_Data\\CustomLevels"));
            Directory.CreateDirectory(userData);

            var modPrefs = Path.Combine(userData, "modprefs.ini");
            if (!File.Exists(modPrefs))
                File.WriteAllText(modPrefs, string.Empty);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void OnApplicationQuit()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Loader.kMenuSceneName)
                Loader.OnLoad();
        }

        public void OnLevelWasLoaded(int level)
        { }

        public void OnLevelWasInitialized(int level)
        { }

        public void OnUpdate()
        { }

        public void OnFixedUpdate()
        { }
    }
}
