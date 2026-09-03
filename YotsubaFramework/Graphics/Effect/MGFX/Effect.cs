using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics.Graphics;

namespace Microsoft.Xna.Framework.Graphics
{
	public partial class Effect : GraphicsResource
	{
		private MGFXEffectData MonoGameEffect;
		public Effect(
			GraphicsDevice graphicsDevice,
			byte[] effectCode,
			int index,
			int count)
		{
			GraphicsDevice = graphicsDevice;

			if (IsMonoGameEffect(effectCode, index))
			{
				LoadMonoGameEffect(
					graphicsDevice,
					effectCode,
					index,
					count
				);

				return;
			}

			// Send the blob to the GLDevice to be parsed/compiled
			IntPtr effectData;
			FNA3D.FNA3D_CreateEffect(
				graphicsDevice.GLDevice,
				effectCode,
				effectCode.Length,
				out glEffect,
				out effectData
			);

			this.effectData = effectData;

			// This is where it gets ugly...
			INTERNAL_parseEffectStruct(effectData);

			// The default technique is the first technique.
			CurrentTechnique = Techniques[0];
		}

		private bool IsMonoGameEffect(byte[] effectCode, int index)
		{

			if (effectCode == null ||
			    index < 0 ||
			    effectCode.Length - index < 4)
			{
				return false;
			}

			return BitConverter.ToInt32(effectCode, index) == MGFXHeader.MGFXSignature;
		}


		private void LoadMonoGameEffect(GraphicsDevice graphicsDevice,
			byte[] effectCode,
			int index,
			int count)
		{
			MGFXHeader header = ReadHeader(effectCode, 0);

			int headerSize = header.HeaderSize;

			(MGFXShader[], MGFXConstantBuffer[]) effectData;
			using (var stream = new MemoryStream(effectCode,
				       index + header.HeaderSize,
				       count - header.HeaderSize,
				       false))
			{
				using (var reader = new BinaryReader(stream))
				{
					effectData = ReadMonoGameEffect(header, reader);
				}
			}

			MonoGameEffect.Header = header;
			MonoGameEffect.Shaders = effectData.Item1;
			MonoGameEffect.ConstantBuffers = effectData.Item2;
		}


		private MGFXHeader ReadHeader(byte[] effectCode, int index)
		{
			MGFXHeader header;

			header.Signature = BitConverter.ToInt32(effectCode, index); index += 4;
			header.Version = (int)effectCode[index++];
			header.Profile = (int)effectCode[index++];
			header.EffectKey = BitConverter.ToInt32(effectCode, index); index += 4;
			header.HeaderSize = 10;

			if (header.Signature != MGFXHeader.MGFXSignature)
				throw new Exception("This does not appear to be a MonoGame MGFX file!");
			if (header.Version < MGFXHeader.MGFXMinVersion)
				throw new Exception("This MGFX effect is for an older release of MonoGame and needs to be rebuilt.");
			if (header.Version > MGFXHeader.MGFXVersion)
				throw new Exception("This MGFX effect seems to be for a newer release of MonoGame.");

			if (header.Profile != Shader.Profile)
				throw new Exception("This MGFX effect was built for a different platform!");

			return header;
		}


		private (MGFXShader[], MGFXConstantBuffer[]) ReadMonoGameEffect(MGFXHeader header, BinaryReader reader)
		{

			int constantBufferCount = reader.ReadInt32();

			Console.WriteLine(
				$"MGFX ConstantBuffers: {constantBufferCount}"
			);

			MGFXConstantBuffer[] constantBuffers = new MGFXConstantBuffer[constantBufferCount];
			for (int c = 0; c < constantBufferCount; c++)
			{
				string name = reader.ReadString();

				int sizeInBytes = reader.ReadInt16();

				int parameterCount = reader.ReadInt32();



				MGFXConstantBufferParameter[] constantBufferParameters = new MGFXConstantBufferParameter[parameterCount];

				for (int i = 0; i < parameterCount; i++)
				{
					int parameterIndex = reader.ReadInt32();
					ushort offset = reader.ReadUInt16();
					constantBufferParameters[i] = new MGFXConstantBufferParameter()
					{
						ParameterIndex = parameterIndex,
						Offset = offset
					};
				}

				constantBuffers[c] = new MGFXConstantBuffer()
				{
					Name = name,
					BufferSize = sizeInBytes,
					Parameters = constantBufferParameters
				};

			}

			int shaderCount = reader.ReadInt32();

			var shaders = new MGFXShader[shaderCount];

			for (var i = 0; i < shaders.Length; i++)
			{
				shaders[i] = ReadMonoGameShader(reader, header.Version);
			}

			return (shaders, constantBuffers);
		}


