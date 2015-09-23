using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace mid
{
    public static class StreamHelper
    {
        public static byte[] Read(this Stream stream, int count)
        {
            var buffer = new byte[count];
            var n = stream.Read(buffer, 0, count);
            if (n != count)
            {
                throw new IndexOutOfRangeException();
            }
            return buffer;
        }

        public static Int32 ReadInt32(this Stream stream)
        {
            var data = stream.Read(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public static Int16 ReadInt16(this Stream stream)
        {
            var data = stream.Read(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public static Int32 ReadVarInt(this Stream stream)
        {
            Int32 result = 0;
            while (true)
            {
                var a = stream.Read(1)[0];
                if ((a & 128) != 128)
                {
                    result += a;
                    return result;
                }
                else
                {
                    a &= 127;
                    result += a;
                    result <<= 7;
                }
            }
        }
    }
}
