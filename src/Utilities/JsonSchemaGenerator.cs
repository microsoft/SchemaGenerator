using Common.Extensions;
using Common.Utilities;
using MoreLinq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Utilities
{
    public sealed class JsonSchemaGenerator : SchemaGenerator.SchemaGenerator
    {
        private readonly bool _shouldDisplayFullName;
        private readonly IDictionary<Type, Type> _typeToConvertedTypeMapping;

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
                throw new ExtendedException("Found faulty types [" +
                                            (_shouldDisplayFullName
                                                ? ""
                                                : $"{nameof(duplicateNameTypes)}={duplicateNameTypes.ToJoinString()} ") +
                                            $"{nameof(missingParameterlessConstructorTypes)}={missingParameterlessConstructorTypes.ToJoinString()}]");
            }
        }
    }
}