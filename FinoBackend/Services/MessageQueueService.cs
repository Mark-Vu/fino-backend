using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using FinoBackend.Endpoints.BankStatementFile;

namespace FinoBackend.Services;

public class MessageQueueService
{
    private readonly IAmazonSQS _sqs;
    private readonly IConfiguration _config;

    public MessageQueueService(IAmazonSQS sqs, IConfiguration config)
    {
        _sqs = sqs;
        _config = config;
    }

    public async Task EnqueueJobAsync(ConversionJobMessage message, CancellationToken ct = default)
    {
        var queueUrl = _config["SQS:QueueUrl"];
        var body = JsonSerializer.Serialize(message);

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = body
        };

        await _sqs.SendMessageAsync(request, ct);
    }
}