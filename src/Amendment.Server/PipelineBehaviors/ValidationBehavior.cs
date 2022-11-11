using FluentValidation;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace Amendment.Server.PipelineBehaviors;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(x => x.Validate(context))
            .SelectMany(x => x.Errors)
            .Where(x => x != null)
            //.Select(x => new { Name = x.PropertyName, Error = x.ErrorMessage })
            .ToList();

        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        return next();
    }
}