namespace AtelieBebe.SharedKernel.Exceptions;

public class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message) : base(message) { }
}
