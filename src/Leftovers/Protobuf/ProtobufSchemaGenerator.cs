namespace Xxx.Common
{
    public sealed class ProtobufSchemaGenerator : SchemaGenerator
    {
        public override IReadOnlyCollection<Type> RootTypes { get; } =
            new[]
            {
                typeof(Request\Response\Batch\Surrogate)
            };

        public override string Generate() => CreateRuntimeTypeModel().GetSchema(null);

        protected override bool ShouldSerializeMember(MemberInfo memberInfo)
        {
            Ensure.NotNull(nameof(memberInfo), memberInfo);

            return memberInfo.
                GetCustomAttribute<SerializeAttribute>()?.
                ProtobufMemberIndex.HasValue == true;
        }

        protected override void ValidateSerializableTypes(IReadOnlyCollection<Type> serializableTypes)
        {
            Ensure.NotNull(nameof(serializableTypes), serializableTypes);

            base.ValidateSerializableTypes(serializableTypes);

            var missingParameterlessConstructorTypes =
                serializableTypes.
                    Where(_ => !_.HasParameterlessConstructor()).
                    ToList();
            var duplicateProtobufMemberIndexTypes =
                serializableTypes.
                    Where(
                        type =>
                        {
                            var protobufMemberIndexes =
                                GetSerializableMemberInfos(type).
                                    Select(_ => _.GetCustomAttribute<SerializeAttribute>().ProtobufMemberIndex.Value).
                                    Concat(
                                        type.
                                            GetCustomAttributes<SerializeDerivedTypeAttribute>().
                                            Select(_ => _.ProtobufMemberIndex)).
                                    ToList();
                            return protobufMemberIndexes.Count != protobufMemberIndexes.Distinct().Count();
                        }).
                    ToList();
            if (missingParameterlessConstructorTypes.Any() || duplicateProtobufMemberIndexTypes.Any())
            {
                throw new ExtendedException(
                    "Faulty serializable types [" +
                    $"{nameof(missingParameterlessConstructorTypes)}={missingParameterlessConstructorTypes.ToJoinString()} " +
                    $"{nameof(duplicateProtobufMemberIndexTypes)}={duplicateProtobufMemberIndexTypes.ToJoinString()}]");
            }
        }

        public RuntimeTypeModel CreateRuntimeTypeModel()
        {
            var runtimeTypeModel = TypeModel.Create();
            runtimeTypeModel.AutoAddMissingTypes = false;
            runtimeTypeModel.IncludeDateTimeKind = true;

            SetProtobufSurrogate<IPAddress, IpAddressProtobufSurrogate>();

            var serializableTypes = GetSerializableTypes();
            serializableTypes.
                ForEach(
                    type =>
                    {
                        var metaType = runtimeTypeModel.Add(type, false);
                        metaType.EnumPassthru = true;

                        GetSerializableMemberInfos(type).
                            ForEach(
                                memberInfo =>
                                    metaType.Add(
                                        memberInfo.GetCustomAttribute<SerializeAttribute>().ProtobufMemberIndex.Value,
                                        memberInfo.Name));

                        var methods =
                            type.
                                GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).
                                ToList();
                        metaType.SetCallbacks(
                            methods.SingleOrDefault(_ => _.HasAttribute<OnSerializingAttribute>()),
                            methods.SingleOrDefault(_ => _.HasAttribute<OnSerializedAttribute>()),
                            methods.SingleOrDefault(_ => _.HasAttribute<OnDeserializingAttribute>()),
                            methods.SingleOrDefault(_ => _.HasAttribute<OnDeserializedAttribute>()));
                    });
            serializableTypes.
                ForEach(
                    type =>
                        type.GetCustomAttributes<SerializeDerivedTypeAttribute>().
                            ForEach(
                                serializeDerivedTypeAttribute =>
                                    runtimeTypeModel[type].AddSubType(
                                        serializeDerivedTypeAttribute.ProtobufMemberIndex,
                                        serializeDerivedTypeAttribute.DerivedType)));

            return runtimeTypeModel;

            void SetProtobufSurrogate<TType, TProtobufSurrogate>()
            {
                var metaType = runtimeTypeModel.Add(typeof(TType), false);
                metaType.SetSurrogate(typeof(TProtobufSurrogate));
            }
        }
    }
}