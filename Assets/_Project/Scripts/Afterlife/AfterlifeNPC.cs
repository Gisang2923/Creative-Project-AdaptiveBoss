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
        GrowthLocked,
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
        Act2TruthPlayerQuestion,
        Act2TruthNpcAdvice,
        Act2NormalDefeat,
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
    private string[] firstDialogueLines = CreateDefaultFirstDialogueLines();

    [SerializeField, TextArea(2, 4)]
    private string[] act1RecoveryDialogueLines = CreateDefaultAct1RecoveryLines();

    [SerializeField]
    private Act1RecoveryDialogueEntry[] act1RecoveryDialogueByStage =
    {
        new Act1RecoveryDialogueEntry
        {
            stage = 2,
            lines = CreateDefaultStage2RecoveryLines()
        },
        new Act1RecoveryDialogueEntry
        {
            stage = 3,
            lines = CreateDefaultStage3RecoveryLines()
        },
        new Act1RecoveryDialogueEntry
        {
            stage = GameProgression.MaxFearRecoveryStage,
            lines = CreateDefaultStage4RecoveryLines()
        }
    };

    [SerializeField, TextArea(2, 4)]
    private string[] farewellDialogueLines = CreateDefaultFarewellLines();

    [SerializeField, TextArea(2, 4)]
    private string[] maxStatsLines = CreateDefaultMaxStatsLines();

    [SerializeField, TextArea(2, 4)]
    private string[] growthLockedLines = CreateDefaultGrowthLockedLines();
    [Header("Act1 Clear Event")]
    [SerializeField]
    private string playerSpeakerName =
        "나";

    [SerializeField, TextArea(2, 4)]
    private string[] clearPlayerLines = CreateDefaultClearPlayerLines();

    [SerializeField, TextArea(2, 4)]
    private string[] clearNpcLines = CreateDefaultClearNpcLines();

    [SerializeField, TextArea(2, 4)]
    private string[] returnFailedLines = CreateDefaultReturnFailedLines();

    [Header("Act2 Anomaly Discovery Event")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2DiscoveryPlayerLines = CreateDefaultAct2DiscoveryPlayerLines();

    [SerializeField, TextArea(2, 4)]
    private string[] act2DiscoveryNpcLines = CreateDefaultAct2DiscoveryNpcLines();

    [Header("Act2 Anomaly Confirmation Event")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthNpcIntroLines = CreateDefaultAct2ConfirmationNpcIntroLines();

    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthPlayerLines = CreateDefaultAct2ConfirmationPlayerLines();

    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthNpcConclusionLines = CreateDefaultAct2ConfirmationNpcConclusionLines();

    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthPlayerQuestionLines = CreateDefaultAct2ConfirmationPlayerQuestionLines();

    [SerializeField, TextArea(2, 4)]
    private string[] act2TruthNpcAdviceLines = CreateDefaultAct2ConfirmationNpcAdviceLines();

    [Header("Act2 Normal Defeat")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2NormalDefeatLines = CreateDefaultAct2NormalDefeatLines();

    [Header("Act2 Repeat Defeat")]
    [SerializeField, TextArea(2, 4)]
    private string[] act2RepeatDefeatLines = CreateDefaultAct2RepeatDefeatLines();

    private static string[] CreateDefaultFirstDialogueLines() => new[]
    {
        "눈을 떴군.",
        "나는 자네를 그곳으로 돌려보내는 역할을 맡았네.",
        "다시 나아가겠는가?",
        "나아갈 의지가 있다면, 나는 자네가 다시 도전할 준비를 돕겠네."
    };

    private static string[] CreateDefaultAct1RecoveryLines() => new[]
    {
        "다시 돌아왔군.",
        "아직 나아갈 의지가 남아 있는가?",
        "그렇다면 준비하게. 나는 자네가 다시 나아갈 수 있도록 도울 것이니."
    };

    private static string[] CreateDefaultStage2RecoveryLines() => new[]
    {
        "다시 돌아왔군.",
        "처음 왔을 때보다 상태가 조금 나아진 것 같군.",
        "다시 나아갈 의지가 있다면 돌려보내겠네."
    };

    private static string[] CreateDefaultStage3RecoveryLines() => new[]
    {
        "돌아왔군.",
        "무슨 일이 있었는지는 모르겠지만, 자네가 조금씩 달라지고 있는 것은 알겠네.",
        "아직 끝을 보려는 마음은 변함없는가?"
    };

    private static string[] CreateDefaultStage4RecoveryLines() => new[]
    {
        "……이제야 자네가 본래의 모습을 되찾은 것 같군.",
        "처음 이곳에 왔을 때와는 확실히 다르네.",
        "그렇다면 나도 자네에게 조금 더 도움을 줄 수 있겠어.",
        "자네가 쌓아온 경험을 바탕으로, 그 힘을 더욱 단련하도록 돕겠네.",
        "무엇을 강화할지는 자네가 선택하게."
    };

    private static string[] CreateDefaultGrowthLockedLines() => new[]
    {
        "아직은 힘을 더 단련할 때가 아니네.",
        "다시 나아갈 준비를 마치면 그때 도와주겠네."
    };

    private static string[] CreateDefaultFarewellLines() => new[]
    {
        "준비가 됐다면 다시 가게.",
        "이번에는 조금 더 나아갈 수 있겠지."
    };

    private static string[] CreateDefaultMaxStatsLines() => new[]
    {
        "지금 자네에게 더 보탤 것은 없군."
    };

    private static string[] CreateDefaultClearPlayerLines() => new[]
    {
        "왜… 다시 여기로 온 거죠?",
        "분명 그 사람을 쓰러뜨렸는데."
    };

    private static string[] CreateDefaultClearNpcLines() => new[]
    {
        "……이상하군.",
        "원래라면 자네가 다시 이곳에 올 이유는 없네.",
        "잠시 있어보게. 다시 돌려보낼 수 있는지 확인해보지."
    };

    private static string[] CreateDefaultReturnFailedLines() => new[]
    {
        "……안 되는군.",
        "돌아갈 길은 남아 있어.",
        "다만 자네가 향해야 할 곳이 달라진 모양이군.",
        "이유는 나도 모르겠네.",
        "다시 가보게. 그곳에서 확인하는 수밖에 없겠어."
    };

    private static string[] CreateDefaultAct2DiscoveryPlayerLines() => new[]
    {
        "저 사람… 이상해요.",
        "같은 사람을 상대하고 있는데, 싸울수록 조금씩 이질감이 들어요."
    };

    private static string[] CreateDefaultAct2DiscoveryNpcLines() => new[]
    {
        "이질감이라…",
        "저자는 원래 그런 식으로 달라지는 존재가 아니네.",
        "싸움의 방식에는 정해진 결이 있어. 몇 번을 다시 마주해도 그 틀을 쉽게 벗어나지는 않지.",
        "자네가 느낀 것이 단순한 착각인지, 아니면 정말 저쪽이 달라지고 있는 건지…",
        "한 번 더 확인해보게."
    };

    private static string[] CreateDefaultAct2ConfirmationNpcIntroLines() => new[]
    {
        "이번에도 같은 이질감이 있었나?"
    };

    private static string[] CreateDefaultAct2ConfirmationPlayerLines() => new[]
    {
        "네.",
        "처음에는 착각한 줄 알았는데… 아닌 것 같아요."
    };

    private static string[] CreateDefaultAct2ConfirmationNpcConclusionLines() => new[]
    {
        "……그렇다면 이상하군.",
        "저자는 싸울수록 달라지고 있어.",
        "그런 일은 원래 일어나지 않네."
    };

    private static string[] CreateDefaultAct2ConfirmationPlayerQuestionLines() => new[]
    {
        "그럼 어떻게 해야 하죠?"
    };

    private static string[] CreateDefaultAct2ConfirmationNpcAdviceLines() => new[]
    {
        "원인은 나도 모르겠네.",
        "다만 상대가 달라지고 있다면, 자네도 같은 방식만 고집해서는 안 되겠지.",
        "이번에는 자네 쪽에서도 변화를 줘보게."
    };

    private static string[] CreateDefaultAct2NormalDefeatLines() => new[]
    {
        "또 돌아왔군.",
        "서두르지 말게.",
        "이번에는 조금 더 보고 움직여보게."
    };

    private static string[] CreateDefaultAct2RepeatDefeatLines() => new[]
    {
        "또 돌아왔군.",
        "상대가 달라진다면 자네도 달라져야 하네.",
        "같은 답만 고집하지 말게."
    };

#if UNITY_EDITOR
    [ContextMenu("Apply Default NPC Dialogue")]
    private void ApplyDefaultNpcDialogue()
    {
        UnityEditor.Undo.RecordObject(this, "Apply Default NPC Dialogue");

        firstDialogueLines = CreateDefaultFirstDialogueLines();
        act1RecoveryDialogueLines = CreateDefaultAct1RecoveryLines();
        act1RecoveryDialogueByStage = new[]
        {
            new Act1RecoveryDialogueEntry
            {
                stage = 2,
                lines = CreateDefaultStage2RecoveryLines()
            },
            new Act1RecoveryDialogueEntry
            {
                stage = 3,
                lines = CreateDefaultStage3RecoveryLines()
            },
            new Act1RecoveryDialogueEntry
            {
                stage = GameProgression.MaxFearRecoveryStage,
                lines = CreateDefaultStage4RecoveryLines()
            }
        };
        farewellDialogueLines = CreateDefaultFarewellLines();
        maxStatsLines = CreateDefaultMaxStatsLines();
        growthLockedLines = CreateDefaultGrowthLockedLines();
        clearPlayerLines = CreateDefaultClearPlayerLines();
        clearNpcLines = CreateDefaultClearNpcLines();
        returnFailedLines = CreateDefaultReturnFailedLines();
        act2DiscoveryPlayerLines = CreateDefaultAct2DiscoveryPlayerLines();
        act2DiscoveryNpcLines = CreateDefaultAct2DiscoveryNpcLines();
        act2TruthNpcIntroLines = CreateDefaultAct2ConfirmationNpcIntroLines();
        act2TruthPlayerLines = CreateDefaultAct2ConfirmationPlayerLines();
        act2TruthNpcConclusionLines = CreateDefaultAct2ConfirmationNpcConclusionLines();
        act2TruthPlayerQuestionLines = CreateDefaultAct2ConfirmationPlayerQuestionLines();
        act2TruthNpcAdviceLines = CreateDefaultAct2ConfirmationNpcAdviceLines();
        act2NormalDefeatLines = CreateDefaultAct2NormalDefeatLines();
        act2RepeatDefeatLines = CreateDefaultAct2RepeatDefeatLines();

        UnityEditor.EditorUtility.SetDirty(this);

        if (!Application.isPlaying && gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }
#endif

    [Header("UI")]
    [SerializeField]
    private GameObject interactPrompt;

    private PlayerMovement
        currentPlayerMovement;

    private AfterlifeWillMessage willMessage;

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
                 (progression.WillAdvanceFearRecovery ||
                  progression.PendingGrowthPoints > 0) &&
                 progression.HasMetAfterlifeNpc)
        {
            state =
                NpcState.Act1RecoveryDialogue;
        }
        else if (progression != null &&
                 progression.CurrentPhase == GamePhase.Act2Adaptive &&
                 progression.PendingGrowthPoints > 0 &&
                 !progression.HasConfirmedAct2Anomaly)
        {
            state =
                NpcState.Act2NormalDefeat;
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

            case NpcState.GrowthLocked:
                dialogueController.StartDialogue(
                    speakerName,
                    growthLockedLines
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

            case NpcState.Act2TruthPlayerQuestion:

                dialogueController
                    .StartDialogue(
                        playerSpeakerName,
                        act2TruthPlayerQuestionLines
                    );

                break;

            case NpcState.Act2TruthNpcAdvice:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        act2TruthNpcAdviceLines
                    );

                break;

            case NpcState.Act2NormalDefeat:

                dialogueController
                    .StartDialogue(
                        speakerName,
                        act2NormalDefeatLines
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
            state =
                NpcState.Act2TruthPlayerQuestion;

            StartCurrentDialogue();

            return;
        }

        if (state ==
            NpcState.Act2TruthPlayerQuestion)
        {
            state =
                NpcState.Act2TruthNpcAdvice;

            StartCurrentDialogue();

            return;
        }

        if (state ==
            NpcState.Act2TruthNpcAdvice)
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
            NpcState.Act2NormalDefeat)
        {
            GameProgression.Instance?
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

        if (state == NpcState.MaxStats ||
            state == NpcState.GrowthLocked)
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

        int previousStage = progression != null
            ? progression.FearRecoveryStage
            : 0;
        bool advanced = false;

        if (progression != null)
        {
            advanced = progression.AdvanceFearRecovery();
            progression.ClaimGrowthPoint();
        }

        EnterMenuReady();

        if (!advanced ||
            progression.FearRecoveryStage <= previousStage ||
            dialogueController == null ||
            dialogueController.IsOpen)
        {
            return;
        }

        int stage = progression.FearRecoveryStage;
        if (!progression.ShouldPresentFearRecoveryMessage(stage))
            return;

        if (willMessage == null)
        {
            willMessage = AfterlifeWillMessage.GetOrCreate(
                dialogueController.DialogueText
            );
        }

        willMessage?.TryShow(stage, HandleWillMessageDisplayed);
    }

    private void HandleWillMessageDisplayed(int stage)
    {
        GameProgression.Instance?
            .TryMarkFearRecoveryMessagePresented(stage);
    }

    private string[] GetAct1RecoveryDialogueLines()
    {
        GameProgression progression =
            GameProgression.Instance;

        if (progression == null ||
            !progression.WillAdvanceFearRecovery ||
            act1RecoveryDialogueByStage == null)
        {
            return act1RecoveryDialogueLines;
        }

        int nextStage = progression.FearRecoveryStage + 1;

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
            !progression.StatsUpgradeUnlocked)
        {
            state = NpcState.GrowthLocked;
            StartCurrentDialogue();
            return;
        }


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
                NpcState.Act2NormalDefeat ||
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
