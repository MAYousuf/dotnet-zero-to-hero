﻿
using Ardalis.Result;
using MediatRPipelineFluentValidation.Domain;
using MediatRPipelineFluentValidation.Persistence;
using MediatR;

namespace MediatRPipelineFluentValidation.Features.Products.Commands.Create;

public class CreateProductCommandHandler(AppDbContext context) : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var product = new Product(command.Name, command.Description, command.Price);
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();
        return Result.Success(product.Id);
    }
}