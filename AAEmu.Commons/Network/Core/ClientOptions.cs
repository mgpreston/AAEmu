#nullable enable
namespace AAEmu.Commons.Network.Core;

public record ClientOptions
{
    /// <summary>
    /// Gets or sets the maximum size of a message that can be received.
    /// </summary>
    public int MaxMessageSize { get; set; } = 64 * 1024; // 64 KiB

    /// <summary>
    /// Gets or sets the size of the socket's receive buffer.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 64 * 1024; // 64 KiB

    /// <summary>
    /// Gets or sets the size of the socket's send buffer.
    /// </summary>
    public int SendBufferSize { get; set; } = 64 * 1024; // 64 KiB

    /// <summary>
    /// Gets or sets the maximum number of packets that can be queued for sending.
    /// </summary>
    public int MaxSendQueueSize { get; set; } = 1024;
}
