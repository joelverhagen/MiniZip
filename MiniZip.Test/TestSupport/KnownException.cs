using System;

namespace Knapcode.MiniZip
{
    public class KnownException : IEquatable<KnownException>
    {
        private readonly int _hashCode;

        public static KnownException Create<T>(string message) where T : Exception
        {
            return new KnownException(typeof(T), message);
        }

        private KnownException(Type type, string message)
        {
            Type = type;
            Message = message;
            _hashCode = $"{Type}/{message}".GetHashCode();
        }

        public Type Type { get; }
        public string Message { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as KnownException);
        }

        public bool Equals(KnownException other)
        {
            if (other == null)
            {
                return false;
            }

            return Type == other.Type && Message == other.Message;
        }

        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
