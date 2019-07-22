// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Common.Extensions;
using Common.Utilities;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SchemaGenerator
{
    /// <summary>
    /// This is a base class for a schema generator.
    /// It can be extended by specific types of schemas.
    /// </summary>
    public abstract class SchemaGenerator
    {
        /// <summary>
        /// All the types in the relevant assemblies
        /// </summary>
        private readonly Lazy<HashSet<Type>> _types;

        /// <summary>
        /// The types from which the scan should start.
        /// These can be the basic types that go into the database,
        /// Types that participate in REST calls, etc.
        /// </summary>
        public abstract IReadOnlyCollection<Type> RootTypes { get; }

        protected SchemaGenerator() =>
            _types =
                new Lazy<HashSet<Type>>(
                    () =>
                    {
                        var assemblies = new HashSet<Assembly>();
                        try
                        {
                            var currentAssemblies =
                                RootTypes.Select(_ => _.Assembly).
                                    Where(_ => IsInScope(_.GetName())).
                                    ToList();
                            while (currentAssemblies.Any())
                            {
                                assemblies.UnionWith(currentAssemblies);
                                currentAssemblies =
                                    currentAssemblies.
                                        SelectMany(_ => _.GetReferencedAssemblies()).
                                        Where(IsInScope).
                                        Select(Assembly.Load).
                                        WhereNotNull().
                                        Where(_ => !assemblies.Contains(_)).
                                        ToList();

                            }
                        }
                        catch (FileNotFoundException exception)
                        {
                            throw new ExtendedException(
                                "Could not load a referenced assembly. " +
                                $"Try limiting {nameof(IsInScope)} method to reachable assemblies.",
                                exception);
                        }

                        return assemblies.
                            SelectMany(_ => _.GetTypes()).
                            Where(_ => !_.IsSecurityTransparent).
                            ToHashSet();
                    });

        /// <summary>
        /// Generate a string from the serializable types.
        /// This may use <see cref="GetSerializableMemberInfos"/> to get the inner member infos of each type.
        /// </summary>
        /// <param name="serializableTypes">The types that are reachable by serialization</param>
        /// <returns>A schema that represents the serializable types, in the desired format</returns>
        protected abstract string Generate(IReadOnlyCollection<Type> serializableTypes);

        /// <summary>
        /// Is the assembly relevant for the schema.
        /// Usually contain only assemblies in the same solution.
        /// </summary>
        /// <param name="assemblyName">The assembly name</param>
        /// <returns>Whether the assembly is relevant for the schema</returns>
        protected abstract bool IsInScope(AssemblyName assemblyName);

        /// <summary>
        /// Is <paramref name="memberInfo"/> included in the schema.
        /// </summary>
        /// <param name="memberInfo">A member of a serializable type</param>
        /// <returns>Whether the member is included in the schema</returns>
        /// <example>Does <paramref name="memberInfo"/> have a specific attribute</example>
        /// <example>Is <paramref name="memberInfo"/> public</example>
        protected abstract bool ShouldSerializeMember(MemberInfo memberInfo);

        /// <summary>
        /// Optional validations of serializable types.
        /// </summary>
        /// <param name="serializableTypes">all the serializable types</param>
        /// <example>All types have parameterless constructors</example>
        protected virtual void ValidateSerializableTypes(IReadOnlyCollection<Type> serializableTypes)
        {
        }

        /// <summary>
        /// Generate the schema of all the serializable types.
        /// </summary>
        /// <returns>The generated schema</returns>
        public string Generate() =>
            Generate(GetSerializableTypes());

        /// <summary>
        /// Get they types that are reachable for serialization.
        /// </summary>
        /// <returns>The serializable types</returns>
        protected IReadOnlyCollection<Type> GetSerializableTypes()
        {
            var serializableTypes = new HashSet<Type>();
            RootTypes.ForEach(_ => AddSerializableType(_, serializableTypes));

            serializableTypes.
                RemoveWhere(
                    _ =>
                        !_types.Value.Contains(_) ||
                        _.IsGenericType && !_.IsGenericTypeDefinition);

            ValidateSerializableTypes(serializableTypes);

            return serializableTypes;
        }

        /// <summary>
        /// Get the members of the types that return true for <see cref="ShouldSerializeMember"/>.
        /// </summary>
        /// <param name="type">A serializable type</param>
        /// <returns>The serializable members</returns>
        protected IReadOnlyCollection<MemberInfo> GetSerializableMemberInfos(Type type)
        {
            Ensure.NotNull(nameof(type), type);

            const BindingFlags bindingFlags =
                BindingFlags.DeclaredOnly |
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;
            return type.
                GetProperties(bindingFlags).
                Cast<MemberInfo>().
                Concat(type.GetFields(bindingFlags)).
                Where(ShouldSerializeMember).
                ToList();
        }

        /// <summary>
        /// Scan a <paramref name="type"/> and add the results of the scan to <paramref name="serializableTypes"/>
        /// </summary>
        /// <param name="type">The type to scan</param>
        /// <param name="serializableTypes">The types that have been found so far</param>
        /// <param name="shouldAddDerivedTypes"><see langword="true"/> if the type is directly referenced in the schema, e.g. one of <see cref="RootTypes"/> or as serialized member of another serializable type</param>
        private void AddSerializableType(
            Type type,
            HashSet<Type> serializableTypes,
            bool shouldAddDerivedTypes = true)
        {
            if (type == null ||
                !type.IsGenericType &&
                !_types.Value.Contains(type))
            {
                return;
            }

            if (type.IsGenericParameter)
            {
                AddSerializableType(
                    type.BaseType,
                    serializableTypes,
                    shouldAddDerivedTypes);
                return;
            }

            if (!serializableTypes.Contains(type))
            {
                serializableTypes.Add(type);

                if (!type.IsInterface)
                {
                    AddSerializableType(
                        type.BaseType,
                        serializableTypes,
                        false);
                }

                GetSerializableMemberInfos(type).
                    Select(_ => _.GetUnderlyingType()).
                    Concat(type.GetGenericArguments()).
                    Distinct().
                    ForEach(_ => AddSerializableType(_, serializableTypes));
            }

            if (shouldAddDerivedTypes)
            {
                _types.Value.
                    Where(_ => type.IsAssignableFrom(_) && _ != type).
                    ForEach(
                        _ =>
                            AddSerializableType(
                                _,
                                serializableTypes,
                                false));
            }
        }
    }
}