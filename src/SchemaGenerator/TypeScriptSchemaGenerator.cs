using MoreLinq;
using SchemaGenerator.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TypeLite;

namespace SchemaGenerator
{
    public sealed class TypeScriptSchemaGenerator : SchemaGenerator
    {
        private readonly string _shortcutSchemaName;

        public TypeScriptSchemaGenerator(
            IReadOnlyCollection<Type> rootTypes,
            Func<AssemblyName, bool> isInScope,
            Func<MemberInfo, bool> shouldSerializeMember,
            string shortcutSchemaName = "Schema")
        : base(
            rootTypes,
            isInScope,
            shouldSerializeMember)
        {
            _shortcutSchemaName = shortcutSchemaName;
        }

        protected override string Generate(IReadOnlyCollection<Type> types)
        {
            var typeScriptFluent =
                TypeScript.Definitions().
                    WithShouldIgnoreMemberPredicate(_ => !ShouldSerializeMember(_)).
                    WithShouldIncludeAllEnums(false).
                    AsCollectionDictionaries(false).
                    AsConstEnums().
                    AsStringEnums();

            typeScriptFluent.WithMemberTypeFormatter(
                (tsProperty, memberTypeName) =>
                    tsProperty.PropertyType.Type.HasAttribute<FlagsAttribute>()
                        ? $"{memberTypeName}[]"
                        : typeScriptFluent.ScriptGenerator.DefaultMemberTypeFormatter(tsProperty, memberTypeName));

            var serializableTypes =
                types.
                    Where(_ => !typeof(IEnumerable).IsAssignableFrom(_)).
                    ToList();

            serializableTypes.ForEach(_ => typeScriptFluent.For(_));

            return $"{typeScriptFluent.Generate().TrimStart().Replace("\t", "    ")}" +
                   $"{GenerateImports(serializableTypes)}";
        }

        protected override void Validate(IReadOnlyCollection<Type> types)
        {
            Ensure.NotNull(nameof(types), types);

            var duplicateNameTypes =
                types.
                    Distinct().
                    GroupBy(_ => _.Name).
                    Where(_ => _.Count() > 1).
                    Select(_ => _.Key).
                    ToList();
            if (_shortcutSchemaName != null && duplicateNameTypes.Any())
            {
                throw new ExtendedException(
                    "Types with duplicate name are not supported with shortcut schema name option [" +
                    $"{nameof(duplicateNameTypes)}={duplicateNameTypes.ToJoinString()}]");
            }
        }

        private string GenerateImports(IReadOnlyCollection<Type> types)
        {
            if (_shortcutSchemaName == null)
            {
                return "";
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"declare namespace {_shortcutSchemaName} {{");
            types.ToDictionary(
                _ => 
                     _.IsGenericType
                         ? _.Name.Split("`", 2).
                             First()
                         : _.Name).
                OrderBy(_ => _.Key).
                ForEach(_ => stringBuilder.AppendLine($"    export import {_.Key} = {_.Value.Namespace}.{_.Key};"));
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }
    }
}
