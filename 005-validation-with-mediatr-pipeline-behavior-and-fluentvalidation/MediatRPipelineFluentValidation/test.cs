using FluentResults;
using FluentValidation;
using FluentValidation.Results;

namespace MediatRPipelineFluentValidation;

public static class ValidationExtensions
{
    public static Result WithValidationResults(this Result result, List<ValidationFailure> failures)
    {
        foreach (var failure in failures)
        {
            var reason = failure.MapToReason(); 
            result.Reasons.Add(reason);
        }

        return result;
    }

    public static IReason MapToReason(this ValidationFailure failure)
    {
        return new Error(failure.ErrorMessage);
    }
}