using Common.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Common.Utilities
{
    [DebuggerStepThrough]
    public static class Ensure
    {
        [AssertionMethod]
        [ContractAnnotation("condition: false => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void That(bool condition)
        {
            if (!condition)
            {
                throw new ArgumentException();
            }
        }

        [AssertionMethod]
        [ContractAnnotation("condition: false => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void That(string message, bool condition)
        {
            if (!condition)
            {
                throw new ArgumentException(message);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object NotNull(string parameterName, object value) =>
            value ?? throw new ArgumentNullException(parameterName);

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue NotNull<TValue>(string parameterName, TValue value) =>
            value == null
                ? throw new ArgumentNullException(parameterName)
                : value;

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(string parameterName, Task value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull<TResult>(string parameterName, Task<TResult> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string NotNullOrWhiteSpace(string parameterName, string value)
        {
            NotNull(parameterName, value);

            return value.IsNullOrWhiteSpace()
                ? throw new ArgumentException("Value cannot be whitespace", parameterName)
                : value;
        }

        [AssertionMethod]
        [ContractAnnotation("collection: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IReadOnlyCollection<TItem> NotNullOrEmpty<TItem>(string parameterName, IReadOnlyCollection<TItem> collection)
        {
            NotNull(parameterName, collection);

            return collection.Count == 0
                ? throw new ArgumentException("Value cannot be empty", parameterName)
                : collection;
        }

        [AssertionMethod]
        [ContractAnnotation("dictionary: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> NotNullOrEmpty<TKey, TValue>(string parameterName, Dictionary<TKey, TValue> dictionary)
        {
            NotNull(parameterName, dictionary);

            return dictionary.Count == 0
                ? throw new ArgumentException("Value cannot be empty", parameterName)
                : dictionary;
        }
    }
}