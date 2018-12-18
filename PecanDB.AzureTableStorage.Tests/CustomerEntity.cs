namespace PecanDB.AzureTableStorage.Tests
{
    using Microsoft.WindowsAzure.Storage.Table;

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