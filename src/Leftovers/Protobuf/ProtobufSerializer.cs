namespace Xxx.Common
{
    public sealed class ProtobufSerializer : Serializer
    {
        private static readonly Lazy<RuntimeTypeModel> _runtimeTypeModel =
            new Lazy<RuntimeTypeModel>(() => new ProtobufSchemaGenerator().CreateRuntimeTypeModel());

        public ProtobufSerializer(
            MemoryStreamPoolConfiguration memoryStreamPoolConfiguration,
            bool isCompressionEnabled)
            : base(
                memoryStreamPoolConfiguration,
                isCompressionEnabled)
        {
        }

        protected override void SerializeInternal<TObject>(Stream stream, TObject obj)
        {
            Ensure.NotNull(nameof(stream), stream);
            Ensure.NotNull(nameof(obj), obj);

            _runtimeTypeModel.Value.Serialize(stream, obj);
        }

        protected override TObject DeserializeInternal<TObject>(Stream stream)
        {
            Ensure.NotNull(nameof(stream), stream);

            return (TObject)_runtimeTypeModel.Value.Deserialize(
                stream,
                null,
                typeof(TObject));
        }
    }
}