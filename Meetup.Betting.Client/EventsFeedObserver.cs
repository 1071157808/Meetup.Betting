using System;
using Meetup.Betting.Contracts;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Client
{
    public class EventsFeedObserver : IObserver<EventsFeedChangeSet>
    {
        public void OnNext(EventsFeedChangeSet changeset)
        {
            Console.WriteLine($"Proccessing innerFeedEvent revision {changeset.Revision}");
            var innerFeedProcessor = GrainClient.GrainFactory.GetGrain<IEventsFeedProcessor>(0);
            innerFeedProcessor.Process(changeset).Wait();
        }

        public void OnError(Exception error)
        {
            Console.WriteLine(error.Message);
        }

        public void OnCompleted()
        {
            Console.WriteLine("Seq completed");
        }
    }
}