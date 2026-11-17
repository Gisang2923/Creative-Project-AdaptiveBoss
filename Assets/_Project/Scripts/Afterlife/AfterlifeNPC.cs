using UnityEngine;
using UnityEngine.InputSystem;

public class AfterlifeNPC : MonoBehaviour
{
    private enum NpcState
    {
        FirstDialogue,

        MenuReady,
        MenuOpen,

        Growth,
        MaxStats,
        Farewell,

        ClearConfusion,
        ClearNpcResponse,
        ReturnAttempt,
        ReturnFailed,

        Act2DiscoveryPlayer,
        Act2DiscoveryNpc,

        Finished
    }
    [Header("Systems")]
    [SerializeField]
    private AfterlifeDialogueController
        dialogueController;

    [SerializeField]
    private AfterlifeGrowthUI
        growthUI;
    [SerializeField]
    private AfterlifeReturnAttemptController
        returnAttemptController;
    [SerializeField]
    private AfterlifeActionMenu
        actionMenu;    


    [Header("Dialogue")]
    [SerializeField]
    private string speakerName = "???";

    [SerializeField, TextArea(2, 4)]
    private string[] firstDialogueLines;

    [SerializeField, TextArea(2, 4)]
    private string[] farewellDialogueLines;

    [SerializeField, TextArea(2, 4)]
    private string[] maxStatsLines =
    {
        "더 올릴 능력치가 없다."
    };
    [Header("Act1 Clear Event")]
    [SerializeField]
    private string playerSpeakerName =
        "나";

    [SerializeField, TextArea(2, 4)]
    private string[] clearPlayerLines =
    {
        "왜... 다시 여기로 온 거죠?",
        "분명 그자를 쓰러뜨렸는데."
    };

    [SerializeField, TextArea(2, 4)]
    private string[] clearNpcLines =
    {
        "이런 적은 없었는데...",
        "어째서 이러는 거지?",
        "잠시 기다리게.",
        "내가 정상적으로 돌아갈 수 있도록 해보겠네."
    };

    [SerializeField, TextArea(2, 4)]
    private string[] returnFailedLines =
    {
        "...안 되는군.",
        "아무래도 다시 한 번 그놈을 처리해야 할 것 같아.",
        "그대에게는 어려운 일은 아니겠지?"
    };

    private static readonly string[] act2DiscoveryPlayerLines =
    {
        "저 사람... 이상해요.",
        "제가 어떻게 움직일지 알고 있는 것 같아요.",
        "쫓아가면 먼저 빠지고...",
        "제가 공격하려는 순간도 기다리고 있어요."
    };

    private static readonly string[] act2DiscoveryNpcLines =
    {
        "...잠깐.",
        "자네가 쫓아가면 먼저 물러난다고?",
        "...그 움직임을 알고 있네.",
        "나도 그렇게 싸웠으니까."
    };

    [Header("UI")]
    [SerializeField]
    private GameObject interactPrompt;

    private PlayerMovement
        currentPlayerMovement;

    private bool playerInRange;

    private NpcState state =
        NpcState.FirstDialogue;

    private void Start()
    {
        GameProgression progression =
            GameProgression.Instance;

        if (progression != null &&
            progression.IsAct1Cleared)
        {
            state =
                NpcState.ClearConfusion;
        }
        else if (progression != null &&
                 progression.HasPendingAct2FirstDefeatDialogue)
        {
            state =
                NpcState.Act2DiscoveryPlayer;
        }
        SetPrompt(false);

        if (dialogueController != null)
        {
            dialogueController
                .OnDialogueEnded +=
                HandleDialogueEnded;
        }

        if (growthUI != null)
        {
            growthUI.OnGrowthClosed +=
                HandleGrowthClosed;
        }
        if (actionMenu != null)
        {
            actionMenu.OnStatusSelected +=
                HandleStatusSelected;

            actionMenu.OnReturnSelected +=
                HandleReturnSelected;
        }
    }

