// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace SchemaGenerator.Common
{
    public static class StringExtension
    {
        public static bool IsNullOrWhiteSpace(this string value) =>
            string.IsNullOrWhiteSpace(value);

        public static string[] Split(this string value, string separator, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
        {
            Ensure.NotNull(nameof(value), value);

            return value.Split(new[] { separator }, options);
        }

        public static string[] Split(this string value, string separator, int count, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries)
        {
            Ensure.NotNull(nameof(value), value);

            return value.Split(new[] { separator }, count, options);
        }
    }
}
