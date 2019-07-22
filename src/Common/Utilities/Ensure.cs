// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Common.Extensions;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Common.Utilities
{
    /// <summary>
    /// Several input verifiers.
    /// </summary>
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
        public static void NotNull(string parameterName, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull<TValue>(string parameterName, TValue value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("value: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrWhiteSpace(string parameterName, string value)
        {
            NotNull(parameterName, value);

            if (value.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value cannot be whitespace", parameterName);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("collection: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrEmpty<TItem>(string parameterName, IReadOnlyCollection<TItem> collection)
        {
            NotNull(parameterName, collection);

            if (collection.Count == 0)
            {
                throw new ArgumentException("Value cannot be empty", parameterName);
            }
        }

        [AssertionMethod]
        [ContractAnnotation("dictionary: null => stop")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNullOrEmpty<TKey, TValue>(string parameterName, Dictionary<TKey, TValue> dictionary)
        {
            NotNull(parameterName, dictionary);

            if (dictionary.Count == 0)
            {
                throw new ArgumentException("Value cannot be empty", parameterName);
            }
        }
    }
}