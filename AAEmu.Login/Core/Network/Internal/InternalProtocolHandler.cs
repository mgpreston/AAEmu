using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using AAEmu.Commons.Network;
using AAEmu.Commons.Network.Core;
using AAEmu.Login.Core.Controllers;
using AAEmu.Login.Core.Network.Connections;
using AAEmu.Login.Models;
using AAEmu.Login.Utils;
using Microsoft.Extensions.Logging;

namespace AAEmu.Login.Core.Network.Internal;

public class InternalProtocolHandler(
    IEnumerable<IInternalPacketDescriptor> packetDescriptors,
    IGameController gameController,
    IInternalConnectionTable internalConnectionTable,
    ILogger<InternalProtocolHandler> logger)
    : BaseProtocolHandler, IInternalProtocolHandler
{
    private readonly ConcurrentDictionary<ushort, IInternalPacketDescriptor> _packets =
        new(packetDescriptors.ToDictionary(d => d.TypeId));

    public override void OnConnect(ISession session)
    {
        logger.LogInformation("GameServer from {IP} connected, session id: {SessionId}", session.Ip, session.SessionId);
        var con = new InternalConnection(session);
        internalConnectionTable.AddConnection(con);
    }

    public override void OnDisconnect(ISession session)
    {
        logger.LogInformation("GameServer from {IP} disconnected", session.Ip);
        if (session.GetAttribute("gsId") is { } gsId)
            gameController.Remove((GameServerId)gsId);
        internalConnectionTable.RemoveConnection(session.SessionId);
    }

    public override bool TryReceivePacket(ISession session, ref SequenceReader<byte> buffer)
    {
        try
        {
            var connection = internalConnectionTable.GetConnection(session.SessionId);
            Debug.Assert(connection != null);
            return TryReceive(connection, ref buffer);
        }
        catch (Exception e)
        {
            session.Close();
            logger.LogError(e, "Error while processing received data for session {SessionId}", session.SessionId);
            return false;
        }
    }

    private bool TryReceive(InternalConnection connection, ref SequenceReader<byte> reader)
    {
        const int MinimumPacketSize = 4; // 2 bytes for length, 2 bytes for type

        try
        {
            // Check there's enough data to read a packet length and type.
            if (reader.Remaining < MinimumPacketSize)
            {
                return false;
            }

            var readLength = reader.TryReadLittleEndian(out ushort length);
            Debug.Assert(readLength);

            if (reader.Remaining < length)
            {
                return false;
            }

            var readType = reader.TryReadLittleEndian(out ushort type);
            Debug.Assert(readType);

            var readData = reader.TryReadExact(length - 2, out var data);
            Debug.Assert(readData);

            var stream = new PacketStream();
            stream.Insert(stream.Count, data.ToArray()); // todo: avoid this copy
            if (!_packets.TryGetValue(type, out var packetDescriptor))
            {
                HandleUnknownPacket(connection, type, stream);
            }
            else
            {
                Dispatch(packetDescriptor, stream, connection);
            }

            return true;
        }
        catch (Exception e)
        {
            connection.Shutdown();
            logger.LogError(e, "Error while processing received data for connection {ConnectionId}", connection.Id);
            return false;
        }
    }

    private void Dispatch(IInternalPacketDescriptor packetDescriptor, PacketStream stream,
        InternalConnection connection)
    {
        _ = DispatchAsync();
        return;

        async Task DispatchAsync()
        {
            try
            {
                await packetDescriptor.DispatchAsync(stream, connection);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error on packet dispatch {PacketTypeId}", packetDescriptor.TypeId);
            }
        }
    }

    private void HandleUnknownPacket(InternalConnection connection, uint type, PacketStream stream)
    {
        var dump = new StringBuilder();
        for (var i = stream.Pos; i < stream.Count; i++)
            dump.Append($"{stream.Buffer[i]:x2} ");
        logger.LogError("Unknown packet 0x{TypeId:x2} from {IPAddress}:\n{HexDump}", type, connection.Ip, dump);
    }
}
