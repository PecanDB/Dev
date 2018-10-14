namespace PecanDB.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class backup_and_restore
    {
        [TestMethod]
        public void NOT_WORKING_it_should_be_able_to_backup_and_restore()
        {
            /*
                try
               {
                   var store = new PecanDocumentStore("PecanDBTest",  new DatabaseOptions(true));

                   Order order;
                   using (ISession session = store.OpenSession())
                   {
                       string orderId = session.Save(
                           new Order
                           {
                               Id = "yo be",
                               Name = "wooo"
                           });
                       order = session.Load<Order>(orderId);
                   }
                   Assert.AreEqual(order.Name, "wooo");

                   store.DeleteAllDatabases();

                   using (ISession session = store.OpenSession())
                   {
                       string orderId = session.Save(
                           new Order
                           {
                               Id = "yo be",
                               Name = "wooo"
                           });
                       order = session.Load<Order>(orderId);
                   }
                   Assert.AreEqual(order.Name, "wooo");
               }
               catch (Exception e)
               {
                   Console.WriteLine(e);
               }
                */
        }
    }
}