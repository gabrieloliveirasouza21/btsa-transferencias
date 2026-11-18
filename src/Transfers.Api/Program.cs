using Transfers.Api.BackgroundJobs;
using Transfers.Api.ErrorHandling;
using Transfers.Application;
using Transfers.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.SectionName));
builder.Services.AddHostedService<ScheduledTransfersWorker>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Transfers API"));
}

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();

app.Run();
