namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class dirty_tests2
    {
        [TestMethod]
        public void savechanges()
        {
            var store = new PecanDocumentStore(
                "PecanDB",
                false,
                new DatabaseOptions
                {
                    PrettifyDocuments = true,
                    EnableCaching = true,
                    MaxResponseTime = TimeSpan.FromMinutes(1)
                });
            ISession session = store.OpenSession();

            foreach (PecanDocument<Order> order in session.QueryDocument<Order>(orders => from order in orders select order))
                session.DeleteForever<Order>(order.Id);

            string t = session.Save(
                new Order
                {
                    Id = "yo be",
                    Name = "wooo"
                });

            var m = session.Load<Order>(t);
            m.Name = "sam";
            session.SaveChanges(true);

            var m1 = store.OpenSession().Load<Order>(t);
            Assert.AreEqual("sam", m1.Name);

            m.Name = "sa";
            session.SaveChanges();
            var m2 = session.Load<Order>(t);
            Assert.AreEqual("sa", m2.Name);
            foreach (PecanDocument<Order> order in session.QueryDocument<Order>(orders => from order in orders select order))
                session.DeleteForever<Order>(order.Id);
        }

        [TestMethod]
        public void performance1()
        {
            var store = new PecanDocumentStore(
                "PecanDB",
                false,
                new DatabaseOptions
                {
                    PrettifyDocuments = true,
                    EnableCaching = true
                });
            ISession session = store.OpenSession();

            foreach (PecanDocument<Order> order in session.QueryDocument<Order>(orders => from order in orders select order))
                session.DeleteForever<Order>(order.Id);

            Parallel.For(
                1,
                100,
                i => session.Save(
                    new Order
                    {
                        Id = "orders/1000",
                        Name = "who"
                    }));

            Parallel.For(
                1,
                100,
                i =>
                {
                    List<Order> ordsw = session.Query<Order>(orders => from order in orders select order).ToList();
                });
            foreach (PecanDocument<Order> order in session.QueryDocument<Order>(orders => from order in orders select order))
                session.DeleteForever<Order>(order.Id);
        }

        [TestMethod]
        public void performance2()
        {
            var store = new PecanDocumentStore(
                "PecanDB",
                false,
                new DatabaseOptions
                {
                    PrettifyDocuments = true
                });
            ISession session = store.OpenSession();

            foreach (PecanDocument<Order> order in session.QueryDocument<Order>(orders => from order in orders select order))
                session.DeleteForever<Order>(order.Id);

            Parallel.For(
                1,
                100,
                i => session.Save(
                    new Order
                    {
                        Id = "orders/1000",
                        Name = "who"
                    }));

            Parallel.For(
                1,
                100,
                i =>
                {
                    List<Order> ordsw = session.Query<Order>(orders => from order in orders select order).ToList();
                });
            foreach (PecanDocument<Order> order in session.QueryDocument<Order>(orders => from order in orders select order))
                session.DeleteForever<Order>(order.Id);
        }

        [TestMethod]
        public void TestMethod1()
        {
            var store = new PecanDocumentStore(
                "PecanDB",
                false,
                new DatabaseOptions
                {
                    PrettifyDocuments = true,
                    EnableCaching = true
                });
            ISession session = store.OpenSession();
            /*
              var total = 10000;
             foreach (int i in Enumerable.Range(0, total))
             {
                 string id = session.StoreAndSave(new Order()
                 {
                     Id = "orders/1000",
                     Name = "who" + i
                 });
             }
             Parallel.For(1, total, i=> session.StoreAndSave(new Order()
             {
                 Id = "orders/1000",
                 Name = "who" + i
             }));
              */
            session.Save(
                new Order
                {
                    Id = "orders/1000",
                    Name = "who"
                });

            Parallel.For(
                1,
                10,
                i =>
                {
                    List<Order> ordsw3 = session.Query<Order>(orders => from order in orders select order).Take(2).ToList();
                    List<Order> ordsw = session.Query<Order>(orders => from order in orders select order).ToList();

                    IEnumerable<Order> ords = session.Query<Order>(orders => from order in orders select order);
                    List<Order> ord2 = session.Search<Order>(s => s.Contains("was10000")).ToList();
                    List<Order> ord3 = session.Search<Order>(s => s.Contains("was100")).ToList();
                    List<Order> ord4 = session.Search<Order>(s => s.Contains("w")).ToList();
                });

            Parallel.For(
                1,
                100,
                i =>
                {
                    List<Order> ordsw = session.Query<Order>(orders => from order in orders select order).ToList();
                });
        }
    }

    public class OrderNew
    {
        public string IdMan { get; set; }

        public string Name { get; set; }
    }

    public class Order
    {
        public string Id { get; set; }

        public string Name { get; set; }
    }
}