using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Ball3D : MonoBehaviour
{
    [Header("Rolling Settings")]
    [SerializeField] private float ballRadius;
    [SerializeField] private float minSpeedForRotation;
    [SerializeField] private float rotationResponse;
    [SerializeField] private float rotationSmoothing; 

    private Rigidbody2D _rb;
    private Vector3 _lastPosition;
    private Vector3 _targetRotationAxis;
    private float _targetRotationAngle;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _lastPosition = transform.position;
        _targetRotationAxis = Vector3.up; 
    }

    private void FixedUpdate()
    {
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - _lastPosition;
        float speed = _rb.linearVelocity.magnitude;

        if (speed > minSpeedForRotation)
        {
            Vector3 moveDirection = _rb.linearVelocity.normalized;
            _targetRotationAxis = Vector3.Cross(moveDirection, Vector3.forward).normalized;
            _targetRotationAngle = (speed * Time.fixedDeltaTime / (2f * Mathf.PI * ballRadius)) * 360f * rotationResponse;
        }

        else if (speed > 0.01f) _targetRotationAngle *= 0.95f;

        else _targetRotationAngle = 0f; 

        if (_targetRotationAngle > 0.01f)
        {
            Quaternion targetRotation = Quaternion.AngleAxis(_targetRotationAngle, _targetRotationAxis) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.fixedDeltaTime);
        }

        _lastPosition = currentPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision) => _targetRotationAngle *= 1.2f; 
}