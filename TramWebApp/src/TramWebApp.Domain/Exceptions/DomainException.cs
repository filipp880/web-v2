namespace TramWebApp.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id) 
        : base($"{entityName} с идентификатором '{id}' не найден.") { }
}

public class InMemoryStorageLimitException : DomainException
{
    public InMemoryStorageLimitException(int maxRecords) 
        : base($"Превышен лимит записей в памяти: максимум {maxRecords} записей.") { }
}

public class InvalidModelInputException : DomainException
{
    public InvalidModelInputException(string message) : base(message) { }
}

public class ModelInferenceException : DomainException
{
    public ModelInferenceException(string message) : base(message) { }
    public ModelInferenceException(string message, Exception innerException) : base(message, innerException) { }
}

public class UnauthorizedAccessException : DomainException
{
    public UnauthorizedAccessException(string message) : base(message) { }
}

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message, IReadOnlyDictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}