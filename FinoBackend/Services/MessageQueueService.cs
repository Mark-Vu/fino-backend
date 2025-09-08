using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace FinoBackend.Services;

public class MessageQueueService
{
    private readonly IAmazonSQS _sqs;
    private readonly Dictionary<QueueType, string> _queueUrls;

    public MessageQueueService(IAmazonSQS sqs, IConfiguration config)
    {
        _sqs = sqs;
        _queueUrls = new Dictionary<QueueType, string>
        {
            { QueueType.BankStatementConversion,  config["SQS:BankStatementConversionQueueUrl"] 
                                           ?? throw new InvalidOperationException("Missing BankStatementConversionQueueUr") },
            { QueueType.PublicBankStatementConversion, config["SQS:PublicBankStatementConversionQueueUrl"] 
                                           ?? throw new InvalidOperationException("Missing PublicBankStatementConversionQueueUrl") },
            { QueueType.DeliveryReceiptConversion,   config["SQS:DeliveryReceiptConversionQueueUrl"] 
                                           ?? throw new InvalidOperationException("Missing DeliveryReceiptConversionQueueUrl") }
        };
    }
    
    public async Task<string> EnqueueAsync<T>(
        QueueType queueType,
        T message,
        CancellationToken ct = default)
    {
        var queueUrl = _queueUrls[queueType];
        var body = JsonSerializer.Serialize(message);

        var request = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = body
        };

        await _sqs.SendMessageAsync(request, ct);
        return queueUrl;
    }
}