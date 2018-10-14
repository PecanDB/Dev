namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using PecanDB.Tests.Helpers;
    using System;
    using System.Linq;

    [TestClass]
    public class dirty_tests_various_documents
    {
        [TestMethod]
        public void test_saving_into_different_directories_with_just_query()
        {
            dynamic data1 = new { Data = Guid.NewGuid().ToString() };
            dynamic data2 = new { Data = Guid.NewGuid().ToString() };
            dynamic data3 = new { Data = Guid.NewGuid().ToString() };
            string t1 = "t1t1t1";
            string t2 = "t2t2t2";
            using (var store = new DocumentStoreInMemory(
                new DatabaseOptions
                {
                    //MaxResponseTime = TimeSpan.FromMinutes(3),
                    // EnableCaching = false,
                    // StorageDirectory = "D://!pecantest"
                }))
            {
                //using (ISession session = store.OpenSession())
                //{
                //    foreach (PecanDocument<dynamic> order in session.QueryDocument<dynamic>(orders => from order in orders select order))
                //        session.DeleteForever<dynamic>(order.Id);
                //    foreach (PecanDocument<dynamic> order in session.QueryDocument<dynamic>(orders => from order in orders select order, t1))
                //        session.DeleteForever<dynamic>(order.Id);
                //    foreach (PecanDocument<dynamic> order in session.QueryDocument<dynamic>(orders => from order in orders select order, t2))
                //        session.DeleteForever<dynamic>(order.Id);
                //}

                string h1;
                using (ISession session = store.OpenSession())
                {
                    h1 = session.Save(data1);
                }
                string h2;
                using (ISession session = store.OpenSession())
                {
                    h2 = session.Save(t1, data2);
                }
                string h3;
                using (ISession session = store.OpenSession())
                {
                    h3 = session.Save(t2, data3);
                }

                using (ISession session = store.OpenSession())
                {
                    TestHelperMethods.AssertAwait(
                        () =>
                        {
                            var h1data = session.Load<dynamic>(h1);
                            var list1 = session.Query<dynamic>((docs) => docs.Select(doc => doc)).Select(x => JsonConvert.SerializeObject(x)).ToList();
                            Assert.AreEqual(data1.Data.ToString(), h1data.Data.ToString());
                            Assert.AreEqual(list1.Count, 1);
                            Assert.AreEqual(JsonConvert.SerializeObject(data1), list1.First());
                        });
                }
                using (ISession session = store.OpenSession())
                {
                    TestHelperMethods.AssertAwait(
                        () =>
                        {
                            var h2data = session.Load<dynamic>(h2, t1);
                            var list2 = session.Query<dynamic>((docs) => docs.Select(doc => doc), t1).Select(x => JsonConvert.SerializeObject(x)).ToList();
                            Assert.AreEqual(data2.Data.ToString(), h2data.Data.ToString());
                            Assert.AreEqual(list2.Count, 1);
                            Assert.AreEqual(JsonConvert.SerializeObject(data2), list2.First());
                        });
                }
                using (ISession session = store.OpenSession())
                {
                    TestHelperMethods.AssertAwait(
                        () =>
                        {
                            var h3data = session.Load<dynamic>(h3, t2);
                            var list3 = session.Query<dynamic>((docs) => docs.Select(doc => doc), t2).Select(x => JsonConvert.SerializeObject(x)).ToList();
                            Assert.AreEqual(data3.Data.ToString(), h3data.Data.ToString());
                            Assert.AreEqual(list3.Count, 1);
                            Assert.AreEqual(JsonConvert.SerializeObject(data3), list3.First());
                        }, 100000);
                }
            }
        }

        [TestMethod]
        public void test_saving_into_different_directories_with_query_document()
        {
            dynamic data1 = new { Data = Guid.NewGuid().ToString() };
            dynamic data2 = new { Data = Guid.NewGuid().ToString() };
            dynamic data3 = new { Data = Guid.NewGuid().ToString() };
            string t1 = "t1t1t1";
            string t2 = "t2t2t2";

            using (var store = new DocumentStoreInMemory(
                new DatabaseOptions
                {
                    //MaxResponseTime = TimeSpan.FromMinutes(3),
                    //   EnableCaching = false
                }))
            {
                //using (ISession session = store.OpenSession())
                //{
                //    foreach (PecanDocument<dynamic> order in session.QueryDocument<dynamic>(orders => from order in orders select order))
                //        session.DeleteForever<dynamic>(order.Id);
                //    foreach (PecanDocument<dynamic> order in session.QueryDocument<dynamic>(orders => from order in orders select order,t1))
                //        session.DeleteForever<dynamic>(order.Id);
                //    foreach (PecanDocument<dynamic> order in session.QueryDocument<dynamic>(orders => from order in orders select order,t2))
                //        session.DeleteForever<dynamic>(order.Id);
                //}

                string h1;
                using (ISession session = store.OpenSession())
                {
                    h1 = session.Save(data1);
                }
                string h2;
                using (ISession session = store.OpenSession())
                {
                    h2 = session.Save(t1, data2);
                }
                string h3;
                using (ISession session = store.OpenSession())
                {
                    h3 = session.Save(t2, data3);
                }

                using (ISession session = store.OpenSession())
                {
                    TestHelperMethods.AssertAwait(
                        () =>
                        {
                            var h1data = session.Load<dynamic>(h1);
                            var list1 = session.QueryDocument<dynamic>((docs) => docs.Select(doc => doc)).Select(x => JsonConvert.SerializeObject(x)).ToList();
                            Assert.AreEqual(data1.Data.ToString(), h1data.Data.ToString());
                            Assert.AreEqual(list1.Count, 1);
                        });
                }
                using (ISession session = store.OpenSession())
                {
                    TestHelperMethods.AssertAwait(
                        () =>
                        {
                            var h2data = session.Load<dynamic>(h2, t1);
                            var list2 = session.QueryDocument<dynamic>((docs) => docs.Select(doc => doc), t1).Select(x => JsonConvert.SerializeObject(x)).ToList();
                            Assert.AreEqual(data2.Data.ToString(), h2data.Data.ToString());
                            Assert.AreEqual(list2.Count, 1);
                        });
                }
                using (ISession session = store.OpenSession())
                {
                    TestHelperMethods.AssertAwait(
                        () =>
                        {
                            var h3data = session.Load<dynamic>(h3, t2);
                            var list3 = session.QueryDocument<dynamic>((docs) => docs.Select(doc => doc), t2).Select(x => JsonConvert.SerializeObject(x)).ToList();
                            Assert.AreEqual(data3.Data.ToString(), h3data.Data.ToString());
                            Assert.AreEqual(list3.Count, 1);
                        });
                }
            }
        }
    }
}