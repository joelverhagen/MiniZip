using System;
using System.Threading;
using System.Threading.Tasks;

namespace Knapcode.MiniZip
{
    /// <summary>
    /// An throttle implementation that uses a <see cref="SemaphoreSlim"/>.
    /// Based on: https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Protocol/HttpSource/SemaphoreSlimThrottle.cs
    /// </summary>
    public class SemaphoreSlimThrottle : IThrottle
    {
        private readonly SemaphoreSlim _semaphore;

        /// <summary>
        /// The number of remaining threads that can enter the semaphore.
        /// </summary>
        public int CurrentCount => _semaphore.CurrentCount;

        /// <summary>
        /// Initializes the throttle with a given <see cref="SemaphoreSlim"/>.
        /// </summary>
        /// <param name="semaphore">The semaphore to use for throttling.</param>
        public SemaphoreSlimThrottle(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
        }

        /// <summary>
        /// Waits for an available "slot" in the internal <see cref="SemaphoreSlim"/>.
        /// </summary>
        public async Task WaitAsync()
        {
            await _semaphore.WaitAsync();
        }

        /// <summary>
        /// Releases a "slot" in the internal <see cref="SemaphoreSlim"/>.
        /// </summary>
        public void Release()
        {
            _semaphore.Release();
        }
    }
}
