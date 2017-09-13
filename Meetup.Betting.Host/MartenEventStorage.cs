using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marten;
using Meetup.Betting.Actors.Persistence;

namespace Meetup.Betting.Host
{
    public class MartenEventStorage : IEventStorage
    {
        private readonly IDocumentStore _documentStore;

        public MartenEventStorage(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }

        public Task<bool> AppendToStream(Guid streamdId, IEnumerable<object> updates, int expectedversion)
        {
            return Task.Run(async () =>
            {
                using (var session = _documentStore.OpenSession())
                {
                    session.Events.Append(streamdId, updates.ToArray());
                    await session.SaveChangesAsync();
                    return true;
                }
            });
        }

        public Task<KeyValuePair<int, TStreamState>> GetStreamState<TStreamState>
            (Guid streamId) where TStreamState : class, new()
        {
            return Task.Run(async () =>
            {
                using (var session = _documentStore.OpenSession())
                {
                    var streamState = await session.Events.FetchStreamStateAsync(streamId);
                    var version = streamState?.Version ?? 0;
                    TStreamState stream;
                    if (version != 0)
                    {
                        Console.WriteLine($"Reading {streamId}");
                        stream = await session.Events.AggregateStreamAsync<TStreamState>(streamId);
                        Console.WriteLine($"Read {streamId}");
                    }
                    else
                    {
                        stream = new TStreamState();
                    }
                    return new KeyValuePair<int, TStreamState>(version, stream);
                }
            });
        }

        public Task<bool> AppendToStream(string streamdId, IEnumerable<object> updates, int expectedversion)
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (var session = _documentStore.OpenSession())
                    {
                        session.Events.Append(streamdId, expectedversion, updates.ToArray());
                        await session.SaveChangesAsync();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
            });
        }

        public Task<KeyValuePair<int, TStreamState>> GetStreamState<TStreamState>(string streamId) where TStreamState : class, new()
        {
            return Task.Run(async () =>
            {
                try
                {
                    using (var session = _documentStore.OpenSession())
                    {
                        var streamState = await session.Events.FetchStreamStateAsync(streamId);
                        var version = streamState?.Version ?? 0;
                        TStreamState stream;
                        if (version != 0)
                        {
                            Console.WriteLine($"Reading {streamId}");
                            stream = await session.Events.AggregateStreamAsync<TStreamState>(streamId);
                            Console.WriteLine($"Read {streamId}");
                        }
                        else
                        {
                            stream = new TStreamState();
                        }
                        return new KeyValuePair<int, TStreamState>(version, stream);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            });
        }
    }
}