using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] public GameObject GameModeSelectionPanel;
    [SerializeField] public GameObject GameModeAgainstBotPanel;
    [SerializeField] public GameObject GameModeWithFriendsPanel;
    [SerializeField] public GameObject MainMenuPanel;

    public static int SelectedDifficulty { get; set; }

    public static void LoadMainMenu()
    {
        GlobalBallManager.Reset();
        SceneManager.LoadScene("MainMenu");
    }

    public static void LoadGameScene()
    {
        GlobalBallManager.Reset();
        SceneManager.LoadScene("GameScene");
    }

    public static void LoadInventoryScene() =>
        SceneManager.LoadScene("InventoryScene");

    public static void LoadOnlineMatchmaking() =>
        SceneManager.LoadScene("OnlineMatchmaking");

    public static void LoadShopScene() =>
        SceneManager.LoadScene("ShopScene");

    public void OpenBotDifficultyLevelSelectionPanel()
    {
        MainMenuPanel.SetActive(false);
        GameModeSelectionPanel.SetActive(false);
        GameModeAgainstBotPanel.SetActive(true);
    }

    public void OpenGameModeWithFriendsPanel()
    {
        MainMenuPanel.SetActive(false);
        GameModeSelectionPanel.SetActive(false);
        GameModeWithFriendsPanel.SetActive(true);
    }

    public void OpenGameModeSelectionPanel()
    {
        MainMenuPanel.SetActive(false);
        GameModeAgainstBotPanel.SetActive(false);
        GameModeWithFriendsPanel.SetActive(false);
        GameModeSelectionPanel.SetActive(true);
    }   

    public void OpenMainMenuPanel()
    {
        GameModeAgainstBotPanel.SetActive(false);
        GameModeWithFriendsPanel.SetActive(false);
        GameModeSelectionPanel.SetActive(false);
        MainMenuPanel.SetActive(true);
    }

    public void LoadGameAgainstEasyBot()
    {
        SelectedDifficulty = 0;
        LoadGameScene();
    }

    public void LoadGameAgainstMediumBot()
    {
        SelectedDifficulty = 1;
        LoadGameScene();
    }

    public void LoadGameAgainstHardBot()
    {
        SelectedDifficulty = 2;
        LoadGameScene();
    }

    public void LoadLocalMultiplayerGame()
    {
        SelectedDifficulty = -10;
        LoadGameScene();
    }
}