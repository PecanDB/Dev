namespace PecanDb.Storage.Actors
{
    using Akka.Actor;
    using PecanDB;
    using System;

    public class StorageWriteActor : ReceiveActor
    {
        public StorageWriteActor(IPecanLogger logger)
        {
            this.Receive<Func<bool>>(
                message =>
                {
                    logger?.Trace(this.GetType().Name, $"{this.GetType().Name} Executing message {message?.GetType().Name}");

                    GlobalStatus.HasStaleResults = true;
                    GlobalStatus.LastWriteUtc = DateTime.UtcNow;
                    this.Sender.Tell(message());
                });
        }
    }
}