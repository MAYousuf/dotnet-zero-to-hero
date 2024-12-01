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
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                validators.Select(v =>
                    v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
            var resultErrors = validationResults.SelectMany(r => r.AsErrors()).ToList();

            // var errorsDictionary = validationResults
            //     .SelectMany(x => x.Errors)
            //     .Where(x => x != null)
            //     .GroupBy(
            //         x => x.PropertyName,
            //         x => x.ErrorMessage,
            //         (propertyName, errorMessages) => new
            //         {
            //             Key = propertyName,
            //             Values = errorMessages.Distinct().ToArray()
            //         })
            //     .ToDictionary(x => x.Key, x => x.Values);
            //var error2 = new Error("Validation Error").Metadata;

            var failures = validators
                .Select(v => v.Validate(context))
                .SelectMany(validationResult => validationResult.Errors)
                .Where(f => f != null)
                .ToList();

            //return Result.Fail("").WithErrors(failures);
            // var errors = failures
            //     .Where(validationFailure => validationFailure is not null)
            //     .Select(failure => new Error(failure.ErrorMessage))
            //     .Distinct()
            //     .ToArray();

            // if (errors.Any())
            // {
            //     var result = new TResponse();
            //
            //     foreach (var error in errors)
            //         result.Reasons.Add(error);
            //
            //     return result;
            // }
            if (failures.Count > 0)
            {
                var responseType = typeof(TResponse);
                //var result = new TResponse();
                //return (TResponse)Result.Invalid(resultErrors);
                //var result   = Activator.CreateInstance(responseType, null) as TResponse;
                
                //return result;
                return (TResponse)(object)Result.Invalid(resultErrors);
            }

            //return await next().ConfigureAwait(false);
        }

        return await next().ConfigureAwait(false);
    }
}