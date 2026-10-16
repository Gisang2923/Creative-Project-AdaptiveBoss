using System.Collections;
using UnityEngine;

public class BossHitReaction : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform visual;

    [Header("Recoil")]
    [SerializeField] private float recoilDistance = 0.18f;
    [SerializeField] private float recoilOutDuration = 0.05f;
    [SerializeField] private float recoilReturnDuration = 0.10f;

    [Header("Enhanced Hit")]
    [SerializeField] private float enhancedMultiplier = 1.6f;

    private Vector3 restLocalPosition;
    private Coroutine recoilCoroutine;

    private void Awake()
    {
        if (visual != null)
        {
            restLocalPosition =
                visual.localPosition;
        }
    }

    public void Play(
        Vector2 hitDirection,
        bool isEnhanced)
    {
        if (visual == null)
            return;

        if (recoilCoroutine != null)
        {
            StopCoroutine(
                recoilCoroutine
            );

            recoilCoroutine = null;

            visual.localPosition =
                restLocalPosition;
        }

        float multiplier =
            isEnhanced
                ? enhancedMultiplier
                : 1f;

        recoilCoroutine =
            StartCoroutine(
                RecoilRoutine(
                    hitDirection,
                    multiplier
                )
            );
    }

    private IEnumerator RecoilRoutine(
        Vector2 hitDirection,
        float multiplier)
    {
        float worldDirection =
            Mathf.Sign(hitDirection.x);

        if (worldDirection == 0f)
            worldDirection = 1f;

        // 부모가 좌우 Flip되어 있어도
        // 실제 피격 방향 기준으로 밀리도록 변환
        Vector3 worldRecoil =
            Vector3.right * worldDirection;

        Vector3 localRecoil =
            visual.parent != null
                ? visual.parent
                    .InverseTransformVector(
                        worldRecoil
                    )
                : worldRecoil;

        localRecoil.Normalize();

        Vector3 recoilPosition =
            restLocalPosition +
            localRecoil *
            recoilDistance *
            multiplier;


        // =====================================
        // 1. 빠르게 뒤로 튕김
        // =====================================

        float elapsed = 0f;

        while (elapsed <
               recoilOutDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    recoilOutDuration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            visual.localPosition =
                Vector3.Lerp(
                    restLocalPosition,
                    recoilPosition,
                    t
                );

            yield return null;
        }


        visual.localPosition =
            recoilPosition;


        // =====================================
        // 2. 조금 천천히 원위치 복귀
        // =====================================

        elapsed = 0f;

        while (elapsed <
               recoilReturnDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed /
                    recoilReturnDuration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            visual.localPosition =
                Vector3.Lerp(
                    recoilPosition,
                    restLocalPosition,
                    t
                );

            yield return null;
        }


        visual.localPosition =
            restLocalPosition;

        recoilCoroutine = null;
    }

    public void ResetReaction()
    {
        if (recoilCoroutine != null)
        {
            StopCoroutine(
                recoilCoroutine
            );

            recoilCoroutine = null;
        }

        if (visual != null)
        {
            visual.localPosition =
                restLocalPosition;
        }
    }
}