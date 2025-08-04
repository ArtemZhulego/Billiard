using UnityEngine;
using System.Collections;
using System.Linq;

public class AIController : MonoBehaviour
{
    [Header("Difficulty Settings")]
    [Range(0, 1)] public float easyAccuracy = 0.5f;
    [Range(0, 1)] public float mediumAccuracy = 0.75f;
    [Range(0, 1)] public float hardAccuracy = 0.9f;

    [Header("Timing Settings")]
    public float minAimingTime = 1f;
    public float maxAimingTime = 3f;
    public float minPowerTime = 0.5f;
    public float maxPowerTime = 2f;

    private Drag dragController;
    private PowerMeter powerMeter;
    public GameManager gameManager;
    private float currentAccuracy;
    private Coroutine aiTurnCoroutine;
    private bool isAITurnActive = false;

    private void Start()
    {
        gameManager = GameManager.Instance;
        dragController = FindObjectOfType<Drag>();
        powerMeter = FindObjectOfType<PowerMeter>();

        if (gameManager is null || dragController is null || powerMeter is null)  return;
        
        switch (SceneLoader.SelectedDifficulty)
        {
            case 0: currentAccuracy = easyAccuracy; break;
            case 1: currentAccuracy = mediumAccuracy; break;
            case 2: currentAccuracy = hardAccuracy; break;
        }
    }

    private void OnEnable()
    {
        if (gameManager is null) gameManager = GameManager.Instance;

        if (gameManager is not null)
        {
            GameManager.OnTurnChanged += HandleTurnChange;
            GameManager.OnBallsStopped += HandleBallsStopped;
        }

        isAITurnActive = false;
        if (aiTurnCoroutine is not null)
        {
            StopCoroutine(aiTurnCoroutine);
            aiTurnCoroutine = null;
        }
    }

    private void OnDisable()
    {
        if (gameManager is not null)
        {
            GameManager.OnTurnChanged -= HandleTurnChange;
            GameManager.OnBallsStopped -= HandleBallsStopped;
        }

        if (aiTurnCoroutine is not null)
        {
            StopCoroutine(aiTurnCoroutine);
            aiTurnCoroutine = null;
        }
        isAITurnActive = false;
    }

    private void HandleTurnChange(bool isPlayer1Turn)
    {
        if (gameManager is null || dragController is null) return;

        if (!isPlayer1Turn && !gameManager.IsAnyBallMoving()) StartAITurn();
    }

    private void HandleBallsStopped()
    {
        if (gameManager is null || dragController is null) return;

        if (!gameManager.IsPlayer1Turn() && !isAITurnActive) StartAITurn();
    }

    private void StartAITurn()
    {
        if (isAITurnActive || dragController.HasSelectedDirection()) return;

        if (aiTurnCoroutine is not null) StopCoroutine(aiTurnCoroutine);

        aiTurnCoroutine = StartCoroutine(AITurnRoutine());
    }

