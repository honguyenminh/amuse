using System.Text;
using System.Text.Json;
using Amuse.Modules.Catalog.Processing;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Amuse.Api;

internal sealed partial class RabbitMqAudioTranscodeJobQueue(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqAudioTranscodeJobQueue> logger)
    : IAudioTranscodeJobQueue, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.General);

    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _connectGate = new(1, 1);

    public async Task PublishAsync(
        AudioTranscodeJobMessage message,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(message, _serializerOptions);
        var body = Encoding.UTF8.GetBytes(json);

        var channel = await EnsureChannelAsync(cancellationToken);

        var props = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    private async Task<IChannel> EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is not null)
            return _channel;

        await _connectGate.WaitAsync(ct);
        try
        {
            if (_channel is not null)
                return _channel;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
            };

            var attempts = 0;
            while (true)
            {
                attempts++;
                try
                {
                    _connection = await factory.CreateConnectionAsync(ct);
                    _channel = await _connection.CreateChannelAsync(cancellationToken: ct);

                    await _channel.QueueDeclareAsync(
                        queue: _options.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        cancellationToken: ct);

                    return _channel;
                }
                catch (Exception ex) when (attempts < 5)
                {
                    LogRabbitMqUnreachableRetry(ex, attempts);
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
            }
        }
        finally
        {
            _connectGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_connection is not null)
            await _connection.CloseAsync();
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "RabbitMQ unreachable; retry {Attempt}/5")]
    private partial void LogRabbitMqUnreachableRetry(Exception ex, int attempt);
}
