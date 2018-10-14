namespace PecanDb.Storage.StorageSystems
{
    using PecanDB;

    public interface ISerializationFactory
    {
        IPecanLogger Logger { set; get; }

        TDocumentWithObject DeserializeObject<TDocumentWithObject>(object obj);

        object SerializeObject<TDocumentWithObject>(TDocumentWithObject obj, bool prettify = false);
    }
}