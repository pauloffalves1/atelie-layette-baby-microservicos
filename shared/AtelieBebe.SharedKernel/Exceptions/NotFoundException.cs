namespace AtelieBebe.SharedKernel.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string entity, object key) : base($"{entity} '{key}' não foi encontrado(a).") { }
}
