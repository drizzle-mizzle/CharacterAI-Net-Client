namespace CharacterAI.Models
{
    public class SetupResult
    {
        public SetupResult(bool isSuccessful, string? errorReason = null)
        {
            IsSuccessful = isSuccessful;
            ErrorReason = errorReason;
        }

        public bool IsSuccessful { get; }
        public string? ErrorReason { get; }
    }
}
