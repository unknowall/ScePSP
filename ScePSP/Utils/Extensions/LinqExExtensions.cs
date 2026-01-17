using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ScePSPUtils.Extensions
{
    public static class LinqExExtensions
    {
        public static bool ContainsSubset<TSource>(this IEnumerable<TSource> superset, IEnumerable<TSource> subset)
        {
            return !subset.Except(superset).Any();
        }

        public static Dictionary<TKey, TValue> CreateDictionary<TValue, TKey>(this IEnumerable<TValue> listItems,
            Func<TValue, TKey> keySelector)
        {
            var dictionary = new Dictionary<TKey, TValue>();
            foreach (var item in listItems) dictionary.Add(keySelector(item), item);
            return dictionary;
        }

        public static Dictionary<TDictionaryKey, TDictionaryValue>
            CreateDictionary<TValue, TDictionaryKey, TDictionaryValue>(this IEnumerable<TValue> listItems,
                Func<TValue, TDictionaryKey> keySelector, Func<TValue, TDictionaryValue> valueSelector)
        {
            var dictionary = new Dictionary<TDictionaryKey, TDictionaryValue>();
            foreach (var item in listItems) dictionary.Add(keySelector(item), valueSelector(item));
            return dictionary;
        }

        public static T[] Compact<T>(this T[,] In)
        {
            var length0 = In.GetLength(0);
            var length1 = In.GetLength(1);
            var @out = new T[length0 * length1];
            var outOffset = 0;
            for (var inOffset0 = 0; inOffset0 < length0; inOffset0++)
            {
                for (var inOffset1 = 0; inOffset1 < length1; inOffset1++)
                {
                    @out[outOffset++] = In[inOffset0, inOffset1];
                }
            }
            return @out;
        }

        public static IEnumerable<TSource> OrderByNatural<TSource, TString>(this IEnumerable<TSource> items, Func<TSource, TString> selector)
        {
            object Convert(string str) => int.TryParse(str, out int result) ? (object)result : str;

            return items.OrderBy(
                item => Regex.Split(selector(item).ToString().Replace(" ", ""), "([0-9]+)").Select(Convert),
                new EnumerableComparer<object>()
            );
        }

        public static IEnumerable<TSource> OrderByNatural<TSource>(this IEnumerable<TSource> items) =>
            items.OrderByNatural(value => value);

        public static IEnumerable<TSource> DistinctByKey<TSource, TResult>(this IEnumerable<TSource> items,
            Func<TSource, TResult> selector) => items.Distinct(new LinqEqualityComparer<TSource, TResult>(selector));

        public static string ToHexString(this IEnumerable<byte> bytes)
        {
            return string.Join("", bytes.Select(Byte => Byte.ToString("x2")));
        }

        public static string Implode<TSource>(this IEnumerable<TSource> items, string separator)
        {
            return string.Join(separator, items.Select(item => item.ToString()));
        }

        public static string ToStringArray<TSource>(this IEnumerable<TSource> items, string separator = ",")
        {
            return items.Implode(separator);
        }

        public static string ToStringList<TSource>(this IEnumerable<TSource> items, string separator = ",")
        {
            return "[" + items.JoinToString(", ") + "]";
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<int, T> action)
        {
            var index = 0;
            foreach (var item in items) action(index++, item);
        }

        public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items) action(item);
        }

        public static T[] ToArray2<T>(this IEnumerable<T> items)
        {
            var listItems = new List<T>();
            foreach (var item in items) listItems.Add(item);
            return listItems.ToArray();
        }

        public static T LinqExFirstOrDefault<T>(this IEnumerable<T> items, T Default)
        {
            foreach (var item in items)
            {
                return item;
            }
            return Default;
        }

        public static T BinarySearch<T, TKey>(this IList<T> list, Func<T, TKey> keySelector, TKey key) where TKey : IComparable<TKey>
        {
            int min = 0;
            int max = list.Count;
            while (min < max)
            {
                int mid = (max + min) / 2;
                T midItem = list[mid];
                TKey midKey = keySelector(midItem);
                int comp = midKey.CompareTo(key);
                if (comp < 0)
                {
                    min = mid + 1;
                }
                else if (comp > 0)
                {
                    max = mid - 1;
                }
                else
                {
                    return midItem;
                }
            }
            if (min == max &&
                keySelector(list[min]).CompareTo(key) == 0)
            {
                return list[min];
            }
            throw new InvalidOperationException("Item not found");
        }
    }
}