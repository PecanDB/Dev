namespace PecanDb.Storage.StorageSystems
{
    using PecanDB;
    using System.Collections.Generic;

    public interface IStorageIO
    {
        IPecanLogger Logger { set; get; }

        IEnumerable<string> DirectoryEnumerateFiles(string name);

        bool DirectoryExists(string db);

        bool FileExists(string db);

        void FileDelete(string db);

        void CreateDirectory(string db);

        string FileReadAllText(string fileName);

        void FileWriteAllText(string fileName, string content);
    }
}