using AAEmu.Commons.Network.Core;
using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Internal;

public class InternalClientFactory(InternalClientOptions clientOptions, ILoggerFactory loggerFactory)
    : ClientFactory(clientOptions, loggerFactory), IInternalClientFactory;
