// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using MoreLinq;
using SchemaGenerator.Core.Extensions;
using SchemaGenerator.Core.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SchemaGenerator.Core
{
    /// <summary>
    /// This is a base class for a schema generator.
    /// It can be extended by specific types of schemas.
    /// </summary>
    public abstract class SchemaGenerator
    {
        private readonly Lazy<HashSet<Type>> _scopeTypes;
        private readonly Lazy<IReadOnlyCollection<Type>> _serializableTypes;

        protected Func<MemberInfo, bool> ShouldSerializeMember { get; }

        /// <summary>
        /// The types that are reachable by serialization from the given root types.
        /// </summary>
        public IReadOnlyCollection<Type> SerializableTypes => _serializableTypes.Value;

        /// <summary>
        /// Create an instance of a <see cref="SchemaGenerator"/>.
        /// </summary>
        /// <param name="rootTypes">The basic serializable types from which to start scanning,
        /// e.g. the types of documents in the database.
        /// or the types that appear in the signatures of REST API calls.</param>
        /// <param name="isInScope">A method to check whether an assembly is relevant for the schema or an external dependency,
        /// e.g. does its name contain the solution name.</param>
        /// <param name="shouldSerializeMember">A method to check whether a member of a serializable type should be included in the schema,
        /// e.g. does it have a specific serialization attribute.</param>
        protected SchemaGenerator(
            IReadOnlyCollection<Type> rootTypes,
            Func<AssemblyName, bool> isInScope,
            Func<MemberInfo, bool> shouldSerializeMember)
        {
            Ensure.NotNull(nameof(rootTypes), rootTypes);
            Ensure.NotNull(nameof(isInScope), isInScope);
            ShouldSerializeMember = Ensure.NotNull(nameof(shouldSerializeMember), shouldSerializeMember);

            _scopeTypes =
                new Lazy<HashSet<Type>>(
                    () =>
                    {
                        var assemblies = new HashSet<Assembly>();
                        try
                        {
                            var currentAssemblies =
                                rootTypes.
                                    Select(type => type.Assembly).
                                    Where(assembly => isInScope(assembly.GetName())).
                                    ToList();
                            while (currentAssemblies.Any())
                            {
                                assemblies.UnionWith(currentAssemblies);
                                currentAssemblies =
                                    currentAssemblies.
                                        SelectMany(assembly => assembly.GetReferencedAssemblies()).
                                        Where(isInScope).
                                        Select(Assembly.Load).
                                        WhereNotNull().
                                        Where(assembly => !assemblies.Contains(assembly)).
                                        ToList();
                            }
                        }
                        catch (FileNotFoundException exception)
                        {
                            throw new ExtendedException(
                                $"Could not load a referenced assembly. Try limiting {nameof(isInScope)} method to reachable assemblies.",
                                exception);
                        }

                        return assemblies.
                            SelectMany(assembly => assembly.GetTypes()).
                            ToHashSet();
                    });

            _serializableTypes =
                new Lazy<IReadOnlyCollection<Type>>(
                    () =>
                    {
                        var serializableTypes = new HashSet<Type>();
                        rootTypes.ForEach(type => AddSerializableType(type, serializableTypes));

                        serializableTypes.
                            RemoveWhere(
                                type =>
                                    !_scopeTypes.Value.Contains(type) ||
                                    type.IsGenericType &&
                                    !type.IsGenericTypeDefinition);

                        return serializableTypes;
                    });
        }

        /// <summary>
        /// Generate a schema for a set of types.
        /// </summary>
        /// <param name="types">The types to include in the schema.</param>
        /// <returns>A schema of the given types.</returns>
        protected abstract string Generate(IReadOnlyCollection<Type> types);

        /// <summary>
        /// Validate that a set of types complies to some conditions.
        /// </summary>
        /// <param name="types">The types on which to validate the conditions.</param>
        /// <example>Validate that all types have a parameter-less constructor.</example>
        /// <example>Validate that all protobuf member indexes are unique.</example>
        protected virtual void Validate(IReadOnlyCollection<Type> types)
        {
        }

        /// <summary>
        /// Generate the schema of all the serializable types.
        /// </summary>
        /// <returns>The generated schema.</returns>
        public string Generate() =>
            Generate(SerializableTypes);

        /// <summary>
        /// Validate that all the serializable types comply to some conditions.
        /// The conditions should be implemented on <see cref="Validate(System.Collections.Generic.IReadOnlyCollection{System.Type})"/>.
        /// </summary>
        public void Validate() =>
            Validate(SerializableTypes);

        /// <summary>
        /// Get the members of the types that return true for <see cref="ShouldSerializeMember"/>.
        /// </summary>
        /// <param name="type">A serializable type.</param>
        /// <returns>The serializable members.</returns>
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
        /// <param name="type">The type to scan.</param>
        /// <param name="serializableTypes">The types that have been found so far.</param>
        /// <param name="shouldAddDerivedTypes"><see langword="true"/> if the type is directly referenced in the schema,
        /// e.g. one of the root types or as serialized member of another serializable type.</param>
        private void AddSerializableType(
            Type type,
            HashSet<Type> serializableTypes,
            bool shouldAddDerivedTypes = true)
        {
            if (type == null ||
                !type.IsGenericType &&
                !_scopeTypes.Value.Contains(type))
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
                    Select(memberInfo => memberInfo.GetUnderlyingType()).
                    Concat(type.GetGenericArguments()).
                    Distinct().
                    ForEach(memberType => AddSerializableType(memberType, serializableTypes));
            }

            if (shouldAddDerivedTypes)
            {
                _scopeTypes.Value.
                    Where(
                        scopeType =>
                            type.IsAssignableFrom(scopeType) &&
                            scopeType != type).
                    ForEach(
                        derivedType =>
                            AddSerializableType(
                                derivedType,
                                serializableTypes,
                                false));
            }
        }
    }
}