namespace PecanDB.AzureTableStorage.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using PecanDb.Storage.StorageSystems;

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var store = new PecanDocumentStore(
                "PecanDBTest",
                new DatabaseOptions(true)
                {
                    StorageIO = new AzureTablesStorageIO(),
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

    public class TestClass
    {
        public string Name { set; get; }
    }

    public class AzureTablesStorageIO : IStorageIO
    {
        public IEnumerable<string> DirectoryEnumerateFiles(string name)
        {
            CloudTable table = GetCloudTable();
            name = Path.GetFileNameWithoutExtension(name);
            // Construct the query operation for all customer entities where PartitionKey="Smith".
            TableQuery<CustomerEntity> query = new TableQuery<CustomerEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, name));

            // Print the fields for each customer.
            return table.ExecuteQuery(query).Select(x => x.PartitionKey + "\\" + x.RowKey);
        }

        public bool DirectoryExists(string db)
        {
            return true;
        }

        public bool FileExists(string db)
        {
            bool result = this.FileReadAllText(db) != null;
            return result;
        }

        public void FileDelete(string db)
        {
            CloudTable table = GetCloudTable();

            string dir = Path.GetDirectoryName(db);
            dir = Path.GetFileNameWithoutExtension(dir);
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(dir, Path.GetFileName(db));

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);
            // Assign the result to a CustomerEntity.
            var deleteEntity = (CustomerEntity)retrievedResult.Result;

            // Create the Delete TableOperation.
            if (deleteEntity != null)
            {
                TableOperation deleteOperation = TableOperation.Delete(deleteEntity);

                // Execute the operation.
                table.Execute(deleteOperation);

                Console.WriteLine("Entity deleted.");
            }
            else
            {
                Console.WriteLine("Could not retrieve the entity.");
            }
        }

        public void CreateDirectory(string db)
        {
        }

        public string FileReadAllText(string fileName)
        {
            CloudTable table = GetCloudTable();
            string dir = Path.GetDirectoryName(fileName);
            dir = Path.GetFileNameWithoutExtension(dir);
            // Create a retrieve operation that takes a customer entity.
            TableOperation retrieveOperation = TableOperation.Retrieve<CustomerEntity>(dir, Path.GetFileName(fileName));

            // Execute the retrieve operation.
            TableResult retrievedResult = table.Execute(retrieveOperation);

            return ((CustomerEntity)retrievedResult.Result)?.Json;
        }

        public void FileWriteAllText(string fileName, string content)
        {
            CloudTable table = GetCloudTable();
            string dir = Path.GetDirectoryName(fileName);
            dir = Path.GetFileNameWithoutExtension(dir);
            var data = new CustomerEntity(dir, Path.GetFileName(fileName));
            data.Json = content;
            // Create the TableOperation object that inserts the customer entity.
            TableOperation insertOperation = TableOperation.Insert(data);
            // Execute the insert operation.
            table.Execute(insertOperation);
        }

        public IPecanLogger Logger { get; set; }

        static CloudTable GetCloudTable()
        {
            // Retrieve the storage account from the connection string.
           
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("xxxxxxxxxxx");
              
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=mobilevideostorage;AccountKey=EM7KpcZ2Dujus/B9yx0FQzoYw7WVLZY/hbJKL/bW3cRO32bTku76Zihxk+CZZKMgJ1RHGfMwPGkYAxPXBI0moQ==;EndpointSuffix=core.windows.net");
              

            // Create the table client.
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            // Create the CloudTable object that represents the "people" table.
            CloudTable table = tableClient.GetTableReference("Test1");
            // Create the table if it doesn't exist.
            table.CreateIfNotExists();
            return table;
        }
    }

    public class CustomerEntity : TableEntity
    {
        public CustomerEntity(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }

        public CustomerEntity()
        {
        }

        public string Json { get; set; }
    }
}