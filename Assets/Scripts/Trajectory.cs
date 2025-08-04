using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Trajectory : MonoBehaviour
{
    public LineRenderer lineRenderer;

    private void Awake() => lineRenderer = GetComponent<LineRenderer>();

    public void RenderLine(Vector3 startPoint, Vector3 endPoint)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPositions(new Vector3[] { startPoint, endPoint });
    }

    public void EndLine()
    {
        if (GetComponent<LineRenderer>() is not null)  GetComponent<LineRenderer>().enabled = false;
    }
}