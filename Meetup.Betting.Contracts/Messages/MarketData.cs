using System;
using System.Collections.Generic;

namespace Meetup.Betting.Contracts.Messages
{
    [Serializable]
    public class MarketData : IEquatable<MarketData>
    {
        public MarketData()
        {
            Odds = new Dictionary<string, OddData>();
        }

        public string Key { get; set; }
        public Dictionary<string, OddData> Odds { get; set; }

        public bool Equals(MarketData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Key, other.Key) && DictionaryComparer<string, OddData>.Default.Equals(Odds, other.Odds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MarketData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Key?.GetHashCode() ?? 0) * 397) ^ (Odds?.GetHashCode() ?? 0);
            }
        }

        public static bool operator ==(MarketData left, MarketData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MarketData left, MarketData right)
        {
            return !Equals(left, right);
        }
    }
}