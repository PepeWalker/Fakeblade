using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    public AudioSource ambienceSource;

    [Header("Music")]
    public AudioClip mainMenuMusic;
    public AudioClip battleMusic;
    public AudioClip victoryMusic;

    [Header("Global SFX")]
    public AudioClip buttonClickSound;
    public AudioClip countdownSound;
    public AudioClip battleStartSound;
    public AudioClip victorySound;

    [Header("BeyBlade SFX")]
    public AudioClip[] collisionSounds;
    public AudioClip[] metalClashSounds;

    [Header("Audio Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private Dictionary<string, AudioClip> soundLibrary = new Dictionary<string, AudioClip>();
    private List<AudioSource> activeSources = new List<AudioSource>();

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
            return;
        }
    }

    public void Initialize()
    {
        LoadSoundLibrary();
        SetupAudioSources();

        // Play main menu music by default
        PlayMusic(mainMenuMusic, true);

        Debug.Log("AudioManager initialized");
    }

    private void LoadSoundLibrary()
    {
        // Add sounds to library for easy access
        if (buttonClickSound != null) soundLibrary["button_click"] = buttonClickSound;
        if (countdownSound != null) soundLibrary["countdown"] = countdownSound;
        if (battleStartSound != null) soundLibrary["battle_start"] = battleStartSound;
        if (victorySound != null) soundLibrary["victory"] = victorySound;
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("Music Source");
            musicGO.transform.parent = transform;
            musicSource = musicGO.AddComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            GameObject sfxGO = new GameObject("SFX Source");
            sfxGO.transform.parent = transform;
            sfxSource = sfxGO.AddComponent<AudioSource>();
        }

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;

        UpdateVolumes();
    }

    public void UpdateVolumes()
    {
        if (musicSource != null)
            musicSource.volume = masterVolume * musicVolume;

        if (sfxSource != null)
            sfxSource.volume = masterVolume * sfxVolume;

        if (ambienceSource != null)
            ambienceSource.volume = masterVolume * musicVolume * 0.5f;
    }

    #region Music Control
    public void PlayMusic(AudioClip clip, bool fadeIn = false)
    {
        if (clip == null || musicSource == null) return;

        if (fadeIn && musicSource.isPlaying)
        {
            StartCoroutine(CrossfadeMusic(clip));
        }
        else
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic(bool fadeOut = false)
    {
        if (musicSource == null) return;

        if (fadeOut)
        {
            StartCoroutine(FadeOutMusic());
        }
        else
        {
            musicSource.Stop();
        }
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        float fadeTime = 1f;
        float originalVolume = musicSource.volume;

        // Fade out current music
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(originalVolume, 0, t / fadeTime);
            yield return null;
        }

        // Switch to new clip
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade in new music
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0, originalVolume, t / fadeTime);
            yield return null;
        }

        musicSource.volume = originalVolume;
    }

    private IEnumerator FadeOutMusic()
    {
        float fadeTime = 1f;
        float startVolume = musicSource.volume;

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = startVolume;
    }
    #endregion

    #region SFX Control
    public void PlaySFX(string soundName)
    {
        if (soundLibrary.ContainsKey(soundName))
        {
            PlaySFX(soundLibrary[soundName]);
        }
        else
        {
            Debug.LogWarning($"Sound '{soundName}' not found in library");
        }
    }

    public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.volume = masterVolume * sfxVolume * volumeMultiplier;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeMultiplier = 1f)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("Temp Audio");
        tempGO.transform.position = position;

        AudioSource tempSource = tempGO.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = masterVolume * sfxVolume * volumeMultiplier;
        tempSource.spatialBlend = 1f; // 3D sound
        tempSource.Play();

        // Destroy after clip finishes
        Destroy(tempGO, clip.length + 0.1f);
    }

    public void PlayCollisionSound(float intensity, AudioClip specificClip = null)
    {
        AudioClip clipToPlay = specificClip;

        if (clipToPlay == null && collisionSounds != null && collisionSounds.Length > 0)
        {
            int index = Mathf.RoundToInt(intensity * (collisionSounds.Length - 1));
            index = Mathf.Clamp(index, 0, collisionSounds.Length - 1);
            clipToPlay = collisionSounds[index];
        }

        if (clipToPlay != null)
        {
            float volume = Mathf.Clamp01(intensity * 0.5f + 0.2f);
            PlaySFX(clipToPlay, volume);
        }
    }

    public void PlayMetalClash(float intensity)
    {
        if (metalClashSounds != null && metalClashSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, metalClashSounds.Length);
            float volume = Mathf.Clamp01(intensity * 0.3f + 0.4f);
            PlaySFX(metalClashSounds[randomIndex], volume);
        }
    }
    #endregion

    #region Game State Audio
    public void OnGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.MainMenu:
                PlayMusic(mainMenuMusic, true);
                break;
            case GameManager.GameState.Battle:
                PlayMusic(battleMusic, true);
                PlaySFX("battle_start");
                break;
            case GameManager.GameState.BattleEnd:
                PlayMusic(victoryMusic, true);
                PlaySFX("victory");
                break;
        }
    }

    public void PlayCountdown()
    {
        PlaySFX("countdown");
    }

    public void PlayButtonClick()
    {
        PlaySFX("button_click");
    }
    #endregion

    #region Volume Control
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateVolumes();
    }
    #endregion
}