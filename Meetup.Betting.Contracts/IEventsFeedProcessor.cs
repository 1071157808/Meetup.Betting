using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Meetup.Betting.Contracts.Messages;
using Orleans;

namespace Meetup.Betting.Contracts
{
    public interface IEventsFeedProcessor : IGrainWithIntegerKey
    {
        Task Process(EventsFeedChangeSet changeSet);
    }
}
