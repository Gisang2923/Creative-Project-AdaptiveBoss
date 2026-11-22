using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AfterlifeNPC : MonoBehaviour
{
    [Serializable]
    private class Act1RecoveryDialogueEntry
    {
        [Range(1, GameProgression.MaxFearRecoveryStage)]
        public int stage = 1;

        [TextArea(2, 4)]
        public string[] lines = Array.Empty<string>();
    }

    private enum NpcState
    {
        FirstDialogue,
        Act1RecoveryDialogue,

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
        Act2TruthNpcIntro,
        Act2TruthPlayer,
        Act2TruthNpcConclusion,
        Act2RepeatDefeat,

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
    private string[] act1RecoveryDialogueLines =
    {
        "다시 돌아왔군."
    };

    [SerializeField]
    private Act1RecoveryDialogueEntry[] act1RecoveryDialogueByStage =
        Array.Empty<Act1RecoveryDialogueEntry>();

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

    [Header("Act2 First Defeat Event")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2DiscoveryPlayerLines =
    {
        "저 사람... 이상해요.",
        "제가 어떻게 움직일지 알고 있는 것 같아요.",
        "쫓아가면 먼저 빠지고...",
        "제가 공격하려는 순간도 기다리고 있어요."
    };

    [SerializeField, TextArea(2, 4)]
    private string[] act2DiscoveryNpcLines =
    {
        "...잠깐.",
        "자네가 쫓아가면 먼저 물러난다고?",
        "...그 움직임을 알고 있네.",
        "나도 그렇게 싸웠으니까."
    };

    [Header("Act2 Truth Discovery Event")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthNpcIntroLines =
    {
        "...잠깐.",
        "이제 알 것 같군.",
        "저자는 처음부터 저렇게 싸웠던 게 아니야.",
        "우리에게서 배운 거였어."
    };

    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthPlayerLines =
    {
        "우리에게서요?"
    };

    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthNpcConclusionLines =
    {
        "그래.",
        "우리가 죽을 때마다,",
        "그자에게 무언가를 남기고 있었던 거야.",
        "내 움직임도,",
        "그리고 지금은 자네의 움직임도.",
        "강해지는 것만으로는 부족하겠군.",
        "놈이 알고 있는 자네와",
        "다른 사람이 되게."
    };

    [Header("Act2 Repeat Defeat")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2RepeatDefeatLines =
    {
        "다시 왔군.",
        "아직 놈이 알고 있는 방식대로 움직이고 있는 건가."
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
        else if (progression != null &&
                 progression.HasPendingAct2TruthDialogue)
        {
            state =
                NpcState.Act2TruthNpcIntro;
        }
        else if (progression != null &&
                 progression.CurrentPhase == GamePhase.Act1 &&
                 progression.PendingGrowthPoints > 0 &&
                 progression.HasMetAfterlifeNpc)
        {
            state =
                NpcState.Act1RecoveryDialogue;
        }
        else if (progression != null &&
                 progression.CurrentPhase == GamePhase.Act2Adaptive &&
                 progression.PendingGrowthPoints > 0)
        {
            state =
                NpcState.Act2RepeatDefeat;
        }
        else if (progression == null ||
                 !progression.HasMetAfterlifeNpc)
        {
            state =
                NpcState.FirstDialogue;
        }
        else
        {
            progression.ClaimGrowthPoint();
            state =
                NpcState.MenuReady;
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

            case NpcState.Act1RecoveryDialogue:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        GetAct1RecoveryDialogueLines()
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

            case NpcState.Act2TruthNpcIntro:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        act2TruthNpcIntroLines
                    );

                break;

            case NpcState.Act2TruthPlayer:

                dialogueController
                    .StartDialogue(
                        playerSpeakerName,
                        act2TruthPlayerLines
                    );

                break;

            case NpcState.Act2TruthNpcConclusion:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        act2TruthNpcConclusionLines
                    );

                break;

            case NpcState.Act2RepeatDefeat:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        act2RepeatDefeatLines
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
                .MarkAfterlifeNpcMet();

            progression?
                .ClaimGrowthPoint();

            EnterMenuReady();

            return;
        }

        if (state ==
            NpcState.Act2TruthNpcIntro)
        {
            state =
                NpcState.Act2TruthPlayer;

            StartCurrentDialogue();

            return;
        }

        if (state ==
            NpcState.Act2TruthPlayer)
        {
            state =
                NpcState.Act2TruthNpcConclusion;

            StartCurrentDialogue();

            return;
        }

        if (state ==
            NpcState.Act2TruthNpcConclusion)
        {
            GameProgression progression =
                GameProgression.Instance;

            progression?
                .CompleteAct2TruthDialogue();

            progression?
                .ClaimGrowthPoint();

            EnterMenuReady();

            return;
        }

        if (state ==
            NpcState.Act2RepeatDefeat)
        {
            GameProgression.Instance?
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

            GameProgression.Instance?
                .MarkAfterlifeNpcMet();

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
                .MarkAfterlifeNpcMet();

            Debug.Log(
                "[Afterlife] " +
                "First Dialogue Complete"
            );

            CompleteAct1DeathVisit();

            return;
        }

        if (state ==
            NpcState.Act1RecoveryDialogue)
        {
            CompleteAct1DeathVisit();

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
    private void CompleteAct1DeathVisit()
    {
        GameProgression progression =
            GameProgression.Instance;

        if (progression != null &&
            progression.ClaimGrowthPoint())
        {
            progression.AdvanceFearRecovery();
        }

        EnterMenuReady();
    }
    private string[] GetAct1RecoveryDialogueLines()
    {
        GameProgression progression =
            GameProgression.Instance;

        if (progression == null ||
            act1RecoveryDialogueByStage == null)
        {
            return act1RecoveryDialogueLines;
        }

        int nextStage = Mathf.Min(
            progression.FearRecoveryStage + 1,
            GameProgression.MaxFearRecoveryStage
        );

        foreach (Act1RecoveryDialogueEntry entry in
                 act1RecoveryDialogueByStage)
        {
            if (entry != null &&
                entry.stage == nextStage &&
                entry.lines != null &&
                entry.lines.Length > 0)
            {
                return entry.lines;
            }
        }

        return act1RecoveryDialogueLines;
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
                NpcState.Act1RecoveryDialogue ||
            state ==
                NpcState.MenuReady ||
            state ==
                NpcState.ClearConfusion ||
            state ==
                NpcState.Act2DiscoveryPlayer ||
            state ==
                NpcState.Act2TruthNpcIntro ||
            state ==
                NpcState.Act2RepeatDefeat)
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
        if (interactPrompt == null)
            return;

        interactPrompt.SetActive(
            visible
        );
    }
}
