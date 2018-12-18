namespace PecanDB.AzureTableStorage.Tests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;

    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var store = new PecanDocumentStore(
                "PecanDBTest",
                new DatabaseOptions(true)
                {
                    StorageIO = new AzureTablesStorageIo(),
                    EnableFasterCachingButWithLeakyUpdates = false,
                    DontWaitForWrites = true,
                    EnableCaching = false
                });

            try
            {
                using (ISession session = store.OpenSession())
                {
                    var data = session.Load<TestClass>("boo2");
                }
            }
            catch (Exception e)
            {
            }

            using (ISession session = store.OpenSession())
            {
                string id = session.Save(
                    new TestClass
                    { Name = "yoyoyo" },
                    "boo2");
                session.SaveChanges();
            }
        }
    }
}