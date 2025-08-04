using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
public class Pockets : MonoBehaviour
{
    private const string PocketTag = "Pocket";

    [Header("References")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private Transform mainBallRespawnPoint;

    [Header("Rendering")]
    [SerializeField] private int normalSortingLayer = 0;
    [SerializeField] private int pocketSortingLayer = 1;
    [SerializeField] private int insidePocketSortingLayer = -1;

    [Header("Pocket Physics")]
    [SerializeField] private float minBounceTime = 0.8f;
    [SerializeField] private float maxBounceTime = 1.5f;
    [SerializeField] private float containForce = 20f;
    [SerializeField] private float fastScaleDuration = 0.2f;
    [SerializeField] private float instantScaleThreshold = 0.5f;
    [SerializeField] private float eightBallDelay = 0.5f;

    [Header("Fall Animation")]
    [SerializeField] private float fallShrinkDuration = 0.5f;
    [SerializeField] private float randomBounceForce = 2f;
    [SerializeField] private Ease shrinkEase = Ease.InBack;

    private BallController _ballController;
    private MeshRenderer _meshRenderer;
    private Rigidbody2D _rb;
    private Vector2 _pocketCenter;
    private CircleCollider2D _collider;
    private bool _isEightBall;
    private bool _isProcessingPocket;

    private void Awake()
    {
        _ballController = GetComponent<BallController>();
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<CircleCollider2D>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag(PocketTag)) return;
        if (_isProcessingPocket) return;
        if (_ballController.isPocketed) return;

        _isProcessingPocket = true;
        _ballController.isPocketed = true;
        _pocketCenter = collision.transform.position;

        if (_ballController.ballNumber == 0) ProcessMainBallInPocket();
        else StartCoroutine(ProcessBallInPocket());
    }

    private void ProcessMainBallInPocket()
    {
        _meshRenderer.sortingOrder = insidePocketSortingLayer;

        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        transform.DOJump(mainBallRespawnPoint.position, 0.3f, 1, 0.5f)
            .OnComplete(() => {
                _meshRenderer.sortingOrder = normalSortingLayer;
                _isProcessingPocket = false;
                _ballController.isPocketed = false;
            });

        GameManager.Instance.HandleFoul();
        GameManager.Instance.SwitchTurn();
    }

    private IEnumerator ProcessBallInPocket()
    {
        _meshRenderer.sortingOrder = insidePocketSortingLayer;

        float initialSpeed = _rb.linearVelocity.magnitude;
        float speedFactor = Mathf.InverseLerp(0f, 15f, initialSpeed); 

        float bounceIntensity = Mathf.Lerp(0.1f, 2f, speedFactor); 
        float attractionForce = Mathf.Lerp(200f, 300f, speedFactor); 
        float animationDuration = Mathf.Lerp(0.3f, 1.2f, speedFactor); 

        _rb.linearDamping = Mathf.Lerp(1f, 3f, speedFactor);
        _rb.angularDamping = Mathf.Lerp(1f, 3f, speedFactor);

        transform.DOScale(Vector3.zero, animationDuration)
            .SetEase(shrinkEase);

        float timer = 0f;
        Vector2 pocketCenter = _pocketCenter;
        float maxDistance = 0.13f; 

        while (timer < animationDuration)
        {
            Vector2 toCenter = pocketCenter - (Vector2)transform.position;
            float distance = toCenter.magnitude;

            if (distance > maxDistance)
            {
                float forceMultiplier = Mathf.Pow(distance * 10f, 2f);
                _rb.AddForce(toCenter.normalized * attractionForce * forceMultiplier * Time.deltaTime);

                if (distance > maxDistance * 2f)
                {
                    _rb.linearVelocity *= 0.5f;
                    transform.position = Vector2.Lerp(transform.position, pocketCenter, 0.3f);
                }
            }

            if (distance < maxDistance && Random.value > 0.9f - speedFactor * 0.2f) 
                _rb.AddForce(Random.insideUnitCircle * bounceIntensity, ForceMode2D.Impulse);
            
            timer += Time.deltaTime;
            yield return null;
        }

        ProcessBallInPocketFinal();
        if (!_isEightBall) Destroy(gameObject);
    }

    private void ProcessBallInPocketFinal()
    {
        int ballNumber = _ballController.ballNumber;
        bool isPlayer1Turn = GameManager.Instance.IsPlayer1Turn();

        if (GlobalBallManager.CurrentPlayerType == GlobalBallManager.BallType.Undefined && ballNumber != 8)
        {
            bool isLowGroup = ballNumber <= 7;
            GlobalBallManager.SetPlayerTypeForCurrentPlayer(isLowGroup, isPlayer1Turn);
        }

        bool isLowGroupBall = ballNumber <= 7;
        bool isPlayer1Ball = GlobalBallManager.Player1Type == GlobalBallManager.BallType.Solids
            ? isLowGroupBall
            : !isLowGroupBall;

        bool isCurrentPlayerBall = (isPlayer1Ball && isPlayer1Turn) || (!isPlayer1Ball && !isPlayer1Turn);

        if (ballNumber == 8)
        {
            _isEightBall = true;
            bool allMyBallsPocketed = CheckAllBallsPocketed(isPlayer1Turn);
            StartCoroutine(HandleEightBall());
        }
        else
        {
            if (!isCurrentPlayerBall)
            {
                scoreManager?.AddBallToScore(!isPlayer1Turn, GetComponent<MeshRenderer>()?.material);
                GameManager.Instance.HandleFoul();
                GameManager.Instance.SwitchTurn();
            }
            else
            {
                scoreManager?.AddBallToScore(isPlayer1Turn, GetComponent<MeshRenderer>()?.material);
                GameManager.Instance.RegisterPocketedBall(true);
            }
        }
    }

    private IEnumerator HandleEightBall()
    {
        yield return new WaitForSeconds(fastScaleDuration + eightBallDelay);

        DOTween.KillAll();
        SceneManager.LoadScene("MainMenu");
    }

    private bool CheckAllBallsPocketed(bool isPlayer1Turn)
    {
        var balls = FindObjectsOfType<BallController>();
        foreach (var ball in balls)
        {
            if (ball.ballNumber == 8 || ball.isPocketed) continue;

            bool isLowGroup = ball.ballNumber <= 7;
            bool isPlayer1Ball = GlobalBallManager.Player1Type == GlobalBallManager.BallType.Solids
                ? isLowGroup
                : !isLowGroup;

            if ((isPlayer1Ball && isPlayer1Turn) || (!isPlayer1Ball && !isPlayer1Turn))
                return false;
        }
        return true;
    }
}