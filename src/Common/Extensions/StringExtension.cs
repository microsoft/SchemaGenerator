// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Common.Extensions
{
    public static class StringExtension
    {
        public static bool IsNullOrWhiteSpace(this string value) =>
            string.IsNullOrWhiteSpace(value);
    }
}
