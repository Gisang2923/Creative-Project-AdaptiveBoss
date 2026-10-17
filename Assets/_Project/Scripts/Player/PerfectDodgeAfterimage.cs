using System.Collections;
using UnityEngine;

public class PerfectDodgeAfterimage : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField]
    private SpriteRenderer sourceRenderer;

    [SerializeField]
    private GameObject ghostPrefab;

    [Header("Settings")]
    [SerializeField]
    private float spawnInterval = 0.035f;

    [SerializeField]
    private float ghostLifeTime = 0.18f;

    [SerializeField, Range(0f, 1f)]
    private float ghostAlpha = 0.4f;

    [SerializeField]
    private int sortingOrderOffset = -1;
    [SerializeField]
    private PlayerDash playerDash;

    [SerializeField]
    private float ghostSpacing = 0.35f;

    private int ghostIndex;
    private Coroutine afterimageRoutine;
    [SerializeField] private int ghostCount = 6;


    public void StartAfterimage()
    {
        if (afterimageRoutine != null)
            return;

        // 가장 먼 위치부터 시작
        ghostIndex = ghostCount - 1;

        afterimageRoutine =
            StartCoroutine(
                AfterimageRoutine()
            );
    }


    public void StopAfterimage()
    {
        if (afterimageRoutine == null)
            return;

        StopCoroutine(
            afterimageRoutine
        );

        afterimageRoutine = null;
    }


    private IEnumerator AfterimageRoutine()
    {
        while (ghostIndex >= 0)
        {
            CreateGhost();

            ghostIndex--;

            yield return new WaitForSeconds(
                spawnInterval
            );
        }

        afterimageRoutine = null;
    }


    private void CreateGhost()
    {
        if (sourceRenderer == null ||
            sourceRenderer.sprite == null ||
            ghostPrefab == null)
        {
            return;
        }

        Vector3 ghostPosition =
            sourceRenderer.transform.position;

        if (playerDash != null)
        {
            ghostPosition.x -=
                playerDash.FacingBeforeDash *
                ghostSpacing *
                ghostIndex;
        }

        GameObject ghost =
            Instantiate(
                ghostPrefab,
                ghostPosition,
                sourceRenderer.transform.rotation
            );

        ghostIndex++;

        // 현재 플레이어 방향 그대로 복사
        ghost.transform.localScale =
            sourceRenderer.transform.lossyScale;

        AfterimageGhost ghostScript =
            ghost.GetComponent<
                AfterimageGhost
            >();

        Color ghostColor =
            new Color(
                0.65f,
                0.85f,
                1.0f,
                ghostAlpha
            );

        ghostScript.Setup(
            sourceRenderer.sprite,
            ghostColor,
            ghostLifeTime,
            sourceRenderer.sortingLayerID,
            sourceRenderer.sortingOrder +
                sortingOrderOffset
        );
    }
}