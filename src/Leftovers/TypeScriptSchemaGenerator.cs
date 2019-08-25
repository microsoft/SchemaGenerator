namespace Xxx.Tools.SchemaGenerator
{
    internal sealed class TypeScriptSchemaGenerator : Common.SchemaGenerator
    {
        public override IReadOnlyCollection<Type> RootTypes { get; } =
            Configuration.
                ControllerAssemblies.
                SelectMany(_ => _.GetTypes()).
                Where(_ => typeof(Controller).IsAssignableFrom(_)).
                SelectMany(_ => _.GetMethods()).
                Where(
                    _ => _.GetCustomAttributes(false).
                        Any(__ => __ is IActionHttpMethodProvider)).
                SelectMany(GetMethodTypes).
                Concat(Configuration.AdditionalTypes).
                Concat(Configuration.TypeToSerializationTypeMapping.Values).
                Distinct().
                ToList();

        public override string Generate()
        {
            var typeScriptFluent =
                TypeScript.Definitions().
                    WithSerializationTypes(Configuration.TypeToSerializationTypeMapping).
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

            typeScriptFluent.WithTypeFormatter<TsClass>(
                (_, __) =>
                {
                    var tsClass = (TsClass)_;
                    var typeName =
                        Configuration.TypeToTypeNameMapping.GetValueOrDefault(
                            tsClass.Type.IsGenericType
                                ? tsClass.Type.GetGenericTypeDefinition()
                                : tsClass.Type) ??
                        tsClass.Name;
                    var typeGenericArguments =
                        tsClass.GenericArguments.
                            Select(___ => $"{typeScriptFluent.ScriptGenerator.GetFullyQualifiedTypeName(___)}{(___ is TsCollection ? "[]" : string.Empty)}").
                            ToJoinString();
                    return $"{typeName}{(tsClass.Type.IsGenericType ? $"<{typeGenericArguments}>" : string.Empty)}";
                });

            Configuration.
                TypesConvertibleToString.
                ForEach(
                    _ => typeScriptFluent.
                        GetType().
                        GetMethod("WithConvertor").
                        MakeGenericMethod(_).
                        Invoke(
                            typeScriptFluent,
                            new object[]
                            {
                                (TypeConvertor)(__ => "string")
                            }));

            var serializableTypes =
                GetSerializableTypes().
                    Where(_ => !typeof(IEnumerable).IsAssignableFrom(_)).
                    Except(Configuration.TypeToSerializationTypeMapping.Keys).
                    Except(Configuration.TypesConvertibleToString).
                    ToList();

            serializableTypes.ForEach(_ => typeScriptFluent.For(_));

            return $"{typeScriptFluent.Generate().TrimStart().Replace("\t", "    ")}{Environment.NewLine}{GenerateImports(serializableTypes)}";
        }

        protected override bool ShouldSerializeMember(MemberInfo memberInfo)
        {
            Ensure.NotNull(nameof(memberInfo), memberInfo);

            return memberInfo.
                GetCustomAttribute<SerializeAttribute>()?.
                ShouldJsonSerialize(JsonScope.Ui) == true;
        }

        protected override Type GetSerializationType(Type type) =>
            Configuration.
                TypeToSerializationTypeMapping.
                GetValueOrDefault(type) ??
            type;

        private static IReadOnlyCollection<Type> GetMethodTypes(MethodInfo methodInfo) =>
            methodInfo.
                GetParameters().
                Select(_ => _.ParameterType).
                Concat(
                    new[]
                    {
                        methodInfo.ReturnType
                    }).
                Distinct().
                SelectMany(
                    _ =>
                    {
                        var genericArgumentTypes =
                            new List<Type>
                            {
                                _
                            };
                        for (var genericArgumentTypeIndex = 0; genericArgumentTypeIndex < genericArgumentTypes.Count; genericArgumentTypeIndex++)
                        {
                            genericArgumentTypes.AddRange(
                                genericArgumentTypes[genericArgumentTypeIndex].
                                    GetGenericArguments().
                                    Where(__ => !genericArgumentTypes.Contains(__)));
                        }

                        return genericArgumentTypes;
                    }).
                Where(_ => !typeof(Task).IsAssignableFrom(_)).
                Distinct().
                ToList();

        private string GenerateImports(IReadOnlyCollection<Type> types)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("declare namespace Schema {");
            types.ToDictionary(
                _ => Configuration.TypeToTypeNameMapping.GetValueOrDefault(_) ??
                     (_.IsGenericType
                         ? _.Name.Split("`", 2).
                             First()
                         : _.Name)).
                OrderBy(_ => _.Key).
                ForEach(_ => stringBuilder.AppendLine($"    export import {_.Key} = {_.Value.Namespace}.{_.Key};"));
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }

        private static class Configuration
        {
            public static IReadOnlyCollection<Assembly> ControllerAssemblies { get; } =
                new[]
                {
                    Assembly.Load($"{Xxx}")
                };
            public static IReadOnlyCollection<Type> AdditionalTypes { get; } = new[]
            {
                typeof(ClientSyncRequestType)
            };
            public static IDictionary<Type, Type> TypeToSerializationTypeMapping { get; } =
                CreateUiJsonSerializerSettings().
                    Converters.
                    OfType<IObjectJsonConverter>().
                    ToDictionary(
                        _ => _.SourceType,
                        _ => _.DestinationType);
            public static IReadOnlyCollection<Type> TypesConvertibleToString { get; } =
                new[]
                {
                    typeof(ByteSize)
                };
            public static IDictionary<Type, string> TypeToTypeNameMapping { get; } =
                new Dictionary<Type, string>
                {
                    [typeof(Xxx<>)] = $"{typeof(Xxx).Name}Generic"
                };
        }
    }
}