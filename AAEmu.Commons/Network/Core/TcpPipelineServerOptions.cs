#nullable enable
using System.Net;

namespace AAEmu.Commons.Network.Core;

public record TcpPipelineServerOptions
{
    public required IPAddress ListenAddress { get; set; }
    public required int Port { get; set; }
}
