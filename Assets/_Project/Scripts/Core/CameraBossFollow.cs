using UnityEngine;

public class CameraBossFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private Transform boss;

    [SerializeField]
    private Transform player;

    [SerializeField]
    private Camera targetCamera;

    [Header("Composition")]
    [SerializeField, Range(0.3f, 0.5f)]
    private float bossScreenPosition = 0.4f;

    [Header("Follow")]
    [SerializeField]
    private float followSmoothTime = 0.35f;

    [SerializeField]
    private float deadZone = 0.25f;

    [Header("Camera Bounds")]
    [SerializeField]
    private float minX = -6f;

    [SerializeField]
    private float maxX = 6f;

    private float velocityX;

    private float fixedY;
    private float fixedZ;

    private void Awake()
    {
        fixedY =
            transform.position.y;

        fixedZ =
            transform.position.z;

        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (boss == null ||
            player == null ||
            targetCamera == null)
        {
            return;
        }

        float cameraWidth =
            targetCamera.orthographicSize *
            2f *
            targetCamera.aspect;

        // Boss가 Player 왼쪽에 있으면
        // 화면 왼쪽에서 40%
        //
        // Boss가 Player 오른쪽에 있으면
        // 화면 왼쪽에서 60%
        float viewportPosition =
            boss.position.x <
            player.position.x
                ? bossScreenPosition
                : 1f -
                  bossScreenPosition;

        // 화면 중심(0.5)에서
        // Boss가 얼마나 벗어나야 하는지 계산
        float bossOffset =
            (
                viewportPosition -
                0.5f
            ) *
            cameraWidth;

        float targetX =
            boss.position.x -
            bossOffset;

        targetX =
            Mathf.Clamp(
                targetX,
                minX,
                maxX
            );

        float difference =
            targetX -
            transform.position.x;

        // 아주 작은 움직임에는
        // 카메라 반응 X
        if (Mathf.Abs(difference) <
            deadZone)
        {
            return;
        }

        float newX =
            Mathf.SmoothDamp(
                transform.position.x,
                targetX,
                ref velocityX,
                followSmoothTime
            );

        transform.position =
            new Vector3(
                newX,
                fixedY,
                fixedZ
            );
    }
}