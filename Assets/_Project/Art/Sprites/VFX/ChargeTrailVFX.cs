using UnityEngine;

public class ChargeTrailVFX : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer trailRenderer;

    [Header("Trail")]
    [SerializeField]
    private float trailHeight = 0.15f;

    [SerializeField]
    private float verticalOffset = 0.3f;

    [SerializeField]
    private float widthMultiplier = 1.8f;

    public void Setup(
        Vector2 start,
        Vector2 end)
    {
        float distance =
            Vector2.Distance(start, end);

        Vector2 center =
            (start + end) * 0.5f;

        transform.position =
            center +
            Vector2.up * verticalOffset;

        float width =
            distance * widthMultiplier;

        trailRenderer.size =
            new Vector2(
                width,
                trailHeight
            );
    }
}