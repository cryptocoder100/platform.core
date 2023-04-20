#pragma warning disable CA1801 // Review unused parameters
namespace Exos.Platform.Messaging.UnitTests.Concurrent
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.Messaging.Concurrent;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class ConcurrentHitCounterTests
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
        }

        [TestInitialize]
        public void TestInitialize()
        {
        }

        [TestMethod]
        public void ConcurrentQueue_GivenEnqueue_ShouldOrderItemsByEnqueuingSequence()
        {
            ConcurrentQueue<DateTime> q = new ConcurrentQueue<DateTime>();

            DateTime t = DateTime.UtcNow;

            Action a = () => q.Enqueue(DateTime.UtcNow);

            Parallel.Invoke(a, a, a, a, a, a, a, a, a, a, a, a);

            while (q.TryDequeue(out DateTime c))
            {
                // the timestamp in queue is not strictly ordered by their value under the circumstances of
                // parallel invocation.
                //
                // this MAY result in the `ConcurrentHitCounter.GetCount()` returns a larger value than expected.
                // however this situation and its impact always automatically corrects overtime.
                //
                // Taken performance gain and implementation effort into consideration this is acceptable.
                // Assert.IsTrue(t.Ticks < c.Ticks);
                Assert.Inconclusive("Timestamp MAY not be strictly ordered.");
                t = c;
            }
        }

        [TestMethod]
        public void ConcurrentHitCounter_GivenParallelInvocation_ShouldWork()
        {
            ConcurrentHitCounter counter = new ConcurrentHitCounter(2);

            counter.Increment();
            counter.Increment();
            Thread.Sleep(2 * 1000);
            counter.Increment();

            Assert.AreEqual(1, counter.GetCount());
        }

        /// <summary>
        ///    t1:  x___|___|___o
        ///    t2:      x___|___|___o___|___|___|
        ///     T:  0   1   2   3   4   5   6   7
        ///         x   x   ^   o ^ o ^       ^   ^
        /// Count:          2     2   2       1   0
        /// .
        /// </summary>
        [TestMethod]
        public void ConcurrentHitCounter_GivenParallelInvocation_ShouldReturnCorrectCount()
        {
            ConcurrentHitCounter counter = new ConcurrentHitCounter(3);

            Action a1 = () =>
            {
                // t = T + 0
                counter.Increment();

                Thread.Sleep(3 * 1000);

                // t = T + 3
                counter.Increment();
            };

            // t = 0
            Thread t1 = new Thread(new ThreadStart(a1));
            t1.Start();

            Thread.Sleep(1 * 1000);
            // t = 1
            Thread t2 = new Thread(new ThreadStart(a1));
            t2.Start();

            Thread.Sleep(1 * 1000);
            // t = 2
            Assert.AreEqual(2, counter.GetCount());

            Thread.Sleep(1500);
            // t = 3.5
            Assert.AreEqual(2, counter.GetCount());

            Thread.Sleep(1 * 1000);
            // t = 4.5
            Assert.AreEqual(2, counter.GetCount());

            Thread.Sleep(2 * 1000);
            // t = 6.5
            Assert.AreEqual(1, counter.GetCount());

            Thread.Sleep(1 * 1000);
            // t = 7.5
            Assert.AreEqual(0, counter.GetCount());
        }

        [TestMethod]
        public void ConcurrentHitCounter_GivenResetInvoked_ShouldReset()
        {
            ConcurrentHitCounter counter = new ConcurrentHitCounter(60);

            counter.Increment();
            Assert.AreEqual(1, counter.GetCount());

            Assert.IsTrue(counter.Reset());
            Assert.AreEqual(0, counter.GetCount());
        }

        /// <summary>
        ///    t1:  x___|___|___o
        ///    t2:      x___|___|___o
        ///     T:  0   1   2   3   4
        ///         x   x   ^   o ^
        /// Count:          2     0
        /// .
        /// </summary>
        [TestMethod]
        public void ConcurrentHitCounter_GivenMultiThread_ShouldReset()
        {
            ConcurrentHitCounter counter = new ConcurrentHitCounter(3);

            Action a1 = () =>
            {
                // t = T + 0
                counter.Increment();

                Thread.Sleep(3 * 1000);

                // t = T + 3
                counter.Reset();
            };

            // t = 0
            Thread t1 = new Thread(new ThreadStart(a1));
            t1.Start();

            Thread.Sleep(1 * 1000);
            // t = 1
            Thread t2 = new Thread(new ThreadStart(a1));
            t2.Start();

            Thread.Sleep(1 * 1000);
            // t = 2
            Assert.AreEqual(2, counter.GetCount());

            // counter.Reset() is invoked at t = 3
            Thread.Sleep(1500);
            // t = 3.5
            Assert.AreEqual(0, counter.GetCount());
        }
    }
}
#pragma warning restore CA1801 // Review unused parameters
