using System.Collections.Generic;
using System.Linq;

namespace Meetup.Betting.Contracts.Messages
{
    public class DictionaryComparer<TKey, TValue> :
        IEqualityComparer<Dictionary<TKey, TValue>>
    {
        private readonly IEqualityComparer<TValue> valueComparer;
        public DictionaryComparer(IEqualityComparer<TValue> valueComparer = null)
        {
            this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public static DictionaryComparer<TKey, TValue> Default = new DictionaryComparer<TKey, TValue>();

        public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
        {
            if (x.Count != y.Count)
                return false;
            if (x.Keys.Except(y.Keys).Any())
                return false;
            if (y.Keys.Except(x.Keys).Any())
                return false;
            foreach (var pair in x)
                if (!valueComparer.Equals(pair.Value, y[pair.Key]))
                    return false;
            return true;
        }

        public int GetHashCode(Dictionary<TKey, TValue> obj)
        {
            var hashCode = 0;
            hashCode = obj.Aggregate(hashCode,
                (current, x) => (((current * 397) ^ x.Key.GetHashCode()) * 397) ^ x.Value.GetHashCode());
            return hashCode;
        }
    }
}