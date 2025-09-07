using StarterAssets;
using UnityEngine;

public class Zipline : MonoBehaviour, IInteractable
{
    [Header("Endpoints & visuals")]
    [SerializeField] private Zipline targetZip;
    [SerializeField] private LineRenderer cable;
    [Tooltip("Start anchor of this zipline")]
    public Transform zipTransform;

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

    // Internals
    private Vector3 _startPos, _endPos, _dir;
    private float _length, _t;

    // Rider state
    private GameObject _rider;
    private CharacterController _riderCC;
    private ThirdPersonController _riderTPC;
    private BasicRigidBodyPush _riderPush;
    private bool _pushPrev;
    private bool _tpcPrevEnabled;

    // Camera state
    private Camera _cam;
    private Behaviour _cinemachineBrain;
    private bool _cinemachineWasEnabled;
    private float _origFOV;
    private bool _hadOrigFOV;

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

        Vector3 basePos = Vector3.LerpUnclamped(_startPos, _endPos, _t);
        Quaternion rot = Quaternion.LookRotation(_dir, Vector3.up);
        Vector3 worldOffset = rot * hangLocalOffset;

        _rider.transform.SetPositionAndRotation(
            basePos + worldOffset,
            faceAlongCable ? rot : _rider.transform.rotation
        );

        if (overrideCameraOnZip)
            DriveCamera(basePos, rot);

        if (_t >= 1f)
        {
            animationManager.Zipline(false);
            ResetZipline();
        }
    }

    public void StartZipline(GameObject player)
    {
        if (zipping || player == null || zipTransform == null || targetZip == null || targetZip.zipTransform == null)
            return;

        // Require bucket handle
        var equipMgr = player.GetComponentInChildren<EquipmentManager>();
        if (equipMgr == null || equipMgr.CurrentType != EquipmentType.BucketHandle)
        {
            Debug.LogWarning("[Zipline] Requires Bucket Handle equipped.");
            return;
        }

        _rider = player;
        _riderCC = _rider.GetComponent<CharacterController>();
        _riderTPC = _rider.GetComponent<ThirdPersonController>();
        _riderPush = _rider.GetComponent<BasicRigidBodyPush>();

        if (_riderTPC != null)
        {
            _tpcPrevEnabled = _riderTPC.enabled;
            _riderTPC.enabled = false; // stops input-driven rotation and movement
        }
        if (_riderPush != null)
        {
            _pushPrev = _riderPush.canPush;
            _riderPush.canPush = false;
        }
        if (_riderCC != null)
        {
            _riderCC.enabled = false;
        }

        _startPos = zipTransform.position;
        _endPos = targetZip.zipTransform.position;
        _dir = (_endPos - _startPos).normalized;
        _length = Vector3.Distance(_startPos, _endPos);
        _t = 0f;

        zipping = true;

        if (overrideCameraOnZip)
            SetupCameraOverride();
    }

    private void ResetZipline()
    {
        if (!zipping) return;

        if (_rider != null)
        {
            Vector3 exitPos = _endPos + _dir * Mathf.Max(0f, exitForward) + Vector3.down * Mathf.Max(0f, exitDown);
            Quaternion exitRot = Quaternion.Euler(0, 0, 0);

            _rider.transform.SetPositionAndRotation(exitPos, exitRot);

            if (_riderCC != null) _riderCC.enabled = true;
            if (_riderTPC != null) _riderTPC.enabled = _tpcPrevEnabled;
            if (_riderPush != null) _riderPush.canPush = _pushPrev;
        }

        if (overrideCameraOnZip)
            TeardownCameraOverride();

        _rider = null;
        _riderCC = null;
        _riderTPC = null;
        _riderPush = null;

        zipping = false;
    }

    private void SetupCameraOverride()
    {
        _cam = Camera.main;
        if (_cam == null) return;

        // If Cinemachine is present, temporarily disable the Brain so we control Camera.main directly.
        // We don't reference the Cinemachine types to keep compilation clean even if the package isn't installed.
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

        // Desired pose: behind & below the rider, aligned to cable forward
        Vector3 desiredPos = basePosOnCable + (cableRotation * camLocalOffset);
        Vector3 lookTarget = _rider != null ? _rider.transform.position : (basePosOnCable + cableRotation * Vector3.forward);
        Quaternion desiredRot = Quaternion.LookRotation((lookTarget - desiredPos).normalized, Vector3.up)
                                * Quaternion.Euler(camLookUpPitch, 0f, 0f);

        // Smooth follow for nicer feel
        _cam.transform.position = Vector3.Lerp(_cam.transform.position, desiredPos, 1f - Mathf.Exp(-camFollowLerp * Time.deltaTime));
        _cam.transform.rotation = Quaternion.Slerp(_cam.transform.rotation, desiredRot, 1f - Mathf.Exp(-camFollowLerp * Time.deltaTime));
    }
}
