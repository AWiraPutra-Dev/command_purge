using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
public class FirstPersonFootstep : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSMovement fpsMovement;

    [Header("Step Settings")]
    [Tooltip("Waktu antar langkah kaki (detik). Semakin besar = semakin lambat.")]
    [SerializeField] private float baseStepInterval = 0.7f;

    [Header("Surface Audio Clips")]
    [SerializeField] private AudioClip[] concreteClips;
    [SerializeField] private AudioClip[] woodClips;
    [SerializeField] private AudioClip[] dirtClips;
    [SerializeField] private AudioClip[] metalClips;
    [SerializeField] private AudioClip[] defaultClips;

    private CharacterController _controller;
    private AudioSource _audioSource;
    private float _stepTimer;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();

        if (fpsMovement == null)
            fpsMovement = GetComponent<FPSMovement>();

        _audioSource.spatialBlend = 1f;
    }

    private void Update()
    {
        HandleFootsteps();
    }

    private void HandleFootsteps()
    {
        if (!_controller.isGrounded) return;

        if (fpsMovement != null && (fpsMovement.IsClimbing() || fpsMovement.IsTransitioningToLadder()))
            return;

        if (fpsMovement != null && fpsMovement.IsMoving())
        {
            _stepTimer -= Time.deltaTime;

            if (_stepTimer <= 0f)
            {
                PlayFootstepSound();
                _stepTimer += baseStepInterval;
            }
        }
        else
        {
            if (_stepTimer < 0f)
                _stepTimer = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        AudioClip[] clips = GetClipsForSurface();

        if (clips == null || clips.Length == 0)
            clips = defaultClips;

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.volume = Random.Range(0.8f, 1f);
        _audioSource.PlayOneShot(clip);
    }

    private AudioClip[] GetClipsForSurface()
    {
        Vector3 origin = transform.position;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _controller.height * 0.5f + 0.2f))
        {
            if (hit.collider.TryGetComponent(out SurfaceDefinition surfaceDef))
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
        return defaultClips;
    }
}
