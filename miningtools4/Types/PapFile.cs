using Lumina.Data;
using Lumina.Extensions;

namespace miningtools4;

public class PapFile : FileResource
{
	public ushort SkeletonId { get; private set; }
	public ushort BaseId { get; private set; }
	
	public override void LoadFile()
	{
		Reader.Seek(0xA);
		SkeletonId = Reader.ReadUInt16();
		BaseId = Reader.ReadUInt16();
	}
}