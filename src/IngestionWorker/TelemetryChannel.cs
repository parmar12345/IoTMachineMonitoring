using System.Threading.Channels;
using Shared.Models;

namespace IngestionWorker;

public sealed class TelemetryChannel
{
    private readonly Channel<TelemetryEnvelope> _channel;

    public TelemetryChannel()
    {
        _channel = Channel.CreateBounded<TelemetryEnvelope>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public ChannelWriter<TelemetryEnvelope> Writer => _channel.Writer;

    public ChannelReader<TelemetryEnvelope> Reader => _channel.Reader;
}