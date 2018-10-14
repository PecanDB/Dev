namespace PecanDB
{
    using System;

    public interface IDocumentStore : IDisposable
    {
        IPecanLogger Logger { set; get; }
        string RemoteServerAdrress { get; set; }

        ISession OpenSession();

        bool ExportAllDatabases(string destDir = null);

        bool DeleteAllDatabases(bool backUpbeforeDelete = true, string destinationDir = null);

        void CleanDirectoryForBackUp(DataBaseParam param);

        DataBaseParam InitializeAndGetDataBaseParams(string destinationDir);
    }
}