using UnityEngine;
using System.Linq;
using TMPro;
using System;
using System.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI turnText;
    public Image playerTurnIndicator;
    public Image opponentTurnIndicator;
    public Color activeTurnColor = Color.green;
    public Color inactiveTurnColor = Color.white;

    private bool _isPlayer1Turn = true;
    private bool _gameEnded = false;
    private bool _hasPlayerScoredThisTurn = false;
    private Coroutine _turnCheckCoroutine;
    private AIController _aiController;

    public static event Action<bool> OnTurnChanged;
    public static event Action OnBallsStopped;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateTurnUI();
        InitializeAIController();
    }

    private void InitializeAIController()
    {
        _aiController = FindObjectOfType<AIController>(true);
        if (_aiController is not null) UpdateAIControllerState();
    }

    public void UpdateAIControllerState()
    {
        if (_aiController is not null) _aiController.gameObject.SetActive(IsAgainstBot());
    }

    public void StartTurnCheck()
    {
        if (_turnCheckCoroutine is not null) StopCoroutine(_turnCheckCoroutine);

        _turnCheckCoroutine = StartCoroutine(TurnCheckRoutine());
    }

    private IEnumerator TurnCheckRoutine()
    {
        yield return new WaitUntil(() => !IsAnyBallMoving());
        yield return new WaitForSeconds(0.5f);

        if (!_hasPlayerScoredThisTurn) SwitchTurn();
        
        _hasPlayerScoredThisTurn = false;

        OnBallsStopped?.Invoke();
    }

    public void SwitchTurn()
    {
        _isPlayer1Turn = !_isPlayer1Turn;
        _hasPlayerScoredThisTurn = false;
        UpdateTurnUI();
        OnTurnChanged?.Invoke(_isPlayer1Turn);
    }

    private void UpdateTurnUI()
    {
        turnText.text = _isPlayer1Turn ? "Player 1 Turn" : "Player 2 Turn";
        playerTurnIndicator.color = _isPlayer1Turn ? activeTurnColor : inactiveTurnColor;
        opponentTurnIndicator.color = !_isPlayer1Turn ? activeTurnColor : inactiveTurnColor;
    }

    public bool IsAnyBallMoving()
    {
        var balls = FindObjectsOfType<Rigidbody2D>();
        bool anyMoving = balls.Any(rb => rb is not null && rb.linearVelocity.magnitude > 0.05f);
        return anyMoving;
    }

    public void HandleFoul()
    {
        _hasPlayerScoredThisTurn = false;
        SwitchTurn();
    }

    public bool IsPlayer1Turn() => _isPlayer1Turn;
    public bool IsGameEnded() => _gameEnded;
    public bool IsAgainstBot() => SceneLoader.SelectedDifficulty >= 0 && SceneLoader.SelectedDifficulty <= 2;    

    public void RegisterPocketedBall(bool isCurrentPlayerBall) =>_hasPlayerScoredThisTurn = isCurrentPlayerBall;
}