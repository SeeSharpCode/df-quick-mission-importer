namespace DFQuickMissionImporter.PFF
{
    public partial class PFFArchive
    {
        private const string PFF3 = "PFF3";
        private const int HEADER_SIZE = 20;

        private readonly byte[] _ipAddress;

        public Dictionary<string, PFFEntry> Entries { get; }
        public int TotalContentSize => Entries.Values.Sum(entry => entry.Size);

        public PFFArchive(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            reader.BaseStream.Seek(8, 0);
            var fileCount = reader.ReadInt32();

            reader.BaseStream.Seek(16, 0);
            var fileEntriesOffset = reader.ReadInt32();

            Entries = LoadEntries(reader, fileCount, fileEntriesOffset);

            reader.BaseStream.Seek(-12, SeekOrigin.End);
            _ipAddress = reader.ReadBytes(4);
        }

        private static Dictionary<string, PFFEntry> LoadEntries(BinaryReader reader, int fileCount, int fileEntriesOffset)
        {
            var entries = new Dictionary<string, PFFEntry>();

            reader.BaseStream.Seek(fileEntriesOffset, 0);
            for (int i = 0; i < fileCount; i++)
            {
                var entryOffset = fileEntriesOffset + (i * 32);
                reader.BaseStream.Seek(entryOffset, 0);
                
                var isDeleted = reader.ReadInt32() == 1;
                // we don't care about deleted entries
                if (isDeleted)
                {
                    continue;
                }

                var contentOffset = reader.ReadInt32();
                var size = reader.ReadInt32();
                var timestamp = reader.ReadInt32();
                var fileName = reader.ReadNullTermString().Trim();

                reader.BaseStream.Seek(contentOffset, 0);
                var content = reader.ReadBytes(size);

                var entry = new PFFEntry(fileName, size, timestamp, content);
                entries.Add(fileName.ToUpper(), entry);
            }

            return entries;
        }

        public byte[] ToBytes()
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(HEADER_SIZE);
            writer.Write(PFF3.ToCharArray());
            writer.Write(Entries.Count);
            writer.Write(PFFEntry.ENTRY_RECORD_SIZE);

            var entryRecordsOffset = HEADER_SIZE + TotalContentSize;
            writer.Write(entryRecordsOffset);

            Entries.Values.ToList().ForEach(entry => writer.Write(entry.Contents));

            var contentsOffset = HEADER_SIZE;
            Entries.Values.ToList().ForEach(entry =>
            {
                writer.Write(0); // not deleted
                writer.Write(contentsOffset);
                contentsOffset += entry.Size;
                writer.Write(entry.Size);
                writer.Write(entry.ModifiedTimestamp);

                var fileName = entry.Name.Substring(0, Math.Min(entry.Name.Length, 15));
                fileName = fileName.PadRight(15, '\0');
                writer.Write(fileName.ToCharArray());
                writer.Write((byte)0);
            });

            writer.Write(_ipAddress);
            writer.Write(0);
            writer.Write("KING".ToCharArray());

            return stream.ToArray();
        }
    }
}
