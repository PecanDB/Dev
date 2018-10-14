namespace PecanDB
{
    using System;
    using System.Collections.Generic;

    public interface ISession : IDisposable
    {
        Guid SessionId { set; get; }

        IPecanLogger Logger { set; get; }

        string SaveFile(string filePath, string destinationFolderForFiles = null);

        FilesStorage UpdateFile(string fileId, string updateOriginalPath, string destinationFolderForFiles = null);

        IEnumerable<FilesStorage> QueryFiles(Func<IEnumerable<FilesStorage>, IEnumerable<FilesStorage>> query = null);

        IEnumerable<FilesStorage> SearchFiles(Predicate<string> predicate, Func<IEnumerable<FilesStorage>, IEnumerable<FilesStorage>> query = null);

        FilesStorage LoadFile(string id);

        /// <summary>
        ///     Load from a table T with tracking
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="documentName"></param>
        /// <param name="includeDeleted"></param>
        /// <returns></returns>
        T Load<T>(string id, string documentName = null, bool includeDeleted = false);

        /// <summary>
        ///     Load from a table T as TAs
        ///     There will be no tracking
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TAs"></typeparam>
        /// <param name="id"></param>
        /// <param name="documentName"></param>
        /// <param name="includeDeleted"></param>
        /// <returns></returns>
        TAs LoadAs<T, TAs>(string id, string documentName = null, bool includeDeleted = false);

        /// <summary>
        ///     Load from a dynamic table as TAs
        ///     There would be no tracking!
        /// </summary>
        /// <typeparam name="TAs"></typeparam>
        /// <param name="id"></param>
        /// <param name="documentName"></param>
        /// <param name="includeDeleted"></param>
        /// <returns></returns>
        TAs LoadAs<TAs>(string id, string documentName = null, bool includeDeleted = false);

        /// <summary>
        ///     Load from a dynamic table
        /// </summary>
        /// <param name="id"></param>
        /// <param name="documentName"></param>
        /// <param name="includeDeleted"></param>
        /// <returns></returns>
        dynamic Load(string id, string documentName = null, bool includeDeleted = false);

        string LoadRawDocument<T>(string id, string documentName = null, bool includeDeleted = false);

        string LoadRawDocument(string id, string documentName = null, bool includeDeleted = false);

        string Save<T>(string documentName, T document, string id = null);

        string Save<T>(T document, string id);

        string Save<T>(T document);

        bool SaveChanges(bool saveWithEtag = false);

        string GetETagFor<T>(string id, string documentName = null);

        IEnumerable<T> Search<T>(Predicate<string> predicate, Func<IEnumerable<T>, IEnumerable<T>> query = null, string documentName = null);

        void DeleteForever<T>(string id, string documentName = null);

        void Delete<T>(string id, string documentName = null);

        IEnumerable<T> Query<T>(Func<IEnumerable<T>, IEnumerable<T>> query, string documentName = null);

        IEnumerable<PecanDocument<T>> QueryDocument<T>(Func<IEnumerable<PecanDocument<T>>, IEnumerable<PecanDocument<T>>> query, string documentName = null);
    }
}