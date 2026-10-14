using System.Text.Json.Serialization;
using Partners.Api.Errors;
using Partners.Application;
using Partners.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Numbers must be JSON numbers ("10.5" as a string is rejected, never coerced).
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    })
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xml = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    options.IncludeXmlComments(xml);
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(
    migrate: app.Configuration.GetValue<bool>("Database:MigrateOnStartup"),
    seedSampleData: app.Configuration.GetValue<bool>("Database:SeedSampleData"));

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program;
