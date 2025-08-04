using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SimpleRotate : MonoBehaviour
{
    [Header("Rolling Settings")]
    [SerializeField] private float ballRadius;
    [SerializeField] private float minRollSpeed;
    [SerializeField] private float velocityToRotationRatio;

    private Rigidbody2D _rb;
    private Vector3 _lastPosition;
    private float _currentRollAngle;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _lastPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (_rb.linearVelocity.magnitude > 0.1f)
        {
            float targetAngularSpeed = -_rb.linearVelocity.x * velocityToRotationRatio;
            _rb.angularVelocity = targetAngularSpeed;

            Vector3 deltaPos = transform.position - _lastPosition;
            if (deltaPos.magnitude > 0.001f)
            {
                float circumference = 2f * Mathf.PI * ballRadius;
                float angle = (deltaPos.magnitude / circumference) * 360f;
                Vector3 axis = Vector3.Cross(deltaPos.normalized, Vector3.forward);
                transform.Rotate(axis, angle, Space.World);
            }
        }
        _lastPosition = transform.position;
    }
}