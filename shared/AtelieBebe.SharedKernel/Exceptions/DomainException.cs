namespace AtelieBebe.SharedKernel.Exceptions;

/// <summary>Raised when a domain invariant is violated.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
