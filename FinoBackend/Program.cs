using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SQS;
using Amazon.Textract;
using FastEndpoints;
using FastEndpoints.Swagger;
using FinoBackend.Common.Extensions;
using FinoBackend.Data;
using FinoBackend.Services;
using FinoBackend.Services.BankStatementConverter;
using FinoBackend.Services.Workers;
using Microsoft.EntityFrameworkCore;
using PrivateBankStatementConverter = FinoBackend.Services.Workers.PrivateBankStatementConverter;
using PublicBankStatementConverter = FinoBackend.Services.Workers.PublicBankStatementConverter;

var builder = WebApplication.CreateBuilder(args);
var awsOptions = builder.Configuration.GetSection("AWS").Get<AwsConfig>();

var awsCredentials = new BasicAWSCredentials(awsOptions.AccessKey, awsOptions.SecretKey);
var region = RegionEndpoint.GetBySystemName(awsOptions.Region);

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(awsCredentials, region));
builder.Services.AddSingleton<IAmazonSQS>(_ => new AmazonSQSClient(awsCredentials, region));
builder.Services.AddSingleton<IAmazonTextract>(_ => new AmazonTextractClient(awsCredentials, region));


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
builder.Services.AddScoped<ConversionJobService>();
builder.Services.AddScoped<BankStatementService>();
builder.Services.AddScoped<MessageQueueService>();
builder.Services.AddSingleton<FinoBackend.Services.BankStatementConverter.PublicBankStatementConversionWorker>();
builder.Services.AddHostedService<PublicBankStatementConverter>();
builder.Services.AddSingleton<FinoBackend.Services.BankStatementConverter.PrivateBankStatementConversionWorker>();
builder.Services.AddHostedService<PrivateBankStatementConverter>();

var app = builder.Build();

app.UseCors(CorsPolicies.Default);
app.UseAuthentication();
app.UseAuthorization();
app.UseFastEndpoints(c =>
{
    c.Endpoints.RoutePrefix = "api"; 
})
.UseSwaggerGen();
app.UseGlobalExceptionHandler();



app.Run();