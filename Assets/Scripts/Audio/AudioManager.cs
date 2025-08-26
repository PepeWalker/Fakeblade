using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;
    public AudioSource collisionAudioSource;

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
        Debug.Log("AudioManager initialized");
    }

    public void PlayAttackSound()
    {
        if (attackSound != null && sfxSource != null)
            sfxSource.PlayOneShot(attackSound);
    }

    public void PlayCollisionSound(float intensity, AudioClip soundClip)
    {
        if (collisionAudioSource != null && soundClip != null)
        {
            // Ajustar volumen basado en intensidad
            float volume = Mathf.Clamp01(intensity / 10f);

            // Ajustar pitch ligeramente para variedad
            float pitch = Random.Range(0.9f, 1.1f);

            collisionAudioSource.clip = soundClip;
            collisionAudioSource.volume = volume;
            collisionAudioSource.pitch = pitch;
            collisionAudioSource.Play();

            Debug.Log($"Playing collision sound - Volume: {volume:F2}, Pitch: {pitch:F2}");
        }
    }

    public void PlayDashSound()
    {
        if (dashSound != null && sfxSource != null)
            sfxSource.PlayOneShot(dashSound);
    }

    

    public void ApplyEffect(BeyBladeController blade)
    {
        // Implementar efectos de zona de arena
        // recuperar RPM, añadir poder especial, etc...
    }

}