using System;
using System.Collections.Generic;
using System.Linq;

namespace Meetup.Betting.Contracts.Messages
{
    [Serializable]
    public class EventData : IEquatable<EventData>
    {
        public static EventData Empty = new EventData {EventKey = ""};

        public EventData()
        {
            Markets = new Dictionary<string, MarketData>();   
        }

        public string EventKey { get; set; }
        public DateTime StartDate { get; set; }
        public string Name { get; set; }
        public string TournamentKey { get; set; }
        public string[] TeamsKeys { get; set; }
        public ScoreboardData Scoreboard { get; set; }  
        public Dictionary<string, MarketData> Markets { get; set; }

        public bool Equals(EventData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(EventKey, other.EventKey) && StartDate.Equals(other.StartDate) &&
                   string.Equals(Name, other.Name) && string.Equals(TournamentKey, other.TournamentKey) &&
                   TeamsKeys.SequenceEqual(other.TeamsKeys) && Scoreboard.Equals(other.Scoreboard) &&
                   DictionaryComparer<string, MarketData>.Default.Equals(Markets, other.Markets);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EventData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (EventKey != null ? EventKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ StartDate.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (TournamentKey != null ? TournamentKey.GetHashCode() : 0);
                hashCode = TeamsKeys.Aggregate(hashCode, (ac, x) => (ac * 397) ^ x.GetHashCode());
                hashCode = (hashCode * 397) ^ (Scoreboard != null ? Scoreboard.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Markets != null ? Markets.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(EventData left, EventData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(EventData left, EventData right)
        {
            return !Equals(left, right);
        }
    }

    [Serializable]
    public class ScoreboardData : IEquatable<ScoreboardData>
    {   
        public TimeSpan? CurrentTime { get; set; }
        public string[] CurrentScore { get; set; }
        public string[] ScoresByPeriod { get; set; }

        public bool Equals(ScoreboardData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CurrentTime.Equals(other.CurrentTime) && CurrentScore.SequenceEqual(other.CurrentScore) &&
                   ScoresByPeriod.SequenceEqual(other.ScoresByPeriod);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScoreboardData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CurrentTime.GetHashCode();
                hashCode = CurrentScore.Aggregate(hashCode,
                    (current, x) => (current * 397) ^ x.GetHashCode());
                hashCode = ScoresByPeriod.Aggregate(hashCode,
                    (current, x) => (current * 397) ^ x.GetHashCode());
                return hashCode;
            }
        }

        public static bool operator ==(ScoreboardData left, ScoreboardData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ScoreboardData left, ScoreboardData right)
        {
            return !Equals(left, right);
        }
    }
}
