namespace Transfers.Application.Common
{
    public class ConcurrencyConflictException(string message, Exception innerException)
        : Exception(message, innerException);
}
