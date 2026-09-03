using Microsoft.Xna.Framework.Graphics.Graphics;

namespace Microsoft.Xna.Framework.Graphics
{
	internal class MGFXEffectData
	{
		public MGFXHeader Header { get; set; }

		public MGFXShader[] Shaders { get; set; }

		public MGFXConstantBuffer[] ConstantBuffers { get; set; }
	}
}
