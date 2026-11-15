using System.Collections;
using UnityEngine;

public class BossAdaptiveAfterimage
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer sourceRenderer;

    [SerializeField]
    private GameObject ghostPrefab;


    [Header("Trail")]
    [SerializeField]
    private float spawnInterval =
        0.055f;

    [SerializeField]
    private float ghostLifeTime =
        0.22f;

    [SerializeField, Range(0f, 1f)]
    private float ghostAlpha =
        0.45f;

    [SerializeField]
    private int sortingOrderOffset =
        -1;


    [Header("Color")]
    [SerializeField]
    private Color ghostColor =
        new Color(
            0.75f,
            0.85f,
            1f,
            1f
        );


    private Coroutine trailRoutine;


    public void StartTrail()
    {
        StopTrail();

        // 시작 위치에도 바로 하나 생성
        CreateGhost();

        trailRoutine =
            StartCoroutine(
                TrailRoutine()
            );
    }


    public void StopTrail()
    {
        if (trailRoutine == null)
            return;

        StopCoroutine(
            trailRoutine
        );

        trailRoutine = null;
    }


    private IEnumerator TrailRoutine()
    {
        while (true)
        {
            yield return
                new WaitForSeconds(
                    spawnInterval
                );

            CreateGhost();
        }
    }


    private void CreateGhost()
    {
        if (sourceRenderer == null ||
            sourceRenderer.sprite == null ||
            ghostPrefab == null)
        {
            return;
        }


        GameObject ghost =
            Instantiate(
                ghostPrefab,
                sourceRenderer
                    .transform
                    .position,
                sourceRenderer
                    .transform
                    .rotation
            );


        // FacingRoot의 좌우 반전까지 복사
        ghost.transform.localScale =
            sourceRenderer
                .transform
                .lossyScale;


        AfterimageGhost ghostScript =
            ghost.GetComponent<
                AfterimageGhost
            >();


        if (ghostScript == null)
        {
            Destroy(ghost);
            return;
        }


        Color finalColor =
            ghostColor;

        finalColor.a =
            ghostAlpha;


        ghostScript.Setup(
            sourceRenderer.sprite,
            finalColor,
            ghostLifeTime,
            sourceRenderer.sortingLayerID,
            sourceRenderer.sortingOrder +
                sortingOrderOffset
        );
    }


    private void OnDisable()
    {
        StopTrail();
    }
}