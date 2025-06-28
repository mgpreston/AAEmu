using AAEmu.Commons.Network.Core;
using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Internal;

public class InternalServer(
    InternalServerOptions options,
    IInternalProtocolHandler protocolHandler,
    IInternalClientFactory clientFactory,
    ILogger<InternalServer> logger) : TcpPipelineServer(options, protocolHandler, clientFactory, logger), IInternalServer;
