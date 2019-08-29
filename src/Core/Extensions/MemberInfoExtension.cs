// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using SchemaGenerator.Core.Utilities;
using System;
using System.Reflection;

namespace SchemaGenerator.Core.Extensions
{
    public static class MemberInfoExtension
    {
        public static bool HasAttribute<TAttribute>(this MemberInfo memberInfo)
            where TAttribute : Attribute
        {
            Ensure.NotNull(nameof(memberInfo), memberInfo);

            return memberInfo.GetCustomAttribute<TAttribute>() != null;
        }

        /// <summary>
        /// Get the type of a <see cref="MemberInfo"/>.
        /// </summary>
        /// <param name="memberInfo">A <see cref="MemberInfo"/></param>
        /// <returns>The field type, property type or return type of <paramref name="memberInfo"/></returns>
        public static Type GetUnderlyingType(this MemberInfo memberInfo)
        {
            Ensure.NotNull(nameof(memberInfo), memberInfo);

            switch (memberInfo)
            {
                case FieldInfo fieldInfo:
                    return fieldInfo.FieldType;
                case MethodInfo methodInfo:
                    return methodInfo.ReturnType;
                case PropertyInfo propertyInfo:
                    return propertyInfo.PropertyType;
                default:
                    throw new UnexpectedException(nameof(MemberInfo), memberInfo.GetType().Name);
            }
        }
    }
}
