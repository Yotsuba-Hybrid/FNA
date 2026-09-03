using Microsoft.Xna.Framework.Graphics.Graphics;
using Microsoft.Xna.Framework.YotsubaFramework.Graphics;
using Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class MGFXShader
	{
		public bool IsVertexShader;

		public string SourceFile;

		public string EntryPoint;

		public byte[] Bytecode;
		internal VulkanShaderData Vulkan;

		public MGFXSampler[] Samplers;
		public byte[] CBuffers;

		public MGFXVertexAttribute[] VertexAttributes;
	}
}
