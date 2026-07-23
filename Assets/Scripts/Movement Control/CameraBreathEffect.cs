using UnityEngine;

public class CameraBreathEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FPSMovement fpsMovement;

    [Header("Breath Settings")]
    [SerializeField] private float breathDuration = 6.5f;
    [SerializeField] private float breathPosAmplitude = 0.06f;
    [SerializeField] private float breathRotAmplitude = 3f;

    [Header("Walk Settings")]
    [SerializeField] private float walkPosAmplitude = 0.04f;
    [SerializeField] private float walkRotAmplitude = 1.5f;
    [SerializeField] private float walkFrequency = 5f;
    [SerializeField] private float walkLateralAmplitude = 0.015f;

    [Header("Blend")]
    [SerializeField] private float blendSpeed = 2.5f;

    private float _breathTimer;
    private float _walkTimer;
    private float _blend;
    private Vector3 _posOffset;
    private Vector3 _rotOffset;
    private void Awake()
    {
        fpsMovement = GetComponentInParent<FPSMovement>();
        if (fpsMovement == null)
            fpsMovement = FindFirstObjectByType<FPSMovement>();
        if (fpsMovement == null)
            fpsMovement = FindAnyObjectByType<FPSMovement>();
    }

    private void Update()
    {
        transform.localPosition -= _posOffset;
        transform.localRotation *= Quaternion.Inverse(Quaternion.Euler(_rotOffset));

        if (fpsMovement == null)
            fpsMovement = FindFirstObjectByType<FPSMovement>();

        if (fpsMovement != null && (fpsMovement.IsClimbing() || fpsMovement.IsTransitioningToLadder()))
        {
            _posOffset = Vector3.zero;
            _rotOffset = Vector3.zero;
            _breathTimer = 0f;
            _walkTimer = 0f;
            _blend = 0f;
            return;
        }

        _breathTimer += Time.deltaTime;
        if (_breathTimer > breathDuration)
            _breathTimer -= breathDuration;

        bool isMoving = fpsMovement != null && fpsMovement.IsMoving();
        float targetBlend = isMoving ? 1f : 0f;
        _blend = Mathf.Lerp(_blend, targetBlend, Time.deltaTime * blendSpeed);

        float breathPhase = _breathTimer / breathDuration;
        float breathValue = EvaluateBreath(breathPhase);

        float depthVar = 1f + (Mathf.PerlinNoise(Time.time * 0.12f, 0f) - 0.5f) * 0.16f;
        float wobbleX = (Mathf.PerlinNoise(Time.time * 0.4f, 1f) - 0.5f) * 0.004f;
        float wobbleY = (Mathf.PerlinNoise(Time.time * 0.3f, 2f) - 0.5f) * 0.002f;
        float wobbleRot = (Mathf.PerlinNoise(Time.time * 0.25f, 3f) - 0.5f) * 0.5f;

        float v = breathValue * depthVar;

        Vector3 breathPos = new Vector3(wobbleX, v * breathPosAmplitude + wobbleY, 0);
        Vector3 breathRot = new Vector3(
            v * breathRotAmplitude + wobbleRot,
            breathValue * breathRotAmplitude * 0.12f,
            v * breathRotAmplitude * 0.3f
        );

        if (isMoving)
            _walkTimer += Time.deltaTime * walkFrequency;

        float vBob = Mathf.Sin(_walkTimer * 2f);
        float hBob = Mathf.Cos(_walkTimer * 2f);

        Vector3 walkPos = new Vector3(
            hBob * walkLateralAmplitude,
            vBob * walkPosAmplitude,
            0
        );
        Vector3 walkRot = new Vector3(
            vBob * walkRotAmplitude,
            hBob * walkRotAmplitude * 0.2f,
            Mathf.Sin(_walkTimer) * walkRotAmplitude * 0.15f
        );

        _posOffset = Vector3.Lerp(breathPos, walkPos, _blend);
        _rotOffset = Vector3.Lerp(breathRot, walkRot, _blend);

        transform.localPosition += _posOffset;
        transform.localRotation *= Quaternion.Euler(_rotOffset);
    }

    private float EvaluateBreath(float t)
    {
        if (t < 0.4f)
        {
            float p = t / 0.4f;
            return p * p * (3f - 2f * p);
        }
        else
        {
            float p = (t - 0.4f) / 0.6f;
            return 1f - p * p * (3f - 2f * p);
        }
    }
}
