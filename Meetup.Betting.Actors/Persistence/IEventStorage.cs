using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meetup.Betting.Actors.Persistence
{
    public interface IEventStorage
    {
        Task<bool> AppendToStream(Guid streamdId, IEnumerable<object> updates, int expectedversion);

        Task<KeyValuePair<int, TStreamState>> GetStreamState<TStreamState>(Guid streamId)
            where TStreamState : class, new();

        Task<bool> AppendToStream(string streamdId, IEnumerable<object> updates, int expectedversion);

        Task<KeyValuePair<int, TStreamState>> GetStreamState<TStreamState>(string streamId)
            where TStreamState : class, new();
    }
}