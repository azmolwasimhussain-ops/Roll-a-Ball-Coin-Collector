using UnityEngine;

namespace RollABallCoinCollector
{
    /// <summary>Optional audio hooks. Assign clips in the Inspector when assets are available.</summary>
    public sealed class GameAudio : MonoBehaviour
    {
        [Header("Optional audio clips")]
        [SerializeField] private AudioClip coinCollectClip;
        [SerializeField] private AudioClip obstacleHitClip;
        [SerializeField] private AudioClip backgroundMusicClip;

        [Header("Audio sources")]
        [SerializeField] private AudioSource effectsSource;
        [SerializeField] private AudioSource musicSource;

        public void ConfigureSources(AudioSource effects, AudioSource music)
        {
            effectsSource = effects;
            musicSource = music;
        }

        private void Awake()
        {
            if (effectsSource == null)
                effectsSource = gameObject.AddComponent<AudioSource>();
            if (musicSource == null)
                musicSource = gameObject.AddComponent<AudioSource>();

            effectsSource.playOnAwake = false;
            effectsSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.loop = true;

            if (backgroundMusicClip != null)
            {
                musicSource.clip = backgroundMusicClip;
                musicSource.Play();
            }
        }

        public void PlayCoin()
        {
            PlayEffect(coinCollectClip);
        }

        public void PlayObstacleHit()
        {
            PlayEffect(obstacleHitClip);
        }

        private void PlayEffect(AudioClip clip)
        {
            if (clip != null && effectsSource != null)
                effectsSource.PlayOneShot(clip);
        }
    }
}
