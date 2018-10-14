namespace PecanDb.Storage.Actors
{
    using Akka.Actor;
    using Akka.Routing;
    using PecanDB;
    using System;

    public class StorageActor : ReceiveActor
    {
        private readonly IPecanLogger Logger;

        public StorageActor(IPecanLogger logger)
        {
            this.Logger = logger;
            logger?.Trace(this.GetType().Name, $"{this.GetType().Name} Creating {nameof(StorageReadActor)} .. ");
            //multi reader
            IActorRef actorReadPool = Context.ActorOf(Props.Create(() => new StorageReadActor(logger)).WithRouter(new RoundRobinPool(1000)), TypeOfWrapper.TypeOf(typeof(StorageReadActor)).Name);

            logger?.Trace(this.GetType().Name, $"{this.GetType().Name} Creating {nameof(StorageWriteActor)} .. ");
            //single writer
            IActorRef actorWriteRef = Context.ActorOf(Props.Create(() => new StorageWriteActor(logger)), TypeOfWrapper.TypeOf(typeof(StorageWriteActor)).Name);

            this.Receive<Func<bool>>(
                message =>
                {
                    logger?.Trace(this.GetType().Name, $"{this.GetType().Name} forwarding message {message?.GetType().Name} to {actorWriteRef.Path.ToStringWithAddress()}");
                    actorWriteRef.Forward(message);
                });
            this.Receive<Func<object>>(
                message =>
                {
                    logger?.Trace(this.GetType().Name, $"{this.GetType().Name} forwarding message {message?.GetType().Name} to {actorReadPool.Path.ToStringWithAddress()}");

                    actorReadPool.Forward(message);
                });
            this.Receive<Terminated>(
                t =>
                {
                    string message = "Actor just got terminated : " + t.ActorRef.Path;
                    logger?.Fatal(this.GetType().Name, message);
                });
        }

        protected override bool AroundReceive(Receive receive, object message)
        {
            this.Logger?.Trace(this.GetType().Name, $"Arround receiving message {message?.GetType().Name} {message}");
            return base.AroundReceive(receive, message);
        }

        protected override void Unhandled(object message)
        {
            this.Logger?.Fatal(this.GetType().Name, $"Unhandled message {message?.GetType().Name} {message}");
        }
    }
}