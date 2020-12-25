using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An throttle implementation that allows any level of concurrency.
    /// Based on: https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Protocol/HttpSource/NullThrottle.cs
    /// </summary>
    public class NullThrottle : IThrottle
    {
        /// <summary>
        /// An shared instance of this throttle.
        /// </summary>
        public static NullThrottle Instance { get; } = new NullThrottle();

        /// <summary>
        /// Waits for nothing. Any level of concurrency is allowed.
        /// </summary>
#if NETFRAMEWORK
        public Task WaitAsync() => _completedTask;
        private static readonly Task _completedTask = Task.FromResult(true);
#else
        public Task WaitAsync() => Task.CompletedTask;
#endif

        /// <summary>
        /// Releases nothing. Any level of concurrency is allowed.
        /// </summary>
        public void Release()
        {
        }
    }
}
