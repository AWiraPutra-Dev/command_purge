using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbientWindSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip windClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    [Header("Timer")]
    [SerializeField] private float interval = 20f;

    [Header("Fade Out")]
    [Range(0f, 0.2f)] [SerializeField] private float fadeOutDuration = 0.01f;

    [Header("Reverb")]
    [SerializeField] private bool reverbEnabled = true;
    [Range(0.1f, 5f)] [SerializeField] private float reverbDecayTime = 1.5f;

    private AudioSource _audioSource;
    private AudioReverbFilter _reverbFilter;
    private Coroutine _fadeCoroutine;
    private float _playVolume;
    private float _timer;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;
        _timer = interval;

        if (reverbEnabled)
        {
            _reverbFilter = GetComponent<AudioReverbFilter>();
            if (_reverbFilter == null)
                _reverbFilter = gameObject.AddComponent<AudioReverbFilter>();
            _reverbFilter.reverbPreset = AudioReverbPreset.Cave;
            _reverbFilter.decayTime = reverbDecayTime;
        }
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer += interval;
            PlayWindSound();
        }
    }

    private void PlayWindSound()
    {
        if (windClip == null) return;

        _playVolume = volume;
        _audioSource.clip = windClip;
        _audioSource.pitch = Random.Range(0.85f, 1.15f);
        _audioSource.volume = _playVolume;
        _audioSource.Play();

        if (fadeOutDuration > 0f)
        {
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        AudioClip clip = _audioSource.clip;
        if (clip == null) yield break;

        float fadeStart = clip.length - fadeOutDuration;
        if (fadeStart < 0f) fadeStart = 0f;

        while (_audioSource.isPlaying && _audioSource.time < fadeStart)
            yield return null;

        if (!_audioSource.isPlaying) yield break;

        float startVol = _playVolume;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration && _audioSource.isPlaying)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        if (_audioSource.isPlaying)
            _audioSource.Stop();

        _fadeCoroutine = null;
    }
}
