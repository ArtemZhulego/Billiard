using UnityEngine;

public static class GlobalBallManager
{
    public enum BallType { Undefined, Solids, Stripes }

    public const int MaxBalls = 7;

    public static BallType CurrentPlayerType { get; private set; } = BallType.Undefined;
    public static int PlayerScore { get; private set; }
    public static int OpponentScore { get; private set; }
    public static BallType Player1Type { get; private set; } = BallType.Undefined;
    public static BallType Player2Type { get; private set; } = BallType.Undefined;

    public static void Reset()
    {
        CurrentPlayerType = BallType.Undefined;
        PlayerScore = 0;
        OpponentScore = 0;
    }

    public static void SetPlayerTypeForCurrentPlayer(bool isLowGroup, bool isPlayer1)
    {
        if (isPlayer1) Player1Type = isLowGroup ? BallType.Solids : BallType.Stripes;
        else Player2Type = isLowGroup ? BallType.Solids : BallType.Stripes;

        CurrentPlayerType = isLowGroup ? BallType.Solids : BallType.Stripes;
    }

    public static void AddScore(bool isPlayer)
    {
        if (isPlayer) PlayerScore++;
        else OpponentScore++;
    }

    public static bool CheckGameEnd() => PlayerScore >= MaxBalls || OpponentScore >= MaxBalls;
}