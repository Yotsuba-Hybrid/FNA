using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

public struct MGFXVertexAttribute
{
	public string Name { get; set; }

	public VertexElementUsage Usage { get; set; }

	public byte UsageIndex { get; set; }
	public short Location { get; set; }
}
