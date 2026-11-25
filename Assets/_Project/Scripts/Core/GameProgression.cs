using System;
using UnityEngine;

public class GameProgression : MonoBehaviour
{
    public static GameProgression Instance { get; private set; }

    public const int MaxAttackLevel = 5;
    public const int MaxHealthLevel = 5;
    public const int MaxFearRecoveryStage = 4;
    public const int PrecisionUnlockStage = 4;

    [Header("Game Phase")]
    [SerializeField]
    private GamePhase currentPhase =
        GamePhase.FirstEncounter;

    [Header("Growth")]
    [SerializeField, Range(0, MaxAttackLevel)]
    private int attackLevel = 0;

    [SerializeField, Range(0, MaxHealthLevel)]
    private int healthLevel = 0;

    [SerializeField, Range(0, MaxFearRecoveryStage)]
    private int fearRecoveryStage = 0;

    [SerializeField]
    private bool fearRecoveryPending = false;

    [SerializeField]
    private int growthPoints = 0;

    [Header("Run History")]
    [SerializeField]
    private int defeatCount = 0;

    [SerializeField]
    private int act2DefeatCount = 0;

    [SerializeField]
    private int victoryCount = 0;

    [SerializeField]
    private int pendingGrowthPoints = 0;

    [Header("Presentation State")]
    [SerializeField]
    private int lastPresentedFearRecoveryStage = 0;
    [SerializeField]
    private bool act2IntroShown = false;
    [SerializeField]
    private bool act2FirstDefeatDialoguePending = false;
    [SerializeField]
    private bool act2FirstDefeatDialogueShown = false;
    [SerializeField]
    private bool act2TruthDialoguePending = false;
    [SerializeField]
    private bool act2TruthDialogueShown = false;
    [SerializeField]
    private bool act2AdaptiveBehaviorExperiencedThisRun = false;
    [SerializeField]
    private bool afterlifeNpcMet = false;
    public int PendingGrowthPoints =>
        pendingGrowthPoints;
    public GamePhase CurrentPhase =>
        currentPhase;

    public int AttackLevel =>
        attackLevel;

    public int HealthLevel =>
        healthLevel;

    public int FearRecoveryStage =>
        fearRecoveryStage;

    public int GrowthPoints =>
        growthPoints;

    public int DefeatCount =>
        defeatCount;

    public int Act2DefeatCount =>
        act2DefeatCount;

    public int VictoryCount =>
        victoryCount;

    public bool IsFirstEncounter =>
        currentPhase ==
        GamePhase.FirstEncounter;

    public bool IsAdaptivePhase =>
        currentPhase ==
        GamePhase.Act2Adaptive;

    public bool HasPendingAct2FirstDefeatDialogue =>
        currentPhase == GamePhase.Act2Adaptive &&
        act2FirstDefeatDialoguePending &&
        !act2FirstDefeatDialogueShown;

    public bool HasPendingAct2TruthDialogue =>
        currentPhase == GamePhase.Act2Adaptive &&
        act2TruthDialoguePending &&
        !act2TruthDialogueShown;

    public bool HasConfirmedAct2Anomaly =>
        act2TruthDialogueShown;

    public bool HasMetAfterlifeNpc =>
        afterlifeNpcMet;

    public bool IsCompleted =>
        currentPhase ==
        GamePhase.Completed;

    public bool PrecisionUnlocked =>
        fearRecoveryStage >= PrecisionUnlockStage &&
        currentPhase != GamePhase.FirstEncounter;
    public bool AreAllStatsMax =>
        attackLevel >= MaxAttackLevel &&
        healthLevel >= MaxHealthLevel;
    public bool StatsUpgradeUnlocked =>
        currentPhase == GamePhase.Act2Adaptive ||
        (currentPhase == GamePhase.Act1 &&
         fearRecoveryStage >= MaxFearRecoveryStage);
    public bool WillAdvanceFearRecovery =>
        currentPhase == GamePhase.Act1 &&
        fearRecoveryPending &&
        fearRecoveryStage < MaxFearRecoveryStage;
    public event Action OnProgressionChanged;
    public event Action<GamePhase> OnPhaseChanged;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Existing serialized progress can contain stages from the old 0-8 scale.
        fearRecoveryStage = Mathf.Clamp(
            fearRecoveryStage, 0, MaxFearRecoveryStage
        );
        lastPresentedFearRecoveryStage = Mathf.Clamp(
            lastPresentedFearRecoveryStage, 0, MaxFearRecoveryStage
        );

