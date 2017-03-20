using System;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Meetup.Betting.Contracts.Messages;

namespace Meetup.Betting.Client
{
    public interface IEventsFeedStream : IConnectableObservable<EventsFeedChangeSet>, IDisposable
    {
        EventData[] GetEvents();
    }

    public class EmptyEventsFeedStream : IEventsFeedStream
    {
        public IDisposable Subscribe(IObserver<EventsFeedChangeSet> observer)
        {
            return Disposable.Empty;
        }

        public IDisposable Connect()
        {
            return Disposable.Empty;
        }

        public void Dispose()
        {
        }

        public EventData[] GetEvents()
        {
            return new EventData[0];
        }
    }
}