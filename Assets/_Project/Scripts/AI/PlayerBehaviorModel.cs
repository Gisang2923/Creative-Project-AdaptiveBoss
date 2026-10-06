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

        if (attackCount < minAttackSamples)
            return 0f;

        if (responseCount < minResponseSamples)
            return 0f;

        float usage =
            GetUsageRate(
                bossAttack,
                response
            );

        float success =
            GetSuccessRate(
                bossAttack,
                response
            );

        float recency =
            GetRecencyScore(
                bossAttack,
                response
            );

        float totalWeight =
            usageWeight +
            successWeight +
            recencyWeight;

        if (totalWeight <= 0f)
            return 0f;

        float score =
            (
                usage * usageWeight +
                success * successWeight +
                recency * recencyWeight
            )
            / totalWeight;

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

        if (behaviorCount < minAttackSamples)
            return 0f;

        if (responseCount < minResponseSamples)
            return 0f;

        float usage =
            GetBehaviorUsageRate(
                bossBehavior,
                response
            );

        float success =
            GetBehaviorSuccessRate(
                bossBehavior,
                response
            );

        float recency =
            GetBehaviorRecencyScore(
                bossBehavior,
                response
            );

        float totalWeight =
            usageWeight +
            successWeight +
            recencyWeight;

        if (totalWeight <= 0f)
            return 0f;

        float score =
            (
                usage * usageWeight +
                success * successWeight +
                recency * recencyWeight
            )
            / totalWeight;

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
            if (log.bossBehavior != bossBehavior)
                continue;

            if (log.playerResponse != response)
                continue;

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
            GetAttackCount(logs, bossAttack);

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
                BossBehaviorType.Attack)
            {
                continue;
            }
            if (log.bossAttack != bossAttack)
                continue;

            if (log.playerResponse != response)
                continue;

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
            if (logs[i].bossBehavior ==
                    BossBehaviorType.Attack &&
                logs[i].bossAttack ==
                    bossAttack)
            {
                recent.Add(logs[i]);
            }
        }

        if (recent.Count == 0)
            return 0f;

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

        if (recent.Count == 0)
            return 0f;

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
                continue;

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

        // 최근 로그부터 확인
        for (int i = logs.Count - 1;
            i >= 0 && checkedCount < spatialWindow;
            i--)
        {
            AttackResponseLog log = logs[i];
            if (log.bossBehavior !=
                    BossBehaviorType.Attack)
                {
                    continue;
                }
            // 아무 대응도 없었던 경우는 공간 습관 분석에서 제외
            if (log.playerResponse == PlayerResponseType.None)
                continue;

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

        if (checkedCount < minSpatialSamples)
            return 0f;

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

        // 최근 FrontStep 로그부터 확인
        for (int i = logs.Count - 1;
            i >= 0 && checkedCount < spatialWindow;
            i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.FrontStep)
            {
                continue;
            }

            // FrontStep이 발생했다면 하나의 관찰 샘플로 계산
            checkedCount++;

            bool movedAway =
                log.responseDirection ==
                ResponseDirection.AwayFromBoss;

            if (movedAway)
            {
                retreatCount++;
            }
           
        }

        if (checkedCount < minSpatialSamples)
            return 0f;

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
            i >= 0 && checkedCount < spatialWindow;
            i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.BackDodge)
            {
                continue;
            }

            checkedCount++;

            bool movedToward =
                log.responseDirection ==
                ResponseDirection.TowardBoss;

            if (movedToward)
            {
                chaseCount++;
            }
        }

        if (checkedCount < minSpatialSamples)
            return 0f;

        return (float)chaseCount /
            checkedCount;
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
            i >= 0 && checkedCount < spatialWindow;
            i--)
        {
            AttackResponseLog log = logs[i];

            if (log.bossBehavior !=
                BossBehaviorType.FrontStep)
            {
                continue;
            }

            checkedCount++;

            bool attacked =
                log.playerResponse ==
                    PlayerResponseType.NormalAttack ||
                log.playerResponse ==
                    PlayerResponseType.ChargeAttack ||
                log.playerResponse ==
                    PlayerResponseType.DashAttack;

            if (attacked)
                attackCount++;
        }

        if (checkedCount < minSpatialSamples)
            return 0f;

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
            i >= 0 && checkedCount < spatialWindow;
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

        if (checkedCount < minSpatialSamples)
            return 0f;

        return (float)counterCount /
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
            i >= 0 && checkedCount < 5;
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
            i >= 0 && checkedCount < 5;
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

        if (delays.Count < 2)
            return false;

        delays.Sort();

        int middle =
            delays.Count / 2;

        if (delays.Count % 2 == 1)
        {
            medianDelay =
                delays[middle];
        }
        else
        {
            medianDelay =
                (
                    delays[middle - 1] +
                    delays[middle]
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
                continue;

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
        if (bossBehavior == BossBehaviorType.FrontStep)
        {
            Debug.Log(
                $"FrontStep Retreat Habit = " +
                $"{GetFrontStepRetreatHabitScore():F2}"
            );
        }
        if (bossBehavior == BossBehaviorType.BackDodge)
        {
            Debug.Log(
                $"BackDodge Chase Habit = " +
                $"{GetBackDodgeChaseHabitScore():F2}"
            );
        }
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