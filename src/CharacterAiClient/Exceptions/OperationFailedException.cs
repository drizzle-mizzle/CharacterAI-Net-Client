namespace CharacterAi.Exceptions
{
    internal class OperationFailedException : Exception
    {
        public OperationFailedException(string? message) : base(message)
        {
        }
    }
}
