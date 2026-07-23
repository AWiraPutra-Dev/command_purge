using UnityEngine;

public class PlayerBreathSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip breathClip;
    [Range(0f, 1f)] [SerializeField] private float breathVolume = 1f;

    [Header("Breath Effect")]
    [Range(0f, 0.5f)] [SerializeField] private float breathIntensity = 0.25f;
    [Range(1f, 6f)] [SerializeField] private float breathRate = 3.5f;

    private AudioSource _audioSource;
    private float _baseVolume;

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = true;
        _audioSource.clip = breathClip;
        _audioSource.volume = breathVolume;
        _audioSource.Play();
        _baseVolume = breathVolume;
    }

    private void Update()
    {
        if (breathClip == null) return;
        float wave = Mathf.Sin(Time.time * breathRate);
        _audioSource.volume = _baseVolume * (1f + breathIntensity * wave);
    }
}
