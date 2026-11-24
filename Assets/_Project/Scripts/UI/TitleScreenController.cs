using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string firstSceneName = "BossBattle";

    [Header("Main Menu")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button settingsBackButton;

    [Header("Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField, Min(0f)] private float fadeDuration = 0.45f;

    private bool transitionStarted;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(AudioListener.volume);
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    private void OnDestroy()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (settingsPanel != null &&
            settingsPanel.activeSelf &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseSettings();
        }
    }

    public void StartGame()
    {
        if (transitionStarted)
            return;

        StartCoroutine(StartGameRoutine());
    }

    public void OpenSettings()
    {
        if (transitionStarted || settingsPanel == null)
            return;

        settingsPanel.SetActive(true);
        SelectButton(settingsBackButton);
    }

    public void CloseSettings()
    {
        if (settingsPanel == null)
            return;

        settingsPanel.SetActive(false);
        SelectButton(settingsButton);
    }

    public void ExitGame()
    {
        if (transitionStarted)
            return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SetMasterVolume(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value);
    }

    private IEnumerator StartGameRoutine()
    {
        transitionStarted = true;

        if (startButton != null)
            startButton.interactable = false;
        if (settingsButton != null)
            settingsButton.interactable = false;
        if (exitButton != null)
            exitButton.interactable = false;

        if (fadeCanvasGroup != null && fadeDuration > 0f)
        {
            fadeCanvasGroup.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeCanvasGroup.alpha =
                    Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        SceneManager.LoadScene(firstSceneName);
    }

    private static void SelectButton(Button button)
    {
        if (button == null)
            return;

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(button.gameObject);

        button.Select();
    }
}
