using Microsoft.Xna.Framework.Graphics.Graphics;
using Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

namespace Microsoft.Xna.Framework.Graphics
{
	internal class MGFXEffectData
	{
		public MGFXHeader Header { get; set; }

		public MGFXConstantBuffer[] ConstantBuffers { get; set; }

		public MGFXShader[] Shaders { get; set; }

		public MGFXParameter[] Parameters { get; set; }

		public MGFXTechnique[] Techniques { get; set; }
	}
}
