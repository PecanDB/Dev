namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Threading;

    [TestClass]
    public class when_pecan_db_is_used
    {
        //[TestMethod]
        //public void basic_example()
        //{
        //    Prop.ForAll<Order, bool>(
        //            (data, enableCaching) => { RunTest(enableCaching, data); })
        //        .QuickCheckThrowOnFailure();
        //}
        [TestMethod]
        public void basic_example_sample_no_inmemory()
        {
            // FileIOFileDB n=new FileIOFileDB();
            RunTest(true, new Order(), true);
        }

        [TestMethod]
        public void basic_example_sample()
        {
            // FileIOFileDB n=new FileIOFileDB();
            RunTest(true, new Order(), true);
        }

        private static void RunTest(bool enableCaching, Order data, bool useInMemoryStorage)
        {
            if (data == null)
                return;
            var store = new PecanDocumentStore(
                Guid.NewGuid().ToString(),
                useInMemoryStorage,
                new DatabaseOptions(enableCaching)
            );

            Order loadedData;
            Order queriedData;
            Order searchedData;
            Order queriedDocumentData;
            Order deletedData;
            string id;
            using (ISession session = store.OpenSession())
            {
                id = session.Save(data);
                Thread.Sleep(1000);
            }
            using (ISession session = store.OpenSession())
            {
                loadedData = session.Load<Order>(id);
            }
            using (ISession session = store.OpenSession())
            {
                queriedData = session.Query<Order>(orders => from order in orders where order.Name == data.Name select order).First();
            }
            using (ISession session = store.OpenSession())
            {
                searchedData = session.Search<Order>(search => search == data.Name).First();
            }
            using (ISession session = store.OpenSession())
            {
                queriedDocumentData = session.QueryDocument<Order>(documents => from document in documents where document.Id == id select document).First().DocumentEntity;
            }

            Order modifiedData;
            using (ISession session = store.OpenSession())
            {
                modifiedData = session.Load<Order>(id);
                //modify order id
                modifiedData.Id = Guid.NewGuid().ToString();
                session.SaveChanges();
            }

            using (ISession session = store.OpenSession())
            {
                Order result = session.Query<Order>(orders => from order in orders where order.Id == modifiedData.Id select order).First();
                Assert.AreEqual(modifiedData.Id, result.Id, "Unable to modify order id");
                var ord = session.Load<Order>(id);
                //swithc id back
                ord.Id = data.Id;
                session.SaveChanges();
                loadedData = session.Load<Order>(id);
            }

            string jsonData = JsonConvert.SerializeObject(data);
            string jsonLoadedData = JsonConvert.SerializeObject(loadedData);
            string jsonQueriedData = JsonConvert.SerializeObject(queriedData);
            string jsonSearchedData = JsonConvert.SerializeObject(searchedData);
            string jsonQueriedDocumentData = JsonConvert.SerializeObject(queriedDocumentData);

            using (ISession session = store.OpenSession())
            {
                session.Delete<Order>(id);
            }
            using (ISession session = store.OpenSession())
            {
                try
                {
                    session.Load<Order>(id);
                    Assert.Fail("tried to load a deleted data");
                }
                catch (Exception e)
                {
                }
            }
            //System.Threading.Thread.Sleep(500);
            using (ISession session = store.OpenSession())
            {
                deletedData = session.Load<Order>(id, null, true);
            }
            string jsonDeletedData = JsonConvert.SerializeObject(deletedData);

            using (ISession session = store.OpenSession())
            {
                session.DeleteForever<Order>(id);
            }

            using (ISession session = store.OpenSession())
            {
                try
                {
                    session.Load<Order>(id, null, true);
                    Assert.Fail("tried to load data already deleted forever");
                }
                catch (Exception e)
                {
                }
            }
            int count = 0;
            while (true)
                try
                {
                    Assert.AreEqual(jsonData, jsonLoadedData, "original data");
                    Assert.AreEqual(jsonData, jsonQueriedData, "query data");
                    Assert.AreEqual(jsonData, jsonSearchedData, "search");
                    Assert.AreEqual(jsonData, jsonQueriedDocumentData, "query document");
                    //Assert.AreEqual(jsonData, jsonDeletedData,"deleted");
                    break;
                }
                catch (Exception e)
                {
                    count++;
                    Thread.Sleep(300);
                    if (count > 10)
                        throw e;
                }
        }
    }
}