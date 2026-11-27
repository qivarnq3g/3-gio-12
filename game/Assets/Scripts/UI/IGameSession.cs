namespace BaGio12.Gameplay
{
    public interface IGameSession
    {
        bool AtStart { get; }
        bool HasSave { get; }
        string Status { get; }
        string SaveSummary { get; }
        bool ShowWarning { get; set; }
        int Quality { get; set; }
        bool ContinueSaved();
        bool StartNewGame();
        bool DeleteProgress();
        bool PrepareQuit();
        void ImportSettings(GameSettings settings);
        bool SaveSettings(GameSettings settings);
        bool AskTelemetryConsent { get; }
        bool TelemetryConsent { get; set; }
        void AnswerTelemetryConsent(bool granted);
        bool CanSubmitEvidence { get; }
        void SubmitEvidence();
    }
}
