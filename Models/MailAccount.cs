namespace AutoTool.Models
{
    public class MailAccount
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
}