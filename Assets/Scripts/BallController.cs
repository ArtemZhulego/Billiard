using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    public bool isSolid;
    public int ballNumber;
    public bool isPocketed = false;
    public bool IsFalling { get; private set; }

    private Rigidbody2D _rb;
    private CircleCollider2D _col;
    private PhysicsMaterial2D _ballMaterial;
    private Vector3 _initialScale;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CircleCollider2D>();
        _initialScale = transform.localScale;
    }

    public void StartFalling()
    {
        if (IsFalling || isPocketed) return;

        IsFalling = true;
        isPocketed = true;
    }

    public void ResetBall()
    {
        IsFalling = false;
        isPocketed = false;
        transform.localScale = _initialScale;
    }
}