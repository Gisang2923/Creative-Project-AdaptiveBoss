using System.Text;
using TMPro;
using UnityEngine;

public class AdaptiveDebugUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BossAdaptiveDecisionSource adaptiveSource;

    [SerializeField]
    private TMP_Text debugText;

    [Header("Settings")]
    [SerializeField]
    private float refreshInterval = 0.1f;

    [SerializeField]
    private float currentResponseDisplayDuration = 2f;

    private float refreshTimer;

    private int lastObservedResponseSerial = -1;
    private float currentResponseTimer;

    private AdaptiveHabitType currentResponseHabit =
        AdaptiveHabitType.None;

    private AdaptiveResponseAction currentResponseAction =
        AdaptiveResponseAction.None;

    private void Update()
    {
        if (adaptiveSource == null ||
            debugText == null)
        {
            return;
        }

        // =====================================
        // CURRENT RESPONSE EVENT 감지
        // =====================================

        int responseSerial =
            adaptiveSource.DebugAdaptiveResponseSerial;

        if (responseSerial !=
            lastObservedResponseSerial)
        {
            lastObservedResponseSerial =
                responseSerial;

            // 0은 아직 실제 Adaptive Response가
            // 한 번도 발생하지 않은 초기 상태.
            if (responseSerial > 0)
            {
                currentResponseHabit =
                    adaptiveSource
                        .DebugLastAdaptiveResponseHabit;

                currentResponseAction =
                    adaptiveSource
                        .DebugLastAdaptiveResponse;

                currentResponseTimer =
                    currentResponseDisplayDuration;
            }
        }

        if (currentResponseTimer > 0f)
        {
            currentResponseTimer -=
                Time.unscaledDeltaTime;
        }

        // =====================================
        // UI Refresh
        // =====================================

        refreshTimer -=
            Time.unscaledDeltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer =
            refreshInterval;

        Refresh();
    }

    private void Refresh()
    {
        StringBuilder sb =
            new StringBuilder();

        sb.AppendLine(
            "<b>ADAPTIVE AI</b>"
        );

        sb.AppendLine();

        // =====================================
        // BACK DODGE
        // =====================================

        float chase =
            adaptiveSource
                .DebugBackDodgeChaseScore;

        float chaseCommit =
            adaptiveSource
                .DebugBackDodgeChaseCommitScore;

        sb.AppendLine(
            "<b>BACK DODGE</b>"
        );

        sb.AppendLine(
            $"Chase        {MakeBar(chase)}  {chase:F2}"
        );

        sb.AppendLine(
            $"Chase Commit {MakeBar(chaseCommit)}  {chaseCommit:F2}"
        );

        sb.AppendLine(
            $"Policy       " +
            $"{FormatPolicy(adaptiveSource.DebugBackDodgePolicy)}"
        );

        sb.AppendLine();

        // =====================================
        // FRONT STEP
        // =====================================

        float retreat =
            adaptiveSource
                .DebugFrontStepRetreatScore;

        float attack =
            adaptiveSource
                .DebugFrontStepAttackScore;

        float counter =
            adaptiveSource
                .DebugFrontStepCounterScore;

        sb.AppendLine(
            "<b>FRONT STEP</b>"
        );

        sb.AppendLine(
            $"Retreat {MakeBar(retreat)}  {retreat:F2}"
        );

        sb.AppendLine(
            $"Attack  {MakeBar(attack)}  {attack:F2}"
        );

        sb.AppendLine(
            $"Counter {MakeBar(counter)}  {counter:F2}"
        );

        sb.AppendLine(
            $"Policy  " +
            $"{FormatPolicy(adaptiveSource.DebugFrontStepPolicy)}"
        );

        sb.AppendLine();

        // =====================================
        // POST-PUNISH
        // =====================================

        float postPunish =
            adaptiveSource
                .DebugPostPunishCommitScore;

        bool postPunishActive =
            adaptiveSource.DebugPostPunishPolicy !=
            AdaptiveHabitType.None;

        sb.AppendLine(
            "<b>POST-PUNISH</b>"
        );

        sb.AppendLine(
            $"Commit {MakeBar(postPunish)}  {postPunish:F2}"
        );

        sb.AppendLine(
            $"Policy {FormatActive(postPunishActive)}"
        );

        sb.AppendLine();

        // =====================================
        // HEAVY / CHARGE TIMING
        // Habit Score나 Threshold는 제거.
        // 실제 Timing Adapt 상태만 표시한다.
        // =====================================

        sb.AppendLine(
            "<b>HEAVY TIMING</b>"
        );

        sb.AppendLine(
            "Boss Timing  " +
            FormatTiming(
                adaptiveSource.DebugHeavyTimingActive,
                adaptiveSource.DebugHeavyTimingMode
            )
        );

        sb.AppendLine();

        sb.AppendLine(
            "<b>CHARGE TIMING</b>"
        );

        sb.AppendLine(
            "Boss Timing  " +
            FormatTiming(
                adaptiveSource.DebugChargeTimingActive,
                adaptiveSource.DebugChargeTimingMode
            )
        );

        // =====================================
        // CURRENT RESPONSE
        // 실제 Adaptive Response가 선택된 직후에만
        // 잠깐 표시한다.
        // =====================================

        if (currentResponseTimer > 0f &&
            currentResponseHabit !=
                AdaptiveHabitType.None &&
            currentResponseAction !=
                AdaptiveResponseAction.None)
        {
            sb.AppendLine();

            sb.AppendLine(
                "<b>CURRENT RESPONSE</b>"
            );

            sb.AppendLine(
                $"{FormatResponseSource(currentResponseHabit)}" +
                "  →  " +
                $"{FormatResponse(currentResponseAction)}"
            );
        }

        debugText.text =
            sb.ToString();
    }

    private string MakeBar(
        float value)
    {
        value =
            Mathf.Clamp01(value);

        const int length = 10;

        int filled =
            Mathf.RoundToInt(
                value * length
            );

        StringBuilder bar =
            new StringBuilder();

        bar.Append("[");

        for (int i = 0;
             i < length;
             i++)
        {
            bar.Append(
                i < filled
                    ? "■"
                    : "□"
            );
        }

        bar.Append("]");

        return bar.ToString();
    }

    private string FormatPolicy(
        AdaptiveHabitType policy)
    {
        if (policy ==
            AdaptiveHabitType.None)
        {
            return
                "<color=#909090>NONE</color>";
        }

        string label =
            GetHabitLabel(policy);

        return
            $"<color=#63FF91><b>" +
            $"{label}" +
            $"</b></color>";
    }

    private string FormatActive(
        bool active)
    {
        if (!active)
        {
            return
                "<color=#909090>NONE</color>";
        }

        return
            "<color=#63FF91><b>ACTIVE</b></color>";
    }

    private string FormatTiming(
        bool active,
        TimingAdaptMode mode)
    {
        if (!active ||
            mode ==
                TimingAdaptMode.None)
        {
            return
                "<color=#909090>NONE</color>";
        }

        string label =
            mode ==
                TimingAdaptMode.Early
                ? "EARLIER"
                : "DELAYED";

        return
            $"<color=#FFD166><b>" +
            $"{label}" +
            $"</b></color>";
    }

    private string FormatResponseSource(
        AdaptiveHabitType habit)
    {
        string label;

        switch (habit)
        {
            case AdaptiveHabitType.Attack:
                label = "AGGRESSION";
                break;

            case AdaptiveHabitType.Counter:
                label = "COUNTER BAIT";
                break;

            case AdaptiveHabitType.PostPunishCommit:
                label = "POST-PUNISH";
                break;

            default:
                label =
                    GetHabitLabel(habit);
                break;
        }

        return
            $"<color=#FFD166><b>" +
            $"{label}" +
            $"</b></color>";
    }

    private string FormatResponse(
        AdaptiveResponseAction response)
    {
        string label =
            response.ToString()
                .Replace(
                    "ChargeSlash",
                    "CHARGE"
                )
                .Replace(
                    "HeavySlash",
                    "HEAVY"
                )
                .Replace(
                    "BasicSlash",
                    "BASIC"
                )
                .Replace(
                    "BackDodge",
                    "BACK DODGE"
                )
                .Replace(
                    "FrontStep",
                    "FRONT STEP"
                )
                .Replace(
                    "NormalDecision",
                    "NORMAL"
                )
                .ToUpper();

        return
            $"<color=#63D7FF><b>" +
            $"{label}" +
            $"</b></color>";
    }

    private string GetHabitLabel(
        AdaptiveHabitType habit)
    {
        switch (habit)
        {
            case AdaptiveHabitType.ChaseCommit:
                return "CHASE COMMIT";

            case AdaptiveHabitType.PostPunishCommit:
                return "POST-PUNISH COMMIT";

            default:
                return
                    habit.ToString()
                        .ToUpper();
        }
    }
}
