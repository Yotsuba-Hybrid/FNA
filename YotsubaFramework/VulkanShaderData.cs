namespace Microsoft.Xna.Framework.Graphics
{
	internal sealed class VulkanShaderData
	{
		public int UniformBufferCount;

		public uint UniformSlots;
		public uint TextureSlots;
		public uint SamplerSlots;

		public uint[] TextureTypes;

		public VulkanBinding[] Bindings;

		public byte[] Spirv;
	}

	internal struct VulkanBinding
	{
		public uint Binding;
		public uint DescriptorType;
		public uint DescriptorCount;
		public uint StageFlags;
		public ulong ImmutableSamplers;
	}
}
