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
            //char[] chars = toWrite.ToCharArray();
            //stream.WriteInt32(chars.Length);
            //chars.ForEach(c => stream.WriteChar(c));
            stream.WritePrefixLengthString(toWrite, 0, toWrite.Length, Encoding.UTF8);
        }

        /// <summary>Reads and returns a string from the BitStream.</summary>
        public static string ReadString(this BitStream stream)
        {
            //char[] result = new char[stream.ReadInt32()];
            //for (int index = 0; index < result.Length; index++)
            //    result[index] = stream.ReadChar();
            //return new string(result);
            return stream.ReadPrefixLengthString(Encoding.UTF8);
        }

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
