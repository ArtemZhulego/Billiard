using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private GameObject[] _player1ScoreDisplays;
    [SerializeField] private GameObject[] _player2ScoreDisplays;

    private Material[] _player1BallMaterials = new Material[7];
    private Material[] _player2BallMaterials = new Material[7];
    private int _player1Index;
    private int _player2Index;

    public void AddBallToScore(bool isPlayer1, Material material)
    {
        GameObject[] targetDisplays = isPlayer1 ? _player1ScoreDisplays : _player2ScoreDisplays;
        int index = isPlayer1 ? _player1Index : _player2Index;

        if (index >= targetDisplays.Length) return;

        targetDisplays[index].SetActive(true);

        var renderer = targetDisplays[index].GetComponent<Renderer>();
        if (renderer is not null && material is not null) renderer.material = material;

        if (isPlayer1) _player1Index++;
        else _player2Index++;
    }

    private void AddPlayer1Ball(Material material)
    {
        if (_player1Index >= 7) return;
        _player1BallMaterials[_player1Index] = material;
        UpdateDisplay(_player1ScoreDisplays, _player1BallMaterials, _player1Index++);
    }

    private void AddPlayer2Ball(Material material)
    {
        if (_player2Index >= 7) return;
        _player2BallMaterials[_player2Index] = material;
        UpdateDisplay(_player2ScoreDisplays, _player2BallMaterials, _player2Index++);
    }

    private void UpdateDisplay(GameObject[] displays, Material[] materials, int index)
    {
        if (index >= displays.Length || displays[index] is null) return;
        displays[index].SetActive(true);
        if (displays[index].TryGetComponent(out MeshRenderer renderer) && materials[index] is not null)
            renderer.material = materials[index];
    }

    public void ResetScore()
    {
        _player1Index = _player2Index = 0;
        foreach (var display in _player1ScoreDisplays) if (display is not null) display.SetActive(false);
        foreach (var display in _player2ScoreDisplays) if (display is not null) display.SetActive(false);
    }
}