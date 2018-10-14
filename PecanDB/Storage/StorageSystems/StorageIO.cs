namespace PecanDb.Storage.StorageSystems
{
    using PecanDB;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class StorageIO : IStorageIO
    {
        public StorageIO(IPecanLogger logger)
        {
            this.Logger = logger;
        }

        public IPecanLogger Logger { get; set; }

        public IEnumerable<string> DirectoryEnumerateFiles(string name)
        {
            return Directory.EnumerateFiles(name).AsQueryable();
        }

        public bool DirectoryExists(string db)
        {
            return Directory.Exists(db);
        }

        public bool FileExists(string db)
        {
            return File.Exists(db);
        }

        public void FileDelete(string db)
        {
            File.Delete(db);
        }

        public void CreateDirectory(string db)
        {
            Directory.CreateDirectory(db);
        }

        public string FileReadAllText(string fileName)
        {
            try
            {
                return File.ReadAllText(fileName);
            }
            catch (Exception e)
            {
                //todo log fatal
                return string.Empty;
            }
        }

        public void FileWriteAllText(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }
    }
}