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
            if (logs[i].bossAttack == bossAttack)
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

            // 아무 대응도 없었던 경우는 공간 습관 분석에서 제외
            if (log.playerResponse == PlayerResponseType.None)
                continue;

            checkedCount++;

            bool movedAway =
                log.responseDirection ==
                ResponseDirection.AwayFromBoss;

            bool increasedDistance =
                log.distanceAfter >
                log.distanceBefore;

            bool successful =
                log.result ==
                CombatResultType.Avoided;

            if (movedAway &&
                increasedDistance &&
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

    private int GetAttackCount(
        IReadOnlyList<AttackResponseLog> logs,
        BossAttackType bossAttack)
    {
        int count = 0;

        foreach (AttackResponseLog log in logs)
        {
            if (log.bossAttack ==
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
            if (log.bossAttack ==
                    bossAttack &&
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