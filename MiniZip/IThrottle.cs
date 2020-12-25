using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An interface used for throttling operations.
    /// Based on: https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Protocol/HttpSource/IThrottle.cs
    /// </summary>
    public interface IThrottle
    {
        /// <summary>
        /// Waits until an appropriate level of concurrency has been reached before allowing the caller to continue.
        /// </summary>
        Task WaitAsync();

        /// <summary>
        /// Signals that the throttled operation has been completed and other threads can being their own throttled operation.
        /// </summary>
        void Release();
    }
}
