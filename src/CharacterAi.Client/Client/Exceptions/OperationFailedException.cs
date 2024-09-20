namespace CharacterAi.Client.Exceptions
{
    public class OperationFailedException : Exception
    {
        public OperationFailedException(string? message) : base(message)
        {
        }
    }
}
