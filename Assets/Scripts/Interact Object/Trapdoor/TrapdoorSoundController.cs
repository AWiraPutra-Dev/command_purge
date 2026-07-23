using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TrapdoorSoundController : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;

    [Header("Animation Sync")]
    [SerializeField] private float openDuration = 0.8f;
    [SerializeField] private float closeDuration = 0.6f;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void PlayOpenSound()
    {
        if (openSound == null) return;
        _audioSource.clip = openSound;
        _audioSource.pitch = Mathf.Clamp(openSound.length / openDuration, 0.3f, 2f);
        _audioSource.volume = Random.Range(0.8f, 1f);
        _audioSource.Play();
    }

    public void PlayCloseSound()
    {
        if (closeSound == null) return;
        _audioSource.clip = closeSound;
        _audioSource.pitch = Mathf.Clamp(closeSound.length / closeDuration, 0.3f, 2f);
        _audioSource.volume = Random.Range(0.8f, 1f);
        _audioSource.Play();
    }
}
