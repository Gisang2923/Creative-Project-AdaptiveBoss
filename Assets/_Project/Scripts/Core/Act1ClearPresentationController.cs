using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Act1ClearPresentationController
    : MonoBehaviour
{
    public static
        Act1ClearPresentationController
        Instance
    {
        get;
        private set;
    }

    [Header("UI")]
    [SerializeField]
    private GameObject clearPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text messageText;

    [SerializeField]
    private Button confirmButton;


    [Header("Player")]
    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerInput playerInput;


    [Header("Text")]
    [SerializeField]
    private string clearTitle =
        "클리어했습니다.";

    [SerializeField, TextArea]
    private string clearMessage =
        "잠시 후 메인 화면으로 돌아갑니다...";


    private bool isPlaying;


    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        clearPanel?
            .SetActive(false);

        confirmButton?
            .onClick
            .AddListener(
                Confirm
            );
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        confirmButton?
            .onClick
            .RemoveListener(
                Confirm
            );
    }


    public void Play()
    {
        if (isPlaying)
            return;

        isPlaying = true;

        playerMovement?
            .SetInputLocked(true);

        playerInput?
            .DeactivateInput();

        if (titleText != null)
        {
            titleText.text =
                clearTitle;
        }

        if (messageText != null)
        {
            messageText.text =
                clearMessage;
        }

        clearPanel?
            .SetActive(true);

        Debug.Log(
            "[Act1 Clear] " +
            "Presentation Start"
        );
    }


    private void Confirm()
    {
        if (!isPlaying)
            return;

        confirmButton.interactable =
            false;

        Debug.Log(
            "[Act1 Clear] " +
            "Return To Main Requested"
        );

        GameFlowController.Instance?
            .CompleteAct1ClearPresentation();
    }
}