using System.Collections.Generic;

namespace SchemaGenerator.Common
{
    public static class DictionaryExtension
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            Ensure.NotNull(nameof(dictionary), dictionary);

            return !dictionary.TryGetValue(key, out var value)
                ? default(TValue)
                : value;
        }
    }
}
