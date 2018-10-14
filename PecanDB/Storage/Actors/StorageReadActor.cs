namespace PecanDb.Storage.Actors
{
    using Akka.Actor;
    using PecanDB;
    using System;

    public class StorageReadActor : ReceiveActor
    {
        public StorageReadActor(IPecanLogger logger)
        {
            this.Receive<Func<object>>(
                message =>
                {
                    if (message == null)
                    {
                        logger?.Fatal(this.GetType().Name, $"Error null{this.GetType().Name} message {message?.GetType().Name}");
                    }
                    else
                    {
                        logger?.Trace(this.GetType().Name, $"{this.GetType().Name} Executing message {message?.GetType().Name}");
                        this.Sender.Tell(message());
                    }
                });
        }
    }
}