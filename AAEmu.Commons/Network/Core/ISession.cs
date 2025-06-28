using System.Net;
using System.Net.Sockets;

namespace AAEmu.Commons.Network.Core;

public interface ISession
{
    public IPAddress Ip { get; }
    public uint SessionId { get; }
    public Socket Socket { get; }
    public void SendPacket(ReadOnlySpan<byte> packet);
    ValueTask SendAsync(PacketStream packet, CancellationToken cancellationToken);
    bool TrySend(PacketStream packet);
    void AddAttribute(string name, object attribute);
    object GetAttribute(string name);
    void ClearAttribute(string name);
    public void Close();
}
