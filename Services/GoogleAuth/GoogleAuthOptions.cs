namespace AutoTool.Services.GoogleAuth
{
    public class GoogleAuthOptions
    {
        public int FindEmailInputTimeoutSeconds { get; set; } = 45;
        public int VerifyEmailTimeoutSeconds { get; set; } = 20;
        public int RecoveryEmailTimeoutSeconds { get; set; } = 12;
        public int AgreeTimeoutSeconds { get; set; } = 15;
        public int CancelTimeoutSeconds { get; set; } = 10;
        public int SkipAfterCancelTimeoutSeconds { get; set; } = 10;
        public int SeeMoreTimeoutSeconds { get; set; } = 10;
        public int AcceptTimeoutSeconds { get; set; } = 10;

        public int MaxEmailRetry { get; set; } = 3;

        public int PollingDelayMs { get; set; } = 400;

        public double DefaultImageThreshold { get; set; } = 0.8;
        public double AgreeImageThreshold { get; set; } = 0.75;

        public float DefaultOcrConfidence { get; set; } = 0.6f;
        public float DeadEmailOcrConfidence { get; set; } = 0.65f;

        public int DelayAfterSkipMs { get; set; } = 5000;
        public int DelayAfterTapMs { get; set; } = 1500;
        public int DelayAfterAgreeMs { get; set; } = 2000;
        public int DelayAfterRecoverySubmitMs { get; set; } = 3000;

        public double FallbackAgreeTapXRatio { get; set; } = 0.90;
        public double FallbackAgreeTapYRatio { get; set; } = 0.92;

        public double SwipeStartXRatio { get; set; } = 0.55;
        public double SwipeStartYRatio { get; set; } = 0.82;
        public double SwipeEndXRatio { get; set; } = 0.55;
        public double SwipeEndYRatio { get; set; } = 0.35;
    }
}