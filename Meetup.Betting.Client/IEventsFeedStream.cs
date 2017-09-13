using System;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using BetLab.Common.Logging;
using BetLab.FeedHandlers.DataContracts;
using BetLab.InnerFeed;
using BetLab.InnerFeed.Consumer;
using BetLab.InnerFeed.DataAccessor;
using Meetup.Betting.Client.InnerFeed;
using Meetup.Betting.Contracts.Messages;
using StackExchange.Redis;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

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
    public class InnerFeedEventsFeedStream : IEventsFeedStream
    {
        private readonly string _redisHost;
        private readonly string _spaceName;
        private int _initialRevision;
        private readonly InnerFeedReader _innerFeedReader;
        private readonly IConnectableObservable<EventsFeedChangeSet> _sourceStream;

        private InnerFeedEventsFeedStream(string redisHost, string spaceName)
        {
            _redisHost = redisHost;
            _spaceName = spaceName;
            var initialState = CreateInitialChangesetStream()
                .OnErrorRetry(er => Console.WriteLine("Failed to load initial revision. Retrying...", er));

            var changesetsStream = CreateNotificationsStream()
                .OnErrorRetry(er => Console.WriteLine("Failed to process inner feed stream", er));

            _sourceStream = initialState.Concat(changesetsStream)
                .OnErrorRetry(er => Console.WriteLine("Error on event processing pipeline", er))
                .ObserveOn(TaskPoolScheduler.Default)
                .SubscribeOn(TaskPoolScheduler.Default.DisableOptimizations())
                .Select(x => new EventsFeedChangeSet
                {
                    Removed = x.Removed,
                    Updated =
                        x.Added.Concat(x.Updated).Select(y => y.Value.ToInnerFeedEventData()).ToArray()
                }).Publish();
            _innerFeedReader = InnerFeedReader.Create(_redisHost, _spaceName);
        }

        public static IEventsFeedStream Create(string redisHost, string spaceName)
        {
            return new InnerFeedEventsFeedStream(redisHost, spaceName);
        }

        private IObservable<ChangeSet<InnerFeedEvent>> CreateInitialChangesetStream()
        {
            return Observable.Using(
                    () =>
                        new FeedAccessor<InnerFeedEvent>(_spaceName,
                            ConnectionMultiplexer.Connect($"{_redisHost},abortConnect=true,connectTimeout=50000")),
                    LoadZeroChangeset)
                .Do(set => _initialRevision = set.Revision);
        }

        private IObservable<ChangeSet<InnerFeedEvent>> LoadZeroChangeset(FeedAccessor<InnerFeedEvent> sourceAccessor)
        {
            return Observable.FromAsync(() => sourceAccessor.GetAsync(0))
                .Do(set => Console.WriteLine(
                    $"Loaded initial revision from space {_spaceName}: {set.Revision}. " +
                    $"Added: {set.Added.Length} Updated: {set.Updated.Length} Removed: {set.Removed.Length}"));
        }

        private IObservable<ChangeSet<InnerFeedEvent>> CreateNotificationsStream()
        {
            return FeedListenerFactory.CreateListener<InnerFeedEvent>(
                    _spaceName,
                    _redisHost,
                    exception => Console.WriteLine($"Failed to deserialize changeset: {exception.Value}", exception),
                    initialRevision: _initialRevision)
                .Do(set => _initialRevision = set.Revision);
        }

        public IDisposable Subscribe(IObserver<EventsFeedChangeSet> observer)
        {
            var initialState = CreateInitialChangesetStream()
                .OnErrorRetry(er => Console.WriteLine("Failed to load initial revision. Retrying...", er));

            var changesetsStream = CreateNotificationsStream()
                .OnErrorRetry(er => Console.WriteLine("Failed to process inner feed stream", er));

            var sourceStream = initialState.Concat(changesetsStream).Publish();

            var sub = sourceStream
                .OnErrorRetry(er => Console.WriteLine("Error on event processing pipeline", er))
                .ObserveOn(TaskPoolScheduler.Default)
                .SubscribeOn(TaskPoolScheduler.Default.DisableOptimizations())
                .Select(x => new EventsFeedChangeSet
                {
                    Revision = x.Revision,
                    Removed = x.Removed,
                    Updated =
                        x.Added.Concat(x.Updated).Select(y => y.Value.ToInnerFeedEventData()).ToArray()
                })
                .Subscribe(observer);

            return new CompositeDisposable(sourceStream.Connect(), sub);
        }

        public EventData[] GetEvents()
        {
            return _innerFeedReader.GetEvents().Select(x => x.ToInnerFeedEventData()).ToArray();
        }

        public void Dispose()
        {
            _sourceStream?.Connect();
            _innerFeedReader?.Dispose();
        }

        public IDisposable Connect()
        {
            return _sourceStream.Connect();
        }
    }
    public class InnerFeedReader : IDisposable
    {
        private static TimeSpan _accessorLoadTimeout = TimeSpan.FromSeconds(10);

        private readonly ConcurrentDictionary<string, InnerFeedEvent> _feedEvents
            = new ConcurrentDictionary<string, InnerFeedEvent>();

        private readonly ILog _logger = new ConsoleLog(); // Logger.Create(typeof(InnerFeedListener));

        private readonly IDisposable _subscription;

        private readonly FeedEventType[] _supportedEventTypes = new[] { FeedEventType.Live, FeedEventType.SoonInLive, FeedEventType.PreMatch, };

        private string _currentSessionId = string.Empty;

        private int _retryCount = 10;

        private int _revision;

        private InnerFeedReader(IObservable<ChangeSet<InnerFeedEvent>> listener, IFeedAccessor<InnerFeedEvent> accessor, string feedName)
        {
            var changesets =
                Observable.FromAsync(() => accessor.GetAsync(0, (int)_accessorLoadTimeout.TotalSeconds))
                    .Do(
                        set =>
                        {
                            _logger.Info(
                                $"['{feedName}'] Loaded initial revision: {set.Revision}. Added: {set.Added.Length} Updated: {set.Updated.Length} Removed: {set.Removed.Length}");

                            _revision = set.Revision;
                            _currentSessionId = set.SessionId;
                        },
                        exception =>
                            _logger.Error($"['{feedName}'] Exception while loading initial revision", exception))
                    .Retry(_retryCount)
                    .Do(
                        set => { },
                        exception =>
                            _logger.Error(
                                $"['{feedName}'] Unable to load load initial revision after {_retryCount} attempts",
                                exception))
                    .Delay(_accessorLoadTimeout)
                    .Publish();

            var changesetsStream = listener
                .SelectMany(set =>
                {
                    _logger.Info($"['{feedName}'] Current revision: {_revision}. Validating new changeset: {set.Revision} (Added: {set.Added.Length}, Updated: {set.Updated.Length}, Removed: {set.Removed.Length})");

                    if (string.IsNullOrWhiteSpace(_currentSessionId))
                    {
                        _currentSessionId = set.SessionId;
                    }

                    if (_currentSessionId != set.SessionId)
                    {
                        _logger.Warn($" => SessionId was changed. Invalidating '{feedName}' cache. Force load Rev: 0");
                        _feedEvents.Clear();
                        _currentSessionId = set.SessionId;
                        return accessor.GetAsync(0, (int)_accessorLoadTimeout.TotalSeconds);
                    }

                    if (_revision > set.Revision)
                    {
                        _logger.Warn($" => current revision {_revision} is greater than changeset: {set.Revision}");
                        return Task.FromResult(set);
                    }

                    if (_revision == (set.Revision - 1))
                    {
                        Interlocked.Exchange(ref _revision, set.Revision);
                        return Task.FromResult(set);
                    }

                    _logger.Warn($" [!] Total {set.Revision - _revision} revision(s) were skipped. Force load all changes since revision: {_revision}");
                    var result = accessor.GetAsync(_revision, (int)_accessorLoadTimeout.TotalSeconds);
                    Interlocked.Exchange(ref _revision, set.Revision);

                    return result;
                })
                .ObserveOn(TaskPoolScheduler.Default)
                .Do(s => { }, exception => _logger.Error("Exception during inner feed processing", exception))
                .Retry()
                .Publish();

            var resultStream = changesets.Merge(changesetsStream).Publish().RefCount();

            changesets.Connect();
            changesetsStream.Connect();

            _subscription = resultStream.Subscribe(
                set =>
                {
                    _logger.Info(
                        $"['{feedName}'] Processing revision: {set.Revision} (Added: {set.Added.Length}, Updated: {set.Updated.Length}, Removed: {set.Removed.Length})");

                    foreach (var item in set.Added.OrderBy(x => x.Value.Id))
                    {
                        var feedEvent = item.Value;

                        if (!_supportedEventTypes.Contains(feedEvent.EventType))
                        {
                            continue;
                        }

                        _feedEvents.AddOrUpdate(item.Id, feedEvent, (k, v) => feedEvent);

                        LogEntry("Added", item);
                    }

                    foreach (var item in set.Updated.OrderBy(x => x.Value.Id))
                    {
                        var feedEvent = item.Value;

                        if (!_supportedEventTypes.Contains(feedEvent.EventType))
                        {
                            continue;
                        }

                        _feedEvents.AddOrUpdate(item.Id, feedEvent, (k, v) => feedEvent);

                        LogEntry("Updated", item);
                    }

                    foreach (var eventKey in set.Removed)
                    {
                        InnerFeedEvent feedEvent;
                        if (_feedEvents.TryRemove(eventKey, out feedEvent))
                        {
                            LogEntry("Removed", eventKey, feedEvent);
                        }
                    }
                    _logger.Info($"['{feedName}'] feed is up to date. Rev: {_revision}. Items in cache : {_feedEvents.Count}");
                });

            _logger.Info("[Started] InnerFeedListener is up and running");
        }

        public static InnerFeedReader Create(string feedHost, string feedName)
        {
            if (string.IsNullOrEmpty(feedHost))
                throw new ArgumentNullException(nameof(feedHost));
            if (string.IsNullOrEmpty(feedName))
                throw new ArgumentNullException(nameof(feedName));

            return new InnerFeedReader(
                FeedListenerFactory.CreateListener<InnerFeedEvent>(feedName, feedHost, timeOutMs: (int)_accessorLoadTimeout.TotalMilliseconds),
                new FeedAccessor<InnerFeedEvent>(feedName, ConnectionMultiplexer.Connect($"{feedHost}, abortConnect=false, syncTimeout=10000")),
                feedName);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        public InnerFeedEvent[] GetEvents()
        {
            return _feedEvents.Values.ToArray();
        }

        private void LogEntry(string action, string entryId, InnerFeedEvent feedEvent)
        {
            _logger.Debug($"[{action}] {entryId} (BLEK:{feedEvent.BetlabEventKey},ExternalId:{feedEvent.ExternalId},Id:{feedEvent.Id})");
        }

        private void LogEntry(string action, FeedEntry<InnerFeedEvent> feedEntry)
        {
            LogEntry(action, feedEntry.Id, feedEntry.Value);
        }

        private class ConsoleLog : ILog
        {
            public void Debug(string text, params object[] args)
            {
                // Console.WriteLine("Debug:" + text);
            }

            public void Info(string text, params object[] args)
            {
                // Console.WriteLine("Info:" + text);
            }

            public void Warn(string text, params object[] args)
            {
                Console.WriteLine("Warn:" + text);
            }

            public void Error(string text, Exception exception, params object[] args)
            {
                Console.WriteLine("Error:" + text);
            }

            public void Fatal(string text, Exception exception, params object[] args)
            {
                Console.WriteLine("Fatal:" + text);
            }

            public LoggerContext Context { get; }
        }
    }
    public static class InnerFeedEventExt
    {
        public static EventData ToInnerFeedEventData([NotNull] this InnerFeedEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new EventData
            {
                EventKey = value.BetlabEventKey,
                Name = value.Name,
                StartDate = value.StartDate,
                TournamentKey = value.Tournament,
                TeamsKeys = value.Teams,
                Scoreboard = new ScoreboardData
                {
                    CurrentTime = value.CurrentTime,
                    CurrentScore = value.CurrentScore,
                    ScoresByPeriod = value.ScoresByPeriod
                },
                Markets =
                    value.Odds.Select(Odd.Parse)
                        .GroupBy(x => x.GetMarketKey())
                        .ToDictionary(x => x.Key,
                            x =>
                                new MarketData
                                {
                                    Key = x.Key,
                                    Odds =
                                        x.ToDictionaryWithoutDublicates(y => y.SelectionKey,
                                            y => new OddData { SelectionKey = y.SelectionKey, Price = y.Price }, y => { })
                                })
            };
        }

        private static string GetMarketKey(this Odd odd)
        {
            try
            {
                return odd.Selection.Market.MarketKey;
            }
            catch (FormatException e)
            {
                return "[0,[],[0],1]";
            }
        }
    }
}