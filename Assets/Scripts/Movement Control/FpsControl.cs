using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FPSMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 3.0f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionReference moveAction;

    [Header("Ladder Settings")]
    [SerializeField] private float climbSpeed = 0.2f;
    [SerializeField] private float climbSwingAmplitude = 0.04f;
    [SerializeField] private float climbSwingSpeed = 4f;
    [SerializeField] private float climbSwingRoll = 1.5f;
    [SerializeField] private float climbBobAmplitude = 0.015f;
    [SerializeField] private float climbBobFrequency = 5f;
    [SerializeField] private float climbAcceleration = 3f;

    [Header("Ladder Release")]
    [SerializeField] private string letGoPrompt = "Let Go Ladder (E)";
    [SerializeField] private GameObject climbPromptPanel;
    [SerializeField] private TextMeshProUGUI climbPromptText;

    [Header("Step Sounds")]
    [SerializeField] private float stepInterval = 0.5f;
    [SerializeField] private AudioSource stepAudioSource;
    [SerializeField] private AudioClip[] climbStepSounds;

    private CharacterController _characterController;
    private Vector2 _moveInput;
    private bool _isGrounded;
    private float _verticalVelocity;

    private bool _isClimbing;
    private Vector3 _ladderGrabDirection;
    private float _climbBobTimer;
    private float _targetDutch;
    private float _currentDutch;
    private Camera _mainCameraComponent;
    private Transform _mainCameraTransform;
    private CinemachineCamera _cinemachineCamera;
    private float _baseFov;
    private float _baseCameraY;
    private float _baseCameraX;

    private float _currentClimbSpeed;
    private float _swingTimer;
    private float _swingRollOffset;
    private float _climbDistanceTraveled;
    private float _climbDismountBoundaryY;
    private int _climbDirection;
    private bool _hasReachedBoundary;
    private float _climbFloorY;
    private bool _isReleasing;

    public System.Action OnLadderReleaseAtBoundary;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            _cinemachineCamera = cameraTransform.GetComponent<CinemachineCamera>();
            _mainCameraComponent = cameraTransform.GetComponentInChildren<Camera>();
            _mainCameraTransform = _mainCameraComponent != null
                ? _mainCameraComponent.transform
                : cameraTransform;

            if (_mainCameraComponent != null)
                _baseFov = _mainCameraComponent.fieldOfView;

            _baseCameraY = _mainCameraTransform.localPosition.y;
            _baseCameraX = _mainCameraTransform.localPosition.x;
        }

        if (climbPromptPanel == null || climbPromptText == null)
        {
            var allGOs = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var go in allGOs)
            {
                if (go.scene.isLoaded && go.name == "InteractionText" && go.GetComponent<TextMeshProUGUI>() != null)
                {
                    climbPromptText = go.GetComponent<TextMeshProUGUI>();
                    climbPromptPanel = go.transform.parent != null ? go.transform.parent.gameObject : go;
                    break;
                }
            }
            if (climbPromptPanel == null || climbPromptText == null)
                Debug.LogWarning("FPSMovement: ClimbPrompt UI tidak ditemukan. Assign manual di Inspector.");
        }
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
            moveAction.action.performed += StoreMovementInput;
            moveAction.action.canceled += StoreMovementInput;
        }
    }

    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.performed -= StoreMovementInput;
            moveAction.action.canceled -= StoreMovementInput;
            moveAction.action.Disable();
        }
    }

    private void Update()
    {
        if (_isReleasing) return;

        if (_isClimbing)
        {
            HandleClimbing();

            if (_hasReachedBoundary)
            {
                if (climbPromptPanel != null) climbPromptPanel.SetActive(true);
                if (climbPromptText != null) climbPromptText.text = letGoPrompt;
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ReleaseLadder();
                    if (climbPromptPanel != null) climbPromptPanel.SetActive(false);
                }
            }
            else
            {
                if (climbPromptPanel != null) climbPromptPanel.SetActive(false);
            }
        }
        else
        {
            _isGrounded = _characterController.isGrounded;
            HandleGravity();
            HandleMovement();
        }

        ApplyDutchTilt();
    }

    private void StoreMovementInput(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
    }

    public void ForceStartClimbing(Vector3 targetPosition, Vector3 grabDirection, float dismountBoundaryY, int climbDirection)
    {
        if (_isClimbing || _isReleasing) return;
        StartCoroutine(SmoothGrabCoroutine(targetPosition, grabDirection, dismountBoundaryY, climbDirection));
    }

    private System.Collections.IEnumerator SmoothGrabCoroutine(Vector3 targetPos, Vector3 grabDir, float boundaryY, int dir)
    {
        _isReleasing = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Quaternion targetRot = startRot;
        if (grabDir.magnitude > 0.01f)
        {
            Vector3 dirVec = grabDir;
            dirVec.y = 0;
            if (dirVec.magnitude > 0.01f)
                targetRot = Quaternion.LookRotation(dirVec.normalized);
        }

        float duration = 3.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            _characterController.enabled = false;
            transform.position = pos;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            _characterController.enabled = true;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _characterController.enabled = false;
        transform.position = targetPos;
        transform.rotation = targetRot;
        _characterController.enabled = true;

        _isClimbing = true;
        _ladderGrabDirection = grabDir;
        _ladderGrabDirection.y = 0;
        if (_ladderGrabDirection.magnitude > 0.01f)
            _ladderGrabDirection.Normalize();
        _verticalVelocity = 0;
        _climbBobTimer = 0f;
        _currentClimbSpeed = 0f;
        _swingTimer = 0f;
        _swingRollOffset = 0f;
        _climbDistanceTraveled = 0f;
        _climbDismountBoundaryY = boundaryY;
        _climbDirection = dir;
        _hasReachedBoundary = false;

        if (_mainCameraComponent != null)
            _mainCameraComponent.fieldOfView = _baseFov + 5f;

        if (_mainCameraTransform != null)
        {
            Vector3 pos = _mainCameraTransform.localPosition;
            pos.x = _baseCameraX;
            pos.y = _baseCameraY;
            _mainCameraTransform.localPosition = pos;
        }

        _isReleasing = false;
    }

    private void HandleGravity()
    {
        if (_isGrounded && _verticalVelocity < 0)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        var move = cameraTransform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.y)).normalized;
        var finalMove = move * walkSpeed;
        finalMove.y = _verticalVelocity;

        var collision = _characterController.Move(finalMove * Time.deltaTime);
        if ((collision & CollisionFlags.Above) != 0)
            _verticalVelocity = 0;
    }

    private void HandleClimbing()
    {
        if (_hasReachedBoundary)
        {
            _currentClimbSpeed = 0;
            return;
        }

        float vertical = 0f;
        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        else if (Input.GetKey(KeyCode.S)) vertical = -1f;

        _currentClimbSpeed = Mathf.Lerp(_currentClimbSpeed, vertical * climbSpeed, Time.deltaTime * climbAcceleration);

        float nextY = transform.position.y + _currentClimbSpeed * Time.deltaTime;
        if (_climbDirection > 0 && nextY >= _climbDismountBoundaryY)
        {
            if (_climbFloorY <= 0f)
                _climbFloorY = _climbDismountBoundaryY + _characterController.height + _characterController.skinWidth * 2f;
            Vector3 clampedPos = transform.position;
            clampedPos.y = _climbDismountBoundaryY;
            transform.position = clampedPos;
            _currentClimbSpeed = 0;
            _hasReachedBoundary = true;
            return;
        }
        if (_climbDirection < 0 && nextY <= _climbDismountBoundaryY)
        {
            Vector3 clampedPos = transform.position;
            clampedPos.y = _climbDismountBoundaryY;
            transform.position = clampedPos;
            _currentClimbSpeed = 0;
            _hasReachedBoundary = true;
            return;
        }

        Vector3 move = new Vector3(0, _currentClimbSpeed, 0);
        CollisionFlags flags = _characterController.Move(move * Time.deltaTime);

        _climbDistanceTraveled += Mathf.Abs(_currentClimbSpeed * Time.deltaTime);
        if (_climbDistanceTraveled >= stepInterval && Mathf.Abs(vertical) > 0.1f)
        {
            _climbDistanceTraveled = 0f;
            PlayStepSound();
        }

        if (Mathf.Abs(_currentClimbSpeed) > 0.1f)
        {
            _swingTimer += Time.deltaTime * climbSwingSpeed;
            float swingX = Mathf.Sin(_swingTimer) * climbSwingAmplitude;
            _swingRollOffset = Mathf.Sin(_swingTimer * 0.7f) * climbSwingRoll;

            Vector3 camPos = _mainCameraTransform.localPosition;
            camPos.x = _baseCameraX + swingX;
            camPos.y = _baseCameraY;
            _mainCameraTransform.localPosition = camPos;
        }
        else
        {
            Vector3 camPos = _mainCameraTransform.localPosition;
            camPos.x = Mathf.Lerp(camPos.x, _baseCameraX, Time.deltaTime * 3f);
            _mainCameraTransform.localPosition = camPos;
            _swingRollOffset = Mathf.Lerp(_swingRollOffset, 0f, Time.deltaTime * 3f);
        }

        _climbBobTimer += Time.deltaTime * climbBobFrequency;
        float bob = Mathf.Sin(_climbBobTimer) * climbBobAmplitude;
        Vector3 pos = _mainCameraTransform.localPosition;
        pos.y = _baseCameraY + bob;
        _mainCameraTransform.localPosition = pos;

        _targetDutch = _swingRollOffset;

        if (_ladderGrabDirection.magnitude > 0.01f)
        {
            Quaternion face = Quaternion.LookRotation(_ladderGrabDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, Time.deltaTime * 5f);
        }

        if ((flags & CollisionFlags.Above) != 0 && _climbDirection > 0)
        {
            _climbFloorY = transform.position.y + _characterController.height + _characterController.skinWidth * 2f;
            _hasReachedBoundary = true;
            _currentClimbSpeed = 0;
            return;
        }
    }

    private void PlayStepSound()
    {
        if (stepAudioSource == null) return;
        if (climbStepSounds == null || climbStepSounds.Length == 0) return;

        AudioClip clip = climbStepSounds[Random.Range(0, climbStepSounds.Length)];
        stepAudioSource.pitch = Random.Range(0.9f, 1.1f);
        stepAudioSource.PlayOneShot(clip);
    }

    public void StopClimbing()
    {
        _isClimbing = false;
        _hasReachedBoundary = false;

        if (climbPromptPanel != null) climbPromptPanel.SetActive(false);

        if (_mainCameraComponent != null)
            _mainCameraComponent.fieldOfView = _baseFov;

        _targetDutch = 0f;

        if (_mainCameraTransform != null)
        {
            Vector3 pos = _mainCameraTransform.localPosition;
            pos.x = _baseCameraX;
            pos.y = _baseCameraY;
            _mainCameraTransform.localPosition = pos;
        }
    }

    private void ApplyDutchTilt()
    {
        _currentDutch = Mathf.Lerp(_currentDutch, _targetDutch, Time.deltaTime * 10f);

        if (_cinemachineCamera != null)
        {
            var lens = _cinemachineCamera.Lens;
            lens.Dutch = _currentDutch;
            _cinemachineCamera.Lens = lens;
        }
    }

    public bool IsClimbing() => _isClimbing;
    public bool HasReachedBoundary() => _hasReachedBoundary;
    public string GetClimbReleasePrompt() => _hasReachedBoundary ? letGoPrompt : "";
    public void ReleaseLadder()
    {
        if (_hasReachedBoundary)
        {
            _hasReachedBoundary = false;
            OnLadderReleaseAtBoundary?.Invoke();

            if (_climbDirection > 0)
            {
                Vector3 targetPos = transform.position;
                targetPos.y = _climbDismountBoundaryY + 1.6f;
                if (_ladderGrabDirection.magnitude > 0.01f)
                {
                    targetPos += _ladderGrabDirection * 1.2f;
                    Vector3 right = Vector3.Cross(Vector3.up, _ladderGrabDirection).normalized;
                    targetPos += right * 0.4f;
                }
                StartCoroutine(SmoothReleaseToFloor(targetPos, 1.5f));
            }
            else
            {
                StopClimbing();
            }
        }
        else if (_isClimbing)
        {
            StopClimbing();
        }
    }

    private System.Collections.IEnumerator SmoothReleaseToFloor(Vector3 targetPos, float duration)
    {
        _isReleasing = true;
        _currentClimbSpeed = 0;

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            _characterController.enabled = false;
            transform.position = pos;
            _characterController.enabled = true;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _characterController.enabled = false;
        transform.position = targetPos;
        _characterController.enabled = true;

        _isReleasing = false;
        StopClimbing();
    }
}
