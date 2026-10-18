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

    private float refreshTimer;

    private void Update()
    {
        if (adaptiveSource == null ||
            debugText == null)
        {
            return;
        }

        refreshTimer -= Time.unscaledDeltaTime;

        if (refreshTimer > 0f)
            return;

        refreshTimer = refreshInterval;

        Refresh();
    }

    private void Refresh()
    {
        StringBuilder sb = new StringBuilder();


        // =====================================
        // BACK DODGE
        // =====================================

        float chase =
            adaptiveSource
                .DebugBackDodgeChaseScore;

        sb.AppendLine(
            "<b>BACK DODGE</b>"
        );

        sb.AppendLine(
            $"Chase   {MakeBar(chase)}  {chase:F2}"
        );

        sb.AppendLine(
            $"Learn Threshold  " +
            $"{adaptiveSource.DebugBackDodgeLearnThreshold:F2}"
        );

        sb.AppendLine(
            $"Policy  " +
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
        // HEAVY
        // =====================================

        float heavy =
            adaptiveSource
                .DebugHeavyTimingScore;

        sb.AppendLine(
            "<b>HEAVY TIMING</b>"
        );

        sb.AppendLine(
            $"Counter {MakeBar(heavy)}  {heavy:F2}"
        );

        sb.AppendLine(
            $"Learn Threshold  " +
            $"{adaptiveSource.DebugHeavyTimingThreshold:F2}"
        );

        sb.AppendLine(
            "Timing  " +
            FormatTiming(
                adaptiveSource.DebugHeavyTimingActive,
                adaptiveSource.DebugHeavyTimingMode
            )
        );

        sb.AppendLine();


        // =====================================
        // CHARGE
        // =====================================

        float charge =
            adaptiveSource
                .DebugChargeTimingScore;

        sb.AppendLine(
            "<b>CHARGE TIMING</b>"
        );

        sb.AppendLine(
            $"Counter {MakeBar(charge)}  {charge:F2}"
        );

        sb.AppendLine(
            $"Learn Threshold  " +
            $"{adaptiveSource.DebugChargeTimingThreshold:F2}"
        );

        sb.AppendLine(
            "Timing  " +
            FormatTiming(
                adaptiveSource.DebugChargeTimingActive,
                adaptiveSource.DebugChargeTimingMode
            )
        );


        debugText.text =
            sb.ToString();
    }

    private string MakeBar(float value)
    {
        value = Mathf.Clamp01(value);

        const int length = 10;

        int filled =
            Mathf.RoundToInt(
                value * length
            );

        StringBuilder bar =
            new StringBuilder();

        bar.Append("[");

        for (int i = 0; i < length; i++)
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

        return
            $"<color=#63FF91><b>" +
            $"{policy.ToString().ToUpper()}" +
            $"</b></color>";
    }

    private string FormatTiming(
        bool active,
        TimingAdaptMode mode)
    {
        if (!active ||
            mode == TimingAdaptMode.None)
        {
            return
                "<color=#909090>NONE</color>";
        }

        string label =
            mode == TimingAdaptMode.Early
                ? "EARLIER"
                : "DELAYED";

        return
            $"<color=#FFD166><b>" +
            $"{label}" +
            $"</b></color>";
    }
}