namespace Xxx.Common
{
    [PublicAPI]
    public sealed class IpAddressProtobufSurrogate
    {
        [Serialize(1)]
        public byte[] Value { get; private set; }

        private IpAddressProtobufSurrogate()
        {
        }

        private IpAddressProtobufSurrogate(byte[] value) =>
            Value = value;

        public static implicit operator IpAddressProtobufSurrogate(IPAddress ipAddress) =>
            new IpAddressProtobufSurrogate(ipAddress?.GetAddressBytes());

        public static implicit operator IPAddress(IpAddressProtobufSurrogate ipAddressProtobufSurrogate) =>
            ipAddressProtobufSurrogate.Value == null
                ? null
                : new IPAddress(ipAddressProtobufSurrogate.Value);
    }
}