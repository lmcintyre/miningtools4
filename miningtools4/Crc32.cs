using System.Linq;

// pmgr on GitHub
namespace miningtools4
{
    /// <summary>
    /// Performs the 32-bit reversed variant of the cyclic redundancy check algorithm
    /// </summary>
    public class Crc32
    {
        private const uint Poly = 0xedb88320;

        private static readonly uint[] CrcArray =
            Enumerable.Range(0, 256).Select(i =>
            {
                var k = (uint)i;
                for (var j = 0; j < 8; j++)
                    k = (k & 1) != 0 ?
                        (k >> 1) ^ Poly :
                        k >> 1;

                return k;
            }).ToArray();

        public uint Checksum => ~Crc;

        public uint Crc = 0xFFFFFFFF;

        /// <summary>
        /// Initializes Crc32's state
        /// </summary>
        public void Init()
        {
            Crc = 0xFFFFFFFF;
        }

        /// <summary>
        /// Updates Crc32's state with new data
        /// </summary>
        /// <param name="data">Data to calculate the new CRC from</param>
        public void Update(byte[] data)
        {
            foreach (var b in data)
                Update(b);
        }

        public void Update(byte b)
        {
            Crc = CrcArray[(Crc ^ b) & 0xFF] ^
                  ((Crc >> 8) & 0x00FFFFFF);
        }

    }
}
