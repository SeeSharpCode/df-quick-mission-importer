namespace DFQuickMissionImporter.PFF
{
    public class PFFEntry
    {
        internal const int ENTRY_RECORD_SIZE = 32;

        public PFFEntry(string name, int size, int modifiedTimestamp, byte[] contents)
        {
            Name = name;
            Size = size;
            ModifiedTimestamp = modifiedTimestamp;
            Contents = contents;
        }

        public string Name { get; }
        public int Size { get; private set; }
        public int ModifiedTimestamp { get; }

        private byte[] _contents;
        public byte[] Contents
        {
            get => _contents;
            set
            {
                _contents = value;
                Size = value.Length;
            }
        }
    }
}
