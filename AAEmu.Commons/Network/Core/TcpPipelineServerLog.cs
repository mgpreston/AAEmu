#nullable enable
using System.Net;
using Microsoft.Extensions.Logging;

namespace AAEmu.Commons.Network.Core;

/// <summary>
/// Provides logging methods for <see cref="TcpPipelineServer"/>.
/// </summary>
public static partial class TcpPipelineServerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Error accepting connection")]
    public static partial void AcceptError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client connected: {Endpoint}")]
    public static partial void ClientConnected(ILogger logger, IPEndPoint endpoint);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Client disconnected: {Endpoint}")]
    public static partial void ClientDisconnected(ILogger logger, IPEndPoint endpoint);
}
