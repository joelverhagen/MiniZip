using System;

namespace Knapcode.MiniZip
{
    public class ZipResult<T>
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }
        public T Data { get; set; }
    }
}
