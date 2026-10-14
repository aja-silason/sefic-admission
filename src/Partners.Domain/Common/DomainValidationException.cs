namespace Partners.Domain.Common;

/// <summary>One or more input values break a domain rule. Carries every failure, keyed by field.</summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