		private MGFXShader ReadMonoGameShader(BinaryReader reader, int version)
		{
		    var result = new MGFXShader();

		    result.IsVertexShader = reader.ReadBoolean();

		    if (version > 10)
		    {
		        result.SourceFile = reader.ReadString();
		        result.EntryPoint = reader.ReadString();
		    }
		    else
		    {
		        result.SourceFile = "<unknown>";
		        result.EntryPoint = "<unknown>";
		    }

		    int shaderLength = reader.ReadInt32();
		    byte[] shaderBytecode = reader.ReadBytes(shaderLength);

		    VulkanShaderData vulkan = ReadVulkanShaderCode(shaderBytecode);
		    result.Vulkan = vulkan;
		    result.Bytecode = vulkan.Spirv;

		    if (shaderBytecode.Length != shaderLength)
		    {
			    throw new EndOfStreamException(
				    $"Expected {shaderLength} shader bytes, " +
				    $"but only read {shaderBytecode.Length}."
			    );
		    }
		    if (result.Bytecode.Length >= 4)
		    {
			    uint magic = BitConverter.ToUInt32(
				    result.Bytecode,
				    0
			    );

			    Console.WriteLine(
				    $"Magic: 0x{magic:X8}"
			    );
		    }

		    Console.WriteLine(
		        $"Shader: {(result.IsVertexShader ? "Vertex" : "Pixel")}"
		    );

		    Console.WriteLine($"Source: {result.SourceFile}");
		    Console.WriteLine($"Entry: {result.EntryPoint}");
		    Console.WriteLine($"Bytecode: {shaderLength} bytes");

		    // Samplers
		    int samplerCount = reader.ReadByte();

		    for (int s = 0; s < samplerCount; s++)
		    {
		        byte type = reader.ReadByte();
		        byte textureSlot = reader.ReadByte();
		        byte samplerSlot = reader.ReadByte();

		        bool hasSamplerState = reader.ReadBoolean();

		        if (hasSamplerState)
		        {
		            byte addressU = reader.ReadByte();
		            byte addressV = reader.ReadByte();
		            byte addressW = reader.ReadByte();

		            byte borderR = reader.ReadByte();
		            byte borderG = reader.ReadByte();
		            byte borderB = reader.ReadByte();
		            byte borderA = reader.ReadByte();

		            byte filter = reader.ReadByte();

		            int maxAnisotropy = reader.ReadInt32();
		            int maxMipLevel = reader.ReadInt32();
		            float mipMapLodBias = reader.ReadSingle();
		        }

		        string name = reader.ReadString();
		        byte parameter = reader.ReadByte();
		    }

		    // Constant buffers usados por este shader
		    int cbufferCount = reader.ReadByte();

		    result.CBuffers = new byte[cbufferCount];

		    for (int c = 0; c < cbufferCount; c++)
		        result.CBuffers[c] = reader.ReadByte();

		    // Vertex attributes
		    int attributeCount = reader.ReadByte();

		    for (int a = 0; a < attributeCount; a++)
		    {
		        string name = reader.ReadString();
		        byte usage = reader.ReadByte();
		        byte index = reader.ReadByte();
		        short location = reader.ReadInt16();
		    }

		    return result;
		}



		private static VulkanShaderData ReadVulkanShaderCode(byte[] shaderCode)
		{
			using (var MemoryStream = new MemoryStream(shaderCode))
			{
				using (BinaryReader reader = new BinaryReader(MemoryStream))
				{
					int uniformBufferCount = reader.ReadInt32();

					uint uniformSlots = reader.ReadUInt32();
					uint textureSlots = reader.ReadUInt32();
					uint samplerSlots = reader.ReadUInt32();

					uint[] textureTypes = new uint[16];

					for (var i = 0; i < textureTypes.Length; i++)
					{
						textureTypes[i] = reader.ReadUInt32();
					}

					uint bindingCount = reader.ReadUInt32();

					Console.WriteLine($"Uniform buffers: {uniformBufferCount}");
					Console.WriteLine($"Uniform slots: 0x{uniformSlots:X8}");
					Console.WriteLine($"Texture slots: 0x{textureSlots:X8}");
					Console.WriteLine($"Sampler slots: 0x{samplerSlots:X8}");
					Console.WriteLine($"Bindings: {bindingCount}");

					VulkanBinding[] bindings = new VulkanBinding[bindingCount];
					for (int i = 0; i < bindingCount; i++)
					{
						uint binding = reader.ReadUInt32();
						uint descriptorType = reader.ReadUInt32();
						uint descriptorCount = reader.ReadUInt32();
						uint stageFlags = reader.ReadUInt32();
						ulong immutableSamplers = reader.ReadUInt64();

						bindings[i] = new VulkanBinding()
						{
							Binding = binding,
							DescriptorType = descriptorType,
							DescriptorCount = descriptorCount,
							StageFlags = stageFlags,
							ImmutableSamplers = immutableSamplers,
						};
						Console.WriteLine(
							$"Binding {binding}: " +
							$"type={descriptorType}, " +
							$"count={descriptorCount}, " +
							$"stage=0x{stageFlags:X}");
					}


					//Pure SPIRV Code
					byte[] spirv = reader.ReadBytes((int) (MemoryStream.Length - MemoryStream.Position));

					if (spirv.Length >= 4)
					{
						uint magic = BitConverter.ToUInt32(spirv, 0);

						Console.WriteLine($"SPIR-V bytes: {spirv.Length}");
						Console.WriteLine($"SPIR-V Magic: 0x{magic:X8}");
					}

					return new VulkanShaderData()
					{
						UniformBufferCount = uniformBufferCount,
						UniformSlots = uniformSlots,
						TextureSlots = textureSlots,
						SamplerSlots = samplerSlots,
						Bindings = bindings,
						Spirv = spirv,
						TextureTypes = textureTypes
					};
				}
			}
		}

	}
}
