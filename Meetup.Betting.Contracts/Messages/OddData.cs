using System;

namespace Meetup.Betting.Contracts.Messages
{
    [Serializable]
    public class OddData : IEquatable<OddData>
    {
        public string SelectionKey { get; set; }
        public float? Price { get; set; }

        public bool Equals(OddData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(SelectionKey, other.SelectionKey) && Price.Equals(other.Price);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OddData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((SelectionKey?.GetHashCode() ?? 0) * 397) ^ Price.GetHashCode();
            }
        }

        public static bool operator ==(OddData left, OddData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(OddData left, OddData right)
        {
            return !Equals(left, right);
        }
    }
}