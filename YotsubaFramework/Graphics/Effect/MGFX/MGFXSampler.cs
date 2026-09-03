using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.YotsubaFramework.Graphics;

public class MGFXSampler
{

	public bool HasSamplerState { get; set; }
	public byte Type { get; set; }

	public byte TextureSlot { get; set; }

	public byte SamplerSlot  { get; set; }

	public TextureAddressMode  AddressModeU { get; set; }
	public TextureAddressMode  AddressModeV { get; set; }
	public TextureAddressMode  AddressModeW { get; set; }


	public byte BorderR { get; set; }
	public byte BorderG { get; set; }
	public byte BorderB { get; set; }
	public byte BorderA { get; set; }

	public TextureFilter TextureFilter { get; set; }

	public int MaxAnisotropy  { get; set; }

	public int MaxMipLevel { get; set; }
	public float MipMapLodBias {get; set; }

	public byte Parameter{get; set; }
	public string Name { get; set; }

}
