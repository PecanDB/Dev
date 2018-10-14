namespace PecanDb.Storage.StorageSystems
{
    using Newtonsoft.Json;
    using PecanDB;

    public class JsonSerializationFactory : ISerializationFactory
    {
        public JsonSerializationFactory(IPecanLogger logger)
        {
            this.Logger = logger;
        }

        public IPecanLogger Logger { get; set; }

        public TDocumentWithObject DeserializeObject<TDocumentWithObject>(object obj)
        {
            if (typeof(TDocumentWithObject) == typeof(string))
                return (TDocumentWithObject)obj;
            return JsonConvert.DeserializeObject<TDocumentWithObject>(obj.ToString());
        }

        public object SerializeObject<TDocumentWithObject>(TDocumentWithObject obj, bool prettify = false)
        {
            return prettify ? JsonConvert.SerializeObject(obj, Formatting.Indented) : JsonConvert.SerializeObject(obj);
        }
    }
}