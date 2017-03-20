using System;
using System.Collections.Generic;

namespace Meetup.Betting.Client.InnerFeed
{
    public static class EnumerableExt
    {
        public static Dictionary<TKey, TValue> ToDictionaryWithoutDublicates<TKey, T, TValue>(
            this IEnumerable<T> source,
            Func<T, TKey> keySelector,
            Func<T, TValue> valueSelector,
            Action<T> onDublicateFound)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));
            if (onDublicateFound == null) throw new ArgumentNullException(nameof(onDublicateFound));

            var dict = new Dictionary<TKey, TValue>();
            foreach (var val in source)
            {
                var key = keySelector(val);
                if (dict.ContainsKey(key))
                {
                    onDublicateFound(val);
                    continue;
                }

                dict.Add(key, valueSelector(val));
            }

            return dict;
        }
    }
}
