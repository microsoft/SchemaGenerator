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
        private readonly Func<MemberInfo, bool> _shouldSerializeMember;

        /// <summary>
        /// All the types in the relevant assemblies
        /// </summary>
        private readonly Lazy<HashSet<Type>> _types;
        private readonly Lazy<IReadOnlyCollection<Type>> _serializableTypes;

        public IReadOnlyCollection<Type> SerializableTypes => _serializableTypes.Value;

        protected SchemaGenerator(
            IReadOnlyCollection<Type> rootTypes,
            Func<AssemblyName, bool> isInScope,
            Func<MemberInfo, bool> shouldSerializeMember)
        {
            Ensure.NotNull(nameof(rootTypes), rootTypes);
            Ensure.NotNull(nameof(isInScope), isInScope);
            _shouldSerializeMember = Ensure.NotNull(nameof(shouldSerializeMember), shouldSerializeMember);

            _types =
                new Lazy<HashSet<Type>>(
                    () =>
                    {
                        var assemblies = new HashSet<Assembly>();
                        try
                        {
                            var currentAssemblies = rootTypes.Select(_ => _.Assembly).Where(_ => isInScope(_.GetName())).ToList();
                            while (currentAssemblies.Any())
                            {
                                assemblies.UnionWith(currentAssemblies);
                                currentAssemblies = currentAssemblies.SelectMany(_ => _.GetReferencedAssemblies()).Where(isInScope).Select(Assembly.Load).WhereNotNull().Where(_ => !assemblies.Contains(_)).ToList();
                            }
                        }
                        catch (FileNotFoundException exception)
                        {
                            throw new ExtendedException(
                                $"Could not load a referenced assembly. Try limiting {nameof(isInScope)} method to reachable assemblies.",
                                exception);
                        }

                        return assemblies.SelectMany(_ => _.GetTypes()).Where(_ => !_.IsSecurityTransparent).ToHashSet();
                    });

            _serializableTypes =
                new Lazy<IReadOnlyCollection<Type>>(
                    () =>
                    {
                        var serializableTypes = new HashSet<Type>();
                        rootTypes.ForEach(_ => AddSerializableType(_, serializableTypes));

                        serializableTypes.
                            RemoveWhere(
                                _ =>
                                    !_types.Value.Contains(_) ||
                                    _.IsGenericType &&
                                    !_.IsGenericTypeDefinition);

                        Validate(serializableTypes);

                        return serializableTypes;
                    });
        }

        /// <summary>
        /// Generate the schema of all the serializable types.
        /// </summary>
        /// <returns>The generated schema</returns>
        public string Generate() =>
            Generate(SerializableTypes);

        protected abstract string Generate(IReadOnlyCollection<Type> serializableTypes);

        protected abstract void Validate(IReadOnlyCollection<Type> serializableTypes);

        protected virtual Type GetSerializationType(Type type) => type;
        
        /// <summary>
        /// Get the members of the types that return true for <see cref="_shouldSerializeMember"/>.
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
                Where(_shouldSerializeMember).
                ToList();
        }

        /// <summary>
        /// Scan a <paramref name="type"/> and add the results of the scan to <paramref name="serializableTypes"/>
        /// </summary>
        /// <param name="type">The type to scan</param>
        /// <param name="serializableTypes">The types that have been found so far</param>
        /// <param name="shouldAddDerivedTypes">
        /// <see langword="true"/> if the type is directly referenced in the schema,
        /// e.g. one of the root types or as serialized member of another serializable type
        /// </param>
        private void AddSerializableType(
            Type type,
            HashSet<Type> serializableTypes,
            bool shouldAddDerivedTypes = true)
        {
            type = GetSerializationType(type);

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
                    Where(
                        _ =>
                            type.IsAssignableFrom(_) &&
                            _ != type).
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