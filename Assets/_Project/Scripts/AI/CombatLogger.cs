using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

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
    public float playerXAtStart;
    public float bossDirectionAtStart;
    public float chargePassEndTime = -1f;
    public float chargeCounterInputTime = -1f;

    public float chargeCounterDelayFromPass;
    public bool hasChargeCounterTiming;

    // 첫 Response와 별개로, 보스 행동 중 플레이어가 공격 입력을
    // 한 번이라도 했는지 추적한다.
    public bool playerAttackObserved;

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
        playerAttackObserved = false;
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
        playerAttackObserved = false;
    }
}

[System.Serializable]
public class PostPunishCommitLog
{
    public int sequenceId;
    public BossAttackType sourceAttack;
    public bool committed;
    public float attackEndTime;
    public float commitDelay;

    public PostPunishCommitLog(
        int sequenceId,
        BossAttackType sourceAttack,
        bool committed,
        float attackEndTime,
        float commitDelay)
    {
        this.sequenceId = sequenceId;
        this.sourceAttack = sourceAttack;
        this.committed = committed;
        this.attackEndTime = attackEndTime;
        this.commitDelay = commitDelay;
    }
}

[System.Serializable]
public class PlayerActionEventLog
{
    public float time;
    public string action;
    public float distanceToBoss;
    public int relatedSequenceId;

    public PlayerActionEventLog(
        float time,
        string action,
        float distanceToBoss,
        int relatedSequenceId)
    {
        this.time = time;
        this.action = action;
        this.distanceToBoss = distanceToBoss;
        this.relatedSequenceId = relatedSequenceId;
    }
}

[System.Serializable]
public class AdaptiveEventLog
{
    public float time;

    public string eventType;
    public string context;
    public string habit;

    public float score;

    public string response;

    public AdaptiveEventLog(
        float time,
        string eventType,
        string context,
        string habit,
        float score,
        string response)
    {
        this.time = time;
        this.eventType = eventType;
        this.context = context;
        this.habit = habit;
        this.score = score;
        this.response = response;
    }
}
[System.Serializable]
public class BattleSessionLog
{
    public string sessionId;
    public string savedAt;

    public float battleDuration;
    public string battleResult;

    public List<AttackResponseLog> combatLogs;
    public List<PostPunishCommitLog> postPunishLogs;

    public List<PlayerActionEventLog> playerActionEvents;
    public List<AdaptiveEventLog> adaptiveEvents;
}

