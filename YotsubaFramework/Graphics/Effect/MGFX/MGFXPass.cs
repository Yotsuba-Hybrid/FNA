using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

public class MGFXPass
{
	public string Name { get; set; }

	public int VertexShaderIndex { get; set; }

	public int PixelShaderIndex { get; set; }

	public BlendState BlendState { get; set; }

	public DepthStencilState DepthStencilState { get; set; }

	public RasterizerState RasterizerState { get; set; }
}
