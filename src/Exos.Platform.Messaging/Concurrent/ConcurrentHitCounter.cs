namespace Exos.Platform.Messaging.Concurrent
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// Defines the <see cref="ConcurrentHitCounter"/>.
    /// </summary>
    public class ConcurrentHitCounter
    {
        /// <summary>
        /// The queue stores objects to be counted.
        /// </summary>
        private readonly ConcurrentQueue<DateTime> _timeQueue;

        /// <summary>
        /// The effective duration of this counter.
        /// </summary>
        private TimeSpan _durationSpan = new TimeSpan(0, 0, 60);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentHitCounter"/> class.
        /// </summary>
        /// <param name="durationInSeconds">The duration to overwrite the default value.</param>
        public ConcurrentHitCounter(int durationInSeconds = 60)
        {
            if (durationInSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(durationInSeconds));
            }

            _durationSpan = new TimeSpan(0, 0, durationInSeconds);
            _timeQueue = new ConcurrentQueue<DateTime>();
        }

        /// <summary>
        /// Increment the counter.
        /// </summary>
        /// <returns>The current count.</returns>
        public int Increment()
        {
            _timeQueue.Enqueue(DateTime.UtcNow);
            return _timeQueue.Count;
        }

        /// <summary>
        /// Get the count of hits within <see cref="_durationSpan"/> till the time of invocation.
        /// </summary>
        /// <returns>The count within time range.</returns>
        public int GetCount()
        {
            DateTime target = DateTime.UtcNow - _durationSpan;
            while (_timeQueue.TryPeek(out DateTime timestamp) && timestamp < target)
            {
                _timeQueue.TryDequeue(out _);
            }

            return _timeQueue.Count;
        }

        /// <summary>
        /// Reset the counter.
        /// </summary>
        /// <returns>True if reset is successful.</returns>
        public bool Reset()
        {
            _timeQueue.Clear();

            return _timeQueue.IsEmpty;
        }
    }
}
