using UnityEngine;

public class PlayerProgressionRuntime :
    MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Health health;

    [SerializeField]
    private PlayerMovement playerMovement;

    [SerializeField]
    private PlayerPrecisionStack precisionStack;

    [Header("Health")]
    [Tooltip(
        "Boss Basic Slash 1타 데미지와 동일하게 설정"
    )]
    [SerializeField]
    private int basicBossHitDamage = 20;

    [Header("Attack Level Multipliers")]
    [Tooltip(
        "Lv.0~5. 아직 공격력 성장 배율 미확정"
    )]
    [SerializeField]
    private float[] attackLevelMultipliers =
    {
        1f,
        1f,
        1f,
        1f,
        1f,
        1f
    };

    private GameProgression progression;

    private void Awake()
    {
        if (health == null)
        {
            health =
                GetComponent<Health>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }

        if (precisionStack == null)
        {
            precisionStack =
                GetComponent<
                    PlayerPrecisionStack>();
        }
    }

    private void Start()
    {
        BindProgression();
        ApplyProgression();
    }

    private void OnDestroy()
    {
        if (progression != null)
        {
            progression
                .OnProgressionChanged -=
                ApplyProgression;
        }
    }

    private void BindProgression()
    {
        progression =
            GameProgression.Instance;

        if (progression == null)
        {
            Debug.LogWarning(
                "[PlayerProgressionRuntime] " +
                "GameProgression not found."
            );

            return;
        }

        progression
            .OnProgressionChanged -=
            ApplyProgression;

        progression
            .OnProgressionChanged +=
            ApplyProgression;
    }

    public void ApplyProgression()
    {
        if (progression == null)
        {
            progression =
                GameProgression.Instance;

            if (progression == null)
                return;
        }

        // 사망 직후 RegisterDefeat()가
        // ProgressionChanged 이벤트를 발생시켜도
        // 현재 죽은 플레이어의 HP를 다시 채우지 않는다.
        //
        // 다음 Battle Scene에 새 Player가 생성되면
        // Start()에서 성장 수치를 정상 적용한다.
        if (health != null &&
            health.IsDead)
        {
            Debug.Log(
                "[Player Progression] " +
                "Player is dead -> Runtime apply skipped."
            );

            return;
        }

        ApplyHealth();
        ApplyMovement();
        ApplyPrecision();

        Debug.Log(
            $"[Player Progression Applied] " +
            $"ATK Lv.{progression.AttackLevel} | " +
            $"HP Lv.{progression.HealthLevel} | " +
            $"MENTAL Lv.{progression.MentalLevel}"
        );
    }

    private void ApplyHealth()
    {
        if (health == null)
            return;

        // HP Lv.0 = Basic 1타
        // HP Lv.5 = Basic 6타
        int baseMaxHealth =
            basicBossHitDamage *
            (
                progression.HealthLevel +
                1
            );

        int finalMaxHealth =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    baseMaxHealth *
                    progression
                        .GetMentalHealthMultiplier()
                )
            );

        health.SetMaxHealth(
            finalMaxHealth,
            true
        );
    }

    private void ApplyMovement()
    {
        playerMovement?
            .SetMoveSpeedMultiplier(
                progression
                    .GetMentalMoveMultiplier()
            );
    }

    private void ApplyPrecision()
    {
        precisionStack?
            .SetPrecisionEnabled(
                progression
                    .PrecisionUnlocked
            );
    }

    public int ModifyOutgoingDamage(
        int baseDamage)
    {
        if (baseDamage <= 0)
            return 0;

        if (progression == null)
        {
            progression =
                GameProgression.Instance;
        }

        if (progression == null)
            return baseDamage;

        float attackLevelMultiplier =
            GetAttackLevelMultiplier(
                progression.AttackLevel
            );

        float mentalMultiplier =
            progression
                .GetMentalAttackMultiplier();

        return Mathf.Max(
            1,
            Mathf.RoundToInt(
                baseDamage *
                attackLevelMultiplier *
                mentalMultiplier
            )
        );
    }

    private float
        GetAttackLevelMultiplier(
            int level)
    {
        if (attackLevelMultipliers == null ||
            attackLevelMultipliers.Length == 0)
        {
            return 1f;
        }

        int index =
            Mathf.Clamp(
                level,
                0,
                attackLevelMultipliers.Length - 1
            );

        return Mathf.Max(
            0f,
            attackLevelMultipliers[index]
        );
    }
}