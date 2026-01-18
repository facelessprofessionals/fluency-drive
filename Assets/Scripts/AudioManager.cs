using UnityEngine;
using System.Collections.Generic;

namespace FluencyDrive
{
    /// <summary>
    /// Simple audio manager for playing game sounds.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip tileSelectSound;
        [SerializeField] private AudioClip tileMatchSound;
        [SerializeField] private AudioClip invalidMatchSound;
        [SerializeField] private AudioClip levelCompleteSound;
        [SerializeField] private AudioClip wordAssembledSound;
        [SerializeField] private AudioClip showDefinitionSound;
        [SerializeField] private AudioClip bonusAwardedSound;
        [SerializeField] private AudioClip gameOverSound;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("Settings")]
        [SerializeField] private float sfxVolume = 1f;
        [SerializeField] private float musicVolume = 0.5f;

        private Dictionary<string, AudioClip> soundDictionary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSoundDictionary();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Start background music
            if (musicSource != null && backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
        }

        /// <summary>
        /// Initialize the sound dictionary for easy access.
        /// </summary>
        private void InitializeSoundDictionary()
        {
            soundDictionary = new Dictionary<string, AudioClip>
            {
                { "TileSelect", tileSelectSound },
                { "TileMatch", tileMatchSound },
                { "InvalidMatch", invalidMatchSound },
                { "LevelComplete", levelCompleteSound },
                { "WordAssembled", wordAssembledSound },
                { "ShowDefinition", showDefinitionSound },
                { "BonusAwarded", bonusAwardedSound },
                { "GameOver", gameOverSound }
            };
        }

        /// <summary>
        /// Play a sound effect by name.
        /// </summary>
        public void PlaySound(string soundName)
        {
            if (sfxSource == null) return;

            if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
            {
                if (clip != null)
                {
                    sfxSource.PlayOneShot(clip, sfxVolume);
                }
            }
            else
            {
                Debug.LogWarning($"Sound '{soundName}' not found in AudioManager");
            }
        }

        /// <summary>
        /// Play a sound effect with custom volume.
        /// </summary>
        public void PlaySound(string soundName, float volume)
        {
            if (sfxSource == null) return;

            if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
            {
                if (clip != null)
                {
                    sfxSource.PlayOneShot(clip, volume);
                }
            }
        }

        /// <summary>
        /// Set the SFX volume.
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Set the music volume.
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }

        /// <summary>
        /// Pause/resume background music.
        /// </summary>
        public void ToggleMusic(bool play)
        {
            if (musicSource == null) return;

            if (play)
                musicSource.UnPause();
            else
                musicSource.Pause();
        }

        /// <summary>
        /// Mute all audio.
        /// </summary>
        public void MuteAll(bool mute)
        {
            if (sfxSource != null)
                sfxSource.mute = mute;
            if (musicSource != null)
                musicSource.mute = mute;
        }
    }
}
