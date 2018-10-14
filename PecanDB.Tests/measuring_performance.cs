namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PecanDB.Tests.Helpers;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class measuring_performance
    {
        private readonly int numberOfSamples = 3;
        private readonly int total = 1000;

        public void RunPerformanceTest<T>(bool isParallel, string description, Func<int, ISession, Action> opearation)
        {
            new List<bool>
            {
                true,
                false
            }.ForEach(
                dontWaitForWritesOnCreate =>
                {
                    for (int j = 0; j < this.numberOfSamples; j++)
                    {
                        var store = new PecanDocumentStore(
                            "PecanDB",
                            false,
                            new DatabaseOptions
                            {
                                EnableCaching = false,
                                DontWaitForWritesOnCreate = dontWaitForWritesOnCreate
                            });
                        ISession session = store.OpenSession();
                        try
                        {
                            foreach (PecanDocument<T> order in session.QueryDocument<T>(orders => from order in orders select order))
                                session.DeleteForever<T>(order.Id);
                        }
                        catch (Exception e)
                        {
                        }
                        // Create new stopwatch
                        var stopwatch = new Stopwatch();
                        var validationOperations = new ConcurrentQueue<Action>();
                        // Begin timing
                        stopwatch.Start();

                        if (isParallel)
                            Parallel.For(1, this.total, i => validationOperations.Enqueue(opearation(i, session)));
                        else
                            foreach (int i in Enumerable.Range(0, this.total))
                                validationOperations.Enqueue(opearation(i, session));
                        // Stop timing
                        stopwatch.Stop();
                        Console.Write($"{j} {(dontWaitForWritesOnCreate ? "" : "DONT WAIT FOR CREATE")} - Time elapsed: {stopwatch.Elapsed.TotalMilliseconds}ms : {description} with {this.total} iteration - RATE {this.total / (stopwatch.Elapsed.TotalMilliseconds / 1000)} op/sec");

                        var validationStopwatch = new Stopwatch();
                        validationStopwatch.Start();

                        Action validationOperation;
                        while (validationOperations.TryDequeue(out validationOperation))
                            if (validationOperation != null)
                                TestHelperMethods.AssertAwait(validationOperation);

                        validationStopwatch.Stop();
                        Console.WriteLine($"VALIDATION TIMING elapsed: {validationStopwatch.Elapsed.TotalMilliseconds}ms ");

                        try
                        {
                            foreach (PecanDocument<T> order in session.QueryDocument<T>(orders => from order in orders select order))
                                session.DeleteForever<T>(order.Id);
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
        }

        [TestMethod]
        public void performance_parallel_writes()
        {
            this.RunPerformanceTest<Order>(
                true,
                $"Paralell",
                (i, session) =>
                {
                    string id = session.Save(
                        new Order
                        {
                            Id = "orders/" + i,
                            Name = "who"
                        });

                    //return () =>
                    //{
                    //    var order = session.Load<Order>(id);
                    //    Assert.IsNotNull(order);
                    //    Assert.AreEqual(order.Id, "orders/" + i);
                    //};
                    return null;
                });
        }

        [TestMethod]
        public void performance_serial_writes()
        {
            this.RunPerformanceTest<Order>(
                false,
                $"Serial",
                (i, session) =>
                {
                    string id = session.Save(
                        new Order
                        {
                            Id = "orders/" + i,
                            Name = "who"
                        });
                    //return () =>
                    //{
                    //    var order = session.Load<Order>(id);
                    //    Assert.IsNotNull(order);
                    //    Assert.AreEqual(order.Id, "orders/" + i);
                    //};
                    return null;
                });
        }
    }
}