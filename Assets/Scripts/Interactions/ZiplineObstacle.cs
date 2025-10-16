using UnityEngine;

public class ZiplineObstacle : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Speed the obstacle moves along the zipline")]
    [SerializeField] private float moveSpeed = 8f;

    [Header("Collision")]
    [Tooltip("Respawn collider (should be slightly larger than visual)")]
    [SerializeField] private Collider respawnCollider;

    private Vector3 _startPos;
    private Vector3 _endPos;
    private Vector3 _direction;
    private float _length;
    private float _t = 0f;
    private bool _isActive = false;
    private Vector3 _hangOffset;
    private Quaternion _initialRotation;

    public void Initialize(Vector3 startPos, Vector3 endPos, Vector3 hangOffset)
    {
        _startPos = startPos;
        _endPos = endPos;
        _direction = (endPos - startPos).normalized;
        _length = Vector3.Distance(startPos, endPos);
        _t = 0f;
        _isActive = true;
        _hangOffset = hangOffset;

        _initialRotation = transform.rotation;

        if (respawnCollider == null)
        {
            respawnCollider = GetComponentInChildren<Collider>();
        }
    }

    private void Update()
    {
        if (!_isActive) return;

        float path_length = Mathf.Max(0.01f, _length);
        _t += (moveSpeed / path_length) * Time.deltaTime;

        if (_t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 basePos = Vector3.Lerp(_startPos, _endPos, _t);
        Quaternion cableRotation = Quaternion.LookRotation(_direction, Vector3.up);

        Vector3 rotatedOffset = cableRotation * _hangOffset;
        Vector3 finalPosition = basePos + rotatedOffset;

        Quaternion finalRotation = cableRotation * _initialRotation;

        transform.SetPositionAndRotation(finalPosition, finalRotation);
    }
}