    private void OnDestroy()
    {
        if (dialogueController != null)
        {
            dialogueController
                .OnDialogueEnded -=
                HandleDialogueEnded;
        }

        if (growthUI != null)
        {
            growthUI.OnGrowthClosed -=
                HandleGrowthClosed;
        }
        if (actionMenu != null)
        {
            actionMenu.OnStatusSelected -=
                HandleStatusSelected;

            actionMenu.OnReturnSelected -=
                HandleReturnSelected;
        }
    }

    private void Update()
    {
        if (!playerInRange)
            return;


        // UI / 연출 진행 중에는
        // NPC 상호작용 차단
        if (state ==
                NpcState.Growth ||
            state ==
                NpcState.MenuOpen ||
            state ==
                NpcState.ReturnAttempt ||
            state ==
                NpcState.Finished)
        {
            return;
        }


        if (dialogueController == null)
            return;


        if (dialogueController.IsOpen)
            return;


        if (Keyboard.current == null)
            return;


        if (!Keyboard.current
            .eKey
            .wasPressedThisFrame)
        {
            return;
        }


        // =====================================
        // 첫 대화 이후부터는
        // NPC 상호작용 = Action Menu
        // =====================================

        if (state ==
            NpcState.MenuReady)
        {
            OpenActionMenu();
            return;
        }


        StartCurrentDialogue();
    }

    private void StartCurrentDialogue()
    {
        SetPrompt(false);

        currentPlayerMovement?
            .SetInputLocked(true);

        switch (state)
        {
            case NpcState.FirstDialogue:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        firstDialogueLines
                    );

                break;

            case NpcState.MaxStats:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        maxStatsLines
                    );

                break;

