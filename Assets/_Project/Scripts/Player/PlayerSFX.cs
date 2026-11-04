using UnityEngine;

public class PlayerSFX : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource sfxSource;

    [SerializeField]
    private AudioSource healingSource;
    [SerializeField]
    private AudioSource precisionSource;
    [Header("Normal Attack")]
    [SerializeField]
    private AudioClip slash1;

    [SerializeField]
    private AudioClip slash2;

    [SerializeField]
    private AudioClip slash3;

    [Header("Movement")]
    [SerializeField]
    private AudioClip dash;

    [Header("Counter")]
    [SerializeField]
    private AudioClip parry;

    [Header("Heal")]
    [SerializeField]
    private AudioClip healing;

    [SerializeField]
    private AudioClip healCompleted;
    [Header("Precision")]
    [SerializeField]
    private AudioClip perfectDodge;

    [SerializeField]
    private AudioClip precisionFull;

    [Header("Hit")]
    [SerializeField]
    private AudioClip hit;

    [SerializeField]
    private AudioClip strongHit;

    public void PlayNormalSlash(
        int comboStep)
    {
        if (sfxSource == null)
            return;

        AudioClip clip;

        switch (comboStep)
        {
            case 2:
                clip = slash2;
                break;

            case 3:
                clip = slash3;
                break;

            default:
                clip = slash1;
                break;
        }

        if (clip != null)
        {
            sfxSource.PlayOneShot(
                clip
            );
        }
    }

    public void PlayDash()
    {
        if (sfxSource == null ||
            dash == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            dash
        );
    }

    public void PlayParry(
        bool perfect)
    {
        if (sfxSource == null ||
            parry == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            parry,
            perfect
                ? 1.15f
                : 0.9f
        );
    }

    public void StartHealing()
    {
        if (healingSource == null ||
            healing == null)
        {
            return;
        }

        healingSource.Stop();

        healingSource.clip =
            healing;

        healingSource.loop = true;

        healingSource.Play();
    }

    public void StopHealing()
    {
        if (healingSource == null)
            return;

        healingSource.Stop();
        healingSource.clip = null;
    }

    public void PlayHealCompleted()
    {
        if (sfxSource == null ||
            healCompleted == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            healCompleted
        );
    }
    public void PlayPerfectDodge()
    {
        if (precisionSource == null ||
            perfectDodge == null)
        {
            return;
        }

        precisionSource.PlayOneShot(
            perfectDodge,
            1.1f
        );
    }

    public void PlayPrecisionFull()
    {
        if (precisionSource == null ||
            precisionFull == null)
        {
            return;
        }

        precisionSource.PlayOneShot(
            precisionFull
        );
    }

    public void PlayHit(
        bool strong)
    {
        if (sfxSource == null)
            return;

        AudioClip clip =
            strong
                ? strongHit
                : hit;

        if (clip == null)
            return;

        sfxSource.PlayOneShot(
            clip
        );
    }
}