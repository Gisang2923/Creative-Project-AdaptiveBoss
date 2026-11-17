using UnityEngine;

public class BossIdentityPresentationController
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer bossRenderer;


    [Header("Materials")]
    [SerializeField]
    private Material normalMaterial;

    [SerializeField]
    private Material silhouetteMaterial;


    private GameProgression progression;


    private void Start()
    {
        BindProgression();

        ApplyCurrentPhase();
    }


    private void OnDestroy()
    {
        if (progression != null)
        {
            progression.OnPhaseChanged -=
                HandlePhaseChanged;
        }
    }


    private void BindProgression()
    {
        progression =
            GameProgression.Instance;


        if (progression == null)
        {
            Debug.LogWarning(
                "[Boss Identity] " +
                "GameProgression not found."
            );

            return;
        }


        progression.OnPhaseChanged -=
            HandlePhaseChanged;

        progression.OnPhaseChanged +=
            HandlePhaseChanged;
    }


    private void HandlePhaseChanged(
        GamePhase phase)
    {
        ApplyPhase(
            phase
        );
    }


    private void ApplyCurrentPhase()
    {
        if (progression == null)
        {
            progression =
                GameProgression.Instance;
        }


        if (progression == null)
            return;


        ApplyPhase(
            progression.CurrentPhase
        );
    }


    private void ApplyPhase(
        GamePhase phase)
    {
        if (bossRenderer == null)
        {
            Debug.LogWarning(
                "[Boss Identity] " +
                "Boss SpriteRenderer missing."
            );

            return;
        }


        switch (phase)
        {
            case GamePhase.FirstEncounter:
            case GamePhase.Act1:
            case GamePhase.Act1Cleared:

                ApplySilhouette();

                break;


            case GamePhase.Act2Adaptive:
            case GamePhase.Completed:

                ApplyNormal();

                break;
        }
    }


    public void ApplySilhouette()
    {
        if (bossRenderer == null ||
            silhouetteMaterial == null)
        {
            return;
        }


        bossRenderer.sharedMaterial =
            silhouetteMaterial;
    }


    public void ApplyNormal()
    {
        if (bossRenderer == null ||
            normalMaterial == null)
        {
            return;
        }


        bossRenderer.sharedMaterial =
            normalMaterial;
    }
}