    private IEnumerator AITurnRoutine()
    {
        if (dragController is null || powerMeter is null || gameManager is null) yield break;

        isAITurnActive = true;

        yield return new WaitForSeconds(Random.Range(0.8f, 1.5f));

        GameObject targetBall = FindTargetBall();
        Vector2 targetDirection;

        if (targetBall is not null)
        {
            Vector2 perfectDirection = (targetBall.transform.position - dragController.MainBall.transform.position).normalized;
            float inaccuracy = (1f - currentAccuracy) * 0.2f;
            targetDirection = perfectDirection + Random.insideUnitCircle * inaccuracy;
            targetDirection.Normalize();
        }
        else
        {
            Vector2 tableCenter = new Vector2(0, 0);
            targetDirection = (tableCenter - (Vector2)dragController.MainBall.transform.position).normalized;
        }

        targetDirection = AdjustDirectionToAvoidWalls(targetDirection);

        Vector3 cuePos = dragController.MainBall.transform.position -
                        (Vector3)targetDirection * dragController.cueDistanceFromBall;
        cuePos.z = -1;

        if (dragController.currentCue is null)
        {
            dragController.CreateCue(cuePos, -targetDirection);
        }
        else
        {
            dragController.currentCue.transform.position = cuePos;
            dragController.UpdateCuePosition(-targetDirection);
        }

        dragController.DirLine = -targetDirection;
        dragController.startPoint = dragController.MainBall.transform.position;
        dragController.startPoint.z = 15;

        Vector3 simulatedMouseWorldPos = dragController.MainBall.transform.position + (Vector3)targetDirection * 2f;
        simulatedMouseWorldPos.z = 15;
        dragController.smoothEndPoint = simulatedMouseWorldPos;

        dragController.HandleDirectionSelection();
        dragController.ShowVisualGuides();

        float aimingTime = Random.Range(minAimingTime, maxAimingTime);
        float elapsedTime = 0f;
        Vector2 baseDirection = targetDirection;

        while (elapsedTime < aimingTime)
        {
            elapsedTime += Time.deltaTime;

            float variationFactor = Mathf.Lerp(0.1f, 0.02f, elapsedTime / aimingTime);
            Vector2 currentDirection = baseDirection + Random.insideUnitCircle * variationFactor;
            currentDirection.Normalize();

            dragController.DirLine = -currentDirection;

            simulatedMouseWorldPos = dragController.MainBall.transform.position + (Vector3)currentDirection * 2f;
            simulatedMouseWorldPos.z = 15;
            dragController.smoothEndPoint = simulatedMouseWorldPos;

            cuePos = dragController.MainBall.transform.position -
                   (Vector3)currentDirection * dragController.cueDistanceFromBall;
            cuePos.z = -1;
            dragController.currentCue.transform.position = cuePos;
            dragController.UpdateCuePosition(-currentDirection);

            dragController.HandleDirectionSelection();

            yield return null;
        }

        dragController.directionSelected = true;
        dragController.cueBasePosition = dragController.currentCue.transform.position;
        dragController.powerMeter.gameObject.SetActive(true);

        yield return new WaitForSeconds(Random.Range(0.3f, 0.7f));

        float powerTime = Random.Range(minPowerTime, maxPowerTime);
        elapsedTime = 0f;
        float targetPower = Random.Range(0.7f, 1f); 
        float currentPower = 0f;

        while (elapsedTime < powerTime)
        {
            elapsedTime += Time.deltaTime;
            currentPower = Mathf.Lerp(0, targetPower, elapsedTime / powerTime);

            float easedPower = EaseOutQuad(currentPower);

            dragController.powerMeter.SetPower(easedPower);
            dragController.SetCuePullDistance(easedPower * dragController.maxCuePullDistance);

            yield return null;
        }

        dragController.ApplyStrike(targetPower * dragController.powerMeter.maxPower);

        yield return new WaitUntil(() => !dragController.isStriking);

        if (dragController.rb.linearVelocity.magnitude < 0.1f)
        {
            dragController.rb.AddForce(
                -dragController.DirLine.normalized *
                targetPower * dragController.powerMeter.maxPower,
                ForceMode2D.Impulse);
        }

        if (dragController.currentCue != null)
        {
            Destroy(dragController.currentCue);
            dragController.currentCue = null;
        }

        isAITurnActive = false;
    }

    private float EaseOutQuad(float t)
    {
        return t * (2 - t);
    }

    private Vector2 AdjustDirectionToAvoidWalls(Vector2 direction)
    {
        float rayLength = 5f;
        RaycastHit2D hit = Physics2D.Raycast(
            dragController.MainBall.transform.position,
            direction,
            rayLength,
            LayerMask.GetMask("Walls"));

        if (hit.collider is not null)
        {
            float angle = Vector2.Angle(direction, hit.normal);

            if (angle < 45f)
            {
                Vector2 wallNormal = hit.normal;
                Vector2 newDirection = Vector2.Reflect(direction, wallNormal).normalized;
                newDirection += Random.insideUnitCircle * 0.1f;
                return newDirection.normalized;
            }
        }

        return direction;
    }

    private GameObject FindTargetBall()
    {
        BallController[] balls = FindObjectsOfType<BallController>()
            .Where(b => !b.isPocketed && b.gameObject != dragController.MainBall)
            .ToArray();

        if (balls.Length == 0) return null;

        var sortedBalls = balls.OrderBy(b =>
            Vector2.Distance(dragController.MainBall.transform.position, b.transform.position));

        foreach (var ball in sortedBalls)
        {
            Vector2 direction = (ball.transform.position - dragController.MainBall.transform.position).normalized;
            float distance = Vector2.Distance(dragController.MainBall.transform.position, ball.transform.position);

            RaycastHit2D hit = Physics2D.Raycast(
                (Vector2)dragController.MainBall.transform.position + direction * 0.1f,
                direction,
                distance - 0.1f,
                LayerMask.GetMask("Walls", "Balls"));

            if (hit.collider is null || hit.collider.gameObject == ball.gameObject)
            {
                return ball.gameObject;
            }
        }

        return sortedBalls.FirstOrDefault()?.gameObject;
    }
}