using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

internal class MGFXParameter
{
	public EffectParameterClass Class { get; set; }

	public EffectParameterType Type { get; set; }

	public string Name { get; set; }

	public string Semantic { get; set; }

	public byte RowCount { get; set; }

	public byte ColumnCount { get; set; }

	public MGFXParameter[] Elements { get; set; }

	public MGFXParameter[] StructMembers { get; set; }

	public object Data { get; set; }

}
