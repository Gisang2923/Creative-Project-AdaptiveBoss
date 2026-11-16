using System;
using UnityEngine;

public class GameProgression : MonoBehaviour
{
    public static GameProgression Instance { get; private set; }

    public const int MaxAttackLevel = 5;
    public const int MaxHealthLevel = 5;
    public const int MaxMentalLevel = 4;

    [Header("Game Phase")]
    [SerializeField]
    private GamePhase currentPhase =
        GamePhase.FirstEncounter;

    [Header("Growth")]
    [SerializeField, Range(0, MaxAttackLevel)]
    private int attackLevel = 0;

    [SerializeField, Range(0, MaxHealthLevel)]
    private int healthLevel = 0;

    // 0 = 첫 조우 전용 상태
    // 첫 강제 사망 후 자동으로 Lv.1
    [SerializeField, Range(0, MaxMentalLevel)]
    private int mentalLevel = 0;

    [SerializeField]
    private int growthPoints = 0;

    [Header("Run History")]
    [SerializeField]
    private int defeatCount = 0;

    [SerializeField]
    private int victoryCount = 0;

    [Header("Mental Test Value")]
    [Tooltip("0.1 = 해당 능력치 90% 감소")]
    [SerializeField, Range(0.01f, 1f)]
    
    private float suppressedStatMultiplier = 0.1f;

    [SerializeField]
    private int pendingGrowthPoints = 0;

    [Header("Presentation State")]
    [SerializeField]
    private bool firstRecoveryLineShown = false;

    [SerializeField]
    private int lastPresentedMentalLevel = 0;
    public int PendingGrowthPoints =>
        pendingGrowthPoints;
    public GamePhase CurrentPhase =>
        currentPhase;

    public int AttackLevel =>
        attackLevel;

    public int HealthLevel =>
        healthLevel;

    public int MentalLevel =>
        mentalLevel;

    public int GrowthPoints =>
        growthPoints;

    public int DefeatCount =>
        defeatCount;

    public int VictoryCount =>
        victoryCount;

    public bool IsFirstEncounter =>
        currentPhase ==
        GamePhase.FirstEncounter;

    public bool IsAdaptivePhase =>
        currentPhase ==
        GamePhase.Act2Adaptive;

    public bool IsCompleted =>
        currentPhase ==
        GamePhase.Completed;

    public bool PrecisionUnlocked =>
        mentalLevel >= MaxMentalLevel &&
        currentPhase != GamePhase.FirstEncounter;
    public bool AreAllStatsMax =>
        attackLevel >= MaxAttackLevel &&
        healthLevel >= MaxHealthLevel &&
        mentalLevel >= MaxMentalLevel;
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

        // 실제 GP를 바로 주지 않고
        // 사후세계 NPC에게 받을 보상으로 저장
        pendingGrowthPoints++;

        if (currentPhase ==
            GamePhase.FirstEncounter)
        {
            mentalLevel =
                Mathf.Max(
                    1,
                    mentalLevel
                );

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
            $"MENTAL {mentalLevel} | " +
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
            GamePhase.Completed)
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

    public bool TryUpgradeMental()
    {
        if (!CanSpendPoint() ||
            mentalLevel <= 0 ||
            mentalLevel >= MaxMentalLevel)
        {
            return false;
        }

        growthPoints--;
        mentalLevel++;

        NotifyChanged();

        return true;
    }

    public float GetMentalMoveMultiplier()
    {
        if (currentPhase ==
            GamePhase.FirstEncounter)
        {
            return 0f;
        }

        // Mental Lv.1만 이동속도 감소
        return mentalLevel <= 1
            ? suppressedStatMultiplier
            : 1f;
    }

    public float GetMentalHealthMultiplier()
    {
        if (currentPhase ==
            GamePhase.FirstEncounter)
        {
            return 0f;
        }

        // Mental Lv.1~2 체력 감소
        return mentalLevel <= 2
            ? suppressedStatMultiplier
            : 1f;
    }

    public float GetMentalAttackMultiplier()
    {
        if (currentPhase ==
            GamePhase.FirstEncounter)
        {
            return 0f;
        }

        // Mental Lv.1~3 공격력 감소
        return mentalLevel <= 3
            ? suppressedStatMultiplier
            : 1f;
    }
    public bool TryConsumeMentalBattleReaction(
        out int level,
        out bool isFirstRecovery)
    {
        level = mentalLevel;
        isFirstRecovery = false;

        if (currentPhase ==
            GamePhase.FirstEncounter)
        {
            return false;
        }

        if (mentalLevel <= 0)
            return false;


        // 첫 강제 사망 이후
        // 첫 정상 전투 진입
        if (!firstRecoveryLineShown)
        {
            firstRecoveryLineShown = true;

            lastPresentedMentalLevel =
                mentalLevel;

            isFirstRecovery = true;

            return true;
        }


        // 이전 전투 이후
        // Mental이 실제로 증가했을 때만 출력
        if (mentalLevel <=
            lastPresentedMentalLevel)
        {
            return false;
        }


        lastPresentedMentalLevel =
            mentalLevel;

        return true;
    }
    private bool CanSpendPoint()
    {
        return
            growthPoints > 0 &&
            currentPhase !=
                GamePhase.FirstEncounter &&
            currentPhase !=
                GamePhase.Act1Cleared &&
            currentPhase !=
                GamePhase.Completed;
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

        NotifyChanged();

        Debug.Log(
            "[Progression] " +
            "Act2 Adaptive Started"
        );
    }
#if UNITY_EDITOR

    [ContextMenu(
        "DEBUG/Reset Progression"
    )]
    private void ResetProgressionForDebug()
    {
        currentPhase =
            GamePhase.FirstEncounter;

        attackLevel = 0;
        healthLevel = 0;
        mentalLevel = 0;
        growthPoints = 0;
        pendingGrowthPoints = 0;

        defeatCount = 0;
        victoryCount = 0;

        firstRecoveryLineShown = false;
        lastPresentedMentalLevel = 0;
        NotifyChanged();

        OnPhaseChanged?
            .Invoke(
                currentPhase
            );

        Debug.Log(
            "[Progression] Debug Reset"
        );
    }

#endif
}