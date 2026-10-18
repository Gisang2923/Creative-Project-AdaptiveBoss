using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviorModel : MonoBehaviour
{
    [Header("Habit Settings")]
    [SerializeField] private int minAttackSamples = 3;
    [SerializeField] private int minResponseSamples = 2;

    [SerializeField, Range(0f, 1f)]
    private float usageWeight = 0.45f;

    [SerializeField, Range(0f, 1f)]
    private float successWeight = 0.35f;

    [SerializeField, Range(0f, 1f)]
    private float recencyWeight = 0.20f;

    [Header("Recency")]
    [SerializeField] private int recencyWindow = 5;

    [Header("Spatial Habit")]
    [SerializeField] private int spatialWindow = 6;
    [SerializeField] private int minSpatialSamples = 3;

    public float GetHabitScore(
        BossAttackType bossAttack,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int attackCount =
            GetAttackCount(logs, bossAttack);

        int responseCount =
            GetResponseCount(
                logs,
                bossAttack,
                response
            );

        if (attackCount < minAttackSamples ||
            responseCount < minResponseSamples)
        {
            return 0f;
        }

        float totalWeight =
            usageWeight +
            successWeight +
            recencyWeight;

        if (totalWeight <= 0f)
            return 0f;

        float score =
            (
                GetUsageRate(
                    bossAttack,
                    response
                ) * usageWeight +
                GetSuccessRate(
                    bossAttack,
                    response
                ) * successWeight +
                GetRecencyScore(
                    bossAttack,
                    response
                ) * recencyWeight
            ) / totalWeight;

        return Mathf.Clamp01(score);
    }

    public float GetBehaviorHabitScore(
        BossBehaviorType bossBehavior,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int behaviorCount =
            GetBehaviorCount(
                logs,
                bossBehavior
            );

        int responseCount =
            GetBehaviorResponseCount(
                logs,
                bossBehavior,
                response
            );

        if (behaviorCount < minAttackSamples ||
            responseCount < minResponseSamples)
        {
            return 0f;
        }

        float totalWeight =
            usageWeight +
            successWeight +
            recencyWeight;

        if (totalWeight <= 0f)
            return 0f;

        float score =
            (
                GetBehaviorUsageRate(
                    bossBehavior,
                    response
                ) * usageWeight +
                GetBehaviorSuccessRate(
                    bossBehavior,
                    response
                ) * successWeight +
                GetBehaviorRecencyScore(
                    bossBehavior,
                    response
                ) * recencyWeight
            ) / totalWeight;

        return Mathf.Clamp01(score);
    }

    public float GetBehaviorUsageRate(
        BossBehaviorType bossBehavior,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null)
            return 0f;

        int behaviorCount =
            GetBehaviorCount(
                logs,
                bossBehavior
            );

        if (behaviorCount == 0)
            return 0f;

        int responseCount =
            GetBehaviorResponseCount(
                logs,
                bossBehavior,
                response
            );

        return (float)responseCount /
            behaviorCount;
    }

    public float GetBehaviorSuccessRate(
        BossBehaviorType bossBehavior,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null)
            return 0f;

        int responseCount = 0;
        int successCount = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossBehavior != bossBehavior ||
                log.playerResponse != response)
            {
                continue;
            }

            responseCount++;

            if (log.result ==
                CombatResultType.Completed)
            {
                successCount++;
            }
        }

        if (responseCount == 0)
            return 0f;

        return (float)successCount /
            responseCount;
    }

    public float GetUsageRate(
        BossAttackType bossAttack,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null)
            return 0f;

        int attackCount =
            GetAttackCount(
                logs,
                bossAttack
            );

        if (attackCount == 0)
            return 0f;

        int responseCount =
            GetResponseCount(
                logs,
                bossAttack,
                response
            );

        return (float)responseCount /
            attackCount;
    }

    public float GetSuccessRate(
        BossAttackType bossAttack,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null)
            return 0f;

        int responseCount = 0;
        int successCount = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossBehavior !=
                    BossBehaviorType.Attack ||
                log.bossAttack !=
                    bossAttack ||
                log.playerResponse !=
                    response)
            {
                continue;
            }

            responseCount++;

            if (WasSuccessful(log))
                successCount++;
        }

        if (responseCount == 0)
            return 0f;

        return (float)successCount /
            responseCount;
    }

    public float GetRecencyScore(
        BossAttackType bossAttack,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        List<AttackResponseLog> recent =
            new List<AttackResponseLog>();

        for (int i = logs.Count - 1;
             i >= 0 &&
             recent.Count < recencyWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior ==
                    BossBehaviorType.Attack &&
                log.bossAttack ==
                    bossAttack)
            {
                recent.Add(log);
            }
        }

        return GetWeightedResponseRecency(
            recent,
            response
        );
    }

    public float GetBehaviorRecencyScore(
        BossBehaviorType bossBehavior,
        PlayerResponseType response)
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        List<AttackResponseLog> recent =
            new List<AttackResponseLog>();

        for (int i = logs.Count - 1;
             i >= 0 &&
             recent.Count < recencyWindow;
             i--)
        {
            if (logs[i].bossBehavior ==
                bossBehavior)
            {
                recent.Add(logs[i]);
            }
        }

        return GetWeightedResponseRecency(
            recent,
            response
        );
    }

    private float GetWeightedResponseRecency(
        List<AttackResponseLog> recent,
        PlayerResponseType response)
    {
        if (recent == null ||
            recent.Count == 0)
        {
            return 0f;
        }

        float matchedWeight = 0f;
        float totalWeight = 0f;

        for (int i = 0;
             i < recent.Count;
             i++)
        {
            float weight =
                recent.Count - i;

            totalWeight += weight;

            if (recent[i].playerResponse ==
                response)
            {
                matchedWeight += weight;
            }
        }

        if (totalWeight <= 0f)
            return 0f;

        return matchedWeight /
            totalWeight;
    }

    public PlayerResponseType GetDominantResponse(
        BossAttackType bossAttack)
    {
        PlayerResponseType best =
            PlayerResponseType.None;

        float bestScore = 0f;

        foreach (PlayerResponseType response
                 in System.Enum.GetValues(
                     typeof(PlayerResponseType)))
        {
            if (response ==
                PlayerResponseType.None)
            {
                continue;
            }

            float score =
                GetHabitScore(
                    bossAttack,
                    response
                );

            if (score > bestScore)
            {
                bestScore = score;
                best = response;
            }
        }

        return best;
    }

    public PlayerResponseType GetDominantBehaviorResponse(
        BossBehaviorType bossBehavior)
    {
        PlayerResponseType best =
            PlayerResponseType.None;

        float bestScore = 0f;

        foreach (PlayerResponseType response
                 in System.Enum.GetValues(
                     typeof(PlayerResponseType)))
        {
            if (response ==
                PlayerResponseType.None)
            {
                continue;
            }

            float score =
                GetBehaviorHabitScore(
                    bossBehavior,
                    response
                );

            if (score > bestScore)
            {
                bestScore = score;
                best = response;
            }
        }

        return best;
    }

    public float GetRetreatHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int retreatCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < spatialWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.Attack)
            {
                continue;
            }

            if (log.playerResponse ==
                PlayerResponseType.None)
            {
                continue;
            }

            checkedCount++;

            bool movedAway =
                log.responseDirection ==
                ResponseDirection.AwayFromBoss;

            bool successful =
                log.result ==
                CombatResultType.Avoided;

            if (movedAway &&
                successful)
            {
                retreatCount++;
            }
        }

        if (checkedCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)retreatCount /
            checkedCount;
    }

    public float GetFrontStepRetreatHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int retreatCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < spatialWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.FrontStep)
            {
                continue;
            }

            checkedCount++;

            if (log.responseDirection ==
                ResponseDirection.AwayFromBoss)
            {
                retreatCount++;
            }
        }

        if (checkedCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)retreatCount /
            checkedCount;
    }

    public float GetBackDodgeChaseHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int chaseCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < spatialWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.BackDodge)
            {
                continue;
            }

            checkedCount++;

            if (log.responseDirection ==
                ResponseDirection.TowardBoss)
            {
                chaseCount++;
            }
        }

        if (checkedCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)chaseCount /
            checkedCount;
    }

    public float GetBackDodgeChaseCommitHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedBackDodgeCount = 0;
        int chaseCount = 0;
        int commitCount = 0;

        // 최근 BackDodge 6회 안에서
        // Chase가 발생한 경우만 Commit의 분모로 사용한다.
        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedBackDodgeCount < spatialWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.BackDodge)
            {
                continue;
            }

            checkedBackDodgeCount++;

            if (log.responseDirection !=
                ResponseDirection.TowardBoss)
            {
                continue;
            }

            chaseCount++;

            if (log.playerAttackObserved ||
                IsAttackResponse(
                    log.playerResponse))
            {
                commitCount++;
            }
        }

        if (chaseCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)commitCount /
            chaseCount;
    }

    public float GetFrontStepAttackHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int attackCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < spatialWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.FrontStep)
            {
                continue;
            }

            checkedCount++;

            if (log.playerAttackObserved ||
                IsAttackResponse(
                    log.playerResponse))
            {
                attackCount++;
            }
        }

        if (checkedCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)attackCount /
            checkedCount;
    }

    public float GetFrontStepCounterHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int counterCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < spatialWindow;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.FrontStep)
            {
                continue;
            }

            checkedCount++;

            if (log.playerResponse ==
                PlayerResponseType.Counter)
            {
                counterCount++;
            }
        }

        if (checkedCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)counterCount /
            checkedCount;
    }

    public float GetPostPunishCommitHabitScore()
    {
        IReadOnlyList<PostPunishCommitLog> logs =
            CombatLogger.Instance?.PostPunishLogs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int commitCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < spatialWindow;
             i--)
        {
            checkedCount++;

            if (logs[i].committed)
            {
                commitCount++;
            }
        }

        if (checkedCount <
            minSpatialSamples)
        {
            return 0f;
        }

        return (float)commitCount /
            checkedCount;
    }

    public float GetHeavyTimingHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int successCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < 5;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                    BossBehaviorType.Attack ||
                log.bossAttack !=
                    BossAttackType.HeavySlash)
            {
                continue;
            }

            checkedCount++;

            if (log.playerResponse ==
                    PlayerResponseType.Counter &&
                log.result ==
                    CombatResultType.ParrySuccess)
            {
                successCount++;
            }
        }

        if (checkedCount < 3)
            return 0f;

        return (float)successCount /
            checkedCount;
    }

    public bool TryGetHeavyCounterDelay(
        out float medianDelay)
    {
        medianDelay = 0f;

        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return false;

        List<float> delays =
            new List<float>();

        int checkedCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < 5;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                    BossBehaviorType.Attack ||
                log.bossAttack !=
                    BossAttackType.HeavySlash)
            {
                continue;
            }

            checkedCount++;

            if (log.playerResponse !=
                    PlayerResponseType.Counter ||
                log.result !=
                    CombatResultType.ParrySuccess ||
                log.responseDelay < 0f)
            {
                continue;
            }

            delays.Add(
                log.responseDelay
            );
        }

        return TryGetMedian(
            delays,
            out medianDelay
        );
    }

    public float GetChargeTimingHabitScore()
    {
        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return 0f;

        int checkedCount = 0;
        int successCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < 5;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                    BossBehaviorType.Attack ||
                log.bossAttack !=
                    BossAttackType.ChargeSlash)
            {
                continue;
            }

            checkedCount++;

            if (log.result ==
                CombatResultType.ParrySuccess)
            {
                successCount++;
            }
        }

        if (checkedCount < 3)
            return 0f;

        return (float)successCount /
            checkedCount;
    }

    public bool TryGetChargeCounterDelayFromPass(
        out float medianDelay)
    {
        medianDelay = 0f;

        IReadOnlyList<AttackResponseLog> logs =
            CombatLogger.Instance?.Logs;

        if (logs == null || logs.Count == 0)
            return false;

        List<float> delays =
            new List<float>();

        int checkedCount = 0;

        for (int i = logs.Count - 1;
             i >= 0 &&
             checkedCount < 5;
             i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                    BossBehaviorType.Attack ||
                log.bossAttack !=
                    BossAttackType.ChargeSlash)
            {
                continue;
            }

            checkedCount++;

            if (log.result !=
                    CombatResultType.ParrySuccess ||
                !log.hasChargeCounterTiming)
            {
                continue;
            }

            delays.Add(
                log.chargeCounterDelayFromPass
            );
        }

        return TryGetMedian(
            delays,
            out medianDelay
        );
    }

    private bool TryGetMedian(
        List<float> values,
        out float median)
    {
        median = 0f;

        if (values == null ||
            values.Count < 2)
        {
            return false;
        }

        values.Sort();

        int middle =
            values.Count / 2;

        if (values.Count % 2 == 1)
        {
            median =
                values[middle];
        }
        else
        {
            median =
                (
                    values[middle - 1] +
                    values[middle]
                ) * 0.5f;
        }

        return true;
    }

    public void PrintSummary(
        BossAttackType bossAttack)
    {
        Debug.Log(
            $"===== Behavior : {bossAttack} ====="
        );

        foreach (PlayerResponseType response
                 in System.Enum.GetValues(
                     typeof(PlayerResponseType)))
        {
            if (response ==
                PlayerResponseType.None)
            {
                continue;
            }

            float habit =
                GetHabitScore(
                    bossAttack,
                    response
                );

            if (habit <= 0f)
                continue;

            Debug.Log(
                $"{response} | " +
                $"Usage={GetUsageRate(bossAttack, response):F2} | " +
                $"Success={GetSuccessRate(bossAttack, response):F2} | " +
                $"Recency={GetRecencyScore(bossAttack, response):F2} | " +
                $"Habit={habit:F2}"
            );
        }

        Debug.Log(
            $"Dominant → " +
            $"{GetDominantResponse(bossAttack)}"
        );
    }

    public void PrintBehaviorSummary(
        BossBehaviorType bossBehavior)
    {
        Debug.Log(
            $"===== Boss Behavior : {bossBehavior} ====="
        );

        foreach (PlayerResponseType response
                 in System.Enum.GetValues(
                     typeof(PlayerResponseType)))
        {
            if (response ==
                PlayerResponseType.None)
            {
                continue;
            }

            float habit =
                GetBehaviorHabitScore(
                    bossBehavior,
                    response
                );

            if (habit <= 0f)
                continue;

            Debug.Log(
                $"{response} | " +
                $"Usage={GetBehaviorUsageRate(bossBehavior, response):F2} | " +
                $"Success={GetBehaviorSuccessRate(bossBehavior, response):F2} | " +
                $"Recency={GetBehaviorRecencyScore(bossBehavior, response):F2} | " +
                $"Habit={habit:F2}"
            );
        }

        Debug.Log(
            $"Dominant → " +
            $"{GetDominantBehaviorResponse(bossBehavior)}"
        );

        if (bossBehavior ==
            BossBehaviorType.FrontStep)
        {
            Debug.Log(
                $"FrontStep Retreat Habit = " +
                $"{GetFrontStepRetreatHabitScore():F2}"
            );

            Debug.Log(
                $"FrontStep Attack Habit = " +
                $"{GetFrontStepAttackHabitScore():F2}"
            );

            Debug.Log(
                $"FrontStep Counter Habit = " +
                $"{GetFrontStepCounterHabitScore():F2}"
            );
        }

        if (bossBehavior ==
            BossBehaviorType.BackDodge)
        {
            Debug.Log(
                $"BackDodge Chase Habit = " +
                $"{GetBackDodgeChaseHabitScore():F2}"
            );

            Debug.Log(
                $"BackDodge Chase Commit Habit = " +
                $"{GetBackDodgeChaseCommitHabitScore():F2}"
            );
        }
    }

    private bool IsAttackResponse(
        PlayerResponseType response)
    {
        return
            response ==
                PlayerResponseType.NormalAttack ||
            response ==
                PlayerResponseType.ChargeAttack ||
            response ==
                PlayerResponseType.DashAttack;
    }

    private int GetAttackCount(
        IReadOnlyList<AttackResponseLog> logs,
        BossAttackType bossAttack)
    {
        int count = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossBehavior ==
                    BossBehaviorType.Attack &&
                log.bossAttack ==
                    bossAttack)
            {
                count++;
            }
        }

        return count;
    }

    private int GetResponseCount(
        IReadOnlyList<AttackResponseLog> logs,
        BossAttackType bossAttack,
        PlayerResponseType response)
    {
        int count = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossBehavior ==
                    BossBehaviorType.Attack &&
                log.bossAttack ==
                    bossAttack &&
                log.playerResponse ==
                    response)
            {
                count++;
            }
        }

        return count;
    }

    private int GetBehaviorCount(
        IReadOnlyList<AttackResponseLog> logs,
        BossBehaviorType bossBehavior)
    {
        int count = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossBehavior ==
                bossBehavior)
            {
                count++;
            }
        }

        return count;
    }

    private int GetBehaviorResponseCount(
        IReadOnlyList<AttackResponseLog> logs,
        BossBehaviorType bossBehavior,
        PlayerResponseType response)
    {
        int count = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossBehavior ==
                    bossBehavior &&
                log.playerResponse ==
                    response)
            {
                count++;
            }
        }

        return count;
    }

    private bool WasSuccessful(
        AttackResponseLog log)
    {
        if (log.playerResponse ==
            PlayerResponseType.Counter)
        {
            return log.result ==
                CombatResultType.ParrySuccess;
        }

        return log.result ==
            CombatResultType.Avoided;
    }
}
