using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public class Drag : MonoBehaviour
{
    [Header("Physics Settings")]
    public float power = 10f;
    public Vector2 minimumPower = Vector2.zero;
    public Vector2 maximumPower = Vector2.one;

    [Header("References")]
    public Rigidbody2D rb;
    public GameObject MainBall;
    public Trajectory trLine;
    public PowerMeter powerMeter;

    [Header("Visual Guides")]
    public GameObject DirectionalLine;
    public GameObject TrajectoryLine;
    public GameObject HittedBall;
    public LayerMask ballLayer;

    [Header("Cue Settings")]
    public GameObject cuePrefab;
    public float cueDistanceFromBall = 0.7f;
    public float maxCuePullDistance = 2.0f;
    public Vector2 cueSize = new Vector2(1.5f, 0.3f);
    public float strikeAnimationSpeed = 15f;

    [Header("Line Settings")]
    public float maxDirectionLineLength = 1.5f;
    public float minDirectionLineLength = 0.3f;
    public float maxTrajectoryLineLength = 1.0f;
    public float minTrajectoryLineLength = 0.2f;

    [Header("Power Meter Settings")]
    public bool allowDirectionChange = true;
    public bool directionSelected = false;
    public float currentPullDistance = 0f;
    public Vector3 cueBasePosition;

    public Vector3 startPoint;
    public Vector3 endPoint;
    public Vector2 DirLine { get; set; } = new Vector2();
    public Vector2 CurPos = new Vector2();
    public Vector3 startPos;
    public Vector3 strikeStartPos;
    public Vector3 smoothEndPoint;
    public Vector3 smoothEndPointVelocity = Vector3.zero;
    public Vector3 smoothTrajectoryStart;
    public Vector3 smoothTrajectoryStartVelocity = Vector3.zero;

    public GameObject currentCue;

    public bool IsChangingDirection = false;
    public bool isStriking = false;
    public float smoothTime = 0.1f;

    private Camera _cam;
    private Vector2 _force; private Vector3 strikeTargetPos;
    private Vector2 _predictedHitBallDirection = Vector2.zero;

    private Rigidbody2D _ballWithAppliedTrajectory = null;
    private float _predictedHitBallForce = 0f;
    private float _strikeProgress = 0f;
    private bool _hasAppliedTrajectoryToHitBall = false;

    void Start()
    {
        _cam = Camera.main;
        trLine = GetComponent<Trajectory>();
        HideVisualGuides();
        powerMeter.gameObject.SetActive(false);

        smoothEndPoint = MainBall.transform.position;
        smoothTrajectoryStart = MainBall.transform.position;
    }

    void Update()
    {
        if (GameManager.Instance.IsAgainstBot() && !GameManager.Instance.IsPlayer1Turn())
        {
            return;
        }
        if (GameManager.Instance.IsAnyBallMoving() ||
            GameManager.Instance.IsGameEnded() ||
            AnyBallsPocketedThisTurn())
        {
            if (currentCue != null)
            {
                Destroy(currentCue);
                currentCue = null;
            }
            HideVisualGuides();
            directionSelected = false;
            powerMeter.gameObject.SetActive(false);
            return;
        }

        if (IsPointerOverTag("UI")) return;

        startPoint = MainBall.transform.position;
        startPoint.z = 15;

        if (Input.GetMouseButtonDown(0) && !isStriking)
        {
            if (directionSelected)
            {
                directionSelected = false;
                powerMeter.gameObject.SetActive(false);
                return;
            }
        }

        if (!directionSelected && Input.GetMouseButton(0) && !isStriking)
        {
            HandleDirectionSelection();
        }
        else if (Input.GetMouseButtonUp(0) && !directionSelected && !isStriking && currentCue != null)
        {
            directionSelected = true;
            powerMeter.gameObject.SetActive(true);
            cueBasePosition = currentCue.transform.position;
        }

        if (directionSelected && currentCue != null)
        {
            UpdateCueVisualPosition();
        }

        if (isStriking)
        {
            HandleStrikeAnimation();
        }
    }

    public void HandleDirectionSelection()
    {
        UpdateMousePosition();
        UpdateCuePositionAndRotation();
        HandleRaycastCollision();
    }

    private void UpdateMousePosition()
    {
        Vector3 currentPoint = _cam.ScreenToWorldPoint(Input.mousePosition);
        currentPoint.z = 15;
        smoothEndPoint = Vector3.SmoothDamp(smoothEndPoint, currentPoint, ref smoothEndPointVelocity, smoothTime);
        DirLine = new Vector2(smoothEndPoint.x - MainBall.transform.position.x,
                             smoothEndPoint.y - MainBall.transform.position.y).normalized;
        startPos = new Vector3(transform.position.x - DirLine.normalized.x * 0.5f,
                             transform.position.y - DirLine.normalized.y * 0.5f, -1);
    }

    private void UpdateCuePositionAndRotation()
    {
        if (currentCue == null)
        {
            CreateNewCue();
        }
        else
        {
            UpdateExistingCuePosition();
        }
    }

    private void CreateNewCue()
    {
        Vector3 cuePosition = MainBall.transform.position + (Vector3)DirLine.normalized * cueDistanceFromBall;
        cuePosition.z = -1;
        CreateCue(cuePosition, DirLine.normalized);
    }

    private void UpdateExistingCuePosition()
    {
        Vector3 cuePosition = MainBall.transform.position + (Vector3)DirLine.normalized * cueDistanceFromBall;
        cuePosition.z = -1;
        currentCue.transform.position = cuePosition;
        UpdateCuePosition(DirLine.normalized);
    }

    private void HandleRaycastCollision()
    {
        RaycastHit2D hit = Physics2D.Raycast(startPos, -DirLine.normalized);
        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Ball"))
            {
                HandleBallHitVisuals(hit);
            }
            else
            {
                HandleWallHitVisuals(hit);
            }
            ShowVisualGuides();
        }
    }

    private void HandleBallHitVisuals(RaycastHit2D hit)
    {
        CalculateTrajectories(hit, out Vector2 hittingTrajectory, out Vector2 bounceTrajectory);
        UpdateVisualElements(hit, hittingTrajectory, bounceTrajectory);
        StorePredictedDirection(hittingTrajectory);
    }

    private void CalculateTrajectories(RaycastHit2D hit, out Vector2 hittingTrajectory, out Vector2 bounceTrajectory)
    {
        hittingTrajectory = (new Vector2(hit.collider.transform.position.x, hit.collider.transform.position.y) - hit.point).normalized;
        bounceTrajectory = Vector2.Perpendicular(hittingTrajectory);

        float angle = Vector2.Angle(-DirLine.normalized, hittingTrajectory);
        float angleFactor = Mathf.Clamp01(angle / 80f);

        if ((-hittingTrajectory.x > 0 && -hittingTrajectory.y > 0) || (-hittingTrajectory.x < 0 && -hittingTrajectory.y < 0))
            bounceTrajectory = -bounceTrajectory;
    }

    private void UpdateVisualElements(RaycastHit2D hit, Vector2 hittingTrajectory, Vector2 bounceTrajectory)
    {
        float angle = Vector2.Angle(-DirLine.normalized, hittingTrajectory);
        float angleFactor = Mathf.Clamp01(angle / 80f);

        float directionLineLength = Mathf.Lerp(minDirectionLineLength, maxDirectionLineLength, angleFactor);
        float trajectoryLineLength = Mathf.Lerp(maxTrajectoryLineLength, minTrajectoryLineLength, angleFactor);

        UpdateDirectionLine(hit, directionLineLength, bounceTrajectory);
        UpdateTrajectoryLine(hit, hittingTrajectory, trajectoryLineLength);
        UpdateHitBallPosition(hit, directionLineLength);
    }

    private void UpdateDirectionLine(RaycastHit2D hit, float length, Vector2 bounceTrajectory)
    {
        Vector3 endPos = new Vector3(
            hit.point.x + DirLine.normalized.x * length * 0.15f,
            hit.point.y + DirLine.normalized.y * length * 0.15f,
            -1
        );

        Vector3 bouncePosition = new Vector3(
            endPos.x + bounceTrajectory.x * length * 0.5f,
            endPos.y + bounceTrajectory.y * length * 0.5f,
            -1
        );

        DirectionalLine.GetComponent<LineRenderer>().SetPosition(0, startPos);
        DirectionalLine.GetComponent<LineRenderer>().SetPosition(1, endPos);
        DirectionalLine.GetComponent<LineRenderer>().SetPosition(2, bouncePosition);
    }

    private void UpdateTrajectoryLine(RaycastHit2D hit, Vector2 hittingTrajectory, float length)
    {
        smoothTrajectoryStart = Vector3.SmoothDamp(
            smoothTrajectoryStart,
            hit.collider.transform.position,
            ref smoothTrajectoryStartVelocity,
            smoothTime
        );
        smoothTrajectoryStart.z = -1;

        Vector3 trajectoryPosition = new Vector3(
            smoothTrajectoryStart.x + hittingTrajectory.x * length,
            smoothTrajectoryStart.y + hittingTrajectory.y * length,
            -1
        );

        TrajectoryLine.GetComponent<LineRenderer>().enabled = true;
        TrajectoryLine.GetComponent<LineRenderer>().SetPosition(0, smoothTrajectoryStart);
        TrajectoryLine.GetComponent<LineRenderer>().SetPosition(1, trajectoryPosition);
    }

    private void UpdateHitBallPosition(RaycastHit2D hit, float length)
    {
        Vector3 endPos = new Vector3(
            hit.point.x + DirLine.normalized.x * length * 0.15f,
            hit.point.y + DirLine.normalized.y * length * 0.15f,
            -1
        );
        HittedBall.transform.position = endPos;
    }

    private void StorePredictedDirection(Vector2 hittingTrajectory)
    {
        _predictedHitBallDirection = hittingTrajectory;
    }

    public void UpdateAITrajectory(Vector2 direction)
    {
        startPos = MainBall.transform.position;
        startPos.z = -1;

        RaycastHit2D hit = PerformAITrajectoryRaycast(direction);
        if (hit.collider != null)
        {
            ProcessAITrajectoryHit(hit, direction);
        }
    }

    private RaycastHit2D PerformAITrajectoryRaycast(Vector2 direction)
    {
        return Physics2D.Raycast(startPos, direction, 10f);
    }

    private void ProcessAITrajectoryHit(RaycastHit2D hit, Vector2 direction)
    {
        Vector3 endPos = hit.point;
        endPos.z = -1;

        UpdateAIDirectionalLine(startPos, endPos);
        UpdateAIHitBallPosition(endPos);

        if (hit.collider.CompareTag("Ball"))
        {
            HandleAIBallCollision(hit);
        }
        else
        {
            HandleAIWallCollision(hit, direction, endPos);
        }

        ShowVisualGuides();
    }

    private void UpdateAIDirectionalLine(Vector3 start, Vector3 end)
    {
        DirectionalLine.GetComponent<LineRenderer>().SetPosition(0, start);
        DirectionalLine.GetComponent<LineRenderer>().SetPosition(1, end);
    }

    private void UpdateAIHitBallPosition(Vector3 position)
    {
        HittedBall.transform.position = position;
    }

    private void HandleAIBallCollision(RaycastHit2D hit)
    {
        Vector2 hitDir = ((Vector2)hit.collider.transform.position - hit.point).normalized;
        Vector3 trajectoryEnd = hit.collider.transform.position + (Vector3)hitDir * 1f;
        trajectoryEnd.z = -1;

        TrajectoryLine.GetComponent<LineRenderer>().SetPosition(0, hit.collider.transform.position);
        TrajectoryLine.GetComponent<LineRenderer>().SetPosition(1, trajectoryEnd);
        TrajectoryLine.GetComponent<LineRenderer>().enabled = true;
    }

    private void HandleAIWallCollision(RaycastHit2D hit, Vector2 direction, Vector3 endPos)
    {
        Vector2 reflectDir = Vector2.Reflect(direction, hit.normal);
        Vector3 bounceEnd = endPos + (Vector3)reflectDir.normalized * 1f;
        bounceEnd.z = -1;

        DirectionalLine.GetComponent<LineRenderer>().SetPosition(2, bounceEnd);
        TrajectoryLine.GetComponent<LineRenderer>().enabled = false;
    }

    public void UpdateCueVisualPosition()
    {
        if (currentCue == null) return;

        Vector3 targetPosition = cueBasePosition + (Vector3)DirLine.normalized * currentPullDistance;
        targetPosition.z = -1;
        currentCue.transform.position = targetPosition;
    }

    public void ApplyStrike(float power)
    {
        _hasAppliedTrajectoryToHitBall = false;
        _ballWithAppliedTrajectory = null;
        _force = -DirLine.normalized * power;
        _predictedHitBallForce = power;

        currentPullDistance = 0f;
        UpdateCueVisualPosition();

        HideVisualGuides();
        trLine.EndLine();
        powerMeter.gameObject.SetActive(false);

        strikeTargetPos = MainBall.transform.position - (Vector3)DirLine.normalized * 0.3f;
        strikeTargetPos.z = -1;
        strikeStartPos = currentCue.transform.position;
        _strikeProgress = 0f;
        isStriking = true;
    }

    private void HandleWallHitVisuals(RaycastHit2D hit)
    {
        Vector2 hitNormal = hit.normal;
        Vector2 reflectDirection = Vector2.Reflect(-DirLine.normalized, hitNormal).normalized;

        float directionLineLength = Mathf.Lerp(minDirectionLineLength, maxDirectionLineLength, currentPullDistance / maxCuePullDistance);

        Vector3 endPos = hit.point;
        Vector3 bounceEndPos = endPos + (Vector3)reflectDirection * directionLineLength;

        TrajectoryLine.GetComponent<LineRenderer>().enabled = false;

        DirectionalLine.GetComponent<LineRenderer>().SetPosition(0, startPos);
        DirectionalLine.GetComponent<LineRenderer>().SetPosition(1, endPos);
        DirectionalLine.GetComponent<LineRenderer>().SetPosition(2, bounceEndPos);
        HittedBall.transform.position = endPos;

        _predictedHitBallDirection = Vector2.zero;
    }

    private void HandleStrikeAnimation()
    {
        _strikeProgress += Time.deltaTime * strikeAnimationSpeed;
        _strikeProgress = Mathf.Clamp01(_strikeProgress);

        currentCue.transform.position = Vector3.Lerp(strikeStartPos, strikeTargetPos, _strikeProgress);

        if (_strikeProgress >= 1f)
        {
            rb.AddForce(_force * power, ForceMode2D.Impulse);
            Destroy(currentCue);
            currentCue = null;
            isStriking = false;
            directionSelected = false;

            GameManager.Instance.StartTurnCheck();
        }
    }

    public bool HasSelectedDirection()
    {
        return directionSelected && !isStriking;
    }

    public void SetCuePullDistance(float distance)
    {
        currentPullDistance = distance;
        UpdateCueVisualPosition();
    }

    private bool AnyBallsPocketedThisTurn()
    {
        return FindObjectsOfType<BallController>().Any(b => b.isPocketed);
    }

    public void CreateCue(Vector3 position, Vector2 direction)
    {
        currentCue = Instantiate(cuePrefab, position, Quaternion.identity);
        currentCue.SetActive(true);

        SpriteRenderer cueSprite = currentCue.GetComponent<SpriteRenderer>();
        if (cueSprite != null)
            cueSprite.size = cueSize;
        else
            currentCue.transform.localScale = new Vector3(cueSize.x, cueSize.y, 1f);

        UpdateCuePosition(direction);
    }

    public void UpdateCuePosition(Vector2 direction)
    {
        if (currentCue == null) return;

        Vector2 normalizedDirection = direction.normalized;
        float targetAngle = Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg + 180f;
        currentCue.transform.rotation = Quaternion.AngleAxis(targetAngle, Vector3.forward);
    }

    public void ShowVisualGuides()
    {
        DirectionalLine.GetComponent<LineRenderer>().enabled = true;
        HittedBall.GetComponent<SpriteRenderer>().enabled = true;
    }

    private void HideVisualGuides()
    {
        DirectionalLine.GetComponent<LineRenderer>().enabled = false;
        HittedBall.GetComponent<SpriteRenderer>().enabled = false;
        TrajectoryLine.GetComponent<LineRenderer>().enabled = false;
    }

    private bool IsPointerOverTag(string tag)
    {
        var pointerEvent = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEvent, results);
        return results.Exists(r => r.gameObject.CompareTag(tag));
    }

    public void ResetCuePosition()
    {
        if (currentCue != null && directionSelected)
        {
            Vector3 cuePosition = MainBall.transform.position + (Vector3)DirLine.normalized * cueDistanceFromBall;
            cuePosition.z = -1;
            currentCue.transform.position = cuePosition;
            cueBasePosition = cuePosition;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_hasAppliedTrajectoryToHitBall) return;

        if (collision.gameObject.CompareTag("Ball"))
        {
            Rigidbody2D otherRb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (otherRb != null)
            {
                if (_ballWithAppliedTrajectory != null && _ballWithAppliedTrajectory != otherRb)
                {
                    return;
                }

                if (_predictedHitBallDirection != Vector2.zero)
                {
                    float currentSpeed = otherRb.linearVelocity.magnitude;
                    float newSpeed = Mathf.Max(currentSpeed, _predictedHitBallForce * 0.5f);
                    otherRb.linearVelocity = _predictedHitBallDirection.normalized * newSpeed;
                }
                else
                {
                    Vector2 incomingDir = rb.linearVelocity.normalized;
                    Vector2 predictedDir = Vector2.Reflect(incomingDir, collision.contacts[0].normal).normalized;
                    float forceMag = rb.linearVelocity.magnitude;
                    otherRb.linearVelocity = predictedDir * forceMag;
                }

                _hasAppliedTrajectoryToHitBall = true;
                _ballWithAppliedTrajectory = otherRb;
            }
        }
    }
}