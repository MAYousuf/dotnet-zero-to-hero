﻿using FluentResults;
using FluentValidation;
using MediatR;

namespace MediatRPipelineFluentValidation.Behaviors;

// public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
//     : IPipelineBehavior<TRequest, TResponse>
//     where TRequest : IRequest<TResponse>
//     where TResponse : ResultBase , new()
// {
//     public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
//     {
//         ArgumentNullException.ThrowIfNull(next);
//
//         if (validators.Any())
//         {
//             var context = new ValidationContext<TRequest>(request);
//
//             var validationResults = await Task.WhenAll(
//                 validators.Select(v =>
//                     v.ValidateAsync(context, cancellationToken))).ConfigureAwait(false);
//
//             var failures = validationResults
//                 .Where(r => r.Errors.Count > 0)
//                 .SelectMany(r => r.Errors)
//                 .ToList();
//
//             // if (failures.Count > 0)
//             //     //throw new FluentValidation.ValidationException(failures);
//             //     return Result.Fail(new ExceptionalError(new FluentValidation.ValidationException(failures)));
//             
//             var errors = failures
//                 .Where(validationFailure => validationFailure is not null)
//                 .Select(failure => new Error(failure.ErrorMessage))
//                 .Distinct()
//                 .ToArray();
//             
//             if (errors.Any())
//             {
//                 var result = new TResponse();
//                 
//                 foreach (var error in errors)
//                     result.Reasons.Add(error);
//                 //return Result.Fail(errors);
//                 result.
//
//                 return result;
//             }
//
//         }
//         return await next().ConfigureAwait(false);
//     }
// }

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : ResultBase, new()
//where TRequest : IRequest<Result<TResponse>>
// public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
//     : IPipelineBehavior<TRequest, Result<TResponse>>
//     where TRequest : IRequest<Result<TResponse>>
//where TResponse : ResultBase , new()
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

            var errorsDictionary = validationResults
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .GroupBy(
                    x => x.PropertyName,
                    x => x.ErrorMessage,
                    (propertyName, errorMessages) => new
                    {
                        Key = propertyName,
                        Values = errorMessages.Distinct().ToArray()
                    })
                .ToDictionary(x => x.Key, x => x.Values);
            var error2 = new Error("Validation Error").Metadata;
            
            var failures = validators
                .Select(v => v.Validate(context))
                .SelectMany(validationResult => validationResult.Errors)
                .Where(f => f != null)
                .ToList();
            
            //return Result.Fail("").WithErrors(failures);
            var errors = failures
                .Where(validationFailure => validationFailure is not null)
                .Select(failure => new Error(failure.ErrorMessage))
                .Distinct()
                .ToArray();

            if (errors.Any())
            {
                var result = new TResponse();

                foreach (var error in errors)
                    result.Reasons.Add(error);

                return result;
            }
        }

        return await next().ConfigureAwait(false);
    }
}

// public  class ValidationBehavior<TRequest, TResponse>
//     : IPipelineBehavior<TRequest, TResponse>
//     where TRequest : IRequest<TResponse>
//     where TResponse : ResultBase, new()
// {
//     private readonly IEnumerable<IValidator> _validators;
//
//     public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
//         => _validators = validators;
//
//     public async Task<TResponse> Handle(
//         TRequest request, 
//         RequestHandlerDelegate<TResponse> next, 
//         CancellationToken cancellationToken)
//     {        
//         if(! _validators.Any())
//         {
//             return await next();
//         }
//
//         var context = new ValidationContext<TRequest>(request);
//         
//         var errors = _validators
//             .Select(validator => validator.Validate(context))
//             .SelectMany(validationResult => validationResult.Errors)
//             .Where(validationFailure => validationFailure is not null)
//             .Select(failure => new Error(failure.ErrorMessage))
//             .Distinct()
//             .ToArray();
//
//         if (errors.Any())
//         {
//             var result = new TResponse();
//
//             foreach (var error in errors)
//                 result.Reasons.Add(error);
//
//             return result;
//         }
//
//         return await next();
//     }
// }