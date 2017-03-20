using System;

namespace Meetup.Betting.Contracts.Messages
{
    [Serializable]
    public class MarketSnapshot
    {
        public string MarketKey { get; set; }
        public OddData[] Odds { get; set; }
        public bool IsActive { get; set; }
        public int BetsCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}