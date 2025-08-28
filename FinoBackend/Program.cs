using Amazon.S3;
using Amazon.SQS;
using Amazon.Textract;
using FastEndpoints;
using FastEndpoints.Swagger;
using FinoBackend.Common.Extensions;
using FinoBackend.Data;
using FinoBackend.Services;
using FinoBackend.Services.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(8000); // http://localhost:8000
});
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient());
builder.Services.AddSingleton<IAmazonTextract>(_ => new AmazonTextractClient());

// === Database ===
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention();
});

// === FastEndpoints ===
builder.Services.AddFastEndpoints()
    .SwaggerDocument();

builder.Services.AddAppCors();
builder.Services.AddSupabaseAuth(builder.Configuration);

// 1) Enable ProblemDetails everywhere
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        // Add useful metadata for all errors
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    };
});


builder.Services.AddAuthorization();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<StorageService>();
builder.Services.AddSingleton<BankStatementConverter>();
builder.Services.AddScoped<ConversionJobService>();
builder.Services.AddScoped<BankStatementService>();
builder.Services.AddScoped<MessageQueueService>();
builder.Services.AddHostedService<BankStatementConversionWorker>();

var app = builder.Build();

app.UseCors(CorsPolicies.Default);
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api"; // 👈 adds /api to all routes
})
.UseSwaggerGen();
app.UseGlobalExceptionHandler();



app.Run();