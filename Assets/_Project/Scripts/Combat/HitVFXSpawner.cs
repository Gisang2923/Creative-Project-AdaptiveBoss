using UnityEngine;

public class HitVFXSpawner : MonoBehaviour
{
    [Header("Hit VFX")]
    [SerializeField]
    private GameObject[] hitVfxPrefabs;

    [SerializeField]
    private Transform spawnPoint;

    public void Play(Vector2 hitDirection)
    {
        if (hitVfxPrefabs == null ||
            hitVfxPrefabs.Length == 0)
        {
            return;
        }

        int index =
            Random.Range(
                0,
                hitVfxPrefabs.Length
            );

        GameObject prefab =
            hitVfxPrefabs[index];

        if (prefab == null)
            return;

        Vector3 position =
            spawnPoint != null
                ? spawnPoint.position
                : transform.position;

        GameObject vfx =
            Instantiate(
                prefab,
                position,
                Quaternion.identity
            );


        // 공격 방향에 따라 좌우 반전
        float direction =
            Mathf.Sign(
                hitDirection.x
            );

        if (direction == 0f)
            direction = 1f;

        Vector3 scale =
            vfx.transform.localScale;

        scale.x =
            Mathf.Abs(scale.x) *
            direction;

        vfx.transform.localScale =
            scale;
    }
}