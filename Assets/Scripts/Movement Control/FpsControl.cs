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
    private float _climbTopY;
    private float _climbBottomY;
    private int _climbDirection;
    private bool _hasReachedBoundary;
    private bool _isAtEndBoundary;
    private float _climbFloorY;
    private bool _isReleasing;
    private float _climbStartBoundaryY;
    private Vector3 _preGrabPosition;
    private float _climbingStuckFrames;
    private bool _hasPassedMidpoint;

    private CinemachinePanTilt _panTilt;
    private Vector2 _savedPanRange;
    private Vector2 _savedTiltRange;
    private bool _hasSavedCameraRanges;

    public System.Action OnLadderReleaseAtBoundary;
    public System.Action OnPassedMidpoint;
    public System.Action OnClimbEnded;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();

        if (cameraTransform != null)
        {
            _cinemachineCamera = cameraTransform.GetComponent<CinemachineCamera>();
            _panTilt = cameraTransform.GetComponent<CinemachinePanTilt>();
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
                    if (!_isAtEndBoundary)
                    {
                        _hasReachedBoundary = false;
                        OnLadderReleaseAtBoundary?.Invoke();

                        StartCoroutine(SmoothReleaseToFloor(_preGrabPosition, 1.5f, false));
                    }
                    else
                    {
                        ReleaseLadder();
                    }

                    if (climbPromptPanel != null) climbPromptPanel.SetActive(false);
                }
                else
                {
                    float vertical = 0f;
                    if (Input.GetKey(KeyCode.W)) vertical = 1f;
                    else if (Input.GetKey(KeyCode.S)) vertical = -1f;

                    if (vertical != 0)
                    {
                        bool pressingOppositeDirection = _isAtEndBoundary && vertical * _climbDirection < 0;
                        bool willClear = !_isAtEndBoundary || pressingOppositeDirection;
                        if (willClear)
                        {
                            _hasReachedBoundary = false;
                            _isAtEndBoundary = false;
                            _climbDirection = Mathf.RoundToInt(vertical);
                            _currentClimbSpeed = vertical * climbSpeed;
                            _characterController.Move(new Vector3(0, _currentClimbSpeed, 0) * Time.deltaTime);
                        }
                    }
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

    public void ForceStartClimbing(Vector3 targetPosition, Vector3 grabDirection, float topBoundaryY, float bottomBoundaryY, int climbDirection, bool rotateDuringGrab = true)
    {
        if (_isClimbing || _isReleasing) return;
        StartCoroutine(SmoothGrabCoroutine(targetPosition, grabDirection, topBoundaryY, bottomBoundaryY, climbDirection, rotateDuringGrab));
    }

    private System.Collections.IEnumerator SmoothGrabCoroutine(Vector3 targetPos, Vector3 grabDir, float topBoundaryY, float bottomBoundaryY, int dir, bool rotateDuringGrab)
    {
        _isReleasing = true;

        Vector3 startPos = transform.position;
        _preGrabPosition = startPos;
        Quaternion startRot = transform.rotation;

        Quaternion targetRot = startRot;
        if (rotateDuringGrab && grabDir.magnitude > 0.01f)
        {
            Vector3 dirVec = grabDir;
            dirVec.y = 0;
            if (dirVec.magnitude > 0.01f)
                targetRot = Quaternion.LookRotation(dirVec.normalized);
        }

        float duration = dir == 1 ? 3.5f / 1.8f : 3.5f;
        float elapsed = 0f;

        float startPan = 0f;
        float startTilt = 0f;
        CinemachinePanTilt panTilt = null;
        if (cameraTransform != null)
        {
            panTilt = cameraTransform.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
            {
                startPan = panTilt.PanAxis.Value;
                startTilt = panTilt.TiltAxis.Value;
            }
        }

        float startFov = _mainCameraComponent != null ? _mainCameraComponent.fieldOfView : _baseFov;
        float startCamX = _mainCameraTransform != null ? _mainCameraTransform.localPosition.x : _baseCameraX;
        float startCamY = _mainCameraTransform != null ? _mainCameraTransform.localPosition.y : _baseCameraY;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
            _characterController.enabled = false;
            transform.position = pos;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            _characterController.enabled = true;

            if (panTilt != null)
            {
                panTilt.PanAxis.Value = Mathf.Lerp(startPan, 0f, t);
                panTilt.TiltAxis.Value = Mathf.Lerp(startTilt, 0f, t);
            }

            if (_mainCameraComponent != null)
                _mainCameraComponent.fieldOfView = Mathf.Lerp(startFov, _baseFov + 5f, t);

            if (_mainCameraTransform != null)
            {
                Vector3 camPos = _mainCameraTransform.localPosition;
                camPos.x = Mathf.Lerp(startCamX, _baseCameraX, t);
                camPos.y = Mathf.Lerp(startCamY, _baseCameraY, t);
                _mainCameraTransform.localPosition = camPos;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        _characterController.enabled = false;
        transform.position = targetPos;
        transform.rotation = targetRot;
        if (panTilt != null)
        {
            panTilt.PanAxis.Value = 0f;
            panTilt.TiltAxis.Value = 0f;
        }
        _characterController.enabled = true;
        _characterController.Move(Vector3.zero);

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
        _climbTopY = topBoundaryY;
        _climbBottomY = bottomBoundaryY;
        _climbDirection = dir;
        _climbStartBoundaryY = transform.position.y;
        _isAtEndBoundary = false;
        _hasReachedBoundary = true;
        _climbingStuckFrames = 0;
        _hasPassedMidpoint = false;

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

        LockClimbCamera();
    }

    private void LockClimbCamera()
    {
        if (_panTilt == null) return;

        _savedPanRange = _panTilt.PanAxis.Range;
        _savedTiltRange = _panTilt.TiltAxis.Range;
        _hasSavedCameraRanges = true;

        _panTilt.PanAxis.Range = new Vector2(0f, 0f);
        _panTilt.TiltAxis.Range = new Vector2(-90f, 90f);
    }

    private void UnlockClimbCamera()
    {
        if (_panTilt == null || !_hasSavedCameraRanges) return;

        _panTilt.PanAxis.Range = _savedPanRange;
        _panTilt.TiltAxis.Range = _savedTiltRange;
        _hasSavedCameraRanges = false;
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
            _targetDutch = Mathf.Lerp(_targetDutch, 0f, Time.deltaTime * 6f);
            if (_mainCameraTransform != null)
            {
                Vector3 camPos = _mainCameraTransform.localPosition;
                camPos.x = Mathf.Lerp(camPos.x, _baseCameraX, Time.deltaTime * 3f);
                camPos.y = Mathf.Lerp(camPos.y, _baseCameraY, Time.deltaTime * 3f);
                _mainCameraTransform.localPosition = camPos;
                _swingRollOffset = Mathf.Lerp(_swingRollOffset, 0f, Time.deltaTime * 3f);
            }
            return;
        }

        float vertical = 0f;
        if (Input.GetKey(KeyCode.W)) vertical = 1f;
        else if (Input.GetKey(KeyCode.S)) vertical = -1f;

        if (vertical != 0 && Mathf.Abs(_currentClimbSpeed) < 0.001f)
        {
            float minSpeedForMove = 0.001f / Mathf.Max(Time.deltaTime, 0.0001f);
            _currentClimbSpeed = vertical * Mathf.Max(climbSpeed * 0.5f, minSpeedForMove);
        }
        else
            _currentClimbSpeed = Mathf.Lerp(_currentClimbSpeed, vertical * climbSpeed, Time.deltaTime * climbAcceleration);

        Vector3 move = new Vector3(0, _currentClimbSpeed, 0);
        float startY = transform.position.y;
        CollisionFlags flags = _characterController.Move(move * Time.deltaTime);

        float actualDY = transform.position.y - startY;
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
            camPos.x = Mathf.Lerp(camPos.x, _baseCameraX + swingX, Time.deltaTime * 6f);
            camPos.y = Mathf.Lerp(camPos.y, _baseCameraY, Time.deltaTime * 6f);
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
        pos.y = Mathf.Lerp(pos.y, _baseCameraY + bob, Time.deltaTime * 6f);
        _mainCameraTransform.localPosition = pos;

        _targetDutch = _swingRollOffset;

        if (_ladderGrabDirection.magnitude > 0.01f)
        {
            Quaternion face = Quaternion.LookRotation(_ladderGrabDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, face, Time.deltaTime * 5f);
        }

        if ((flags & CollisionFlags.Above) != 0)
        {
            _climbFloorY = transform.position.y + _characterController.height + _characterController.skinWidth * 2f;
            _hasReachedBoundary = true;
            _isAtEndBoundary = true;
            _currentClimbSpeed = 0;
            _targetDutch = 0;
            return;
        }

        if ((flags & CollisionFlags.Below) != 0)
        {
            _hasReachedBoundary = true;
            _isAtEndBoundary = true;
            _currentClimbSpeed = 0;
            _targetDutch = 0;
            return;
        }

        if (!_hasPassedMidpoint)
        {
            float midY = (_climbTopY + _climbBottomY) * 0.5f;
            if (_climbDirection == -1 && transform.position.y <= midY)
            {
                _hasPassedMidpoint = true;
                OnPassedMidpoint?.Invoke();
            }
            else if (_climbDirection == 1 && transform.position.y >= midY)
            {
                _hasPassedMidpoint = true;
                OnPassedMidpoint?.Invoke();
            }
        }

        if (vertical != 0 && Mathf.Abs(actualDY) < 0.0001f)
        {
            _climbingStuckFrames++;
            if (_climbingStuckFrames >= 3)
            {
                _climbingStuckFrames = 0;
                _currentClimbSpeed = 0;
            }
        }
        else
        {
            _climbingStuckFrames = 0;
        }
    }

    private void ResetClimbCamera()
    {
        _swingRollOffset = 0f;
        if (_mainCameraTransform != null)
        {
            Vector3 pos = _mainCameraTransform.localPosition;
            pos.x = _baseCameraX;
            pos.y = _baseCameraY;
            _mainCameraTransform.localPosition = pos;
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
        _isAtEndBoundary = false;

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

        UnlockClimbCamera();
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
    public bool IsMoving() => _moveInput.magnitude > 0.01f;
    public float GetMoveMagnitude() => Mathf.Clamp01(_moveInput.magnitude);
    public bool IsTransitioningToLadder() => _isReleasing;
    public bool HasReachedBoundary() => _hasReachedBoundary;
    public string GetClimbReleasePrompt() => _hasReachedBoundary ? letGoPrompt : "";
    public void ReleaseLadder()
    {
        if (_hasReachedBoundary)
        {
            _hasReachedBoundary = false;
            OnLadderReleaseAtBoundary?.Invoke();

            Vector3 targetPos = transform.position;

            if (_climbDirection == 1)
            {
                if (_climbTopY > 0f)
                    targetPos.y = _climbTopY + 1.7f;
                if (_ladderGrabDirection.magnitude > 0.01f)
                {
                    targetPos += _ladderGrabDirection * 0.5f;
                    Vector3 right = Vector3.Cross(Vector3.up, _ladderGrabDirection).normalized;
                    targetPos += right * 0.3f;
                }
            }
            else
            {
                targetPos.y = transform.position.y;
                if (_ladderGrabDirection.magnitude > 0.01f)
                {
                    targetPos -= _ladderGrabDirection * 0.8f;
                }
            }

            StartCoroutine(SmoothReleaseToFloor(targetPos, 1.5f, false));
        }
        else if (_isClimbing)
        {
            StopClimbing();
        }
    }

    private System.Collections.IEnumerator SmoothReleaseToFloor(Vector3 targetPos, float duration, bool rotateToLadder)
    {
        _isReleasing = true;
        _currentClimbSpeed = 0;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot;
        if (rotateToLadder && _ladderGrabDirection.magnitude > 0.01f)
        {
            Vector3 faceDir = -_ladderGrabDirection;
            faceDir.y = 0;
            if (faceDir.magnitude > 0.01f)
                targetRot = Quaternion.LookRotation(faceDir.normalized);
        }

        float startFov = _mainCameraComponent != null ? _mainCameraComponent.fieldOfView : _baseFov;
        float startCamX = _mainCameraTransform != null ? _mainCameraTransform.localPosition.x : _baseCameraX;
        float startCamY = _mainCameraTransform != null ? _mainCameraTransform.localPosition.y : _baseCameraY;
        float startDutch = _targetDutch;

        _characterController.enabled = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            if (_mainCameraComponent != null)
                _mainCameraComponent.fieldOfView = Mathf.Lerp(startFov, _baseFov, t);
            if (_mainCameraTransform != null)
            {
                Vector3 camPos = _mainCameraTransform.localPosition;
                camPos.x = Mathf.Lerp(startCamX, _baseCameraX, t);
                camPos.y = Mathf.Lerp(startCamY, _baseCameraY, t);
                _mainCameraTransform.localPosition = camPos;
            }
            _targetDutch = Mathf.Lerp(startDutch, 0f, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
        _characterController.enabled = true;
        _characterController.Move(Vector3.zero);
        yield return null;

        if (_mainCameraComponent != null)
            _mainCameraComponent.fieldOfView = _baseFov;
        if (_mainCameraTransform != null)
        {
            Vector3 camPos = _mainCameraTransform.localPosition;
            camPos.x = _baseCameraX;
            camPos.y = _baseCameraY;
            _mainCameraTransform.localPosition = camPos;
        }
        _targetDutch = 0f;

        _isReleasing = false;
        StopClimbing();
        OnClimbEnded?.Invoke();
    }

    public void StartLadderClimb(Vector3 grabPosition, Vector3 grabDirection, float topBoundaryY, float bottomBoundaryY, bool fromTop)
    {
        int climbDirection = fromTop ? -1 : 1;
        ForceStartClimbing(grabPosition, grabDirection, topBoundaryY, bottomBoundaryY, climbDirection, fromTop);
    }
}
