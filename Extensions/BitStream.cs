using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VRage.Library.Collections;

namespace SEPC.Extensions
{
    /// <summary> 
    /// Helpers for using additional types with BitStreams
    /// </summary>
    public static class BitStreamExtensions
    {
        /// <summary> Writes a given string to the BitStream.</summary>
        public static void WriteString(this BitStream stream, string toWrite)
        {
            stream.WritePrefixLengthString(toWrite, 0, toWrite.Length, Encoding.UTF8);
        }

        /// <summary>Reads and returns a string from the BitStream.</summary>
        public static string ReadString(this BitStream stream)
        {
            return stream.ReadPrefixLengthString(Encoding.UTF8);
        }

        /// <summary> Serializes a string with the BitStream.</summary>
        public static void Serialize(this BitStream stream, ref string toSerialize)
        {
            if (stream.Reading)
                stream.WriteString(toSerialize);
            else
                toSerialize = stream.ReadString();
        }

        /*
        /// <summary>Writes a given enum derived from byte to the BitStream.</summary>
        public static void WriteByteEnum(this BitStream stream, Enum toWrite)
        {
            stream.WriteByte(Convert.ToByte(toWrite));
        }

        /// <summary>Writes a given enum derived from byte to the BitStream.</summary>
        public static void ReadByteEnum<TEnum>(this BitStream stream, ref TEnum toWrite) where TEnum : IConvertible
        {
            toWrite = stream.ReadByte();
        }
        */

        /// <summary>Returns a new byte[] from the stream's contents.</summary>
        public static byte[] ToBytes(this BitStream stream)
        {
            byte[] bytes = new byte[stream.BytePosition];
            stream.ResetRead();
            stream.ReadBytes(bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>Returns a new BitStream using the given bytes</summary>
        public static BitStream ToBitStream(this byte[] bytes)
        {
            var stream = new BitStream(bytes.Length);
            stream.ResetWrite();
            stream.WriteBytes(bytes, 0, bytes.Length);
            return stream;
        }
    }
}
