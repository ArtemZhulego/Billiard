using UnityEngine;

public class BallsTrajectory : MonoBehaviour
{
    private GameObject _directionalLine;
    private GameObject _trajectoryLine;
    private GameObject _hitCircle;

    private void Start()
    {
        _directionalLine = GameObject.FindGameObjectWithTag("DirectionLine");
        _trajectoryLine = GameObject.FindGameObjectWithTag("TrajectoryLine");
        _hitCircle = GameObject.FindWithTag("Hit");
    }
}