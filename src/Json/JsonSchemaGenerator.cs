using MoreLinq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SchemaGenerator.Core.Extensions;
using SchemaGenerator.Core.Utilities;

namespace SchemaGenerator.Json
{
    public sealed class JsonSchemaGenerator : Core.SchemaGenerator
    {
        private readonly bool _shouldDisplayFullName;
        private readonly IDictionary<Type, Type> _typeToConvertedTypeMapping;

        /// <summary>
        /// Creates a <see cref="JsonSchemaGenerator"/> to generate a json schema.
        /// </summary>
        /// <param name="rootTypes">The basic serializable types from which to start scanning,
        /// e.g. the types of documents in the database.
        /// or the types that appear in the signatures of REST API calls.</param>
        /// <param name="isInScope">A method to check whether an assembly is relevant for the schema or an external dependency,
        /// e.g. does its name contain the solution name.</param>
        /// <param name="shouldSerializeMember">A method to check whether a member of a serializable type should be included in the schema,
        /// e.g. does it have a specific serialization attribute.</param>
        /// <param name="shouldDisplayFullName">Should the schema display full names or just names of classes.</param>
        /// <param name="typeToConvertedTypeMapping">Types that are converted by the serializer to other types.</param>
        public JsonSchemaGenerator(
            IReadOnlyCollection<Type> rootTypes,
            Func<AssemblyName, bool> isInScope,
            Func<MemberInfo, bool> shouldSerializeMember,
            bool shouldDisplayFullName = true,
            Dictionary<Type, Type> typeToConvertedTypeMapping = null)
            : base(
                rootTypes,
                isInScope,
                shouldSerializeMember)
        {
            _shouldDisplayFullName = shouldDisplayFullName;
            _typeToConvertedTypeMapping =
                typeToConvertedTypeMapping ?? new Dictionary<Type, Type>();
        }

        protected override string Generate(IReadOnlyCollection<Type> types)
        {
            types =
                types.
                    OrderBy(_ => _.Name).
                    ToList();

            var typeJObjects = new JArray();
            foreach (var type in types.Where(_ => !_.IsEnum))
            {
                var propertyJObjects = new JArray();
                GetSerializableMemberInfos(type).
                    OrderBy(_ => _.Name).
                    ForEach(
                        _ =>
                        {
                            var memberUnderlyingType = _.GetUnderlyingType();
                            propertyJObjects.Add(
                                new JObject
                                {
                                    { "name", _.Name },
                                    {
                                        "type",
                                        (_typeToConvertedTypeMapping.GetValueOrDefault(memberUnderlyingType) ?? memberUnderlyingType).
                                        GetDisplayName(_shouldDisplayFullName)
                                    }
                                });
                        });

                typeJObjects.Add(
                    new JObject
                    {
                        { "name", type.GetDisplayName(_shouldDisplayFullName) },
                        { "baseType", type.BaseType?.GetDisplayName(_shouldDisplayFullName) },
                        { "properties", propertyJObjects }
                    });
            }

            var enumJObjects = new JArray();
            foreach (var enumType in types.Where(_ => _.IsEnum))
            {
                enumJObjects.Add(
                    new JObject
                    {
                        { "name", enumType.Name },
                        { "values", new JArray(enumType.GetEnumNames()) }
                    });
            }

            return new JObject
            {
                { "types", typeJObjects },
                { "enums", enumJObjects }
            }.ToString();
        }

        /// <summary>
        /// Validate that all types have a parameterless constructor (important in some serialization methods),
        /// and that if the shouldDisplayFullName flag is off there are no duplicate type names.
        /// </summary>
        protected override void Validate(IReadOnlyCollection<Type> serializableTypes)
        {
            Ensure.NotNull(nameof(serializableTypes), serializableTypes);

            var duplicateNameTypes =
                serializableTypes.
                    Distinct().
                    GroupBy(_ => _.Name).
                    Where(_ => _.Count() > 1).
                    Select(_ => _.Key).
                    ToList();
            var missingParameterlessConstructorTypes =
                serializableTypes.
                    Where(_ => !_.HasParameterlessConstructor() && !_.IsAbstract).
                    ToList();
            if (!_shouldDisplayFullName && duplicateNameTypes.Any() ||
                missingParameterlessConstructorTypes.Any())
            {
                throw new ExtendedException(
                    "Found faulty types [" +
                    (_shouldDisplayFullName
                    ? ""
                    : $"{nameof(duplicateNameTypes)}={duplicateNameTypes.ToJoinString()} ") +
                    $"{nameof(missingParameterlessConstructorTypes)}={missingParameterlessConstructorTypes.ToJoinString()}]");
            }
        }
    }
}