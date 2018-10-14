namespace WPecanTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PecanDb.Storage;
    using PecanDB.Tests.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void load_test_with_nocaching()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(3);

            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            StorageDatabase<History<object>, object> History = DatabaseService.Documents<History<object>, object>();

            fin.LoadAll(true).ToList().ForEach(x => fin.DeleteForever(x.Id));
            History.LoadAll(true).ToList().ForEach(x => History.DeleteForever(x.Id));
            int count = 1000;
            for (int i = 0; i < count; i++)
                fin.Create(new Finance<object>());
            fin.Create(new Finance<object>(), true);
            TestHelperMethods.AssertAwait(
                () =>
                {
                    int ttt = fin.LoadAll(true).Count();
                    Assert.AreEqual(count + 1, ttt);
                });

            List<Finance<object>> x1 = fin.LoadAll(true).Take(10).ToList();
            List<Finance<object>> x2 = fin.LoadAll(true).Take(1000).ToList();
            List<Finance<object>> x3 = fin.LoadAll(true).Take(1000).ToList();

            IEnumerable<Finance<object>> tester = fin.LoadAll(
                finances =>
                    from finance in finances
                    where finance.ETag != null
                    select finance);

            fin.LoadAll(true).ToList().ForEach(x => fin.DeleteForever(x.Id));

            History.LoadAll(true).ToList().ForEach(x => History.DeleteForever(x.Id));
            //(3000);
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
        }

        [TestMethod]
        public void load_test_withcaching()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(3);
            DatabaseService.DataBaseSettings.EnableCaching = true;
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            StorageDatabase<History<object>, object> History = DatabaseService.Documents<History<object>, object>();

            fin.LoadAll(true).ToList().ForEach(x => fin.DeleteForever(x.Id));
            History.LoadAll(true).ToList().ForEach(x => History.DeleteForever(x.Id));
            int count = 1000;
            for (int i = 0; i < count; i++)
                fin.Create(new Finance<object>());
            fin.Create(new Finance<object>(), true);
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(count + 1, fin.LoadAll(true).Count()));

            List<Finance<object>> x1 = fin.LoadAll(true).Take(10).ToList();
            List<Finance<object>> x2 = fin.LoadAll(true).Take(1000).ToList();
            List<Finance<object>> x3 = fin.LoadAll(true).Take(1000).ToList();

            IEnumerable<Finance<object>> tester = fin.LoadAll(
                finances =>
                    from finance in finances
                    where finance.ETag != null
                    select finance);

            fin.LoadAll(true).ToList().ForEach(x => fin.DeleteForever(x.Id));

            History.LoadAll(true).ToList().ForEach(x => History.DeleteForever(x.Id));
            //(3000);
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
        }

        [TestMethod]
        public void transaction()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(10);
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            int total = 100;
            Task.Run(
                () =>
                {
                    DatabaseService.Transaction(
                        handle =>
                        {
                            for (int i = 0; i < total; i++)
                            {
                                Finance<object> ti = handle.GetDBRef<Finance<object>, object>().Create(new Finance<object>());
                            }
                            return true;
                        });
                });
            IEnumerable<Finance<object>> t = fin.LoadAll(true);
            TestHelperMethods.AssertAwait(1000);
            fin.Create(new Finance<object>());
            //(3000);
            Assert.AreEqual(total + 1, fin.LoadAll(true).Count());

            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Assert.AreEqual(0, fin.LoadAll(true).Count());
            Task task = Task.Run(
                () =>
                {
                    for (int i = 0; i < total; i++)
                    {
                        Finance<object> ti = fin.Create(new Finance<object>());
                    }
                });
            IEnumerable<Finance<object>> ttt = fin.LoadAll(true);
            TestHelperMethods.AssertAwait(1000);
            fin.Create(new Finance<object>());
            // Assert.AreNotEqual(total + 1, fin.LoadAll(true).Count());
            task.Wait();
            //(3000);

            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Assert.AreEqual(0, fin.LoadAll(true).Count());

            StorageDatabase<History<object>, object> his = DatabaseService.Documents<History<object>, object>();
            his.LoadAll(true).ToList().ForEach(
                x =>
                {
                    his.Delete(x.Id);
                    his.DeleteForever(x.Id);
                });
        }

        [TestMethod]
        public void transaction_with_caching()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.EnableCaching = true;
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(10);
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            int total = 100;
            Task.Run(
                () =>
                {
                    DatabaseService.Transaction(
                        handle =>
                        {
                            for (int i = 0; i < total; i++)
                            {
                                Finance<object> ti = handle.GetDBRef<Finance<object>, object>().Create(new Finance<object>());
                            }
                            return true;
                        });
                });
            IEnumerable<Finance<object>> t = fin.LoadAll(true);
            TestHelperMethods.AssertAwait(1000);
            fin.Create(new Finance<object>());
            //(3000);
            Assert.AreEqual(total + 1, fin.LoadAll(true).Count());

            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Assert.AreEqual(0, fin.LoadAll(true).Count());
            Task task = Task.Run(
                () =>
                {
                    for (int i = 0; i < total; i++)
                    {
                        Finance<object> ti = fin.Create(new Finance<object>());
                    }
                });
            IEnumerable<Finance<object>> ttt = fin.LoadAll(true);
            TestHelperMethods.AssertAwait(1000);
            fin.Create(new Finance<object>());
            //(3000);
            //   Assert.AreNotEqual(total + 1, fin.LoadAll(true).Count());

            task.Wait();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Assert.AreEqual(0, fin.LoadAll(true).Count());

            StorageDatabase<History<object>, object> his = DatabaseService.Documents<History<object>, object>();
            his.LoadAll(true).ToList().ForEach(
                x =>
                {
                    his.Delete(x.Id);
                    his.DeleteForever(x.Id);
                });
        }

        [TestMethod]
        public void TEST_serach()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.EnableCaching = true;
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            fin.LoadAll(true).ToList().ForEach(
                x => { fin.DeleteForever(x.Id); });

            fin.Create(
                new Finance<object>
                {
                    FileDescription = "yo",
                    FileName = "wow"
                });

            fin.Create(
                new Finance<object>
                {
                    FileDescription = "yay",
                    FileName = "way"
                });

            IEnumerable<Finance<object>> all = fin.LoadAll();
            Assert.AreEqual(2, all.Count());

            IEnumerable<Finance<object>> all2 = fin.Search(s => s.Contains("yay"));
            Assert.AreEqual(1, all2.Count());

            IEnumerable<Finance<object>> all3 = fin.Search(s => s.Contains("way"));
            Assert.AreEqual(1, all3.Count());

            IEnumerable<Finance<object>> all4 = fin.Search(s => s.Contains("y"));
            Assert.AreEqual(2, all4.Count());
        }

        [TestMethod]
        public void TEST_with_caching()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            for (int i = 0; i < 100; i++)
            {
                DatabaseService.DataBaseSettings.EnableCaching = true;
                StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
                StorageDatabase<History<object>, object> History = DatabaseService.Documents<History<object>, object>();

                fin.LoadAll(true).ToList().ForEach(
                    x =>
                    {
                        fin.Delete(x.Id);
                        fin.DeleteForever(x.Id);
                    });
                History.LoadAll(true).ToList().ForEach(
                    x =>
                    {
                        History.Delete(x.Id);
                        History.DeleteForever(x.Id);
                    });

                Finance<object> data = fin.Create(new Finance<object>());
                data.FileContent = "bla";
                fin.Update(data);
                History<object> h = History.Load(History.LoadAll(true).LastOrDefault().Id);
                data = fin.Load(data.Id);
                TestHelperMethods.AssertAwait(() => Assert.AreEqual(1, fin.LoadAll(true).Count()));
                fin.Delete(data.Id);
                h = History.Load(History.LoadAll(true).Last().Id);
                DbStats stats = SystemDbService.GetSystemStatistics(DatabaseService, x => x.DocumentName != typeof(History<object>).Name);

                fin.DeleteForever(data.Id);
                h = History.Load(History.LoadAll(true).LastOrDefault().Id);
                stats = SystemDbService.GetSystemStatistics(DatabaseService, x => x.DocumentName != typeof(History<object>).Name);

                TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            }
        }

        [TestMethod]
        public void TEST_no_caching()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(5);
            for (int i = 0; i < 100; i++)
            {
                StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
                StorageDatabase<History<object>, object> History = DatabaseService.Documents<History<object>, object>();

                fin.LoadAll(true).ToList().ForEach(
                    x =>
                    {
                        fin.Delete(x.Id);
                        fin.DeleteForever(x.Id);
                    });
                History.LoadAll(true).ToList().ForEach(
                    x =>
                    {
                        History.Delete(x.Id);
                        History.DeleteForever(x.Id);
                    });
                IEnumerable<Finance<object>> allx = fin.LoadAll(true);
                Finance<object> data = fin.Create(new Finance<object>());
                IEnumerable<Finance<object>> allk = fin.LoadAll(true);
                IEnumerable<Finance<object>> allhhjjw = fin.LoadAll(true);
                data.FileContent = "bla";
                IEnumerable<Finance<object>> allhhw = fin.LoadAll(true);
                fin.Update(data);
                IEnumerable<Finance<object>> allw = fin.LoadAll(true);
                History<object> h = History.Load(History.LoadAll(true).LastOrDefault().Id);
                data = fin.Load(data.Id);
                IEnumerable<Finance<object>> ally = fin.LoadAll(true);
                TestHelperMethods.AssertAwait(
                    () =>
                    {
                        IEnumerable<Finance<object>> all = fin.LoadAll(true);
                        Assert.AreEqual(1, all.Count());
                    });
                fin.Delete(data.Id);
                h = History.Load(History.LoadAll(true).Last().Id);
                DbStats stats = SystemDbService.GetSystemStatistics(DatabaseService, x => x.DocumentName != typeof(History<object>).Name);

                fin.DeleteForever(data.Id);
                h = History.Load(History.LoadAll(true).LastOrDefault().Id);
                stats = SystemDbService.GetSystemStatistics(DatabaseService, x => x.DocumentName != typeof(History<object>).Name);

                TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            StorageDatabase<History<object>, object> History = DatabaseService.Documents<History<object>, object>();

            IEnumerable<History<object>> h = History.LoadAll(true);

            IEnumerable<Finance<object>> f = fin.LoadAll(true);
            Finance<object> mod = fin.Create(
                new Finance<object>
                {
                    FileName = "shdlkhslkdhlksd"
                });

            h = History.LoadAll(true);

            IEnumerable<Finance<object>> f1 = fin.LoadAll();
            DbStats t = SystemDbService.GetSystemStatistics(DatabaseService);
            DbStats q = SystemDbService.GetSystemStatistics(DatabaseService, x => x.LastOperation == "CREATE");
            foreach (Finance<object> id in f1)
            {
                Finance<object> d = fin.Load(id.Id);
                fin.Delete(id.Id);
                fin.DeleteForever(id.Id);
            }
            h = History.LoadAll(true);
            f = fin.LoadAll(true);

            IEnumerable<History<object>> h1 = History.LoadAll();
            foreach (History<object> id in h1)
            {
                History<object> d = History.Load(id.Id);
                History.Delete(id.Id);
                History.DeleteForever(id.Id);
            }
            h = History.LoadAll(true);
            f = fin.LoadAll(true);
            t = SystemDbService.GetSystemStatistics(DatabaseService);
        }

        [TestMethod]
        public void load_test_withcaching_inmemory()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates = true;
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(10);
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            int total = 100;
            Task.Run(
                () =>
                {
                    DatabaseService.Transaction(
                        handle =>
                        {
                            for (int i = 0; i < total; i++)
                            {
                                Finance<object> ti = handle.GetDBRef<Finance<object>, object>().Create(new Finance<object>());
                            }
                            return true;
                        });
                });
            IEnumerable<Finance<object>> t = fin.LoadAll(true);
            TestHelperMethods.AssertAwait(1000);
            fin.Create(new Finance<object>());
            //(3000);
            Assert.AreEqual(total + 1, fin.LoadAll(true).Count());

            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Assert.AreEqual(0, fin.LoadAll(true).Count());
            Task task = Task.Run(
                () =>
                {
                    for (int i = 0; i < total; i++)
                    {
                        Finance<object> ti = fin.Create(new Finance<object>());
                    }
                });
            IEnumerable<Finance<object>> ttt = fin.LoadAll(true);
            TestHelperMethods.AssertAwait(1000);
            fin.Create(new Finance<object>());
            //(3000);
            // Assert.AreNotEqual(total + 1, fin.LoadAll(true).Count());

            task.Wait();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Assert.AreEqual(0, fin.LoadAll(true).Count());

            StorageDatabase<History<object>, object> his = DatabaseService.Documents<History<object>, object>();
            his.LoadAll(true).ToList().ForEach(
                x =>
                {
                    his.Delete(x.Id);
                    his.DeleteForever(x.Id);
                });
        }

        [TestMethod]
        public void transaction_with_caching_inmemory()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            DatabaseService.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates = true;
            DatabaseService.DataBaseSettings.MaxResponseTime = TimeSpan.FromMinutes(10);
            StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            int total = 100;
            Task taski = Task.Run(
                () =>
                {
                    DatabaseService.Transaction(
                        handle =>
                        {
                            for (int i = 0; i < total; i++)
                            {
                                Finance<object> ti = handle.GetDBRef<Finance<object>, object>().Create(new Finance<object>());
                            }
                            return true;
                        });
                });
            IEnumerable<Finance<object>> t = fin.LoadAll(true);
            taski.Wait(); //rem
            fin.Create(new Finance<object>());
            //(3000);
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(total + 1, fin.LoadAll(true).Count()));

            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            Task task = Task.Run(
                () =>
                {
                    for (int i = 0; i < total; i++)
                    {
                        Finance<object> ti = fin.Create(new Finance<object>());
                    }
                });
            fin.Create(new Finance<object>());
            //(1000);
            TestHelperMethods.AssertAwait(() => Assert.AreNotEqual(total + 1, fin.LoadAll(true).Count()));

            task.Wait();
            fin.LoadAll(true).ToList().ForEach(
                x =>
                {
                    fin.Delete(x.Id);
                    fin.DeleteForever(x.Id);
                });
            StorageDatabase<History<object>, object> his = DatabaseService.Documents<History<object>, object>();
            his.LoadAll(true).ToList().ForEach(
                x =>
                {
                    his.Delete(x.Id);
                    his.DeleteForever(x.Id);
                });
            TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
        }

        [TestMethod]
        public void TEST_with_caching_inmemory()
        {
            var DatabaseService = new DatabaseService(null, "TestDB" + Guid.NewGuid().ToString(), null, null, null);
            for (int i = 0; i < 100; i++)
            {
                DatabaseService.DataBaseSettings.EnableFasterCachingButWithLeakyUpdates = true;
                StorageDatabase<Finance<object>, object> fin = DatabaseService.Documents<Finance<object>, object>();
                StorageDatabase<History<object>, object> History = DatabaseService.Documents<History<object>, object>();

                fin.LoadAll(true).ToList().ForEach(
                    x =>
                    {
                        fin.Delete(x.Id);
                        fin.DeleteForever(x.Id);
                    });
                History.LoadAll(true).ToList().ForEach(
                    x =>
                    {
                        History.Delete(x.Id);
                        History.DeleteForever(x.Id);
                    });

                Finance<object> data = fin.Create(new Finance<object>());
                data.FileContent = "bla";
                fin.Update(data);
                History<object> h = History.Load(History.LoadAll(true).LastOrDefault().Id);
                data = fin.Load(data.Id);
                TestHelperMethods.AssertAwait(() => Assert.AreEqual(1, fin.LoadAll(true).Count()));
                fin.Delete(data.Id);
                h = History.Load(History.LoadAll(true).Last().Id);
                DbStats stats = SystemDbService.GetSystemStatistics(DatabaseService, x => x.DocumentName != typeof(History<object>).Name);

                fin.DeleteForever(data.Id);
                h = History.Load(History.LoadAll(true).LastOrDefault().Id);
                stats = SystemDbService.GetSystemStatistics(DatabaseService, x => x.DocumentName != typeof(History<object>).Name);

                TestHelperMethods.AssertAwait(() => Assert.AreEqual(0, fin.LoadAll(true).Count()));
            }
        }
    }
}