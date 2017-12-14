using System.Collections.Generic;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// Source: https://stackoverflow.com/a/7244729/52749
    /// </summary>
    internal sealed class ListEqualityComparer<T> : IEqualityComparer<IReadOnlyList<T>>
    {
        public static readonly ListEqualityComparer<T> Default = new ListEqualityComparer<T>();

        private static readonly EqualityComparer<T> ElementComparer = EqualityComparer<T>.Default;

        public bool Equals(IReadOnlyList<T> x, IReadOnlyList<T> y)
        {
            if (x == y)
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Count != y.Count)
            {
                return false;
            }

            for (var i = 0; i < x.Count; i++)
            {
                if (!ElementComparer.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(IReadOnlyList<T> obj)
        {
            unchecked
            {
                if (obj == null)
                {
                    return 0;
                }

                var hash = 17;

                foreach (T element in obj)
                {
                    hash = hash * 31 + ElementComparer.GetHashCode(element);
                }

                return hash;
            }
        }
    }
}
