namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class server_client_db_test
    {
        [TestMethod]
        public void client_should_be_able_to_call_server_over_http()
        {
            var store = new PecanDocumentStore(
                "PecanDBTest",
                new DatabaseOptions(true)
                {
                    RunAsServerWithAddress = "http://localhost:8018/",
                    UseRemoteServerAdrressForClientRequests = "http://localhost:8018/"
                });

            string orderId;
            using (ISession session = store.OpenSession())
            {
                orderId = session.Save(
                    new Order
                    {
                        Id = "yo be",
                        Name = "wooo"
                    });
            }

            using (ISession session = store.OpenSession())
            {
                var order = session.Load<Order>(orderId);
                Assert.AreEqual(order.Id, "yo be");
            }

            var store2 = new PecanDocumentStore(
                "PecanDBTest",
                new DatabaseOptions(true)
                {
                    UseRemoteServerAdrressForClientRequests = "http://localhost:8018/"
                });

            using (ISession session = store2.OpenSession())
            {
                var order = session.Load<Order>(orderId);
                Assert.AreEqual(order.Id, "yo be");
            }
        }

        [TestMethod]
        public void client_should_be_able_to_call_server_over_http_with_as()
        {
            var store = new PecanDocumentStore(
                "PecanDBTest",
                new DatabaseOptions(true)
                {
                    RunAsServerWithAddress = "http://localhost:8018/",
                    UseRemoteServerAdrressForClientRequests = "http://localhost:8018/"
                });

            string orderId;
            using (ISession session = store.OpenSession())
            {
                orderId = session.Save(
                   new Order
                   {
                       Id = "yo be",
                       Name = "wooo"
                   });
            }
            using (ISession session = store.OpenSession())
            {
                var order = session.LoadAs<Order>(orderId);
                Assert.AreEqual(order.Id, "yo be");
            }
            using (ISession session = store.OpenSession())
            {
                var order = session.LoadAs<OrderIsh>(orderId);
                Assert.AreEqual(order.Id, "yo be");
            }
        }
    }

    public class OrderIsh
    {
        public string Id { get; set; }
    }
}