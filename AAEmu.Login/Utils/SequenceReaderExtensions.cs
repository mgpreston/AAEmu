using System.Buffers;
using System.Runtime.CompilerServices;

namespace AAEmu.Login.Utils;

public static class SequenceReaderExtensions
{
    public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out ushort value)
    {
        Unsafe.SkipInit(out value);
        return reader.TryReadLittleEndian(out Unsafe.As<ushort, short>(ref value));
    }

    public static bool TryReadLittleEndian(this ref SequenceReader<byte> reader, out uint value)
    {
        Unsafe.SkipInit(out value);
        return reader.TryReadLittleEndian(out Unsafe.As<uint, int>(ref value));
    }
}

