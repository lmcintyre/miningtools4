using Lumina.Data.Parsing;

namespace miningtools4
{
    public static class QuadExtensions
    {
        public static void SetA(this ref Quad q, ushort value)
        {
            q.Data &= (ulong) ~q.A << 48;
            q.Data |= (ulong) value << 48;
        }
        
        public static void SetB(this ref Quad q, ushort value)
        {
            q.Data &= (ulong) ~q.A << 32;
            q.Data |= (ulong) value << 32;
        }
        
        public static void SetC(this ref Quad q, ushort value)
        {
            q.Data &= (ulong) ~q.A << 16;
            q.Data |= (ulong) value << 16;
        }
        
        public static void SetD(this ref Quad q, ushort value)
        {
            q.Data &= (ulong) ~q.A;
            q.Data |= (ulong) value;
        }
    }
}