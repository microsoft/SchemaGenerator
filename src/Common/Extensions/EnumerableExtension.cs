// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Common.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Common.Extensions
{
    public static class EnumerableExtension
    {
        public static IEnumerable<TItem> WhereNotNull<TItem>(this IEnumerable<TItem> enumerable)
        {
            Ensure.NotNull(nameof(enumerable), enumerable);

            return enumerable.Where(_ => _ != null);
        }
    }
}
