namespace PecanDb.Storage
{
    using PecanDB;

    public class PecanDatabaseUtilityObj
    {
        public static string DetermineDatabaseName<TTDocumentWithObject, TTObjectOnly>(string documentName)
        {
            string dname = "";
            bool dataIsSomeObject = typeof(TTObjectOnly) == typeof(object);

            if (string.IsNullOrEmpty(documentName))
                if (dataIsSomeObject)
                    dname = TypeOfWrapper.TypeOf(typeof(TTDocumentWithObject)).Name;
                else
                    dname = TypeOfWrapper.TypeOf(typeof(TTObjectOnly)).Name;
            else
                dname = documentName;
            return dname;
        }
    }
}