            case NpcState.Farewell:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        farewellDialogueLines
                    );

                break;


            case NpcState.ClearConfusion:

                dialogueController
                    .StartDialogue(
                        playerSpeakerName,
                        clearPlayerLines
                    );

                break;


            case NpcState.ClearNpcResponse:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        clearNpcLines
                    );

                break;


            case NpcState.ReturnFailed:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        returnFailedLines
                    );

                break;

            case NpcState.Act2DiscoveryPlayer:

                dialogueController
                    .StartDialogue(
                        playerSpeakerName,
                        act2DiscoveryPlayerLines
                    );

                break;

            case NpcState.Act2DiscoveryNpc:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        act2DiscoveryNpcLines
                    );

                break;
        }
    }

    private void HandleDialogueEnded()
    {
        if (state ==
            NpcState.Act2DiscoveryPlayer)
        {
            state =
                NpcState.Act2DiscoveryNpc;

            StartCurrentDialogue();

            return;
        }

        if (state ==
            NpcState.Act2DiscoveryNpc)
        {
            GameProgression progression =
                GameProgression.Instance;

            progression?
                .CompleteAct2FirstDefeatDialogue();

            progression?
                .ClaimGrowthPoint();

            EnterMenuReady();

            return;
        }

        // =====================================
        // Act1 Cleared Special Route
        // =====================================

        if (state ==
            NpcState.ClearConfusion)
        {
            state =
                NpcState.ClearNpcResponse;

            StartCurrentDialogue();

            return;
        }


        if (state ==
            NpcState.ClearNpcResponse)
        {
            state =
                NpcState.ReturnAttempt;

            returnAttemptController?
                .PlayAttempt(
                    HandleReturnAttemptFailed
                );

            return;
        }


        if (state ==
            NpcState.ReturnFailed)
        {
            GameProgression.Instance?
                .BeginAct2Adaptive();

            EnterMenuReady();

            return;
        }

        if (state ==
            NpcState.MaxStats)
        {
            EnterMenuReady();

            return;
        }
        // =====================================
        // Normal Death Route
        // =====================================

        if (state ==
            NpcState.FirstDialogue)
        {
            GameProgression.Instance?
                .ClaimGrowthPoint();


            Debug.Log(
                "[Afterlife] " +
                "First Dialogue Complete"
            );


            EnterMenuReady();

            return;
        }


        if (state ==
            NpcState.Farewell)
        {
            state =
                NpcState.Finished;

            SetPrompt(false);

            if (returnAttemptController != null)
            {
                returnAttemptController
                    .PlayReturn(
                        HandleReturnPresentationComplete
                    );
            }
            else
            {
                HandleReturnPresentationComplete();
            }

            return;
        }
    }
    private void EnterMenuReady()
    {
        state =
            NpcState.MenuReady;


        // 다시 사후세계를 돌아다닐 수 있음
        currentPlayerMovement?
            .SetInputLocked(false);


        if (playerInRange)
        {
            SetPrompt(true);
        }


        Debug.Log(
            "[Afterlife] Menu Ready"
        );
    }
    private void OpenActionMenu()
    {
        if (actionMenu == null)
        {
            Debug.LogWarning(
                "[Afterlife] " +
                "ActionMenu is missing."
            );

            return;
        }


        SetPrompt(false);


        state =
            NpcState.MenuOpen;


        actionMenu.Open(
            currentPlayerMovement
        );


        Debug.Log(
            "[Afterlife] " +
            "Action Menu Open"
        );
    }
    private void HandleStatusSelected()
    {
        if (state !=
            NpcState.MenuOpen)
        {
            return;
        }


        GameProgression progression =
            GameProgression.Instance;


        if (progression != null &&
            progression.AreAllStatsMax)
        {
            state =
                NpcState.MaxStats;

            StartCurrentDialogue();

            Debug.Log(
                "[Afterlife] " +
                "All Stats Max"
            );

            return;
        }


        state =
            NpcState.Growth;


        growthUI?
            .Open(
                currentPlayerMovement
            );


        Debug.Log(
            "[Afterlife] " +
            "Status Selected"
        );
    }
    private void HandleReturnAttemptFailed()
    {
        if (state !=
            NpcState.ReturnAttempt)
        {
            return;
        }

        Debug.Log(
            "[Afterlife] " +
            "Return Attempt Failed"
        );

        state =
            NpcState.ReturnFailed;

        StartCurrentDialogue();
    }
    private void HandleGrowthClosed()
    {
        Debug.Log(
            "[Afterlife] " +
            "Growth UI Closed"
        );


        EnterMenuReady();
    }
    private void HandleReturnPresentationComplete()
    {
        GameFlowController.Instance?
            .ReturnToBattle();
    }
    private void HandleReturnSelected()
    {
        if (state !=
            NpcState.MenuOpen)
        {
            return;
        }


        state =
            NpcState.Farewell;


        StartCurrentDialogue();


        Debug.Log(
            "[Afterlife] " +
            "Return Selected"
        );
    }
    private void OnTriggerEnter2D(
        Collider2D other)
    {
        PlayerMovement movement =
            other.GetComponentInParent<
                PlayerMovement>();

        if (movement == null)
            return;

        currentPlayerMovement =
            movement;

        playerInRange = true;

        if (state ==
                NpcState.FirstDialogue ||
            state ==
                NpcState.MenuReady ||
            state ==
                NpcState.ClearConfusion ||
            state ==
                NpcState.Act2DiscoveryPlayer)
        {
            SetPrompt(true);
        }
    }

    private void OnTriggerExit2D(
        Collider2D other)
    {
        PlayerMovement movement =
            other.GetComponentInParent<
                PlayerMovement>();

        if (movement == null)
            return;

        if (movement !=
            currentPlayerMovement)
        {
            return;
        }

        playerInRange = false;

        SetPrompt(false);
    }

    private void SetPrompt(
        bool visible)
    {
        interactPrompt?
            .SetActive(visible);
    }
}
