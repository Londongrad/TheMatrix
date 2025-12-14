using FluentValidation;
using Matrix.BuildingBlocks.Application.Abstractions;
using MediatR;

namespace Matrix.BuildingBlocks.Application.Behaviors
{
    public sealed class ValidationBehavior<TRequest, TResponse>(
        IEnumerable<IValidator<TRequest>> validators,
        IValidationExceptionFactory errorFactory)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
                return await next(cancellationToken);

            var context = new ValidationContext<TRequest>(request);

            var failures = validators
               .Select(v => v.Validate(context))
               .SelectMany(r => r.Errors)
               .Where(f => f is not null)
               .ToList();

            if (failures.Count == 0)
                return await next(cancellationToken);

            var errors = failures
               .GroupBy(f => f.PropertyName)
               .ToDictionary(
                    keySelector: g => g.Key,
                    elementSelector: g =>
                        g.Select(f => f.ErrorMessage)
                           .ToArray());

            throw errorFactory.Create(
                requestType: typeof(TRequest),
                errors: errors);
        }
    }
}
