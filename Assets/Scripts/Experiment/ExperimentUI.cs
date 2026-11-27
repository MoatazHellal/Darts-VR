
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentUI : MonoBehaviour
{
    [Header("Manager Reference")]
    [SerializeField] private ExperimentManager experimentManager;

    [Header("Main Panels")]
    [SerializeField] private GameObject experimentPanel;
    [SerializeField] private GameObject progressHUD;

    [Header("Experiment Panel Texts")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text phaseTitleText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text timerText;

    [Header("Progress HUD")]
    [SerializeField] private TMP_Text hudConditionText;
    [SerializeField] private TMP_Text trialCounterText;
    [SerializeField] private Image progressBarFill;

    private bool IsFrench =>
        SessionSettings.Language == "FR";

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Start()
    {
        if (experimentManager == null)
        {
            experimentManager = ExperimentManager.Instance;
        }

        if (experimentManager == null ||
            experimentPanel == null ||
            progressHUD == null ||
            headerText == null ||
            phaseTitleText == null ||
            conditionText == null ||
            instructionText == null ||
            timerText == null ||
            hudConditionText == null ||
            trialCounterText == null ||
            progressBarFill == null)
        {
            Debug.LogError(
                "ExperimentUI: Missing references."
            );

            enabled = false;
            return;
        }

        RefreshUI();
    }

    private void LateUpdate()
    {
        RefreshUI();
    }

    // ==========================================
    // MAIN UI UPDATE
    // ==========================================

    private void RefreshUI()
    {
        headerText.text = Translate(
            "EXPÉRIMENTATION",
            "EXPERIMENT"
        );

        switch (experimentManager.CurrentPhase)
        {
            case ExperimentPhase.WaitingForHands:
                ShowWaitingForHands();
                break;

            case ExperimentPhase.Preparation:
                ShowPreparation();
                break;

            case ExperimentPhase.FirstCondition:
            case ExperimentPhase.SecondCondition:
                ShowActiveCondition();
                break;

            case ExperimentPhase.Break:
                ShowBreak();
                break;

            case ExperimentPhase.Completed:
                ShowCompleted();
                break;

            case ExperimentPhase.Error:
                ShowError();
                break;
        }
    }

    // ==========================================
    // WAITING FOR HAND TRACKING
    // ==========================================

    private void ShowWaitingForHands()
    {
        ShowMainPanel();

        phaseTitleText.text = Translate(
            "Détection des mains",
            "Hand detection"
        );

        conditionText.text = Translate(
            "Main dominante : ",
            "Dominant hand: "
        ) + GetHandName();

        instructionText.text = Translate(
            "Placez votre main dominante devant vous "
            + "et attendez sa détection.",
            "Place your dominant hand in front of you "
            + "and wait for tracking."
        );

        timerText.gameObject.SetActive(false);
    }

    // ==========================================
    // PREPARATION
    // ==========================================

    private void ShowPreparation()
    {
        ShowMainPanel();

        phaseTitleText.text = Translate(
            "Préparation",
            "Preparation"
        );

        conditionText.text = Translate(
            "Méthode : ",
            "Method: "
        ) + GetConditionName(
            SessionSettings.FirstCondition
        );

        int total = experimentManager.TrialsPerCondition;

        instructionText.text = Translate(
            $"Préparez-vous à effectuer {total} lancers "
            + "de fléchettes en visant le centre de la cible.",
            $"Get ready to perform {total} dart throws "
            + "while aiming at the center of the target."
        );

        timerText.gameObject.SetActive(true);

        timerText.text = FormatTime(
            experimentManager.RemainingTime
        );
    }

    // ==========================================
    // ACTIVE EXPERIMENTAL CONDITION
    // ==========================================

    private void ShowActiveCondition()
    {
        experimentPanel.SetActive(false);
        progressHUD.SetActive(true);

        string condition =
            experimentManager.CurrentCondition;

        hudConditionText.text =
            GetConditionName(condition).ToUpperInvariant();

        int total =
            experimentManager.TrialsPerCondition;

        int completed = Mathf.Clamp(
            experimentManager.CompletedTrials,
            0,
            total
        );

        int currentTrial = Mathf.Min(
            completed + 1,
            total
        );

        trialCounterText.text = Translate(
            $"Essai {currentTrial} / {total}",
            $"Trial {currentTrial} / {total}"
        );

        progressBarFill.fillAmount =
            total > 0
                ? (float)completed / total
                : 0f;
    }

    // ==========================================
    // BREAK BETWEEN CONDITIONS
    // ==========================================

    private void ShowBreak()
    {
        ShowMainPanel();

        phaseTitleText.text = Translate(
            "Pause",
            "Break"
        );

        conditionText.text = Translate(
            "Prochaine méthode : ",
            "Next method: "
        ) + GetConditionName(
            SessionSettings.SecondCondition
        );

        float remaining =
            experimentManager.RemainingTime;

        if (remaining > 0f)
        {
            instructionText.text = Translate(
                "Reposez-vous avant de commencer "
                + "la prochaine méthode.",
                "Rest before starting "
                + "the next condition."
            );

            timerText.gameObject.SetActive(true);
            timerText.text = FormatTime(remaining);
        }
        else
        {
            instructionText.text = Translate(
                "La pause est terminée. Placez votre "
                + "main dominante devant vous.",
                "The break is over. Place your "
                + "dominant hand in front of you."
            );

            timerText.gameObject.SetActive(false);
        }
    }

    // ==========================================
    // EXPERIMENT COMPLETED
    // ==========================================

    private void ShowCompleted()
    {
        ShowMainPanel();

        phaseTitleText.text = Translate(
            "Expérience terminée",
            "Experiment completed"
        );

        int completed =
            experimentManager.GlobalCompletedTrials;

        int total =
            experimentManager.TotalRequiredTrials;

        conditionText.text = Translate(
            $"Essais terminés : {completed} / {total}",
            $"Completed trials: {completed} / {total}"
        );

        instructionText.text = Translate(
            "Merci pour votre participation ! "
            + "Veuillez attendre les instructions "
            + "de l'expérimentateur.",
            "Thank you for participating! "
            + "Please wait for instructions "
            + "from the experimenter."
        );

        timerText.gameObject.SetActive(false);
    }

    // ==========================================
    // ERROR
    // ==========================================

    private void ShowError()
    {
        ShowMainPanel();

        phaseTitleText.text = Translate(
            "Erreur",
            "Error"
        );

        conditionText.text = Translate(
            "Expérience interrompue",
            "Experiment interrupted"
        );

        instructionText.text = Translate(
            "Une erreur est survenue. Veuillez "
            + "contacter l'expérimentateur.",
            "An error occurred. Please "
            + "contact the experimenter."
        );

        timerText.gameObject.SetActive(false);
    }

    // ==========================================
    // PANEL MANAGEMENT
    // ==========================================

    private void ShowMainPanel()
    {
        experimentPanel.SetActive(true);
        progressHUD.SetActive(false);
    }

    // ==========================================
    // LANGUAGE HELPERS
    // ==========================================

    private string Translate(string french, string english)
    {
        return IsFrench ? french : english;
    }

    private string GetConditionName(string condition)
    {
        switch (condition)
        {
            case "Guidance":
                return Translate("Guidage", "Guidance");

            case "Biofeedback":
                return "Biofeedback";

            default:
                return condition ?? "";
        }
    }

    private string GetHandName()
    {
        return SessionSettings.DominantHand == "Left"
            ? Translate("Gauche", "Left")
            : Translate("Droite", "Right");
    }

    // ==========================================
    // TIMER FORMATTING
    // ==========================================

    private string FormatTime(float remaining)
    {
        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, remaining)
        );

        int minutes = seconds / 60;
        int remainingSeconds = seconds % 60;

        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
