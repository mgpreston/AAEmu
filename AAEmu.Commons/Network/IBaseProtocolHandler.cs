using System.Buffers;
using AAEmu.Commons.Network.Core;

namespace AAEmu.Commons.Network;

public interface IBaseProtocolHandler
{
    void OnConnect(ISession session);
    void OnReceive(ISession session, byte[] buf, int offset, int bytes);
    /// <summary>
    /// Attempts to read and process a packet from the buffer.
    /// </summary>
    /// <param name="session">The session the data was received from.</param>
    /// <param name="buffer">A reader over the received data buffer.</param>
    /// <returns>true if a packet was successfully read from the buffer; otherwise, false.</returns>
    /// <remarks>
    /// <para>
    /// This method is the successor to <see cref="OnReceive"/> and should eventually replace it.
    /// </para>
    /// <para>
    /// When a packet is successfully read, the method should return true and the buffer reader's position should be the
    /// end of the packet data. If no packet could be read, the method should return false.
    /// </para>
    /// </remarks>
    bool TryReceivePacket(ISession session, ref SequenceReader<byte> buffer);
    void OnSend(ISession session, byte[] buf, int offset, int bytes);
    void OnDisconnect(ISession session);
}
