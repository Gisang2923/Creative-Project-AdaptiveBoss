using System.Collections;
using UnityEngine;

public class AfterimageGhost : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public void Setup(
        Sprite sprite,
        Color color,
        float lifeTime,
        int sortingLayerID,
        int sortingOrder)
    {
        spriteRenderer =
            GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        spriteRenderer.sortingLayerID =
            sortingLayerID;

        spriteRenderer.sortingOrder =
            sortingOrder;

        StartCoroutine(
            FadeRoutine(lifeTime)
        );
    }

    private IEnumerator FadeRoutine(
        float lifeTime)
    {
        float timer = 0f;

        Color startColor =
            spriteRenderer.color;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;

            float t =
                timer / lifeTime;

            Color color =
                startColor;

            color.a =
                Mathf.Lerp(
                    startColor.a,
                    0f,
                    t
                );

            spriteRenderer.color =
                color;

            yield return null;
        }

        Destroy(gameObject);
    }
}