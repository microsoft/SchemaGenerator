using MoreLinq;
using SchemaGenerator.Core.Extensions;
using SchemaGenerator.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TypeLiteTypeScript = TypeLite.TypeScript;

namespace SchemaGenerator.TypeScript
{
    public sealed class TypeScriptSchemaGenerator : Core.SchemaGenerator
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
                TypeLiteTypeScript.Definitions().
                    WithShouldIgnoreMemberPredicate(memberInfo => !ShouldSerializeMember(memberInfo)).
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
                    Where(type => !typeof(IEnumerable).IsAssignableFrom(type)).
                    ToList();

            serializableTypes.ForEach(type => typeScriptFluent.For(type));

            return $"{typeScriptFluent.Generate().TrimStart().Replace("\t", "    ")}" +
                   $"{GenerateImports(serializableTypes)}";
        }

        protected override void Validate(IReadOnlyCollection<Type> types)
        {
            Ensure.NotNull(nameof(types), types);

            var duplicateNameTypes =
                types.
                    Distinct().
                    GroupBy(type => type.Name).
                    Where(nameToTypes => nameToTypes.Count() > 1).
                    Select(nameToTypes => nameToTypes.Key).
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
                type => 
                     type.IsGenericType
                         ? type.Name.Split("`", 2).
                             First()
                         : type.Name).
                OrderBy(nameToType => nameToType.Key).
                ForEach(nameToType => stringBuilder.AppendLine($"    export import {nameToType.Key} = {nameToType.Value.Namespace}.{nameToType.Key};"));
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }
    }
}