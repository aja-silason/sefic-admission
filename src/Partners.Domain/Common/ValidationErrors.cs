namespace Partners.Domain.Common;

/// <summary>Accumulates field errors so a request reports all of its problems at once.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
    }

    public void AddRange(DomainValidationException exception)
    {
        foreach (var (field, messages) in exception.Errors)
        {
            foreach (var message in messages)
            {
                Add(field, message);
            }
        }
    }

    public void ThrowIfAny()
    {
        if (HasErrors)
        {
            throw new DomainValidationException(
                _errors.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal));
        }
    }
}
