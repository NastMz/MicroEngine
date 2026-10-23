using System.Buffers.Binary;
namespace MicroEngine.Desktop;

internal static class WaveFile
{
    internal static byte[] CopyAndValidate(byte[] encoded)
    {
        ArgumentNullException.ThrowIfNull(encoded);
        if(encoded.Length is <44 or >1048576)throw new InvalidDataException("WAV byte budget exceeded.");
        var bytes=encoded.ToArray();ReadOnlySpan<byte> b=bytes;
        if(!b[..4].SequenceEqual("RIFF"u8)||!b.Slice(8,4).SequenceEqual("WAVE"u8)||BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(4,4))!=(uint)(b.Length-8))
            throw new InvalidDataException("Invalid RIFF/WAVE container.");
        bool format=false,data=false;uint rate=0,byteRate=0,dataSize=0;ushort channels=0,align=0;
        int position=12;
        while(position<b.Length)
        {
            if(b.Length-position<8)throw new InvalidDataException("Truncated WAV chunk.");
            uint size=BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(position+4,4));
            if(size>(uint)(b.Length-position-8))throw new InvalidDataException("WAV chunk exceeds container.");
            var id=b.Slice(position,4);var payload=b.Slice(position+8,(int)size);
            if(id.SequenceEqual("fmt "u8))
            {
                if(format||size<16||BinaryPrimitives.ReadUInt16LittleEndian(payload)!=1||BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(14))!=16)
                    throw new InvalidDataException("PCM16 format required once.");
                format=true;channels=BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(2));rate=BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(4));
                byteRate=BinaryPrimitives.ReadUInt32LittleEndian(payload.Slice(8));align=BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(12));
            }
            else if(id.SequenceEqual("data"u8))
            {
                if(data)throw new InvalidDataException("Duplicate WAV samples.");data=true;dataSize=size;
            }
            position+=8+(int)size;
            if((size&1)!=0){if(position==b.Length)throw new InvalidDataException("Missing WAV chunk padding.");position++;}
        }
        if(!format||!data||channels is <1 or >2||rate is <8000 or >48000||align!=channels*2||byteRate!=rate*align
            ||dataSize==0||dataSize%align!=0||dataSize>rate*align*3)
            throw new InvalidDataException("WAV exceeds the Desktop PCM16 mono/stereo 8..48kHz three-second profile.");
        return bytes;
    }
}
