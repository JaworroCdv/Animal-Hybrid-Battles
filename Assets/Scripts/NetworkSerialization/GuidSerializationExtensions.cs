namespace AnimalHybridBattles.NetworkSerialization
{
    using System;
    using Unity.Netcode;

    public static class GuidSerializationExtensions
    {
        public static void WriteValueSafe(this FastBufferWriter writer, in Guid guid)
        {
            writer.WriteValueSafe(guid.ToString());
        }

        public static void ReadValueSafe(this FastBufferReader reader, out Guid guid)
        {
            reader.ReadValueSafe(out string guidString);
            guid = Guid.Parse(guidString);
        }

        public static void DuplicateValue(in Guid value, ref Guid duplicatedvalue)
        {
            duplicatedvalue = value;
        }
    }
}