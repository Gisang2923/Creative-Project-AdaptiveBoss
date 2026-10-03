using System.Collections.Generic;
using UnityEngine;

public enum PlayerResponseType
{
    None,
    Counter,
    Dash,
    DashAttack,
    Jump,
    NormalAttack,
    ChargeAttack,
    Heal
}
public enum ResponseDirection
{
    None,
    TowardBoss,
    AwayFromBoss,
    Neutral
}
public enum CombatResultType
{
    Pending,
    Avoided,
    PlayerHit,
    ParrySuccess,
    Completed
}
public enum BossBehaviorType
{
    Attack,
    BackDodge,
    FrontStep,
    Parry
}
[System.Serializable]
public class AttackResponseLog
{
    public int sequenceId;

    public BossBehaviorType bossBehavior;
    public BossAttackType bossAttack;
    public PlayerResponseType playerResponse;
    public ResponseDirection responseDirection;
    public CombatResultType result;

    public float attackStartTime;
    public float responseDelay;

    public float distanceBefore;
    public float distanceAtResponse;
    public float distanceAfter;

    public AttackResponseLog(
        int sequenceId,
        BossAttackType bossAttack,
        float attackStartTime,
        float distanceBefore)
    {
        this.sequenceId = sequenceId;
        this.bossBehavior = BossBehaviorType.Attack;
        this.bossAttack = bossAttack;
        this.attackStartTime = attackStartTime;
        this.distanceBefore = distanceBefore;

        playerResponse = PlayerResponseType.None;
        responseDirection = ResponseDirection.None;
        result = CombatResultType.Pending;

        responseDelay = -1f;
        distanceAtResponse = -1f;
        distanceAfter = distanceBefore;
    }
    public AttackResponseLog(
        int sequenceId,
        BossBehaviorType bossBehavior,
        float behaviorStartTime,
        float distanceBefore)
    {
        this.sequenceId = sequenceId;
        this.bossBehavior = bossBehavior;

        this.attackStartTime = behaviorStartTime;
        this.distanceBefore = distanceBefore;

        playerResponse = PlayerResponseType.None;
        responseDirection = ResponseDirection.None;
        result = CombatResultType.Pending;

        responseDelay = -1f;
        distanceAtResponse = -1f;
        distanceAfter = distanceBefore;
    }
}

public class CombatLogger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform boss;
    [SerializeField]
    private PlayerBehaviorModel behaviorModel;
    public static CombatLogger Instance { get; private set; }

    private readonly List<AttackResponseLog> logs = new();

    private AttackResponseLog currentLog;
    private bool responseRecorded;
    private int nextSequenceId = 0;

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
        if (currentLog != null)
        {
            EndCurrentBehavior(
                GetCurrentDistance()
            );
        }

        currentLog = new AttackResponseLog(
            nextSequenceId++,
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
    public void BeginBossBehavior(
        BossBehaviorType behaviorType,
        float distance)
    {
        if (behaviorType == BossBehaviorType.Attack)
            return;

        // 이전 행동 로그가 남아 있으면 먼저 종료
        if (currentLog != null)
        {
            EndCurrentBehavior(
                GetCurrentDistance()
            );
        }

        currentLog = new AttackResponseLog(
            nextSequenceId++,
            behaviorType,
            Time.time,
            distance
        );

        responseRecorded = false;

        Debug.Log(
            $"[CombatLog] START | " +
            $"{behaviorType} | Distance={distance:F2}"
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

        currentLog.distanceAtResponse =
            GetCurrentDistance();

        currentLog.responseDirection =
            GetResponseDirection();

        string behaviorName =
            currentLog.bossBehavior ==
            BossBehaviorType.Attack
                ? currentLog.bossAttack.ToString()
                : currentLog.bossBehavior.ToString();

        Debug.Log(
            $"[CombatLog] RESPONSE | " +
            $"{behaviorName} → {response} | " +
            $"Delay={currentLog.responseDelay:F2}"
        );
    }
    private float GetCurrentDistance()
    {
        if (player == null || boss == null)
            return -1f;

        return Mathf.Abs(
            player.position.x - boss.position.x
        );
    }
    private ResponseDirection GetResponseDirection()
    {
        if (player == null || boss == null)
            return ResponseDirection.None;

        Rigidbody2D playerRb =
            player.GetComponent<Rigidbody2D>();

        if (playerRb == null)
            return ResponseDirection.None;

        float velocityX =
            playerRb.linearVelocity.x;

        if (Mathf.Abs(velocityX) < 0.1f)
            return ResponseDirection.Neutral;

        float bossDirection =
            Mathf.Sign(
                boss.position.x -
                player.position.x
            );

        float moveDirection =
            Mathf.Sign(velocityX);

        if (moveDirection == bossDirection)
            return ResponseDirection.TowardBoss;

        return ResponseDirection.AwayFromBoss;
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
    public void EndBossAttack(float distanceAfter)
    {
        if (currentLog == null)
            return;

        if (currentLog.bossBehavior !=
            BossBehaviorType.Attack)
        {
            return;
        }

        EndCurrentBehavior(distanceAfter);
    }
    public void EndBossBehavior(float distanceAfter)
    {
        if (currentLog == null)
            return;

        if (currentLog.bossBehavior ==
            BossBehaviorType.Attack)
        {
            return;
        }

        EndCurrentBehavior(distanceAfter);
    }
    private void EndCurrentBehavior(float distanceAfter)
    {
        if (currentLog == null)
            return;

        currentLog.distanceAfter = distanceAfter;

        if (currentLog.result == CombatResultType.Pending)
        {
            if (currentLog.bossBehavior ==
                BossBehaviorType.Attack)
            {
                // 공격인데 맞지도, 패링되지도 않았다면 회피
                currentLog.result =
                    CombatResultType.Avoided;
            }
            else
            {
                // 이동/방어 행동은 단순 정상 종료
                currentLog.result =
                    CombatResultType.Completed;
            }
        }

        logs.Add(currentLog);

        if (currentLog.bossBehavior ==
            BossBehaviorType.Attack)
        {
            behaviorModel?.PrintSummary(
                currentLog.bossAttack
            );
        }
        else
        {
            behaviorModel?.PrintBehaviorSummary(
                currentLog.bossBehavior
            );
        }

        string behaviorName =
            currentLog.bossBehavior ==
            BossBehaviorType.Attack
                ? currentLog.bossAttack.ToString()
                : currentLog.bossBehavior.ToString();

        Debug.Log(
            $"[CombatLog #{currentLog.sequenceId}] " +
            $"{behaviorName} → " +
            $"{currentLog.playerResponse} " +
            $"({currentLog.responseDirection}) | " +
            $"{currentLog.result} | " +
            $"Distance " +
            $"{currentLog.distanceBefore:F2} → " +
            $"{currentLog.distanceAtResponse:F2} → " +
            $"{currentLog.distanceAfter:F2} | " +
            $"Delay={currentLog.responseDelay:F2}"
        );

        currentLog = null;
        responseRecorded = false;
    }
}