using LegacyCore.Custom;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LegacyCore
{
    public class ProgressBar : MonoBehaviour
    {
        private static readonly Vector3 _position = new(0, 2.5f, 2.5f);
        private static readonly Vector3 _rotation = new(0, 0, 0);
        private static readonly Vector3 _scale = new(.01f, .01f, .01f);

        private static readonly Vector2 _canvasSize = new(100, 50);

        private const string kAuthorNameText = "Kaitlyn's";
        private const float kAuthorNameFontSize = 7f;

        private static readonly Vector2 _authorNamePosition = new(0, 28);

        private const string kPluginNameText = "LegacyCore <size=75%>v" + Plugin.VersionString + "</size>";
        private const float kPluginNameFontSize = 9f;

        private static readonly Vector2 _pluginNamePosition = new(0, 22);

        private const string kHeaderText = "Loading songs...";
        private const float kHeaderFontSize = 15f;

        private static readonly Vector2 _headerPosition = new(0, 15);
        private static readonly Vector2 _headerSize = new(100, 20);

        private static readonly Vector2 _loadingBarSize = new(100, 10);
        private static readonly Color _backgroundColor = new(0, 0, 0, .2f);

        private Canvas _canvas;

        private TMP_Text _authorNameText;
        private TMP_Text _pluginNameText;
        private TMP_Text _headerText;

        private Image _loadingBackground;
        private Image _loadingBar;

        private bool _showingMessage;

        public static ProgressBar Create()
            => new GameObject("Progress Bar").AddComponent<ProgressBar>();

        public void ShowMessage(string message, float time)
        {
            StopAllCoroutines();

            _showingMessage = true;
            _headerText.text = message;
            _loadingBar.enabled = false;
            _loadingBackground.enabled = false;
            _canvas.enabled = true;

            StartCoroutine(DisableCanvasRoutine(time));
        }

        public void ShowMessage(string message)
        {
            StopAllCoroutines();

            _showingMessage = true;
            _headerText.text = message;
            _loadingBar.enabled = false;
            _loadingBackground.enabled = false;
            _canvas.enabled = true;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;

            Loader.loadingStartedEvent += OnLoadingStartedEvent;
            Loader.levelsLoadedEvent += OnLevelsLoadedEvent;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            Loader.loadingStartedEvent -= OnLoadingStartedEvent;
            Loader.levelsLoadedEvent -= OnLevelsLoadedEvent;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == Loader.kMenuSceneName)
            {
                if (_showingMessage)
                    _canvas.enabled = true;
            }
            else
                _canvas.enabled = false;
        }

        private void OnLoadingStartedEvent(Loader loader)
        {
            StopAllCoroutines();

            _showingMessage = false;
            _headerText.text = kHeaderText;
            _loadingBar.enabled = true;
            _loadingBackground.enabled = true;
            _canvas.enabled = true;
        }

        private void OnLevelsLoadedEvent(Loader loader, List<CustomLevelSO> customLevels)
        {
            _showingMessage = false;
            _headerText.text = $"{customLevels.Count} songs loaded.";
            _loadingBar.enabled = false;
            _loadingBackground.enabled = false;

            StartCoroutine(DisableCanvasRoutine(5));
        }

        private IEnumerator DisableCanvasRoutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);

            _canvas.enabled = false;
            _showingMessage = false;
        }

        private void Awake()
        {
            gameObject.transform.position = _position;
            gameObject.transform.eulerAngles = _rotation;
            gameObject.transform.localScale = _scale;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;

            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = _canvasSize;

            _authorNameText = new GameObject("Author Name").AddComponent<TextMeshProUGUI>();

            rectTransform = _authorNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = _authorNamePosition;
            rectTransform.sizeDelta = _headerSize;

            _authorNameText.text = kAuthorNameText;
            _authorNameText.fontSize = kAuthorNameFontSize;

            _pluginNameText = new GameObject("Plugin Name").AddComponent<TextMeshProUGUI>();

            rectTransform = _pluginNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = _pluginNamePosition;
            rectTransform.sizeDelta = _headerSize;

            _pluginNameText.text = kPluginNameText;
            _pluginNameText.fontSize = kPluginNameFontSize;

            _headerText = new GameObject("Header").AddComponent<TextMeshProUGUI>();

            rectTransform = _headerText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = _headerPosition;
            rectTransform.sizeDelta = _headerSize;

            _headerText.text = kHeaderText;
            _headerText.fontSize = kHeaderFontSize;

            _loadingBackground = new GameObject("Background").AddComponent<Image>();

            rectTransform = _loadingBackground.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = _loadingBarSize;

            _loadingBackground.color = _backgroundColor;

            _loadingBar = new GameObject("Loading Bar").AddComponent<Image>();

            rectTransform = _loadingBar.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = _loadingBarSize;

            var texture = Texture2D.whiteTexture;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f);

            _loadingBar.sprite = sprite;
            _loadingBar.type = Image.Type.Filled;
            _loadingBar.fillMethod = Image.FillMethod.Horizontal;
            _loadingBar.color = new Color(1, 1, 1, .5f);

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_canvas.enabled)
                return;

            _loadingBar.fillAmount = Loader.loadingProgress;
        }
    }
}
