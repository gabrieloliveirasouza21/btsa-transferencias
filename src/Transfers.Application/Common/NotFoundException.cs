namespace Transfers.Application.Common
{
    public class NotFoundException(string resource, Guid id)
        : Exception($"{resource} '{id}' was not found.")
    {
        public string Resource { get; } = resource;
        public Guid ResourceId { get; } = id;
    }
}
