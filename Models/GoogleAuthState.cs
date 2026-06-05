namespace AutoTool.Models
{
    public enum GoogleAuthState
    {
        FindEmailInput,
        BuyAndInputEmail,
        VerifyEmailAlive,
        InputPassword,
        HandleRecoveryEmail,
        AcceptTerms,
        HandleCancelAndSkip,
        HandleServiceConsent,
        FinalAgree,
        Success,
        Failed
    }
}