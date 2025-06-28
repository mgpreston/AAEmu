using System.Buffers;
using AAEmu.Commons.Network.Core;

namespace AAEmu.Commons.Network;

public abstract class BaseProtocolHandler : IBaseProtocolHandler
{
    public virtual void OnConnect(ISession session) { }

    public virtual void OnReceive(ISession session, byte[] buf, int offset, int bytes) { }

    public virtual bool TryReceivePacket(ISession session, ref SequenceReader<byte> buffer) { return false; }

    public virtual void OnSend(ISession session, byte[] buf, int offset, int bytes) { }

    public virtual void OnDisconnect(ISession session) { }
}
