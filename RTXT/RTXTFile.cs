namespace DFQuickMissionImporter.RTXT
{
    internal class RTXTFile
    {
        private const string RTXT = "RTXT";
        private const int HEADER_SIZE = 16;
        private const int VALUE_RECORD_SIZE = 16;
        private const int GROUP_RECORD_SIZE = 8;

        internal record StringValue(string Content, int Unknown1 = 0, int Unknown2 = 0, int Unknown3 = 0)
        {
            // includes omitted relative offset field
            // does not include Content which is loaded from the values array
            public const int RECORD_SIZE = 16;
        }

        public Dictionary<string, Dictionary<string, StringValue>> Entries { get; } = new();

        public RTXTFile(byte[] fileContents)
        {
            using var stream = new MemoryStream(fileContents);
            using var reader = new BinaryReader(stream);

            reader.BaseStream.Seek(4, 0);
            var groupCountOffset = reader.ReadInt32();

            reader.BaseStream.Seek(4, SeekOrigin.Current);
            var valuesCount = reader.ReadInt32();

            var valueRecordsOffset = reader.BaseStream.Position;
            var valueArrayOffset = HEADER_SIZE + valuesCount * VALUE_RECORD_SIZE;

            var stringValues = new StringValue[valuesCount];

            for (int i = 0; i < valuesCount; i++)
            {
                reader.BaseStream.Seek(valueRecordsOffset + i * VALUE_RECORD_SIZE, 0);
                var valueOffset = reader.ReadInt32();
                var unknown1 = reader.ReadInt32();
                var unknown2 = reader.ReadInt32();
                var unknown3 = reader.ReadInt32();

                reader.BaseStream.Seek(valueArrayOffset + valueOffset, 0);
                var content = reader.ReadNullTermString();

                stringValues[i] = new StringValue(content, unknown1, unknown2, unknown3);
            }

            reader.BaseStream.Seek(groupCountOffset, 0);
            var groupCount = reader.ReadInt32();
            var groupRecordsOffset = reader.BaseStream.Position;

            var groupNamesOffset = reader.BaseStream.Position + groupCount * GROUP_RECORD_SIZE;
            var currentGroupNameOffset = groupNamesOffset;

            var valueIndex = 0;
            for (int i = 0; i < groupCount; i++)
            {
                reader.BaseStream.Seek(groupRecordsOffset + i * GROUP_RECORD_SIZE, 0);
                var keysOffset = reader.ReadInt32();
                var keysCount = reader.ReadInt32();

                reader.BaseStream.Seek(currentGroupNameOffset, 0);
                var groupName = reader.ReadNullTermString();
                currentGroupNameOffset = reader.BaseStream.Position;

                reader.BaseStream.Seek(groupRecordsOffset + keysOffset, 0);
                var groupEntries = new Dictionary<string, StringValue>();
                for (int j = 0; j < keysCount; j++)
                {
                    var key = reader.ReadNullTermString();
                    groupEntries.Add(key, stringValues[valueIndex]);
                    valueIndex++;
                }

                Entries.Add(groupName, groupEntries);
            }
        }

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(RTXT.ToCharArray());

            var groupNames = Entries.Keys;
            var groupNamesLength = groupNames.Sum(g => g.Length + 1);

            var keys = Entries.Values.SelectMany(kvPair => kvPair.Select(pair => pair.Key));
            var keysLength = keys.Sum(k => k.Length + 1);

            var values = Entries.Values.SelectMany(keyValuePairs => keyValuePairs.Select(pair => pair.Value)).ToList();
            var valuesLength = values.Sum(value => value.Content.Length + 1);

            var groupTableOffset = HEADER_SIZE + VALUE_RECORD_SIZE * values.Count + valuesLength;
            writer.Write(groupTableOffset);

            var groupDataSize = GROUP_RECORD_SIZE * groupNames.Count() + groupNamesLength + keysLength;
            writer.Write(groupDataSize);

            writer.Write(values.Count);

            var valueOffset = 0;
            for (int i = 0; i < values.Count; i++)
            {
                writer.Write(valueOffset);

                var value = values[i];
                valueOffset += value.Content.Length + 1;
                writer.Write(value.Unknown1);
                writer.Write(value.Unknown2);
                writer.Write(value.Unknown3);
            }

            values.ForEach(value =>
            {
                writer.Write(value.Content.ToCharArray());
                writer.Write((byte)0);
            });

            writer.Write(groupNames.Count);
            var groupRecordOffset = writer.BaseStream.Position;

            var keyOffset = GROUP_RECORD_SIZE * groupNames.Count + groupNamesLength;
            foreach (var entry in Entries)
            {
                writer.Write(keyOffset);

                var groupKeys = entry.Value.Select(pair => pair.Key);
                keyOffset += groupKeys.Sum(k => k.Length + 1);

                writer.Write(groupKeys.Count());
            }

            groupNames.ToList().ForEach(groupName =>
            {
                writer.Write(groupName.ToCharArray());
                writer.Write((byte)0);
            });

            keys.ToList().ForEach(key =>
            {
                writer.Write(key.ToCharArray());
                writer.Write((byte)0);
            });

            return stream.ToArray();
        }
    }
}