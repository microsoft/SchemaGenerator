using SchemaGenerator.Core.Utilities;
using System;
using System.Linq;
using System.Reflection;

namespace SchemaGenerator.Core.Extensions
{
    public static class TypeExtension
    {
        public static string GetDisplayName(
            this Type type,
            bool shouldDisplayFullName)
        {
            Ensure.NotNull(nameof(type), type);

            var name = shouldDisplayFullName ? type.FullName : type.Name;

            return type.IsGenericType
                ? $"{name.Split("`", 2)[0]}" +
                  $"<{type.GetGenericArguments().Select(_ => GetDisplayName(_, shouldDisplayFullName)).ToJoinString()}>"
                : name;
        }

        public static bool HasParameterlessConstructor(this Type type)
        {
            Ensure.NotNull(nameof(type), type);

            return
                !type.IsClass ||
                type.IsArray ||
                type.GetConstructor(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    null,
                    Array.Empty<Type>(),
                    null) != null;
        }
    }
}