        DontDestroyOnLoad(
            gameObject
        );
    }

    public void RegisterDefeat()
    {
        if (currentPhase ==
            GamePhase.Completed)
        {
            return;
        }

        defeatCount++;

        // The first three required defeats grant no GP. The Stage 3 defeat
        // grants the first point when the NPC confirms Stage 4.
        if (currentPhase != GamePhase.FirstEncounter &&
            (currentPhase != GamePhase.Act1 ||
             fearRecoveryStage >= MaxFearRecoveryStage - 1))
        {
            pendingGrowthPoints++;
        }

        if (currentPhase == GamePhase.FirstEncounter ||
            (currentPhase == GamePhase.Act1 &&
             fearRecoveryStage < MaxFearRecoveryStage))
        {
            fearRecoveryPending = true;
        }

        if (currentPhase == GamePhase.Act2Adaptive)
        {
            act2DefeatCount++;

            if (act2AdaptiveBehaviorExperiencedThisRun &&
                !act2FirstDefeatDialoguePending &&
                !act2FirstDefeatDialogueShown)
            {
                act2FirstDefeatDialoguePending = true;
            }
            else if (act2AdaptiveBehaviorExperiencedThisRun &&
                     act2FirstDefeatDialogueShown &&
                     !act2TruthDialoguePending &&
                     !act2TruthDialogueShown)
            {
                act2TruthDialoguePending = true;
            }

            act2AdaptiveBehaviorExperiencedThisRun = false;
        }

        if (currentPhase ==
            GamePhase.FirstEncounter)
        {
            SetPhase(
                GamePhase.Act1
            );
        }

        NotifyChanged();

        Debug.Log(
            $"[Progression] Defeat | " +
            $"Pending GP {pendingGrowthPoints} | " +
            $"GP {growthPoints} | " +
            $"ATK {attackLevel} | " +
            $"HP {healthLevel} | " +
            $"FEAR RECOVERY {fearRecoveryStage} | " +
            $"Phase {currentPhase}"
        );
    }
    public bool ClaimGrowthPoint()
    {
        if (pendingGrowthPoints <= 0)
            return false;

        pendingGrowthPoints--;
        growthPoints++;

        NotifyChanged();

        Debug.Log(
            $"[Progression] Growth Point Claimed | " +
            $"GP {growthPoints} | " +
            $"Pending {pendingGrowthPoints}"
        );

        return true;
    }
    public void RegisterVictory()
    {
        if (currentPhase ==
            GamePhase.Completed ||
            (currentPhase == GamePhase.Act1 &&
             fearRecoveryStage < MaxFearRecoveryStage))
        {
            return;
        }

        victoryCount++;

        // 1막 첫 승리:
        // 아직 Adaptive로 넘어가지 않는다.
        if (currentPhase ==
            GamePhase.Act1)
        {
            SetPhase(
                GamePhase.Act1Cleared
            );
        }
        // 2막 최종 승리
        else if (currentPhase ==
                GamePhase.Act2Adaptive)
        {
            SetPhase(
                GamePhase.Completed
            );
        }

        NotifyChanged();

        Debug.Log(
            $"[Progression] Victory | " +
            $"Victories {victoryCount} | " +
            $"Phase {currentPhase}"
        );
    }

    public bool TryUpgradeAttack()
    {
        if (!CanSpendPoint() ||
            attackLevel >= MaxAttackLevel)
        {
            return false;
        }

        growthPoints--;
        attackLevel++;

        NotifyChanged();

        return true;
    }

    public bool TryUpgradeHealth()
    {
        if (!CanSpendPoint() ||
            healthLevel >= MaxHealthLevel)
        {
            return false;
        }

        growthPoints--;
        healthLevel++;

        NotifyChanged();

        return true;
    }

    public bool AdvanceFearRecovery()
    {
        if (currentPhase != GamePhase.Act1 ||
            !fearRecoveryPending)
        {
            return false;
        }

        fearRecoveryPending = false;

        if (fearRecoveryStage >= MaxFearRecoveryStage)
            return false;

        fearRecoveryStage++;
        NotifyChanged();
        return true;
    }

    public bool ShouldPresentFearRecoveryMessage(int stage)
    {
        return currentPhase == GamePhase.Act1 &&
            stage > 0 &&
            stage <= MaxFearRecoveryStage &&
            stage == fearRecoveryStage &&
            stage > lastPresentedFearRecoveryStage;
    }

    public bool TryMarkFearRecoveryMessagePresented(int stage)
    {
        if (!ShouldPresentFearRecoveryMessage(stage))
            return false;

        // Commit only after the AfterLife UI reports that it displayed the text.
        lastPresentedFearRecoveryStage = stage;
        return true;
    }

    public bool TryConsumeAct2Intro()
    {
        if (currentPhase !=
            GamePhase.Act2Adaptive)
        {
            return false;
        }

        if (act2IntroShown)
            return false;

        act2IntroShown = true;

        return true;
    }
    public void MarkAct2AdaptiveBehaviorExperienced()
    {
        if (currentPhase == GamePhase.Act2Adaptive)
        {
            act2AdaptiveBehaviorExperiencedThisRun = true;
        }
    }
    public void CompleteAct2FirstDefeatDialogue()
    {
        if (!HasPendingAct2FirstDefeatDialogue)
            return;

        act2FirstDefeatDialoguePending = false;
        act2FirstDefeatDialogueShown = true;
    }
    public void CompleteAct2TruthDialogue()
    {
        if (!HasPendingAct2TruthDialogue)
            return;

        act2TruthDialoguePending = false;
        act2TruthDialogueShown = true;
    }
    public void MarkAfterlifeNpcMet()
    {
        afterlifeNpcMet = true;
    }
    private bool CanSpendPoint()
    {
        return growthPoints > 0 && StatsUpgradeUnlocked;
    }

    private void SetPhase(
        GamePhase nextPhase)
    {
        if (currentPhase ==
            nextPhase)
        {
            return;
        }

        currentPhase =
            nextPhase;

        OnPhaseChanged?
            .Invoke(
                currentPhase
            );
    }

    private void NotifyChanged()
    {
        OnProgressionChanged?
            .Invoke();
    }
    public bool IsAct1Cleared =>
        currentPhase ==
        GamePhase.Act1Cleared;

    public void BeginAct2Adaptive()
    {
        if (currentPhase !=
            GamePhase.Act1Cleared)
        {
            return;
        }

        SetPhase(
            GamePhase.Act2Adaptive
        );

        act2AdaptiveBehaviorExperiencedThisRun = false;

        NotifyChanged();

        Debug.Log(
            "[Progression] " +
            "Act2 Adaptive Started"
        );
    }

    public void ResetForNewGame()
    {
        currentPhase = GamePhase.FirstEncounter;

        attackLevel = 0;
        healthLevel = 0;
        fearRecoveryStage = 0;
        fearRecoveryPending = false;
        growthPoints = 0;
        pendingGrowthPoints = 0;

        defeatCount = 0;
        act2DefeatCount = 0;
        victoryCount = 0;

        lastPresentedFearRecoveryStage = 0;
        act2IntroShown = false;
        act2FirstDefeatDialoguePending = false;
        act2FirstDefeatDialogueShown = false;
        act2TruthDialoguePending = false;
        act2TruthDialogueShown = false;
        act2AdaptiveBehaviorExperiencedThisRun = false;
        afterlifeNpcMet = false;

        NotifyChanged();
        OnPhaseChanged?.Invoke(currentPhase);
    }
#if UNITY_EDITOR

    [ContextMenu(
        "DEBUG/Reset Progression"
    )]
    private void ResetProgressionForDebug()
    {
        ResetForNewGame();

        Debug.Log(
            "[Progression] Debug Reset"
        );
    }

#endif
}
