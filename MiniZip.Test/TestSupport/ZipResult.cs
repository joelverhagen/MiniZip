using System;
using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    public class ZipResult<T> : IEquatable<ZipResult<T>>
    {
        public bool Success { get; set; }
        public Exception Exception { get; set; }
        public T Data { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ZipResult<T>);
        }

        public bool Equals(ZipResult<T> other)
        {
            return other != null &&
                   Success == other.Success &&
                   ExceptionComparer.Instance.Equals(Exception, other.Exception) &&
                   EqualityComparer<T>.Default.Equals(Data, other.Data);
        }

        public override int GetHashCode()
        {
            var hashCode = 277263931;
            hashCode = hashCode * -1521134295 + Success.GetHashCode();
            hashCode = hashCode * -1521134295 + ExceptionComparer.Instance.GetHashCode(Exception);
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Data);
            return hashCode;
        }

        private class ExceptionComparer : IEqualityComparer<Exception>
        {
            public static readonly ExceptionComparer Instance = new ExceptionComparer();

            public bool Equals(Exception x, Exception y)
            {
                if (x == null && y == null)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.Message == y.Message &&
                       x.GetType() == y.GetType();
            }

            public int GetHashCode(Exception obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                var hashCode = 277263931;
                hashCode = hashCode * -1521134295 + obj.Message.GetHashCode();
                hashCode = hashCode * -1521134295 + obj.GetType().GetHashCode();
                return hashCode;
            }
        }
    }
}
