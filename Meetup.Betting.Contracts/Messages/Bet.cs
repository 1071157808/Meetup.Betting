using System;

namespace Meetup.Betting.Contracts.Messages
{
    [Serializable]
    public class Bet
    {
        public Guid Id { get; set; }    
        public string EventKey { get; set; }
        public string EventName { get; set; }   
        public string MarketKey { get; set; }   
        public string SelectionKey { get; set; }    
        public decimal Amount { get; set; }
    }
}