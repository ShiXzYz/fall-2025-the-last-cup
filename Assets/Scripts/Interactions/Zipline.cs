using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class Zipline : MonoBehaviour, IInteractable
{
    [Header("Endpoints & visuals")]
    [SerializeField] public Zipline targetZip;
    [SerializeField] private LineRenderer cable;
    [Tooltip("Start anchor of this zipline")]
    public Transform zipTransform;

    [Header("Parallel Ziplines")]
    [Tooltip("The zipline parallel to this one on the left side")]
    [SerializeField] public Zipline leftParallelZipline;
    [Tooltip("The zipline parallel to this one on the right side")]
    [SerializeField] public Zipline rightParallelZipline;
    [Tooltip("Cooldown time between zipline switches in seconds")]
    [SerializeField] private float switchCooldown = 0.5f;
    [Tooltip("Time window for double-tap detection in seconds")]
    [SerializeField] private float doubleTapWindow = 0.3f;
    [Tooltip("Duration of the smooth transition when switching ziplines")]
    [SerializeField] private float switchTransitionDuration = 0.4f;
    [Tooltip("How much to swing in the opposite direction before switching (wind-up)")]
    [SerializeField] private float switchWindupAmount = 0.8f;
    private float _lastSwitchTime = -999f;
    private bool _isSwitching = false;
    private float _switchTransitionProgress = 0f;
    private float _switchDirection = 0f;

    [Header("Dodge Mechanics")]
    [Tooltip("Enable dodge/swing mechanic while on zipline")]
    [SerializeField] private bool enableDodge = true;
    [Tooltip("Maximum horizontal swing distance from the cable")]
    [SerializeField] private float maxDodgeDistance = 1.5f;
    [Tooltip("How quickly the player swings left/right")]
    [SerializeField] private float dodgeSpeed = 3f;
    [Tooltip("How quickly the player returns to center when not dodging")]
    [SerializeField] private float dodgeReturnSpeed = 2f;
    [Tooltip("Rotation angle when fully dodged to the side")]
    [SerializeField] private float dodgeRotationAngle = 15f;
    [Tooltip("Smoothing applied to dodge rotation changes")]
    [SerializeField] private float dodgeSmoothTime = 0.1f;
    private float _currentDodgeOffset = 0f;
    private float _targetDodgeOffset = 0f;
    private float _dodgeVelocity = 0f;

    [Header("Motion (parametric, no physics)")]
    [Tooltip("Meters per second along the cable")]
    [SerializeField] private float zipSpeed = 10f;

    [Tooltip("Local offset while hanging relative to cable-forward/world-up frame. Use negative Y to hang below.")]
    [SerializeField] private Vector3 hangLocalOffset = new Vector3(0f, -1.0f, 0f);

    [Tooltip("Forward nudge (meters) on exit to avoid clipping the end anchor")]
    [SerializeField] private float exitForward = 0.6f;

    [Tooltip("Downward nudge (meters) on exit so the CharacterController re-ground checks cleanly")]
    [SerializeField] private float exitDown = 0.5f;

    [Tooltip("Rotate the rider to face along the cable")]
    [SerializeField] private bool faceAlongCable = true;

    [Header("Camera while zipping")]
    [Tooltip("If true, we override the camera to always be behind & under the rider along the cable.")]
    [SerializeField] private bool overrideCameraOnZip = true;

    [Tooltip("Camera local offset in cable space while zipping. Z negative = behind the rider, Y negative = below.")]
    [SerializeField] private Vector3 camLocalOffset = new Vector3(0f, -1.2f, -3.0f);

    [Tooltip("Extra upward tilt (degrees) so more of the player is visible")]
    [SerializeField] private float camLookUpPitch = 10f;

    [Tooltip("Override FOV while zipping (<= 0 means no override)")]
    [SerializeField] private float ziplineFOV = 65f;

    [Tooltip("How quickly the camera follows the target pose")]
    [SerializeField] private float camFollowLerp = 12f;

    [Header("Runtime (read-only)")]
    public bool zipping = false;

    [Header("AnimationManager")]
    public AnimationManager animationManager;

    [Header("Obstacle Detection")]
    [Tooltip("Distance to check ahead for obstacles")]
    [SerializeField] private float obstacleCheckDistance = 0.5f;
    [Tooltip("Radius for obstacle detection sphere cast")]
    [SerializeField] private float obstacleCheckRadius = 0.5f;
    [SerializeField] private LayerMask obstacleLayer = -1; // Set to "Default" layer in Inspector


    private Vector3 _startPos, _endPos, _dir;
    private float _length, _t;

    private GameObject _rider;
    // Good to handle when the player gets off the zipline, providing smooth fall rather than a snap to the ground
    private ThirdPersonController _riderTPC; 
    private bool _tpcPrevEnabled;

    private Camera _cam;
    private Behaviour _cinemachineBrain;
    private bool _cinemachineWasEnabled;
    private float _origFOV;
    private bool _hadOrigFOV;

    private InputAction _moveAction;

    private float _lastLeftTapTime = -999f;
    private float _lastRightTapTime = -999f;
    private bool _leftWasPressed = false;
    private bool _rightWasPressed = false;

    private void Awake()
    {
        if (cable && zipTransform && targetZip && targetZip.zipTransform)
        {
            cable.positionCount = 2;
            cable.SetPosition(0, zipTransform.position);
            cable.SetPosition(1, targetZip.zipTransform.position);
        }
    }

    public bool CanInteract() =>
        !zipping && zipTransform && targetZip && targetZip.zipTransform;

    public bool Interact(Interactor interactor)
    {
        if (!CanInteract()) return false;
        if (!Input.GetKeyDown(KeyCode.Q)) return false;

        GameObject player = interactor ? interactor.GetComponent<Interactor>()?.gameObject : null;
        if (interactor != null && interactor.GetType() == typeof(Interactor))
            player = interactor.GetComponent<Interactor>()?.player ?? player;

        if (player == null) player = interactor?.gameObject;

        StartZipline(player);
        return true;
    }

    private void Update()
    {
        if (!zipping || _rider == null) return;

        animationManager.Zipline(true);

        float unitsPer01 = Mathf.Max(0.01f, _length);
        _t = Mathf.Min(1f, _t + (zipSpeed / unitsPer01) * Time.deltaTime);

        if (!_isSwitching)
        {
            HandleDodgeAndSwitchInput();
        }

        Vector3 basePos = Vector3.LerpUnclamped(_startPos, _endPos, _t);
        Quaternion cableRot = Quaternion.LookRotation(_dir, Vector3.up);

        if (_isSwitching)
        {
            _switchTransitionProgress += Time.deltaTime / switchTransitionDuration;

            float arcOffset = CalculateSwitchArcOffset(_switchTransitionProgress, _switchDirection);
            _currentDodgeOffset = arcOffset;

            if (_switchTransitionProgress >= 1f)
            {
                _isSwitching = false;
                _currentDodgeOffset = 0f;
                _targetDodgeOffset = 0f;
                _dodgeVelocity = 0f;
            }
        }
        else
        {
            _currentDodgeOffset = Mathf.SmoothDamp(_currentDodgeOffset, _targetDodgeOffset, ref _dodgeVelocity, dodgeSmoothTime);
        }

        Quaternion finalRotation = cableRot;
        if (faceAlongCable)
        {
            float dodgeRotation = (_currentDodgeOffset / maxDodgeDistance) * dodgeRotationAngle;
            finalRotation = cableRot * Quaternion.Euler(0f, 0f, -dodgeRotation);
        }

        Vector3 rotatedHangOffset = finalRotation * hangLocalOffset;
        Vector3 finalPosition = basePos + rotatedHangOffset;

        _rider.transform.SetPositionAndRotation(finalPosition, finalRotation);
        Physics.SyncTransforms();

        // Check for obstacles ahead
        CheckForObstaclesAhead(finalPosition);

        if (overrideCameraOnZip)
            DriveCamera(basePos, cableRot);

        if (_t >= 1f)
        {
            animationManager.Zipline(false);
            ResetZipline();
        }
    }

    private void CheckForObstaclesAhead(Vector3 currentPos)
    {
        RaycastHit[] hits = Physics.SphereCastAll(
            currentPos,
            obstacleCheckRadius,
            _dir,
            obstacleCheckDistance,
            obstacleLayer
        );

        foreach (RaycastHit hit in hits)
        {
            RespawnScript respawn = hit.collider.GetComponentInChildren<RespawnScript>();
            if (respawn != null && respawn.player != null && respawn.respawnPoint != null)
            {
                CharacterController controller = respawn.player.GetComponent<CharacterController>();
                if (controller != null)
                {
                    animationManager.Zipline(false);
                    ResetZipline();  // ← MOVE THIS BEFORE RESPAWN

                    controller.enabled = false;
                    respawn.player.transform.position = respawn.respawnPoint.transform.position;
                    controller.enabled = true;

                    return;
                }
            }
        }
    }

    private float CalculateSwitchArcOffset(float progress, float direction)
    {
        float windupPhase = 0.3f;

        if (progress < windupPhase)
        {
            float windupProgress = progress / windupPhase;
            float eased = EaseOutQuad(windupProgress);
            return -direction * switchWindupAmount * maxDodgeDistance * eased;
        }
        else
        {
            float swingPhase = (progress - windupPhase) / (1f - windupPhase);
            float eased = EaseInOutCubic(swingPhase);

            float startOffset = -direction * switchWindupAmount * maxDodgeDistance;
            float endOffset = 0f;

            return Mathf.Lerp(startOffset, endOffset, eased);
        }
    }

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private float EaseInOutCubic(float t)
    {
        return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
    }

    private void HandleDodgeAndSwitchInput()
    {
        bool leftPressed = false;
        bool rightPressed = false;
        float horizontalAxis = 0f;

        if (_moveAction != null && _moveAction.enabled)
        {
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            horizontalAxis = moveInput.x;

            leftPressed = moveInput.x < -0.5f && !_leftWasPressed;
            rightPressed = moveInput.x > 0.5f && !_rightWasPressed;

            _leftWasPressed = moveInput.x < -0.5f;
            _rightWasPressed = moveInput.x > 0.5f;
        }
        else
        {
            leftPressed = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
            rightPressed = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);

            horizontalAxis = Input.GetAxisRaw("Horizontal");
        }

        if (leftPressed)
        {
            if (Time.time - _lastLeftTapTime <= doubleTapWindow)
            {
                TrySwitchToParallelZipline(leftParallelZipline, -1f);
                _lastLeftTapTime = -999f;
            }
            else
            {
                _lastLeftTapTime = Time.time;
            }
        }

        if (rightPressed)
        {
            if (Time.time - _lastRightTapTime <= doubleTapWindow)
            {
                TrySwitchToParallelZipline(rightParallelZipline, 1f);
                _lastRightTapTime = -999f;
            }
            else
            {
                _lastRightTapTime = Time.time;
            }
        }

        if (enableDodge)
        {
            if (Mathf.Abs(horizontalAxis) > 0.1f)
            {
                _targetDodgeOffset -= horizontalAxis * dodgeSpeed * Time.deltaTime;
                _targetDodgeOffset = Mathf.Clamp(_targetDodgeOffset, -maxDodgeDistance, maxDodgeDistance);
            }
            else
            {
                _targetDodgeOffset = Mathf.MoveTowards(_targetDodgeOffset, 0f, dodgeReturnSpeed * Time.deltaTime);
            }
        }
    }

    private void TrySwitchToParallelZipline(Zipline targetParallelZip, float switchDirection)
    {
        if (targetParallelZip == null || targetParallelZip.zipping)
            return;

        if (Time.time - _lastSwitchTime < switchCooldown)
            return;

        Vector3 currentPos = Vector3.LerpUnclamped(_startPos, _endPos, _t);

        Vector3 newStartPos = targetParallelZip.zipTransform.position;
        Vector3 newEndPos = targetParallelZip.targetZip.zipTransform.position;
        Vector3 newDir = (newEndPos - newStartPos).normalized;

        Vector3 toCurrentPos = currentPos - newStartPos;
        float projectedT = Vector3.Dot(toCurrentPos, newDir) / Vector3.Distance(newStartPos, newEndPos);

        Vector3 closestPointOnNewZipline = Vector3.LerpUnclamped(newStartPos, newEndPos, projectedT);
        float distance = Vector3.Distance(currentPos, closestPointOnNewZipline);

        if (projectedT >= 0f && projectedT <= 1f)
        {
            ResetZiplineState();

            targetParallelZip.StartZiplineAtProgress(_rider, projectedT, switchDirection);

            _lastSwitchTime = Time.time;
        }
    }

    public void StartZiplineAtProgress(GameObject player, float progress, float switchDirection = 0f)
    {
        if (zipping || player == null || zipTransform == null || targetZip == null || targetZip.zipTransform == null)
            return;

        var equipMgr = player.GetComponentInChildren<EquipmentManager>();
        if (equipMgr == null || equipMgr.CurrentType != EquipmentType.BucketHandle)
        {
            Debug.LogWarning("[Zipline] Requires Bucket Handle equipped.");
            return;
        }

        _rider = player;
        _riderTPC = _rider.GetComponent<ThirdPersonController>();

        if (_riderTPC != null)
        {
            _tpcPrevEnabled = _riderTPC.enabled;
            _riderTPC.enabled = false;
        }

        _startPos = zipTransform.position;
        _endPos = targetZip.zipTransform.position;
        _dir = (_endPos - _startPos).normalized;
        _length = Vector3.Distance(_startPos, _endPos);
        _t = Mathf.Clamp01(progress);

        _currentDodgeOffset = 0f;
        _targetDodgeOffset = 0f;
        _dodgeVelocity = 0f;

        if (Mathf.Abs(switchDirection) > 0.01f)
        {
            _isSwitching = true;
            _switchTransitionProgress = 0f;
            _switchDirection = switchDirection;
        }
        else
        {
            _isSwitching = false;
        }

        zipping = true;

        SetupInputActions();

        if (overrideCameraOnZip)
            SetupCameraOverride();
    }

    public void StartZipline(GameObject player)
    {
        StartZiplineAtProgress(player, 0f, 0f);
    }

    private void SetupInputActions()
    {
        var playerInput = _rider?.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            _moveAction = playerInput.actions.FindAction("Move");
        }

        _lastLeftTapTime = -999f;
        _lastRightTapTime = -999f;
        _leftWasPressed = false;
        _rightWasPressed = false;
    }

    private void ResetZiplineState()
    {
        if (_rider != null)
        {
            if (_riderTPC != null) _riderTPC.enabled = _tpcPrevEnabled;
        }

        if (overrideCameraOnZip)
            TeardownCameraOverride();

        _moveAction = null;
        _currentDodgeOffset = 0f;
        _targetDodgeOffset = 0f;
        _dodgeVelocity = 0f;
        _lastLeftTapTime = -999f;
        _lastRightTapTime = -999f;
        _leftWasPressed = false;
        _rightWasPressed = false;
        _isSwitching = false;

        zipping = false;
    }

    private void ResetZipline()
    {
        if (!zipping) return;

        if (_rider != null)
        {
            Vector3 exitPos = _endPos + _dir * Mathf.Max(0f, exitForward) + Vector3.down * Mathf.Max(0f, exitDown);
            Quaternion exitRot = Quaternion.Euler(0, 0, 0);

            _rider.transform.SetPositionAndRotation(exitPos, exitRot);
        }

        ResetZiplineState();

        _rider = null;
        _riderTPC = null;
    }

    private void SetupCameraOverride()
    {
        _cam = Camera.main;
        if (_cam == null) return;

        var brain = _cam.GetComponent("CinemachineBrain") as Behaviour;
        if (brain != null)
        {
            _cinemachineBrain = brain;
            _cinemachineWasEnabled = brain.enabled;
            brain.enabled = false;
        }

        if (ziplineFOV > 0f)
        {
            _origFOV = _cam.fieldOfView;
            _hadOrigFOV = true;
            _cam.fieldOfView = ziplineFOV;
        }
        else
        {
            _hadOrigFOV = false;
        }
    }

    private void TeardownCameraOverride()
    {
        if (_cam != null && _hadOrigFOV)
            _cam.fieldOfView = _origFOV;

        if (_cinemachineBrain != null)
        {
            _cinemachineBrain.enabled = _cinemachineWasEnabled;
            _cinemachineBrain = null;
        }

        _cam = null;
        _hadOrigFOV = false;
    }

    private void DriveCamera(Vector3 basePosOnCable, Quaternion cableRotation)
    {
        if (_cam == null) return;

        Vector3 desiredPos = basePosOnCable + (cableRotation * camLocalOffset);
        Vector3 lookTarget = _rider != null ? _rider.transform.position : (basePosOnCable + cableRotation * Vector3.forward);
        Quaternion desiredRot = Quaternion.LookRotation((lookTarget - desiredPos).normalized, Vector3.up)
                                * Quaternion.Euler(camLookUpPitch, 0f, 0f);

        _cam.transform.position = Vector3.Lerp(_cam.transform.position, desiredPos, 1f - Mathf.Exp(-camFollowLerp * Time.deltaTime));
        _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, desiredRot, 1f - Mathf.Exp(-camFollowLerp * Time.deltaTime));
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !zipping) return;

        Gizmos.color = Color.yellow;
        if (leftParallelZipline != null)
        {
            Vector3 currentPos = Vector3.LerpUnclamped(_startPos, _endPos, _t);
            Vector3 leftStart = leftParallelZipline.zipTransform.position;
            Gizmos.DrawLine(currentPos, leftStart);
        }

        Gizmos.color = Color.cyan;
        if (rightParallelZipline != null)
        {
            Vector3 currentPos = Vector3.LerpUnclamped(_startPos, _endPos, _t);
            Vector3 rightStart = rightParallelZipline.zipTransform.position;
            Gizmos.DrawLine(currentPos, rightStart);
        }
    }
}