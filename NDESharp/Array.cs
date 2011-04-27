using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NDESharp
{
    class Array
    {
        public static byte[] Add(byte[] ar1, byte[] ar2)
        {
            byte[] concat = new byte[ar1.Length + ar2.Length];
            System.Buffer.BlockCopy(ar1, 0, concat, 0, ar1.Length);
            System.Buffer.BlockCopy(ar2, 0, concat, ar1.Length - 1, ar2.Length);
            return concat;
        }
    }
}
