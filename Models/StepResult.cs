namespace AutoTool.Models
{
    public class StepResult
    {
        public bool Success { get; set; }
        public bool ShouldAbort { get; set; }
        public bool ShouldRetryWithNewEmail { get; set; }
        public GoogleAuthState NextState { get; set; }
        public string Message { get; set; } = "";

        public static StepResult GoTo(GoogleAuthState nextState, string message = "")
        {
            return new StepResult
            {
                Success = true,
                ShouldAbort = false,
                NextState = nextState,
                Message = message
            };
        }

        public static StepResult Fail(string message)
        {
            return new StepResult
            {
                Success = false,
                ShouldAbort = true,
                NextState = GoogleAuthState.Failed,
                Message = message
            };
        }

        public static StepResult RetryEmail(GoogleAuthState nextState, string message = "")
        {
            return new StepResult
            {
                Success = false,
                ShouldAbort = false,
                ShouldRetryWithNewEmail = true,
                NextState = nextState,
                Message = message
            };
        }
    }
}