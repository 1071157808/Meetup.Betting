using System;

namespace Meetup.Betting.Contracts.Messages
{
    [Serializable]
    public class EventStatistics
    {
        public string EventKey { get; set; }
        public string EventName { get; set; }
        public int BetsCount { get; set; }
        public int TotalAmount { get; set; }    
    }
}