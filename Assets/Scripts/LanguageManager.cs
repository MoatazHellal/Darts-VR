using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LanguageManager : MonoBehaviour
{
    [Header("Language Buttons")]
    public Button frenchButton;
    public Button englishButton;

    [Header("Welcome Panel")]
    public TMP_Text welcomeText;
    public TMP_Text welcomeDescription;
    public TMP_Text startButtonText;

    [Header("Hand Selection Panel")]
    public TMP_Text handQuestion;
    public TMP_Text handDescription;
    public TMP_Text leftHandText;
    public TMP_Text rightHandText;
    public TMP_Text handBackText;

    [Header("Mode Selection Panel")]
    public TMP_Text modeQuestion;
    public TMP_Text modeDescription;
    public TMP_Text guidanceText;
    public TMP_Text biofeedbackText;
    public TMP_Text modeBackText;

    private Color activeColor =
        new Color32(98, 212, 223, 255);

    private Color inactiveColor =
        new Color32(34, 51, 68, 255);

    private Color darkTextColor =
        new Color32(13, 20, 35, 255);

    private void Start()
    {
        string language =
            PlayerPrefs.GetString("Language", "FR");

        if (language == "EN")
            SetEnglish();
        else
            SetFrench();
    }

    public void SetFrench()
    {
        SessionSettings.Language = "FR";

        welcomeText.text = "Bienvenue !";

        welcomeDescription.text =
            "Découvrez une expérience immersive de lancer de fléchettes.";

        startButtonText.text = "Commencer";

        handQuestion.text =
            "Quelle est votre main dominante ?";

        handDescription.text =
            "Sélectionnez la main que vous utilisez habituellement pour lancer une fléchette.";

        leftHandText.text = "Gaucher";
        rightHandText.text = "Droitier";
        handBackText.text = "← Retour";

        modeQuestion.text =
            "Choisissez votre mode d'expérience";

        modeDescription.text =
            "Sélectionnez le mode avec lequel vous souhaitez vous entraîner.";

        guidanceText.text = "Guidage";
        biofeedbackText.text = "Biofeedback";
        modeBackText.text = "← Retour";

        UpdateButtonColors(true);

        PlayerPrefs.SetString("Language", "FR");
        PlayerPrefs.Save();

        Debug.Log("Language: " + SessionSettings.Language);
    }

    public void SetEnglish()
    {
        SessionSettings.Language = "EN";

        welcomeText.text = "Welcome!";

        welcomeDescription.text =
            "Discover an immersive dart-throwing experience.";

        startButtonText.text = "Start";

        handQuestion.text =
            "What is your dominant hand?";

        handDescription.text =
            "Select the hand you usually use to throw a dart.";

        leftHandText.text = "Left-handed";
        rightHandText.text = "Right-handed";
        handBackText.text = "← Back";

        modeQuestion.text =
            "Choose your experience mode";

        modeDescription.text =
            "Select the mode you would like to train with.";

        guidanceText.text = "Guidance";
        biofeedbackText.text = "Biofeedback";
        modeBackText.text = "← Back";

        UpdateButtonColors(false);

        PlayerPrefs.SetString("Language", "EN");
        PlayerPrefs.Save();

        Debug.Log("Language: " + SessionSettings.Language);
    }

    private void UpdateButtonColors(bool isFrench)
    {
        frenchButton.image.color =
            isFrench ? activeColor : inactiveColor;

        englishButton.image.color =
            isFrench ? inactiveColor : activeColor;

        frenchButton.GetComponentInChildren<TMP_Text>().color =
            isFrench ? darkTextColor : Color.white;

        englishButton.GetComponentInChildren<TMP_Text>().color =
            isFrench ? Color.white : darkTextColor;
    }
}
