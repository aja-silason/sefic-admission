namespace Partners.Domain.Common;

/// <summary>Base type for every business-rule failure raised by the domain.</summary>
public abstract class DomainException(string message) : Exception(message);
