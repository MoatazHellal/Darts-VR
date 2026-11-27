
using System;
using System.IO;
using System.Text;
using System.Globalization;
using UnityEngine;

public static class CSVExporter
{
    private static readonly CultureInfo Culture =
        CultureInfo.InvariantCulture;

    public static string CurrentFilePath { get; private set; }

    private static readonly string[] Headers =
    {
        "participant_id",
        "session_id",
        "is_test_mode",
        "dominant_hand",
        "language",

        "condition_group",
        "condition_order",
        "condition_index",
        "condition",

        "attempt_number",
        "trial_number",
        "global_trial_number",
        "is_counted",

        "elapsed_time_s",

        "release_position_x_m",
        "release_position_y_m",
        "release_position_z_m",

        "release_velocity_x_ms",
        "release_velocity_y_ms",
        "release_velocity_z_ms",
        "release_speed_ms",

        "release_direction_x",
        "release_direction_y",
        "release_direction_z",

        "vertical_tilt_deg",
        "horizontal_tilt_deg",

        "impact_position_x_m",
        "impact_position_y_m",
        "impact_position_z_m",

        "impact_distance_cm",
        "score",
        "zone",

        "trial_status",
        "failure_reason",

        "release_timestamp_utc",
        "completion_timestamp_utc"
    };

    // Create a CSV file for the current session
    public static bool InitializeSession()
    {
        CurrentFilePath = null;

        try
        {
            if (string.IsNullOrEmpty(
                    SessionSettings.ParticipantId) ||
                string.IsNullOrEmpty(
                    SessionSettings.SessionId))
            {
                Debug.LogError(
                    "Session is not initialized."
                );

                return false;
            }

            string folder = Path.Combine(
                Application.persistentDataPath,
                "ExperimentData",
                SessionSettings.IsTestMode
                    ? "Test"
                    : "Participants"
            );

            Directory.CreateDirectory(folder);

            string fileName =
                $"{SessionSettings.ParticipantId}_" +
                $"{SessionSettings.SessionId}.csv";

            string filePath =
                Path.Combine(folder, fileName);

            string expectedHeader =
                string.Join(",", Headers);

            // Create a new CSV file
            if (!File.Exists(filePath))
            {
                File.WriteAllText(
                    filePath,
                    expectedHeader + "\n",
                    new UTF8Encoding(false)
                );
            }
            else
            {
                // Verify the existing CSV structure
                using (var reader =
                    new StreamReader(filePath))
                {
                    string existingHeader =
                        reader.ReadLine();

                    if (existingHeader != expectedHeader)
                    {
                        Debug.LogError(
                            "CSV header mismatch. " +
                            "A new session is required."
                        );

                        return false;
                    }

                    // Prevent duplicate trial numbering
                    // when restarting an existing session
                    if (!reader.EndOfStream)
                    {
                        Debug.LogError(
                            "CSV already contains trials. " +
                            "Start a new session."
                        );

                        return false;
                    }
                }
            }

            CurrentFilePath = filePath;

            Debug.Log(
                "CSV initialized: " + CurrentFilePath
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "CSV initialization failed: " +
                exception.Message
            );

            return false;
        }
    }

    // Append one completed trial
    public static bool ExportTrial(TrialData trial)
    {
        if (trial == null)
        {
            Debug.LogError("Trial data is null.");
            return false;
        }

        if (trial.Status == TrialStatus.Pending)
        {
            Debug.LogError(
                "Cannot export a pending trial."
            );

            return false;
        }

        if (string.IsNullOrEmpty(CurrentFilePath) ||
            !File.Exists(CurrentFilePath))
        {
            Debug.LogError(
                "CSV file is not initialized."
            );

            return false;
        }

        if (trial.SessionId !=
                SessionSettings.SessionId ||
            trial.ParticipantId !=
                SessionSettings.ParticipantId ||
            trial.IsTestMode !=
                SessionSettings.IsTestMode)
        {
            Debug.LogError("Trial session mismatch.");
            return false;
        }

        try
        {
            string[] values =
            {
                Escape(trial.ParticipantId),
                Escape(trial.SessionId),
                Format(trial.IsTestMode),
                Escape(trial.DominantHand),
                Escape(trial.Language),

                Escape(trial.ConditionGroup),
                Escape(trial.ConditionOrder),
                Format(trial.ConditionIndex),
                Escape(trial.Condition),

                Format(trial.AttemptNumber),
                Format(trial.TrialNumber),
                Format(trial.GlobalTrialNumber),
                Format(trial.IsCounted),

                Format(trial.ElapsedTime),

                Format(trial.ReleasePosition?.x),
                Format(trial.ReleasePosition?.y),
                Format(trial.ReleasePosition?.z),

                Format(trial.ReleaseVelocity?.x),
                Format(trial.ReleaseVelocity?.y),
                Format(trial.ReleaseVelocity?.z),
                Format(trial.ReleaseSpeed),

                Format(trial.ReleaseDirection?.x),
                Format(trial.ReleaseDirection?.y),
                Format(trial.ReleaseDirection?.z),

                Format(trial.VerticalTilt),
                Format(trial.HorizontalTilt),

                Format(trial.ImpactPosition?.x),
                Format(trial.ImpactPosition?.y),
                Format(trial.ImpactPosition?.z),

                Format(trial.ImpactDistanceCm),
                Format(trial.Score),
                Escape(trial.Zone),

                Escape(trial.Status.ToString()),
                Escape(trial.FailureReason),

                Escape(trial.ReleaseTimestampUtc),
                Escape(trial.CompletionTimestampUtc)
            };

            // Verify that every column has a value
            // or an empty placeholder
            if (values.Length != Headers.Length)
            {
                Debug.LogError(
                    "CSV column count mismatch."
                );

                return false;
            }

            File.AppendAllText(
                CurrentFilePath,
                string.Join(",", values) + "\n",
                new UTF8Encoding(false)
            );

            Debug.Log(
                $"CSV: Attempt {trial.AttemptNumber} " +
                $"saved. Status: {trial.Status}. " +
                $"Counted: {trial.IsCounted}"
            );

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "CSV export failed: " +
                exception.Message
            );

            return false;
        }
    }

    // Format numerical values independently
    // of the system language
    private static string Format(float? value)
    {
        return value.HasValue
            ? value.Value.ToString("G9", Culture)
            : "";
    }

    private static string Format(double? value)
    {
        return value.HasValue
            ? value.Value.ToString("G17", Culture)
            : "";
    }

    private static string Format(int? value)
    {
        return value.HasValue
            ? value.Value.ToString(Culture)
            : "";
    }

    private static string Format(bool value)
    {
        return value ? "true" : "false";
    }

    // Escape special characters according
    // to CSV conventions
    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(",") ||
            value.Contains("\"") ||
            value.Contains("\n") ||
            value.Contains("\r"))
        {
            return "\"" +
                value.Replace("\"", "\"\"") +
                "\"";
        }

        return value;
    }
}
