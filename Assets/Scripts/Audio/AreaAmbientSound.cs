using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource), typeof(Collider))]
public class AreaAmbientSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip ambientClip;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float fadeOutDuration = 4f;

    private AudioSource _audioSource;
    private Coroutine _fadeCoroutine;
    private bool _playerInside;
    private bool _alreadyPlayed;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = ambientClip;
        _audioSource.loop = false;
        _audioSource.volume = 0f;
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FPSMovement>() == null) return;
        if (_alreadyPlayed) return;

        _playerInside = true;
        _alreadyPlayed = true;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        if (!_audioSource.isPlaying)
            _audioSource.Play();

        _fadeCoroutine = StartCoroutine(FadeVolume(volume, fadeInDuration));
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FPSMovement>() == null) return;

        _playerInside = false;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(FadeVolume(0f, fadeOutDuration));
    }

    private IEnumerator FadeVolume(float target, float duration)
    {
        float startVol = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVol, target, elapsed / duration);
            yield return null;
        }

        _audioSource.volume = target;

        if (target == 0f)
        {
            _audioSource.Stop();
            _audioSource.volume = 0f;
        }

        _fadeCoroutine = null;
    }
}