public class CombatLogger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform boss;

    [SerializeField]
    private PlayerBehaviorModel behaviorModel;

    [Header("Post-Punish Commit")]
    [SerializeField]
    private float postPunishGraceTime = 0.2f;

    [SerializeField]
    private float postPunishCommitWindow = 0.6f;

    public static CombatLogger Instance { get; private set; }

    private readonly List<AttackResponseLog> logs = new();
    private readonly List<PostPunishCommitLog> postPunishLogs = new();

    private float battleStartTime;
    private string sessionId;

    private AttackResponseLog currentLog;
    private bool responseRecorded;
    private int nextSequenceId = 0;
    private int nextPostPunishSequenceId = 0;

    private bool postPunishObservationActive;
    private float postPunishObservationStartTime;
    private BossAttackType postPunishSourceAttack;

    public IReadOnlyList<AttackResponseLog> Logs => logs;
    public IReadOnlyList<PostPunishCommitLog> PostPunishLogs =>
        postPunishLogs;
    private readonly List<PlayerActionEventLog>
        playerActionEvents = new();

    private readonly List<AdaptiveEventLog>
        adaptiveEvents = new();
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        battleStartTime = Time.time;

        sessionId =
            DateTime.Now.ToString(
                "yyyyMMdd_HHmmss"
            );
    }

    private void Update()
    {
        if (!postPunishObservationActive)
            return;

        float elapsed =
            Time.time - postPunishObservationStartTime;

        if (elapsed > postPunishCommitWindow)
        {
            CompletePostPunishObservation(
                committed: false,
                commitDelay: -1f
            );
        }
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

        if (player != null && boss != null)
        {
            currentLog.playerXAtStart =
                player.position.x;

            currentLog.bossDirectionAtStart =
                Mathf.Sign(
                    boss.position.x -
                    player.position.x
                );
        }

        responseRecorded = false;

        Debug.Log(
            $"[CombatLog] START | " +
            $"{behaviorType} | Distance={distance:F2}"
        );
    }

    public void RecordPlayerResponse(
        PlayerResponseType response)
    {
        RecordPlayerAction(
            response.ToString()
        );
        // Post-Punish 관찰은 현재 Boss 로그가 없어도
        // 플레이어 공격 입력을 받아야 한다.
        TryRecordPostPunishCommit(response);

        if (currentLog == null)
            return;

        // 첫 Response가 이미 기록되어 있어도
        // 공격 입력 자체는 별도로 남긴다.
        if (IsAttackResponse(response))
        {
            currentLog.playerAttackObserved = true;
        }

        RecordChargeCounterTiming(response);

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

    private void UpdateSpatialResponseFromMovement()
    {
        if (currentLog == null)
            return;

        if (player == null || boss == null)
            return;

        float playerDeltaX =
            player.position.x -
            currentLog.playerXAtStart;

        if (Mathf.Abs(playerDeltaX) < 0.1f)
        {
            currentLog.responseDirection =
                ResponseDirection.Neutral;

            currentLog.distanceAtResponse =
                GetCurrentDistance();

            return;
        }

        float moveDirection =
            Mathf.Sign(playerDeltaX);

        if (moveDirection ==
            currentLog.bossDirectionAtStart)
        {
            currentLog.responseDirection =
                ResponseDirection.TowardBoss;
        }
        else
        {
            currentLog.responseDirection =
                ResponseDirection.AwayFromBoss;
        }

        // Spatial 행동은 순간 속도보다 행동 전체의 순이동을
        // 최종 방향 데이터로 사용한다.
        currentLog.distanceAtResponse =
            GetCurrentDistance();
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

    public void EndBossAttack(
        float distanceAfter,
        bool allowPostPunishObservation = true)
    {
        if (currentLog == null)
            return;

        if (currentLog.bossBehavior !=
            BossBehaviorType.Attack)
        {
            return;
        }

        bool shouldObservePostPunish =
            allowPostPunishObservation &&
            currentLog.playerAttackObserved;

        BossAttackType sourceAttack =
            currentLog.bossAttack;

        EndCurrentBehavior(distanceAfter);

        if (shouldObservePostPunish)
        {
            BeginPostPunishObservation(
                sourceAttack
            );
        }
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

        // FrontStep / BackDodge는 액션 입력 유무와 상관없이
        // 행동 전체의 실제 위치 변화로 방향을 확정한다.
        if (currentLog.bossBehavior ==
                BossBehaviorType.FrontStep ||
            currentLog.bossBehavior ==
                BossBehaviorType.BackDodge)
        {
            UpdateSpatialResponseFromMovement();
        }

        if (currentLog.result == CombatResultType.Pending)
        {
            if (currentLog.bossBehavior ==
                BossBehaviorType.Attack)
            {
                currentLog.result =
                    CombatResultType.Avoided;
            }
            else
            {
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

    private void BeginPostPunishObservation(
        BossAttackType sourceAttack)
    {
        if (postPunishObservationActive)
        {
            CompletePostPunishObservation(
                committed: false,
                commitDelay: -1f
            );
        }

        postPunishObservationActive = true;
        postPunishObservationStartTime =
            Time.time;
        postPunishSourceAttack = sourceAttack;

        Debug.Log(
            $"[Post-Punish Observe] START | " +
            $"Source={sourceAttack}"
        );
    }

    private void TryRecordPostPunishCommit(
        PlayerResponseType response)
    {
        if (!postPunishObservationActive)
            return;

        if (!IsAttackResponse(response))
            return;

        float elapsed =
            Time.time -
            postPunishObservationStartTime;

        // 기존 공격의 입력 버퍼나 자연스러운 연속 입력은
        // Commit으로 보지 않는다.
        if (elapsed < postPunishGraceTime)
            return;

        if (elapsed > postPunishCommitWindow)
        {
            CompletePostPunishObservation(
                committed: false,
                commitDelay: -1f
            );
            return;
        }

        CompletePostPunishObservation(
            committed: true,
            commitDelay: elapsed
        );
    }

    private void CompletePostPunishObservation(
        bool committed,
        float commitDelay)
    {
        if (!postPunishObservationActive)
            return;

        PostPunishCommitLog log =
            new PostPunishCommitLog(
                nextPostPunishSequenceId++,
                postPunishSourceAttack,
                committed,
                postPunishObservationStartTime,
                commitDelay
            );

        postPunishLogs.Add(log);

        Debug.Log(
            $"[Post-Punish #{log.sequenceId}] " +
            $"Source={log.sourceAttack} | " +
            $"Commit={log.committed} | " +
            $"Delay={log.commitDelay:F2}"
        );

        postPunishObservationActive = false;
        postPunishObservationStartTime = 0f;
    }

    private void RecordChargeCounterTiming(
        PlayerResponseType response)
    {
        if (currentLog == null)
            return;

        if (currentLog.bossBehavior !=
                BossBehaviorType.Attack ||
            currentLog.bossAttack !=
                BossAttackType.ChargeSlash)
        {
            return;
        }

        if (response !=
            PlayerResponseType.Counter)
        {
            return;
        }

        currentLog.chargeCounterInputTime =
            Time.time;

        if (currentLog.chargePassEndTime >= 0f)
        {
            currentLog.chargeCounterDelayFromPass =
                currentLog.chargeCounterInputTime -
                currentLog.chargePassEndTime;

            currentLog.hasChargeCounterTiming = true;

            Debug.Log(
                $"[Charge Timing Sample] " +
                $"CounterFromPass=" +
                $"{currentLog.chargeCounterDelayFromPass:F2}"
            );
        }
    }

    public void MarkChargePassEnd()
    {
        if (currentLog == null)
            return;

        if (currentLog.bossBehavior !=
                BossBehaviorType.Attack ||
            currentLog.bossAttack !=
                BossAttackType.ChargeSlash)
        {
            return;
        }

        currentLog.chargePassEndTime =
            Time.time;

        if (currentLog.chargeCounterInputTime >= 0f)
        {
            currentLog.chargeCounterDelayFromPass =
                currentLog.chargeCounterInputTime -
                currentLog.chargePassEndTime;

            currentLog.hasChargeCounterTiming = true;

            Debug.Log(
                $"[Charge Timing Sample] " +
                $"CounterFromPass=" +
                $"{currentLog.chargeCounterDelayFromPass:F2}"
            );
        }

        Debug.Log(
            $"[Charge Timing Anchor] " +
            $"Pass End"
        );
    }
    public void SaveBattleLog(
        string battleResult)
    {
        BattleSessionLog session =
            new BattleSessionLog
            {
                sessionId = sessionId,

                savedAt =
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss"
                    ),

                battleDuration =
                    Time.time -
                    battleStartTime,

                battleResult =
                    battleResult,

                combatLogs =
                    new List<AttackResponseLog>(
                        logs
                    ),
                postPunishLogs =
                    new List<PostPunishCommitLog>(
                        postPunishLogs
                    ),

                playerActionEvents =
                    new List<PlayerActionEventLog>(
                        playerActionEvents
                    ),

                adaptiveEvents =
                    new List<AdaptiveEventLog>(
                        adaptiveEvents
                    )    
            };


        string json =
            JsonUtility.ToJson(
                session,
                true
            );


        string folder =
            Path.Combine(
                Application.persistentDataPath,
                "CombatLogs"
            );

        Directory.CreateDirectory(
            folder
        );


        string path =
            Path.Combine(
                folder,
                $"Battle_{sessionId}.json"
            );


        File.WriteAllText(
            path,
            json
        );


        Debug.Log(
            $"[Combat Log Saved] {path}"
        );
    }
    public void RecordPlayerAction(
        string action)
    {
        int relatedSequenceId =
            currentLog != null
                ? currentLog.sequenceId
                : -1;

        PlayerActionEventLog log =
            new PlayerActionEventLog(
                Time.time,
                action,
                GetCurrentDistance(),
                relatedSequenceId
            );

        playerActionEvents.Add(log);
    }
    public void RecordAdaptiveEvent(
        string eventType,
        string context,
        string habit,
        float score,
        string response = "")
    {
        AdaptiveEventLog log =
            new AdaptiveEventLog(
                Time.time,
                eventType,
                context,
                habit,
                score,
                response
            );

        adaptiveEvents.Add(log);

        Debug.Log(
            $"[Adaptive Log] " +
            $"{eventType} | " +
            $"{context} | " +
            $"{habit} | " +
            $"Score={score:F2} | " +
            $"Response={response}"
        );
    }
    [ContextMenu("Save Battle Log")]
    private void SaveTestBattleLog()
    {
        SaveBattleLog("Test");
    }
}
