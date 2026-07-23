using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class FirstPersonFootstep : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSMovement fpsMovement;

    [Header("Step Settings")]
    [Tooltip("Waktu antar langkah kaki (detik). Semakin besar = semakin lambat.")]
    [SerializeField] private float baseStepInterval = 0.7f;
    [SerializeField] private float playDelay = 0.004f;
    [Tooltip("Kecepatan referensi untuk dynamic step interval.")]
    [SerializeField] private float referenceWalkSpeed = 3f;

    [Header("Surface Audio Clips")]
    [SerializeField] private AudioClip[] concreteClips;
    [SerializeField] private AudioClip[] woodClips;
    [SerializeField] private AudioClip[] dirtClips;
    [SerializeField] private AudioClip[] metalClips;
    [SerializeField] private AudioClip[] defaultClips;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float footstepsVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float ladderVolume = 1f;

    [Header("Fade Out")]
    [SerializeField] private bool fadeOutEnabled = true;
    [Range(0f, 0.2f)] [SerializeField] private float fadeOutDuration = 0.01f;

    [Header("Ladder Climb Settings")]
    [SerializeField] private float ladderStepInterval = 0.4f;
    [SerializeField] private AudioClip[] ladderClips;

    private CharacterController _controller;
    private AudioSource _audioSource;
    private float _stepTimer;
    private float _ladderStepTimer;
    private float _playDelayTimer;
    private Vector3 _lastPosition;
    private Coroutine _fadeCoroutine;
    private float _lastPlayVolume;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();

        if (fpsMovement == null)
            fpsMovement = GetComponent<FPSMovement>();

        _audioSource.spatialBlend = 1f;
        _lastPosition = transform.position;
    }

    private void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (fpsMovement != null && fpsMovement.IsTransitioningToLadder())
        {
            _lastPosition = transform.position;
            return;
        }

        if (fpsMovement != null && fpsMovement.IsClimbing())
        {
            HandleLadderClimbSound();
            return;
        }

        if (!_controller.isGrounded) return;

        if (fpsMovement != null && fpsMovement.IsMoving())
        {
            Vector3 displacement = transform.position - _lastPosition;
            _lastPosition = transform.position;
            float horizontalSpeed = new Vector3(displacement.x, 0, displacement.z).magnitude / Time.deltaTime;

            if (horizontalSpeed < 0.01f)
            {
                _playDelayTimer = 0f;
                return;
            }

            _stepTimer -= Time.deltaTime;

            float effectiveInterval = Mathf.Lerp(baseStepInterval * 1.25f, baseStepInterval * 0.75f, Mathf.Clamp01(horizontalSpeed / referenceWalkSpeed));

            if (_stepTimer <= 0f)
            {
                _playDelayTimer = playDelay;
                _stepTimer = Mathf.Max(0f, _stepTimer) + effectiveInterval;
            }

            if (_playDelayTimer > 0f)
            {
                _playDelayTimer -= Time.deltaTime;
                if (_playDelayTimer <= 0f)
                    PlayFootstepSound();
            }
        }
        else
        {
            if (_stepTimer < 0f)
                _stepTimer = 0f;
            _playDelayTimer = 0f;
        }
    }

    private void HandleLadderClimbSound()
    {
        Vector3 displacement = transform.position - _lastPosition;
        _lastPosition = transform.position;
        float verticalSpeed = Mathf.Abs(displacement.y) / Time.deltaTime;

        if (verticalSpeed > 0.01f)
        {
            _ladderStepTimer -= Time.deltaTime;
            if (_ladderStepTimer <= 0f)
            {
                _ladderStepTimer += ladderStepInterval;
                if (!_audioSource.isPlaying)
                    PlayLadderSound();
            }
        }
    }

    private void PlayLadderSound()
    {
        if (ladderClips == null || ladderClips.Length == 0) return;

        AudioClip clip = ladderClips[Random.Range(0, ladderClips.Length)];
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.volume = ladderVolume * Random.Range(0.8f, 1f);
        _audioSource.PlayOneShot(clip);
    }

    private void PlayFootstepSound()
    {
        AudioClip[] clips = GetClipsForSurface();

        if (clips == null || clips.Length == 0)
            clips = defaultClips;

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        _lastPlayVolume = footstepsVolume * Random.Range(0.8f, 1f);
        PlayStepWithFade(clip, _lastPlayVolume);
    }

    private void PlayStepWithFade(AudioClip clip, float targetVolume)
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        _audioSource.clip = clip;
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.volume = targetVolume;
        _audioSource.Play();

        if (fadeOutEnabled && fadeOutDuration > 0f)
            _fadeCoroutine = StartCoroutine(FadeOutRoutine(fadeOutDuration));
    }

    private IEnumerator FadeOutRoutine(float fadeDuration)
    {
        AudioClip clip = _audioSource.clip;
        if (clip == null) yield break;

        float clipLength = clip.length;
        float fadeStartTime = clipLength - fadeDuration;
        if (fadeStartTime < 0f) fadeStartTime = 0f;

        while (_audioSource.isPlaying && _audioSource.time < fadeStartTime)
            yield return null;

        if (!_audioSource.isPlaying) yield break;

        float startVolume = _lastPlayVolume;
        float elapsed = 0f;
        while (elapsed < fadeDuration && _audioSource.isPlaying)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        if (_audioSource.isPlaying)
            _audioSource.Stop();

        _audioSource.volume = _lastPlayVolume;
        _fadeCoroutine = null;
    }

    private AudioClip[] GetClipsForSurface()
    {
        Vector3 origin = transform.position;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _controller.height * 0.5f + 0.2f))
        {
            SurfaceDefinition surfaceDef = GetSurfaceDefinitionOnHierarchy(hit.collider.gameObject);
            if (surfaceDef != null)
            {
                switch (surfaceDef.surfaceType)
                {
                    case SurfaceType.Concrete: return concreteClips;
                    case SurfaceType.Wood: return woodClips;
                    case SurfaceType.Dirt: return dirtClips;
                    case SurfaceType.Metal: return metalClips;
                }
            }
        }

        if (defaultClips != null && defaultClips.Length > 0)
            return defaultClips;

        AudioClip[][] allArrays = { concreteClips, woodClips, dirtClips, metalClips };
        foreach (var arr in allArrays)
        {
            if (arr != null && arr.Length > 0)
                return arr;
        }
        return null;
    }

    private SurfaceDefinition GetSurfaceDefinitionOnHierarchy(GameObject obj)
    {
        SurfaceDefinition surface = obj.GetComponent<SurfaceDefinition>();
        if (surface != null) return surface;

        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            surface = parent.GetComponent<SurfaceDefinition>();
            if (surface != null) return surface;
            parent = parent.parent;
        }
        return null;
    }
}
