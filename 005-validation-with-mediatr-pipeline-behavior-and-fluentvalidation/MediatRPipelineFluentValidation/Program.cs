using FluentValidation;
using MediatR;
using MediatRPipelineFluentValidation.Behaviors;
using MediatRPipelineFluentValidation.Exceptions;
using MediatRPipelineFluentValidation.Features.Products.Commands.Create;
using MediatRPipelineFluentValidation.Features.Products.Commands.Delete;
using MediatRPipelineFluentValidation.Features.Products.Notifications;
using MediatRPipelineFluentValidation.Features.Products.Queries.Get;
using MediatRPipelineFluentValidation.Features.Products.Queries.List;
using MediatRPipelineFluentValidation.Persistence;
using System.Reflection;
using FluentResults;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
    //cfg.AddOpenBehavior(typeof(RequestResponseLoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    
});

//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

//builder.Services.AddScoped(typeof(IPipelineBehavior<CreateProductCommand, Result<Guid>>), typeof(ValidationBehavior<CreateProductCommand, Guid>));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance =
            $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
        var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});

var app = builder.Build();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/products/{id:guid}", async (Guid id, ISender mediatr) =>
{
    var product = await mediatr.Send(new GetProductQuery(id));
    if (product == null) return Results.NotFound();
    return Results.Ok(product);
});

app.MapGet("/products", async (ISender mediatr) =>
{
    var products = await mediatr.Send(new ListProductsQuery());
    return Results.Ok(products);
});

app.MapPost("/products", async (CreateProductCommand command, IMediator mediatr) =>
{
    // var productId = await mediatr.Send(command);
    // if (Guid.Empty == productId) return Results.BadRequest();

    var result = await mediatr.Send(command);
    if (result.IsFailed)
    {
        return Results.BadRequest(
            CreateProblemDetails(
                "Validation Error",
                StatusCodes.Status400BadRequest,
                result.Reasons));

        ProblemDetails CreateProblemDetails(
            string title,
            int status,
            List<IReason> reasons) =>
            new()
            {
                Title = title,
                Status = status,
                Type = string.Empty,
                Detail = string.Empty,
                Extensions = { { nameof(reasons), reasons } }
            };
    }


    await mediatr.Publish(new ProductCreatedNotification(result.Value));
    return Results.Created($"/products/{result.Value}", new { id = result.Value });

    // result switch
    // {
    //     { IsSuccess: true } => throw new InvalidOperationException(),
    //     _ => BadRequest(
    //         CreateProblemDetails(
    //             "Validation Error",
    //             StatusCodes.Status400BadRequest,
    //             result.Reasons))
    // }

});

app.MapDelete("/products/{id:guid}", async (Guid id, ISender mediatr) =>
{
    await mediatr.Send(new DeleteProductCommand(id));
    return Results.NoContent();
});

app.UseHttpsRedirection();
app.Run();

  

