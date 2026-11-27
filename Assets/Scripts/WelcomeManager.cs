
using UnityEngine;
using UnityEngine.SceneManagement;

public class WelcomeManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject panelWelcome;
    public GameObject panelHandSelection;
    public GameObject panelModeSelection;

    private bool isLoading = false;

    // User choices
    public static string DominantHand =>
        SessionSettings.DominantHand;

    public static string ExperienceMode =>
        SessionSettings.ExperienceMode;

    private void Start()
    {
        ShowWelcome();
    }

    // Screen 1: Welcome
    public void ShowWelcome()
    {
        panelWelcome.SetActive(true);
        panelHandSelection.SetActive(false);
        panelModeSelection.SetActive(false);
    }

    // Screen 2: Dominant hand selection
    public void ShowHandSelection()
    {
        panelWelcome.SetActive(false);
        panelHandSelection.SetActive(true);
        panelModeSelection.SetActive(false);
    }

    // Legacy mode selection panel
    public void ShowModeSelection()
    {
        panelWelcome.SetActive(false);
        panelHandSelection.SetActive(false);
        panelModeSelection.SetActive(true);
    }

    public void SelectLeftHand()
    {
        SessionSettings.DominantHand = "Left";

        Debug.Log("Dominant hand: Left");

        LoadExperienceScene();
    }

    public void SelectRightHand()
    {
        SessionSettings.DominantHand = "Right";

        Debug.Log("Dominant hand: Right");

        LoadExperienceScene();
    }

    // Legacy button callbacks
    public void SelectGuidance()
    {
        LoadExperienceScene();
    }

    public void SelectBiofeedback()
    {
        LoadExperienceScene();
    }

    // Initialize and load the experimental scene
    private void LoadExperienceScene()
    {
        // Prevent multiple session initializations
        if (isLoading)
            return;

        isLoading = true;

        // Automatic participant ID and counterbalancing
        SessionSettings.InitializeSession();

        Debug.Log("Loading NewScene...");

        SceneManager.LoadScene("NewScene");
    }
}
