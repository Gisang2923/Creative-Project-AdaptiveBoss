using System.Collections.Generic;
using UnityEngine;

public enum PlayerResponseType
{
    None,
    Counter,
    Dash,
    Jump,
    NormalAttack,
    ChargeAttack,
    Heal
}

public enum CombatResultType
{
    Pending,
    Avoided,
    PlayerHit,
    ParrySuccess
}

[System.Serializable]
public class AttackResponseLog
{
    public BossAttackType bossAttack;
    public PlayerResponseType playerResponse;
    public CombatResultType result;

    public float attackStartTime;
    public float responseDelay;
    public float distance;

    public AttackResponseLog(
        BossAttackType bossAttack,
        float attackStartTime,
        float distance)
    {
        this.bossAttack = bossAttack;
        this.attackStartTime = attackStartTime;
        this.distance = distance;

        playerResponse = PlayerResponseType.None;
        result = CombatResultType.Pending;
        responseDelay = -1f;
    }
}

public class CombatLogger : MonoBehaviour
{
    public static CombatLogger Instance { get; private set; }

    private readonly List<AttackResponseLog> logs = new();

    private AttackResponseLog currentLog;
    private bool responseRecorded;

    public IReadOnlyList<AttackResponseLog> Logs => logs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BeginBossAttack(
        BossAttackType attackType,
        float distance)
    {
        // 혹시 이전 공격 로그가 정상 종료되지 않았으면 먼저 마감
        if (currentLog != null)
        {
            EndBossAttack();
        }

        currentLog = new AttackResponseLog(
            attackType,
            Time.time,
            distance
        );

        responseRecorded = false;

        Debug.Log(
            $"[CombatLog] START | " +
            $"{attackType} | Distance={distance:F2}"
        );
    }

    public void RecordPlayerResponse(
        PlayerResponseType response)
    {
        // 현재 보스 공격이 없으면 기록하지 않음
        if (currentLog == null)
            return;

        // 공격 하나당 최초의 의미 있는 대응 하나만 기록
        if (responseRecorded)
            return;

        responseRecorded = true;

        currentLog.playerResponse = response;
        currentLog.responseDelay =
            Time.time - currentLog.attackStartTime;

        Debug.Log(
            $"[CombatLog] RESPONSE | " +
            $"{currentLog.bossAttack} → {response} | " +
            $"Delay={currentLog.responseDelay:F2}"
        );
    }

    public void RecordPlayerHit()
    {
        if (currentLog == null)
            return;

        currentLog.result = CombatResultType.PlayerHit;
    }

    public void RecordParrySuccess()
    {
        if (currentLog == null)
            return;

        currentLog.result = CombatResultType.ParrySuccess;
    }

    public void EndBossAttack()
    {
        if (currentLog == null)
            return;

        // 맞지도 않았고 패링도 안 당했다면 회피 성공으로 처리
        if (currentLog.result == CombatResultType.Pending)
        {
            currentLog.result = CombatResultType.Avoided;
        }

        logs.Add(currentLog);

        Debug.Log(
            $"[CombatLog] END | " +
            $"{currentLog.bossAttack} → " +
            $"{currentLog.playerResponse} → " +
            $"{currentLog.result}"
        );

        currentLog = null;
        responseRecorded = false;
    }
}