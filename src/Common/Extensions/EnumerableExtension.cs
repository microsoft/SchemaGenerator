// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchemaGenerator.Common
{
    public static class EnumerableExtension
    {
        public static string ToJoinString<TItem>(
            this IEnumerable<TItem> enumerable,
            string delimiter = null)
        {
            Ensure.NotNull(nameof(enumerable), enumerable);

            var firstItem = true;
            var stringBuilder = new StringBuilder();
            foreach (var item in enumerable)
            {
                if (!firstItem)
                {
                    stringBuilder.Append(delimiter ?? ",");
                }

                stringBuilder.Append(item);
                firstItem = false;
            }

            return stringBuilder.ToString();
        }

        public static IEnumerable<TItem> WhereNotNull<TItem>(this IEnumerable<TItem> enumerable)
        {
            Ensure.NotNull(nameof(enumerable), enumerable);

            return enumerable.Where(_ => _ != null);
        }
    }
}
