#nullable enable
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace AAEmu.Commons.Network.Core;

public interface IClient2 : ISession, IAsyncDisposable
{
    /// <summary>
    /// Gets the remote endpoint of the client.
    /// </summary>
    IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    /// Gets a <see cref="Task"/> that completes when the client processing is finished.
    /// </summary>
    Task Completion { get; }

    /// <summary>
    /// Starts the client processing loop. This method should be called after the client is created.
    /// </summary>
    void Start();
}

public sealed class Client2(
    TcpClient client,
    IBaseProtocolHandler handler,
    ClientOptions options,
    ILogger<Client2> logger) : IClient2
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<string, object> _attributes = [];
    private readonly NetworkStream _stream = client.GetStream();

    private readonly Channel<PacketStream> _sendChannel = Channel.CreateBounded<PacketStream>(
        new BoundedChannelOptions(options.MaxSendQueueSize)
        {
            SingleWriter = false, SingleReader = true, FullMode = BoundedChannelFullMode.Wait
        });

    private Task? _processingTask;

    public IPEndPoint RemoteEndPoint { get; } = (IPEndPoint)client.Client.RemoteEndPoint!;
    public IPAddress Ip => RemoteEndPoint.Address;
    public uint SessionId { get; } = (uint)client.Client.RemoteEndPoint!.GetHashCode();
    public Socket Socket { get; } = client.Client;
    public Task Completion => _processingTask ?? Task.CompletedTask;

    public void SendPacket(ReadOnlySpan<byte> packet) => throw new NotImplementedException();

    public ValueTask SendAsync(PacketStream packet, CancellationToken cancellationToken = default) =>
        _sendChannel.Writer.WriteAsync(packet, cancellationToken);

    public bool TrySend(PacketStream packet) => _sendChannel.Writer.TryWrite(packet);

    void ISession.AddAttribute(string name, object attribute) => _attributes.Add(name, attribute);

    object? ISession.GetAttribute(string name) => _attributes.GetValueOrDefault(name);

    void ISession.ClearAttribute(string name) => _attributes.Remove(name);

    public void Close() => _cts.Cancel();

    public void Start()
    {
        if (_processingTask is not null)
        {
            throw new InvalidOperationException("Client has already been started.");
        }

        _processingTask = ProcessAsync();
    }

    private async Task ProcessAsync()
    {
        ConfigureSocket();

        handler.OnConnect(this);

        try
        {
            var readTask = ReceiveLoopAsync();
            var sendTask = SendLoopAsync();
            await Task.WhenAny(sendTask, readTask);

            await _cts.CancelAsync();

            await Task.WhenAll(sendTask, readTask);
        }
        finally
        {
            handler.OnDisconnect(this);
        }
    }

    private void ConfigureSocket()
    {
        client.NoDelay = true; // Disable Nagle's algorithm for low latency
        client.ReceiveBufferSize = options.ReceiveBufferSize;
        client.SendBufferSize = options.SendBufferSize;
    }

    private async Task ReceiveLoopAsync()
    {
        // pauseWriterThreshold ensures we limit the pipe buffer size, which otherwise could be a denial-of-service attack vector.
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: options.MaxMessageSize,
            resumeWriterThreshold: options.MaxMessageSize / 2));
        var writing = FillPipeAsync(pipe.Writer);
        var reading = ReadPipeAsync(pipe.Reader);

        try
        {
            await Task.WhenAll(reading, writing);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error with client {RemoteEndPoint}", RemoteEndPoint);
        }
        finally
        {
            client.Close();
        }
    }

    private async Task SendLoopAsync()
    {
        try
        {
            var writer = PipeWriter.Create(_stream);

            while (await _sendChannel.Reader.WaitToReadAsync(_cts.Token))
            {
                while (_sendChannel.Reader.TryRead(out var packet))
                {
                    // Write the packet to the PipeWriter
                    var buffer = writer.GetSpan(packet.Count);
                    packet.GetBytes().CopyTo(buffer); // todo: avoid this copy
                    writer.Advance(packet.Count);
                }

                var result = await writer.FlushAsync(_cts.Token);
                if (result.IsCompleted || result.IsCanceled)
                    break;
            }
        }
        catch (OperationCanceledException) when (_cts.Token.IsCancellationRequested)
        {
            // Suppress.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending packet");
        }
        finally
        {
            _sendChannel.Writer.TryComplete();
        }
    }

    private async Task FillPipeAsync(PipeWriter writer)
    {
        const int MinimumBufferSize = 1024;

        while (!_cts.IsCancellationRequested)
        {
            var memory = writer.GetMemory(MinimumBufferSize);
            try
            {
                var bytesRead = await _stream.ReadAsync(memory, _cts.Token);
                if (bytesRead == 0) break;

                writer.Advance(bytesRead);
            }
            catch
            {
                break;
            }

            var result = await writer.FlushAsync(_cts.Token);
            if (result.IsCompleted || result.IsCanceled) break;
        }

        await writer.CompleteAsync();
    }

    private async Task ReadPipeAsync(PipeReader reader)
    {
        while (!_cts.IsCancellationRequested)
        {
            var result = await reader.ReadAsync(_cts.Token);
            var buffer = result.Buffer;

            var sequenceReader = new SequenceReader<byte>(buffer);
            var consumed = buffer.Start;
            var examined = buffer.End;

            // Read packets from the buffer until no more complete packets are available.
            while (handler.TryReceivePacket(this, ref sequenceReader))
            {
                // Update consumed as we successfully parsed a packet and no longer need the bytes for it.
                consumed = sequenceReader.Position;
            }

            reader.AdvanceTo(consumed, examined);

            if (result.IsCompleted) break;
        }

        await reader.CompleteAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Close();

        if (_processingTask is not null)
        {
            try
            {
                await _processingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during client processing cleanup for {RemoteEndPoint}", RemoteEndPoint);
            }
        }

        await _stream.DisposeAsync();
        client.Dispose();
        _cts.Dispose();
    }
}
