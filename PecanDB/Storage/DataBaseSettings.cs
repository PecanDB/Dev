namespace PecanDb.Storage
{
    using Akka.Actor;
    using Akka.Configuration;
    using PecanDb.Storage.Actors;
    using PecanDb.Storage.StorageSystems;
    using PecanDB;
    using System;
    using System.Reflection;

    public class DataBaseSettings
    {
        public static string DbStorageLocation;
        public TimeSpan MaxResponseTime = TimeSpan.FromSeconds(5);

        public DataBaseSettings(string databaseName, ISerializationFactory serializationFactory, IStorageIO fileio, string storageName, IPecanLogger logger, string baseDirectory)
        {
            if (databaseName == null)
                throw new ArgumentNullException(nameof(databaseName));

            this.FileIo = fileio ?? new StorageIO(logger);

            if (baseDirectory != null)
            {
                baseDirectory = baseDirectory.TrimEnd('/').TrimEnd('\\').TrimEnd('/').TrimEnd('\\');
                if (!this.FileIo.DirectoryExists(baseDirectory))
                {
                    this.FileIo.CreateDirectory(baseDirectory);
                }
            }

            DbStorageLocation = (string.IsNullOrEmpty(baseDirectory) ? AppDomain.CurrentDomain.BaseDirectory : baseDirectory) + "\\" + (string.IsNullOrEmpty(storageName) ? "pecan" : storageName);

            this.StorageMechanismMech = new JsonStorageMechanism(
                new FileStorageSystem(this, this.FileIo, serializationFactory ?? new JsonSerializationFactory(logger), logger),
                databaseName,
                DbStorageLocation,
                logger
            );
            Type pecanDbLoggerType = typeof(PecanDbLogger);
            PecanDbLogger.DefaultPecanLogger = logger;
            Config akkaConfig =
                $@"akka.loglevel = DEBUG
                    akka.loggers=[""{pecanDbLoggerType.FullName}, {pecanDbLoggerType.GetTypeInfo().Assembly.GetName().Name}""]";

            this.AppActorSystemystem = ActorSystem.Create("StorageActorSystem-" + Guid.NewGuid().ToString(), akkaConfig);
            this.StorageActor = this.AppActorSystemystem.ActorOf(Props.Create(() => new StorageActor(logger)), TypeOfWrapper.TypeOf(typeof(StorageActor)).Name);
            this.AkkaCommonStorageMechanism = new AkkaActorSystemMechanism(this.StorageActor, this.StorageMechanismMech, this, logger);
        }

        public IStorageIO FileIo { get; set; }

        public IStorageMechanism StorageMechanismMech { set; get; }

        public ActorSystem AppActorSystemystem { set; get; }

        public IActorRef StorageActor { set; get; }

        public IStorageMechanism AkkaCommonStorageMechanism { set; get; }

        public bool EnableCaching { get; set; }

        /// <summary>
        ///     Use with caution
        /// </summary>
        public bool EnableFasterCachingButWithLeakyUpdates { get; set; }

        public bool PrettifyDocuments { get; set; }

        public bool DontWaitForWrites { get; set; }

        public bool DontWaitForWritesOnCreate { get; set; }
    }
}