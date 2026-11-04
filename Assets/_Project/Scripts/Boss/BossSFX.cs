using System.Collections;
using UnityEngine;

public class BossSFX : MonoBehaviour
{
    public static BossSFX Instance
    {
        get;
        private set;
    }

    [SerializeField]
    private AudioSource audioSource;

    private Coroutine duckRoutine;
    private float defaultVolume;

    private void Awake()
    {
        Instance = this;

        if (audioSource != null)
        {
            defaultVolume =
                audioSource.volume;
        }
    }

    [Header("Boss")]
    [SerializeField]
    private AudioClip start;

    [SerializeField]
    private AudioClip charge;

    [SerializeField]
    private AudioClip heavyHit;

    [SerializeField]
    private AudioClip counter;

    public void PlayStart()
    {
        Play(start);
    }

    public void PlayCharge()
    {
        Play(charge);
    }

    public void PlayHeavyHit()
    {
        Play(heavyHit);
    }

    public void PlayCounter()
    {
        Play(counter);
    }

    private void Play(
        AudioClip clip)
    {
        if (audioSource == null ||
            clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(
            clip
        );
    }
    public void Duck(
        float duckVolume,
        float duration)
    {
        if (audioSource == null)
            return;

        if (duckRoutine != null)
        {
            StopCoroutine(
                duckRoutine
            );
        }

        duckRoutine =
            StartCoroutine(
                DuckRoutine(
                    duckVolume,
                    duration
                )
            );
    }

    private IEnumerator DuckRoutine(
        float duckVolume,
        float duration)
    {
        audioSource.volume =
            defaultVolume * duckVolume;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            yield return null;
        }

        audioSource.volume =
            defaultVolume;

        duckRoutine = null;
    }
}