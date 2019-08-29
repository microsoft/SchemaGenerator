using SchemaGenerator.Core.Utilities;
using System.Collections.Generic;

namespace SchemaGenerator.Core.Extensions
{
    public static class DictionaryExtension
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            Ensure.NotNull(nameof(dictionary), dictionary);

            return !dictionary.TryGetValue(key, out var value)
                ? default
                : value;
        }
    }
}
