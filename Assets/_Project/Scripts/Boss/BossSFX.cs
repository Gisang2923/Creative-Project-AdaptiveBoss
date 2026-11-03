using UnityEngine;

public class BossSFX : MonoBehaviour
{
    [SerializeField]
    private AudioSource audioSource;

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
}