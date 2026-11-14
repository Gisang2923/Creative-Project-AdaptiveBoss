using UnityEngine;

public class Act2WorldPresentationController
    : MonoBehaviour
{
    [Header("Sky")]
    [SerializeField]
    private SpriteRenderer
        skyRenderer;

    [SerializeField]
    private Material
        act2SkyMaterial;


    [Header("Far Background")]
    [SerializeField]
    private SpriteRenderer
        farBackgroundRenderer;

    [SerializeField]
    private Material
        act2FarMaterial;


    [Header("Foreground")]
    [SerializeField]
    private SpriteRenderer
        foregroundRenderer;

    [SerializeField]
    private Material
        act2ForegroundMaterial;


    [Header("Ground")]
    [SerializeField]
    private SpriteRenderer
        groundRenderer;

    [SerializeField]
    private Material
        act2GroundMaterial;


    private void Start()
    {
        GameProgression progression =
            GameProgression.Instance;


        if (progression == null)
        {
            Debug.LogWarning(
                "[Act2 World] " +
                "GameProgression missing."
            );

            return;
        }


        if (!progression.IsAdaptivePhase)
        {
            Debug.Log(
                "[Act2 World] " +
                "Normal World"
            );

            return;
        }


        ApplyAct2World();
    }


    private void ApplyAct2World()
    {
        ApplyMaterial(
            skyRenderer,
            act2SkyMaterial
        );

        ApplyMaterial(
            farBackgroundRenderer,
            act2FarMaterial
        );

        ApplyMaterial(
            foregroundRenderer,
            act2ForegroundMaterial
        );

        ApplyMaterial(
            groundRenderer,
            act2GroundMaterial
        );


        Debug.Log(
            "[Act2 World] " +
            "Ash World Applied"
        );
    }


    private void ApplyMaterial(
        SpriteRenderer renderer,
        Material material)
    {
        if (renderer == null ||
            material == null)
        {
            return;
        }


        renderer.material =
            material;
    }
}