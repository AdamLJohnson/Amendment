using Amendment.Shared;
using FluentValidation;
using MediatR;
using ValidationException = FluentValidation.ValidationException;

namespace Amendment.Server.PipelineBehaviors;

public class ValidationBehavior<TRequest> : IPipelineBehavior<TRequest, IApiResult>
    where TRequest : IRequest<IApiResult> //where f : IApiResult
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public Task<IApiResult> Handle(TRequest request, RequestHandlerDelegate<IApiResult> next, CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(x => x.Validate(context))
            .SelectMany(x => x.Errors)
            .Where(x => x != null)
            .Select(x => new Shared.ValidationError { Name = x.PropertyName, Message = x.ErrorMessage })
            .ToList();

        if (failures.Any())
            return Task.FromResult((IApiResult) new ApiFailedResult(failures));

        return next();
    }
}