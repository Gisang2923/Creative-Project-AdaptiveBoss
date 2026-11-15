using System;
using UnityEngine;
using UnityEngine.UI;

public class AfterlifeActionMenu :
    MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private GameObject menuPanel;

    [SerializeField]
    private Button statusButton;

    [SerializeField]
    private Button returnButton;


    private PlayerMovement playerMovement;


    public bool IsOpen =>
        menuPanel != null &&
        menuPanel.activeSelf;


    public event Action OnStatusSelected;
    public event Action OnReturnSelected;


    private void Awake()
    {
        menuPanel?
            .SetActive(false);

        statusButton?
            .onClick
            .AddListener(
                SelectStatus
            );

        returnButton?
            .onClick
            .AddListener(
                SelectReturn
            );
    }


    public void Open(
        PlayerMovement movement)
    {
        if (IsOpen)
            return;

        playerMovement =
            movement;

        playerMovement?
            .SetInputLocked(true);

        menuPanel?
            .SetActive(true);

        statusButton?
            .Select();

        Debug.Log(
            "[Afterlife Menu] Open"
        );
    }


    private void SelectStatus()
    {
        if (!IsOpen)
            return;

        menuPanel?
            .SetActive(false);

        OnStatusSelected?
            .Invoke();
    }


    private void SelectReturn()
    {
        if (!IsOpen)
            return;

        menuPanel?
            .SetActive(false);

        OnReturnSelected?
            .Invoke();
    }


    public void Close()
    {
        menuPanel?
            .SetActive(false);
    }
}