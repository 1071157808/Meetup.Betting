using System;
using System.Reactive.Linq;

namespace Meetup.Betting.Client.InnerFeed
{
    public static class ObservableExt
    {
        public static IObservable<T> OnErrorRetry<T>(this IObservable<T> source, Action<Exception> handler)
        {
            return source.Do(_ => { }, handler).Retry();
        }
    }
}