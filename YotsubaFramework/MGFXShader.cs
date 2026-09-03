namespace Microsoft.Xna.Framework.Graphics
{
	public sealed class MGFXShader
	{
		public bool IsVertexShader;

		public string SourceFile;

		public string EntryPoint;

		public byte[] Bytecode;
		public byte[] CBuffers;
		internal VulkanShaderData Vulkan;
	}
}
