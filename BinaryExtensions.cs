using System.Text;

namespace DFQuickMissionImporter
{
    internal static class BinaryExtensions
    {
        internal static string ReadNullTermString(this BinaryReader reader)
        {
            var bytes = new List<byte>();

            byte currentByte;
            while ((currentByte = reader.ReadByte()) != 0)
            {
                bytes.Add(currentByte);
            }

            return new string(Encoding.UTF8.GetString(bytes.ToArray()));
        }
    }
}
