using Ardalis.Result;
using FluentValidation;
using MediatR;
using IResult = Ardalis.Result.IResult;
using Ardalis.Result.FluentValidation;

//using IResult = Microsoft.AspNetCore.Http.IResult;

namespace MediatRPipelineFluentValidation.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    //where TResponse : new()
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        if (validators.Any())
        {
            // var context = new ValidationContext<TRequest>(request);
            //
            // var validationResults = await Task.WhenAll(
            //     validators.Select(v =>
            //         v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
            // var resultErrors = validationResults.SelectMany(r => r.AsErrors()).ToList();
            //
            //
            //
            // var failures = validators
            //     .Select(v => v.Validate(context))
            //     .SelectMany(validationResult => validationResult.Errors)
            //     .Where(f => f != null)
            //     .ToList();

            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var resultErrors = validationResults.SelectMany(r => r.AsErrors()).ToList();
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();


            if (failures.Count > 0)
            {
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var resultType = typeof(TResponse).GetGenericArguments()[0];
                    var invalidMethod = typeof(Result<>)
                        .MakeGenericType(resultType)
                        .GetMethod(nameof(Result<int>.Invalid), new[] { typeof(List<ValidationError>) });

                    if (invalidMethod != null)
                    {
                        return (TResponse)invalidMethod.Invoke(null, new object[] { resultErrors });
                    }
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Invalid(resultErrors);
                }
                else
                {
                    throw new ValidationException(failures);
                }
            }

            //return await next().ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }
}