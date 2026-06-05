namespace AutoTool.Models
{
    public enum GoogleMailSourceMode
    {
        Manual,
        Api
    }

    public enum GoogleLoginStatus
    {
        None,
        ApiNoMail,
        EmailInputted,
        EmailAccepted,
        EmailRejected,
        PasswordInputted,
        RecoveryEmailInputted,
        LoginSuccess,
        LoginFailed,
        Cancelled
    }

    public class GoogleLoginCredential
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string RecoveryEmail { get; set; } = "";

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Email)
                && !string.IsNullOrWhiteSpace(Password);
        }
    }

    public class GoogleLoginOptions
    {
        public GoogleMailSourceMode MailSourceMode { get; set; } = GoogleMailSourceMode.Api;

        public string ManualEmail { get; set; } = "";
        public string ManualPassword { get; set; } = "";
        public string ManualRecoveryEmail { get; set; } = "";

        public string MailApiKey { get; set; } = "";
        public string MailProductId { get; set; } = "";
    }

    public class GoogleLoginResult
    {
        public bool Success { get; set; }
        public GoogleLoginStatus Status { get; set; }
        public string Email { get; set; } = "";
        public string Message { get; set; } = "";

        public static GoogleLoginResult Ok(GoogleLoginStatus status, string email, string message)
        {
            return new GoogleLoginResult
            {
                Success = true,
                Status = status,
                Email = email,
                Message = message
            };
        }

        public static GoogleLoginResult Fail(GoogleLoginStatus status, string email, string message)
        {
            return new GoogleLoginResult
            {
                Success = false,
                Status = status,
                Email = email,
                Message = message
            };
        }
    }
}