namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using PecanDB.Logger;
    using PecanDB.Tests.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class dirty_tests
    {
        // [TestMethod]
        public void delete()
        {
            using (var store = new PecanDocumentStore("tester", null))
            {
                string lastId = "";
                using (ISession session = store.OpenSession())
                {
                    List<FilesStorage> t = session.SearchFiles(s => s.Contains("me")).ToList();
                    string m = session.SaveFile(@"C:\Users\Sa\Desktop\discard\www.saptarang.org\premium\html\onevent\img\icons\me.jpg");
                }
            }
        }

        [TestMethod]
        public void canUseNewStorageName()
        {
            var st = new PecanDocumentStore("tester", new DatabaseOptions()
            {
                StorageName = "tsn_" + Guid.NewGuid().ToString()
            });
            using (var store = st)
            {
                string lastId = "";
                using (ISession session = store.OpenSession())
                {
                    session.Save(new Order(), "tsn");
                }
            }

            using (var store = st)
            {
                string lastId = "";
                using (ISession session = store.OpenSession())
                {
                    session.Load<Order>("tsn");
                }
            }
            using (var store = st)
            {
                string lastId = "";
                using (ISession session = store.OpenSession())
                {
                    try
                    {
                        session.Load<Order>("tsn");
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void test_speed()
        {
            const int Total = 300;
            using (var store = new DocumentStoreInMemory(
                new DatabaseOptions
                {
                    MaxResponseTime = TimeSpan.FromSeconds(3)
                }))
            {
                string lastId = "";
                using (ISession session = store.OpenSession())
                {
                    foreach (int i in Enumerable.Range(0, Total))
                        lastId = session.Save(new Order());
                }
                using (ISession session = store.OpenSession())
                {
                    foreach (int i in Enumerable.Range(0, Total))
                        session.Load<Order>(lastId);
                }
                using (ISession session = store.OpenSession())
                {
                    foreach (int i in Enumerable.Range(0, Total))
                    {
                        IEnumerable<PecanDocument<Order>> result = session.QueryDocument<Order>(x => x.Where(y => y.Id == lastId));
                    }
                }
                using (ISession session = store.OpenSession())
                {
                    foreach (int i in Enumerable.Range(0, Total))
                    {
                        List<PecanDocument<Order>> result = session.QueryDocument<Order>(x => x.Where(y => y.Id == lastId)).ToList();
                    }
                }
            }
        }

        [TestMethod]
        public void it_can_save_object_into_custom_document_names()
        {
            CheckThatPecanCanSaveAndRetrieveSimpleData(1, 2);
            CheckThatPecanCanSaveAndRetrieveSimpleData(0, 00);
            CheckThatPecanCanSaveAndRetrieveSimpleData(100, 200);
            CheckThatPecanCanSaveAndRetrieveSimpleData("2", "8");
            CheckThatPecanCanSaveAndRetrieveSimpleData("hello", "hi");
            CheckThatPecanCanSaveAndRetrieveSimpleData(true, false);
            CheckThatPecanCanSaveAndRetrieveSimpleData(false, true);
            CheckThatPecanCanSaveAndRetrieveSimpleData(12.6, 1.1);
            CheckThatPecanCanSaveAndRetrieveSimpleData(new Tuple<string, int>("y", 567), new Tuple<string, int>("yes", 5667));
            CheckThatPecanCanSaveAndRetrieveSimpleData(
                new List<bool>
                {
                    true,
                    false
                },
                new List<bool>
                {
                    false,
                    true
                });
            CheckThatPecanCanSaveAndRetrieveSimpleData(
                new Dictionary<string, string>
                {
                    { "hello", "hi" }
                },
                new Dictionary<string, string>
                {
                    { "hello1", "hi1" }
                });
            CheckThatPecanCanSaveAndRetrieveSimpleData(
                new Order
                {
                    Id = "12344",
                    Name = "sam"
                },
                new Order
                {
                    Id = "12djhf4",
                    Name = "samuel"
                });

            // CheckThatPecanCanSaveAndRetrieveSimpleData(new { id=100 }, new {id=200});
        }

        private static void CheckThatPecanCanSaveAndRetrieveSimpleData<T>(T data, T updateData)
        {
            CheckThatPecanCanSaveAndRetrieveSimpleDataWithPredefinedId(data, updateData);
            string DocumentName = Guid.NewGuid().ToString();
            using (var store = new PecanDocumentStore(
                "TestDB2",

                new DatabaseOptions
                //using (var store = new DocumentStoreInMemory(new DatabaseOptions()
                {
                    MaxResponseTime = TimeSpan.FromMinutes(10)
                }))
            {
                CleanDB<T>(store, DocumentName);
                string id;
                string id2;
                using (ISession session = store.OpenSession())
                {
                    id = session.Save(DocumentName, data);
                    id2 = session.Save(data);
                }
                T savedData;
                T savedData2;
                using (ISession session = store.OpenSession())
                {
                    try
                    {
                        savedData = session.Load<T>(id);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }
                    savedData2 = session.Load<T>(id2);
                }
                AssertAreExactlyEqualByValue(data, savedData2);
                using (ISession session = store.OpenSession())
                {
                    try
                    {
                        savedData = session.Load<T>(id2);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    savedData = session.Load<T>(id, DocumentName);
                }
                AssertAreExactlyEqualByValue(data, savedData);

                //todo fix why save changes doesnt work when load document and doc is replaced
            }
        }

        private static void CleanDB<T>(PecanDocumentStore store, string DocumentName)
        {
            using (ISession session = store.OpenSession())
            {
                foreach (PecanDocument<T> x in session.QueryDocument<T>(x => x, DocumentName))
                    try
                    {
                        session.DeleteForever<T>(x.Id, DocumentName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                foreach (PecanDocument<T> x in session.QueryDocument<T>(x => x))
                    try
                    {
                        session.DeleteForever<T>(x.Id);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }
        }

        private static void CheckThatPecanCanSaveAndRetrieveSimpleDataWithPredefinedId<T>(T data, T updateData)
        {
            string DocumentName = Guid.NewGuid().ToString();
            using (var store = new PecanDocumentStore(
                "TestDB",

                new DatabaseOptions
                {
                    MaxResponseTime = TimeSpan.FromMinutes(10)
                }))
            {
                CleanDB<T>(store, DocumentName);
                string id = "id";
                string id2 = "ids";
                using (ISession session = store.OpenSession())
                {
                    session.Save(DocumentName, data, id);
                    session.Save(data, id2);
                }
                T savedData;
                T savedData2;
                using (ISession session = store.OpenSession())
                {
                    try
                    {
                        savedData = session.Load<T>(id);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }
                    savedData2 = session.Load<T>(id2);
                }
                AssertAreExactlyEqualByValue(data, savedData2);
                using (ISession session = store.OpenSession())
                {
                    try
                    {
                        savedData = session.Load<T>(id2);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    savedData = session.Load<T>(id, DocumentName);
                }
                AssertAreExactlyEqualByValue(data, savedData);
                using (ISession session = store.OpenSession())
                {
                    try
                    {
                        session.Save(DocumentName, data, id);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }
                    try
                    {
                        session.Save(data, id2);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }
                }
                //todo fix why save changes doesnt work when load document and doc is replaced
            }
        }

        [TestMethod]
        public void test()
        {
            using (var store = new DocumentStoreInMemory())
            {
                using (ISession session = store.OpenSession())
                {
                }
                using (ISession session = store.OpenSession())
                {
                }
            }
        }

        [TestMethod]
        public void create_multiple_ids()
        {
            var logger = new DefaultPecanLogger(false); // new DefaultPecanLogger( true,"D:\\!MUST_TRASH.txt",null,  TimeSpan.FromSeconds(30));

            new List<bool>
            {
                true,
                false
            }.ForEach(
                cache =>
                {
                    var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(cache) { Logger = logger });
                    ISession session = store.OpenSession();
                    foreach (int i1 in Enumerable.Range(0, 10))
                    {
                        logger.Trace($"Test {i1}", $"ON {i1} ord1");
                        string orderId1 = session.Save(
                            "doc1",
                            new
                            {
                                Id = "yo be",
                                Name = "doc1"
                            });
                        logger.Trace($"Test {i1}", $"ON {i1} ord2");
                        string orderId2 = session.Save(
                            "doc2",
                            new
                            {
                                Id = "yo be",
                                Name = "doc2"
                            });
                        logger.Trace($"Test {i1}", $"ON {i1} DONE");
                    }
                });
        }

        [TestMethod]
        public void dynamic_document_test()
        {
            var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(true));
            ISession session = store.OpenSession();
            string orderId1 = session.Save(
                "doc1",
                new
                {
                    Id = "yo be",
                    Name = "doc1"
                });
            string orderId2 = session.Save(
                "doc2",
                new
                {
                    Id = "yo be",
                    Name = "doc2"
                });

            TestHelperMethods.AssertAwait(
                () =>
                {
                    var order1 = session.Load<dynamic>(orderId1, "doc1");
                    Assert.AreEqual(order1.Name.ToString(), "doc1");

                    var order2 = session.Load<dynamic>(orderId2, "doc2");
                    Assert.AreEqual(order2.Name.ToString(), "doc2");

                    dynamic order11 = session.Load(orderId1, "doc1");
                    Assert.AreEqual(order11.Name.ToString(), "doc1");

                    dynamic order22 = session.Load(orderId2, "doc2");
                    Assert.AreEqual(order22.Name.ToString(), "doc2");
                    try
                    {
                        var order5 = session.Load<dynamic>(orderId1);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    try
                    {
                        var order6 = session.Load<dynamic>(orderId2);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    try
                    {
                        dynamic order7 = session.Load(orderId1);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    try
                    {
                        dynamic order8 = session.Load(orderId2);
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    try
                    {
                        var order3 = session.Load<dynamic>(orderId1, "doc2");
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }

                    try
                    {
                        var order4 = session.Load<dynamic>(orderId2, "doc1");
                        Assert.Fail();
                    }
                    catch (Exception e)
                    {
                    }
                }
            );
        }

        [TestMethod]
        public void dynamic_test()
        {
            new List<bool>
            {
                true,
                false
            }.ForEach(
                cache =>
                {
                    var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(cache));
                    ISession session = store.OpenSession();
                    foreach (int i1 in Enumerable.Range(0, 10))
                    {
                        string orderId = session.Save(
                            new
                            {
                                Id = "yo be",
                                Name = i1
                            });
                        string orderId2 = session.Save(
                            new object());
                        dynamic order = null;
                        TestHelperMethods.AssertAwait(
                            () =>
                            {
                                order = session.Load<dynamic>(orderId);
                                Assert.AreEqual(order.Name.ToString(), i1.ToString());

                                Order oo = session.LoadAs<dynamic, Order>(orderId);
                                Assert.AreEqual(oo.Name, i1.ToString());

                                Order oo2 = session.LoadAs<dynamic, Order>(orderId2);

                                var oo3 = session.LoadAs<Order>(orderId2);
                                dynamic oo4 = session.Load(orderId2);
                            });

                        order.Name = "sam";
                        session.SaveChanges(true);
                        var o = session.Load<object>(orderId);
                        dynamic order2 = o;
                        Assert.AreEqual(order2.Name.ToString(), "sam");
                        order2.Name = "sowhat";
                        Assert.AreEqual(order2.Name.ToString(), order.Name.ToString());
                        List<dynamic> result = session.Query<object>(all => from every in all select every).ToList();
                    }
                });
        }

        [TestMethod]
        public void buggy_using_single_file()
        {
            var store = new PecanDocumentStore(
                "PecanDBTest",

                new DatabaseOptions(true)
                {
                    //no ready yet
                    //UseSingleFile = true
                });
            ISession session = store.OpenSession();
            foreach (int i1 in Enumerable.Range(0, 100))
            {
                string orderId = session.Save(
                    new Order
                    {
                        Id = "yo be",
                        Name = "wooo"
                    });
                var order = session.Load<Order>(orderId);
                Assert.AreEqual(order.Name, "wooo");
                order.Name = "sam";
                session.SaveChanges(true);
                var order2 = session.Load<Order>(orderId);
                Assert.AreEqual(order2.Name, "sam");
                order2.Name = "sowhat";
                Assert.AreEqual(order2.Name, order.Name);
                List<Order> result = session.Query<Order>(all => from every in all select every).ToList();
            }
        }

        [TestMethod]
        public void it_should_save_dynamic_and_load_json()
        {
            var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(true));
            ISession session = store.OpenSession();
            foreach (int i1 in Enumerable.Range(0, 100))
            {
                string orderId = session.Save(
                    new
                    {
                        Id = "yo be",
                        Name = "wooo"
                    });
                dynamic order = session.Load(orderId);
                Assert.AreEqual(order.Name.ToString(), "wooo");

                string orderJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<PecanDocument<Order>>(session.LoadRawDocument(orderId)).DocumentEntity);
                dynamic orderJsonExisting = JsonConvert.SerializeObject(order);
                Assert.AreEqual(orderJsonExisting, orderJson);
            }
        }

        [TestMethod]
        public void it_should_save_dynamic_and_load_json2()
        {
            var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(true));
            ISession session = store.OpenSession();
            foreach (int i1 in Enumerable.Range(0, 100))
            {
                string orderId = session.Save(
                    new Order
                    {
                        Id = "yo be",
                        Name = "wooo"
                    });
                var order = session.Load<Order>(orderId);
                Assert.AreEqual(order.Name, "wooo");

                string orderJson = JsonConvert.SerializeObject(JsonConvert.DeserializeObject<PecanDocument<Order>>(session.LoadRawDocument<Order>(orderId)).DocumentEntity);
                string orderJsonExisting = JsonConvert.SerializeObject(order);
                Assert.AreEqual(orderJsonExisting, orderJson);
            }
        }

        [TestMethod]
        public void load_type_as_another_type()
        {
            var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(true));
            ISession session = store.OpenSession();
            foreach (int i1 in Enumerable.Range(0, 100))
            {
                string orderId = session.Save(
                    new Order
                    {
                        Id = "yo be",
                        Name = "wooo"
                    });
                var order = session.Load<Order>(orderId);
                OrderNew order2 = session.LoadAs<Order, OrderNew>(orderId);
                Assert.AreEqual(order.Name, order2.Name);
            }
        }

        [TestMethod]
        public void it_should_perform_basic_operations()
        {
            var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(true));
            ISession session = store.OpenSession();
            foreach (int i1 in Enumerable.Range(0, 100))
            {
                string orderId = session.Save(
                    new Order
                    {
                        Id = "yo be",
                        Name = "wooo"
                    });
                var order = session.Load<Order>(orderId);
                Assert.AreEqual(order.Name, "wooo");
                order.Name = "sam";
                session.SaveChanges(true);
                var order2 = session.Load<Order>(orderId);
                Assert.AreEqual(order2.Name, "sam");
                order2.Name = "sowhat";
                Assert.AreEqual(order2.Name, order.Name);
                List<Order> result = session.Query<Order>(all => from every in all select every).ToList();
            }
        }

        [TestMethod]
        public void it_should_perform_basic_operations2()
        {
            int total = 1000;
            var store = new PecanDocumentStore("PecanDBTest", new DatabaseOptions(true));
            ISession session = store.OpenSession();
            Task task = Task.Run(
                () =>
                {
                    foreach (int i1 in Enumerable.Range(0, total))
                    {
                        List<Order> result = session.Query<Order>(all => from every in all select every).ToList();
                    }
                });
            foreach (int i1 in Enumerable.Range(0, total))
            {
                string orderId = session.Save(
                    new Order
                    {
                        Name = "wooo"
                    });
                var order = session.Load<Order>(orderId);
                session.SaveChanges(true);
            }
            // task.Wait();
        }

        public static void AssertAreExactlyEqualByValue(object a, object b)
        {
            Assert.AreEqual(JsonConvert.SerializeObject(a), JsonConvert.SerializeObject(b));
        }

        public static void AssertAreNotExactlyEqualByValue(object a, object b)
        {
            Assert.AreNotEqual(JsonConvert.SerializeObject(a), JsonConvert.SerializeObject(b));
        }
    }
}