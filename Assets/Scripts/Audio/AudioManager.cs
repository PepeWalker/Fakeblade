using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Sound Effects")]
    public AudioClip attackSound;
    public AudioClip collisionSound;
    public AudioClip dashSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        // Configuración inicial
    }

    public void PlayAttackSound()
    {
        if (attackSound != null && sfxSource != null)
            sfxSource.PlayOneShot(attackSound);
    }

    public void PlayCollisionSound(float intensity)
    {
        if (collisionSound != null && sfxSource != null)
        {
            sfxSource.volume = Mathf.Clamp01(intensity / 10f);
            sfxSource.PlayOneShot(collisionSound);
        }
    }

    public void PlayDashSound()
    {
        if (dashSound != null && sfxSource != null)
            sfxSource.PlayOneShot(dashSound);
    }
}