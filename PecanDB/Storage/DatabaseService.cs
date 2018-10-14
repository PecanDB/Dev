namespace PecanDb.Storage
{
    using Akka.Actor;
    using PecanDb.Storage.StorageSystems;
    using PecanDB;
    using System;
    using System.Collections.Concurrent;

    public class DatabaseService : IDisposable
    {
        private readonly ConcurrentDictionary<string, object> cache = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, object> cacheForTransactionScenarios = new ConcurrentDictionary<string, object>();
        private readonly IPecanLogger Logger;
        private readonly object padlock = new object();
        private readonly object padlock2 = new object();

        public DatabaseService(string databaseDirectory, string databaseName, ISerializationFactory serializationFactory, IStorageIO fileIo, IPecanLogger logger, bool useInMemoryStorage = false, string storageName = null)
        {
            this.Logger = logger;
            if (useInMemoryStorage)
            {
                fileIo = new InMemoryStorageIO(this.Logger);
            }
            this.DataBaseSettings = new DataBaseSettings(databaseName, serializationFactory, fileIo, storageName, logger, databaseDirectory);
            this.DirectDocumentManipulator = new DirectDocumentManipulator(this.DataBaseSettings.StorageMechanismMech, this, logger);
        }

        public DirectDocumentManipulator DirectDocumentManipulator { set; get; }

        public DataBaseSettings DataBaseSettings { set; get; }

        public void Dispose()
        {
            //dont do this
            // Task.Run(() => this.DataBaseSettings.system?.Terminate()).Wait();
        }

        [Obsolete("Please use with caution! Access to database outside the usual pipeline")]
        internal StorageDatabase<TDocumentWithObject, TObjectOnly> DatabaseAccessRegardlessOfTransaction<TDocumentWithObject, TObjectOnly>(IStorageMechanism storageMechanism, string directoryName)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            string key = TypeOfWrapper.TypeOf(typeof(StorageDatabase<TDocumentWithObject, TObjectOnly>)).FullName + "+" + directoryName;

            this.Logger?.Trace(this.GetType().Name, $"Accessing database storage outside the usual pipeline with key {key} for directory name {directoryName} with storage mechanism {storageMechanism?.GetType().Name}. Documment with object : {typeof(TDocumentWithObject).Name}, Object type only :  {typeof(TObjectOnly).Name}");

            if (!this.cacheForTransactionScenarios.ContainsKey(key))
                lock (this.padlock2)
                {
                    if (!this.cacheForTransactionScenarios.ContainsKey(key))
                    {
                        var val = new StorageDatabase<TDocumentWithObject, TObjectOnly>(
                            storageMechanism,
                            directoryName,
                            this,
                            this.Logger);
                        this.cacheForTransactionScenarios[key] = val;
                    }
                }
            return this.cacheForTransactionScenarios[key] as StorageDatabase<TDocumentWithObject, TObjectOnly>;
        }

        public StorageDatabase<TDocumentWithObject, TObjectOnly> Documents<TDocumentWithObject, TObjectOnly>(string directoryName = null)
            where TDocumentWithObject : IStandardDocument<TObjectOnly>
        {
            string key = TypeOfWrapper.TypeOf(typeof(StorageDatabase<TDocumentWithObject, TObjectOnly>)).FullName + "+" + directoryName;

            this.Logger?.Trace(this.GetType().Name, $"Accessing database storage with key {key} for directory name {directoryName} . Documment with object : {typeof(TDocumentWithObject).Name}, Object type only :  {typeof(TObjectOnly).Name}");

            if (!this.cache.ContainsKey(key))
                lock (this.padlock)
                {
                    if (!this.cache.ContainsKey(key))
                    {
                        var val = new StorageDatabase<TDocumentWithObject, TObjectOnly>(
                            this.DataBaseSettings.AkkaCommonStorageMechanism,
                            directoryName,
                            this,
                            this.Logger);

                        this.cache[key] = val;
                    }
                }
            return this.cache[key] as StorageDatabase<TDocumentWithObject, TObjectOnly>;
        }

        public bool Transaction
            <T1, T2, T3, T4, T5, T6, TObjectOnly>(
                Action<
                        StorageDatabase<T1, TObjectOnly>,
                        StorageDatabase<T2, TObjectOnly>,
                        StorageDatabase<T3, TObjectOnly>,
                        StorageDatabase<T4, TObjectOnly>,
                        StorageDatabase<T5, TObjectOnly>,
                        StorageDatabase<T6, TObjectOnly>
                    >
                    transactionOperation,
                string d1 = null,
                string d2 = null,
                string d3 = null,
                string d4 = null,
                string d5 = null,
                string d6 = null)
            where T1 : IStandardDocument<TObjectOnly>
            where T2 : IStandardDocument<TObjectOnly>
            where T3 : IStandardDocument<TObjectOnly>
            where T4 : IStandardDocument<TObjectOnly>
            where T5 : IStandardDocument<TObjectOnly>
            where T6 : IStandardDocument<TObjectOnly>
        {
            this.Logger?.Trace(this.GetType().Name, $"Transaction access. Object type only :  {typeof(TObjectOnly).Name} with {d1}:{typeof(T1).Name}, {d2}:{typeof(T2).Name},{d3}: {typeof(T3).Name}, {d4}:{typeof(T4).Name}, {d5}:{typeof(T5).Name}, {d6}:{typeof(T6).Name},");

            Func<bool> operation = () =>
            {
                transactionOperation(
                    this.DatabaseAccessRegardlessOfTransaction<T1, TObjectOnly>(this.DataBaseSettings.StorageMechanismMech, d1),
                    this.DatabaseAccessRegardlessOfTransaction<T2, TObjectOnly>(this.DataBaseSettings.StorageMechanismMech, d2),
                    this.DatabaseAccessRegardlessOfTransaction<T3, TObjectOnly>(this.DataBaseSettings.StorageMechanismMech, d3),
                    this.DatabaseAccessRegardlessOfTransaction<T4, TObjectOnly>(this.DataBaseSettings.StorageMechanismMech, d4),
                    this.DatabaseAccessRegardlessOfTransaction<T5, TObjectOnly>(this.DataBaseSettings.StorageMechanismMech, d5),
                    this.DatabaseAccessRegardlessOfTransaction<T6, TObjectOnly>(this.DataBaseSettings.StorageMechanismMech, d6)
                );
                return true;
            };
            object result = this.DataBaseSettings.StorageActor.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            return true;
        }

        public bool Transaction(Func<DirectDocumentManipulator, bool> transactionOperation)
        {
            this.Logger?.Trace(this.GetType().Name, $"Transaction access");

            Func<bool> operation = () =>
            {
                try
                {
                    return transactionOperation(this.DirectDocumentManipulator);
                }
                catch (Exception e)
                {
                    return false;
                }
            };
            object result = this.DataBaseSettings.StorageActor.Ask(operation, this.DataBaseSettings.MaxResponseTime).Result;
            return (bool)result;
        }
    }
}