using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics.Graphics;
using Microsoft.Xna.Framework.YotsubaFramework.Graphics;
using Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

namespace Microsoft.Xna.Framework.Graphics
{
	public partial class Effect : GraphicsResource
	{
		public bool IsMGFXEffect { get; internal set; } = false;
		private MGFXEffectData MonoGameEffect = new();
		public Effect(
			GraphicsDevice graphicsDevice,
			byte[] effectCode,
			int index,
			int count)
		{
			GraphicsDevice = graphicsDevice;

			if (IsMonoGameEffect(effectCode, index))
			{
				IsMGFXEffect = true;
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

		private void BuildMGFXParameters()
		{
			Parameters =
				BuildMGFXParameters(
					MonoGameEffect.Parameters,
					true
				);
		}
		#region  Setup MGFX Context

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
			MGFXHeader header = ReadHeader(effectCode, index);
			MonoGameEffect.Header = header;

			int headerSize = header.HeaderSize;

			using (var stream = new MemoryStream(effectCode,
				       index + header.HeaderSize,
				       count - header.HeaderSize,
				       false))
			{
				using (var reader = new BinaryReader(stream))
				{
					ReadMonoGameEffect(header, reader);
				}
			}


			BuildMGFXTechniques();
			BuildMGFXParameters();
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


			private void ReadMonoGameEffect(MGFXHeader header, BinaryReader reader)
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

				MonoGameEffect.Parameters = ReadParameters(reader);
				MonoGameEffect.Techniques =ReadTechniques(reader);
				MonoGameEffect.Shaders = shaders;
				MonoGameEffect.ConstantBuffers = constantBuffers;
				int tailSignature = reader.ReadInt32();

				if (tailSignature != MGFXHeader.MGFXSignature)
				{
					throw new InvalidDataException(
						$"Invalid MGFX tail signature. " +
						$"Expected 0x{MGFXHeader.MGFXSignature:X8}, " +
						$"but got 0x{tailSignature:X8}."
					);
				}

				if (reader.BaseStream.Position != reader.BaseStream.Length)
				{
					throw new InvalidDataException(
						$"MGFX parser did not consume the entire stream. " +
						$"Position: {reader.BaseStream.Position}, " +
						$"Length: {reader.BaseStream.Length}, " +
						$"Remaining: {reader.BaseStream.Length - reader.BaseStream.Position}"
					);
				}
				Console.WriteLine("MGFX parsed successfully.");
			}

			private static MGFXTechnique[] ReadTechniques(
				BinaryReader reader)
			{
				int techniqueCount =
					reader.ReadInt32();

				MGFXTechnique[] techniques =
					new MGFXTechnique[techniqueCount];

				for (int i = 0; i < techniqueCount; i++)
				{
					string name =
						reader.ReadString();

					ReadAnnotations(reader);

					MGFXPass[] passes =
						ReadPasses(reader);

					techniques[i] =
						new MGFXTechnique
						{
							Name = name,
							Passes = passes
						};
				}

				return techniques;
			}

			private static void ReadAnnotations(BinaryReader reader)
			{
				int count = reader.ReadInt32();

				if (count == 0)
					return;

				// MonoGame actualmente tampoco deserializa
				// el contenido real de las annotations.
				//
				// Importante:
				// NO leas más bytes aquí.
			}

			private static MGFXPass[] ReadPasses(BinaryReader reader)
{
    int passCount = reader.ReadInt32();

    MGFXPass[] passes =
        new MGFXPass[passCount];

    for (int i = 0; i < passCount; i++)
    {
        string name =
            reader.ReadString();

        ReadAnnotations(reader);


        // ========================================================
        // Shaders
        // ========================================================

        int vertexShaderIndex =
            reader.ReadInt32();

        int pixelShaderIndex =
            reader.ReadInt32();


        // ========================================================
        // BlendState
        // ========================================================

        BlendState blendState = null;

        bool hasBlendState =
            reader.ReadBoolean();

        if (hasBlendState)
        {
            blendState = new BlendState
            {
                AlphaBlendFunction =
                    (BlendFunction)reader.ReadByte(),

                AlphaDestinationBlend =
                    (Blend)reader.ReadByte(),

                AlphaSourceBlend =
                    (Blend)reader.ReadByte(),

                BlendFactor = new Color(
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte(),
                    reader.ReadByte()
                ),

                ColorBlendFunction =
                    (BlendFunction)reader.ReadByte(),

                ColorDestinationBlend =
                    (Blend)reader.ReadByte(),

                ColorSourceBlend =
                    (Blend)reader.ReadByte(),

                ColorWriteChannels =
                    (ColorWriteChannels)reader.ReadByte(),

                ColorWriteChannels1 =
                    (ColorWriteChannels)reader.ReadByte(),

                ColorWriteChannels2 =
                    (ColorWriteChannels)reader.ReadByte(),

                ColorWriteChannels3 =
                    (ColorWriteChannels)reader.ReadByte(),

                MultiSampleMask =
                    reader.ReadInt32()
            };
        }


        // ========================================================
        // DepthStencilState
        // ========================================================

        DepthStencilState depthStencilState = null;

        bool hasDepthStencilState =
            reader.ReadBoolean();

        if (hasDepthStencilState)
        {
            depthStencilState = new DepthStencilState
            {
                CounterClockwiseStencilDepthBufferFail =
                    (StencilOperation)reader.ReadByte(),

                CounterClockwiseStencilFail =
                    (StencilOperation)reader.ReadByte(),

                CounterClockwiseStencilFunction =
                    (CompareFunction)reader.ReadByte(),

                CounterClockwiseStencilPass =
                    (StencilOperation)reader.ReadByte(),

                DepthBufferEnable =
                    reader.ReadBoolean(),

                DepthBufferFunction =
                    (CompareFunction)reader.ReadByte(),

                DepthBufferWriteEnable =
                    reader.ReadBoolean(),

                ReferenceStencil =
                    reader.ReadInt32(),

                StencilDepthBufferFail =
                    (StencilOperation)reader.ReadByte(),

                StencilEnable =
                    reader.ReadBoolean(),

                StencilFail =
                    (StencilOperation)reader.ReadByte(),

                StencilFunction =
                    (CompareFunction)reader.ReadByte(),

                StencilMask =
                    reader.ReadInt32(),

                StencilPass =
                    (StencilOperation)reader.ReadByte(),

                StencilWriteMask =
                    reader.ReadInt32(),

                TwoSidedStencilMode =
                    reader.ReadBoolean()
            };
        }


        // ========================================================
        // RasterizerState
        // ========================================================

        RasterizerState rasterizerState = null;

        bool hasRasterizerState =
            reader.ReadBoolean();

        if (hasRasterizerState)
        {
            rasterizerState = new RasterizerState
            {
                CullMode =
                    (CullMode)reader.ReadByte(),

                DepthBias =
                    reader.ReadSingle(),

                FillMode =
                    (FillMode)reader.ReadByte(),

                MultiSampleAntiAlias =
                    reader.ReadBoolean(),

                ScissorTestEnable =
                    reader.ReadBoolean(),

                SlopeScaleDepthBias =
                    reader.ReadSingle()
            };
        }


        // ========================================================
        // Store
        // ========================================================

        passes[i] = new MGFXPass
        {
            Name = name,

            VertexShaderIndex =
                vertexShaderIndex,

            PixelShaderIndex =
                pixelShaderIndex,

            BlendState =
                blendState,

            DepthStencilState =
                depthStencilState,

            RasterizerState =
                rasterizerState
        };
    }

    return passes;
}
			private static MGFXParameter[] ReadParameters(BinaryReader reader)
			{
			    int count = reader.ReadInt32();

			    if (count == 0)
			        return Array.Empty<MGFXParameter>();

			    MGFXParameter[] parameters =
			        new MGFXParameter[count];

			    for (int i = 0; i < count; i++)
			    {
		        // --------------------------------------------------------
		        // Parameter metadata
		        // --------------------------------------------------------

		        EffectParameterClass parameterClass =
		            (EffectParameterClass)reader.ReadByte();

		        EffectParameterType parameterType =
		            (EffectParameterType)reader.ReadByte();

		        string name =
		            reader.ReadString();

		        string semantic =
		            reader.ReadString();


		        // --------------------------------------------------------
		        // Annotations
		        // --------------------------------------------------------

		        int annotationCount =
		            reader.ReadInt32();


		        // --------------------------------------------------------
		        // Matrix/vector dimensions
		        // --------------------------------------------------------

		        byte rowCount =
		            reader.ReadByte();

		        byte columnCount =
		            reader.ReadByte();


		        // --------------------------------------------------------
		        // Recursive parameters
		        // --------------------------------------------------------

		        MGFXParameter[] elements =
		            ReadParameters(reader);

		        MGFXParameter[] structMembers =
		            ReadParameters(reader);


		        // --------------------------------------------------------
		        // Default data
		        // --------------------------------------------------------

		        object data = null;


		        /*
		         * El dato solo aparece aquí cuando NO estamos ante
		         * un array ni un struct.
		         */
		        if (elements.Length == 0 &&
		            structMembers.Length == 0)
		        {
		            int valueCount =
		                rowCount * columnCount;

		            switch (parameterType)
		            {
		                case EffectParameterType.Bool:
		                case EffectParameterType.Int32:
		                {
		                    int[] buffer =
		                        new int[valueCount];

		                    for (int j = 0; j < buffer.Length; j++)
		                    {
		                        buffer[j] =
		                            reader.ReadInt32();
		                    }

		                    data = buffer;

		                    break;
		                }

		                case EffectParameterType.Single:
		                {
		                    float[] buffer =
		                        new float[valueCount];

		                    for (int j = 0; j < buffer.Length; j++)
		                    {
		                        buffer[j] =
		                            reader.ReadSingle();
		                    }

		                    data = buffer;

		                    break;
		                }

		                case EffectParameterType.String:
		                {
		                    throw new NotSupportedException(
		                        "MGFX string parameters are not supported."
		                    );
		                }

		                default:
		                {
		                    /*
		                     * Texture, Sampler, etc. no tienen aquí
		                     * datos de constant-buffer que leer.
		                     */
		                    break;
		                }
		            }
		        }


        // --------------------------------------------------------
        // Store
        // --------------------------------------------------------

        parameters[i] =
            new MGFXParameter
            {
                Class = parameterClass,
                Type = parameterType,

                Name = name,
                Semantic = semantic,

                RowCount = rowCount,
                ColumnCount = columnCount,

                Elements = elements,
                StructMembers = structMembers,

                Data = data
            };
    }

    return parameters;
}


			private MGFXShader ReadMonoGameShader(BinaryReader reader, int version)
		{
		    var result = new MGFXShader();

		    // ------------------------------------------------------------
		    // Shader metadata
		    // ------------------------------------------------------------

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


		    // ------------------------------------------------------------
		    // Shader bytecode
		    // ------------------------------------------------------------

		    int shaderLength = reader.ReadInt32();

		    byte[] shaderBytecode = reader.ReadBytes(shaderLength);

		    if (shaderBytecode.Length != shaderLength)
		    {
		        throw new EndOfStreamException(
		            $"Expected {shaderLength} shader bytes, " +
		            $"but only read {shaderBytecode.Length}."
		        );
		    }


		    // ------------------------------------------------------------
		    // Vulkan metadata + pure SPIR-V
		    // ------------------------------------------------------------

		    VulkanShaderData vulkan =
		        ReadVulkanShaderCode(shaderBytecode);

		    result.Vulkan = vulkan;

		    // Dejamos Bytecode conteniendo únicamente SPIR-V.
		    result.Bytecode = vulkan.Spirv;


		    // ------------------------------------------------------------
		    // Debug information
		    // ------------------------------------------------------------

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


		    // ============================================================
		    // Samplers
		    // ============================================================

		    int samplerCount = reader.ReadByte();

		    MGFXSampler[] samplers =
		        new MGFXSampler[samplerCount];

		    for (int s = 0; s < samplerCount; s++)
		    {
		        // --------------------------------------------------------
		        // Sampler binding information
		        // --------------------------------------------------------

		        byte type = reader.ReadByte();
		        byte textureSlot = reader.ReadByte();
		        byte samplerSlot = reader.ReadByte();


		        // --------------------------------------------------------
		        // Optional SamplerState
		        // --------------------------------------------------------

		        bool hasSamplerState =
		            reader.ReadBoolean();



		        /*
		         * Valores default en caso de que este sampler
		         * no tenga un SamplerState explícito.
		         */

		        TextureAddressMode addressModeU = default;
		        TextureAddressMode addressModeV = default;
		        TextureAddressMode addressModeW = default;

		        byte borderR = 0;
		        byte borderG = 0;
		        byte borderB = 0;
		        byte borderA = 0;

		        TextureFilter textureFilter = default;

		        int maxAnisotropy = 0;
		        int maxMipLevel = 0;

		        float mipMapLodBias = 0.0f;


		        if (hasSamplerState)
		        {
		            addressModeU =
		                (TextureAddressMode)reader.ReadByte();

		            addressModeV =
		                (TextureAddressMode)reader.ReadByte();

		            addressModeW =
		                (TextureAddressMode)reader.ReadByte();


		            borderR = reader.ReadByte();
		            borderG = reader.ReadByte();
		            borderB = reader.ReadByte();
		            borderA = reader.ReadByte();


		            textureFilter =
		                (TextureFilter)reader.ReadByte();


		            maxAnisotropy =
		                reader.ReadInt32();

		            maxMipLevel =
		                reader.ReadInt32();

		            mipMapLodBias =
		                reader.ReadSingle();
		        }


		        // --------------------------------------------------------
		        // MGFX sampler metadata
		        // --------------------------------------------------------

		        string name =
		            reader.ReadString();

		        byte parameter =
		            reader.ReadByte();


		        // --------------------------------------------------------
		        // Store sampler
		        // --------------------------------------------------------

		        samplers[s] = new MGFXSampler
		        {
		            Type = type,

		            HasSamplerState = hasSamplerState,
		            TextureSlot = textureSlot,
		            SamplerSlot = samplerSlot,

		            AddressModeU = addressModeU,
		            AddressModeV = addressModeV,
		            AddressModeW = addressModeW,

		            BorderR = borderR,
		            BorderG = borderG,
		            BorderB = borderB,
		            BorderA = borderA,

		            TextureFilter = textureFilter,

		            MaxAnisotropy = maxAnisotropy,
		            MaxMipLevel = maxMipLevel,

		            MipMapLodBias = mipMapLodBias,

		            Parameter = parameter,
		            Name = name
		        };
		    }


		    // IMPORTANTE:
		    // antes creabas el array, pero nunca lo guardabas.
		    result.Samplers = samplers;


		    // ============================================================
		    // Constant buffers usados por este shader
		    // ============================================================

		    int cbufferCount =
		        reader.ReadByte();

		    result.CBuffers =
		        new byte[cbufferCount];

		    for (int c = 0; c < cbufferCount; c++)
		    {
		        result.CBuffers[c] =
		            reader.ReadByte();
		    }


		    // ============================================================
		    // Vertex attributes
		    // ============================================================

		    int attributeCount =
		        reader.ReadByte();

		    MGFXVertexAttribute[] attributes = new MGFXVertexAttribute[attributeCount];

		    for (int a = 0; a < attributeCount; a++)
		    {
		        string name =
		            reader.ReadString();

		        byte usage =
		            reader.ReadByte();

		        byte index =
		            reader.ReadByte();

		        short location =
		            reader.ReadInt16();

		        attributes[a] = new MGFXVertexAttribute()
		        {
					Name = name,
					Usage = (VertexElementUsage)usage,
					UsageIndex = index,
					Location = location
		        };
		    }

		    result.VertexAttributes = attributes;


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
#endregion


#region Effect Passes methods


		internal void INTERNAL_applyMGFXPass(
			MGFXPass mgfxPass,
			uint passIndex)
		{
			// Próxima fase:
			// - VertexShaderIndex
			// - PixelShaderIndex
			// - BlendState
			// - DepthStencilState
			// - RasterizerState
			// - SDL_GPU pipeline

			throw new NotImplementedException(
				"MGFX EffectPass.Apply is not implemented yet."
			);
		}

		private void BuildMGFXTechniques()
		{
			var techniques =
				new List<EffectTechnique>(
					MonoGameEffect.Techniques.Length
				);

			for (int i = 0;
			     i < MonoGameEffect.Techniques.Length;
			     i++)
			{
				MGFXTechnique mgfxTechnique =
					MonoGameEffect.Techniques[i];

				EffectPassCollection passes =
					BuildMGFXPasses(mgfxTechnique);

				techniques.Add(
					new EffectTechnique(
						mgfxTechnique.Name,
						IntPtr.Zero,
						passes,
						EffectAnnotationCollection.Empty
					)
				);
			}

			Techniques =
				new EffectTechniqueCollection(techniques);

			CurrentTechnique =
				Techniques[0];
		}

		private EffectPassCollection BuildMGFXPasses(
			MGFXTechnique technique)
		{
			var passes =
				new List<EffectPass>(
					technique.Passes.Length
				);

			for (int i = 0;
			     i < technique.Passes.Length;
			     i++)
			{
				passes.Add(
					new EffectPass(
						technique.Passes[i],
						this,
						(uint)i
					)
				);
			}

			return new EffectPassCollection(passes);
		}

		private EffectParameterCollection BuildMGFXParameters(
    MGFXParameter[] parameters,
    bool topLevel)
{
    var result =
        new List<EffectParameter>(
            parameters.Length
        );

    for (int i = 0; i < parameters.Length; i++)
    {
        MGFXParameter source =
            parameters[i];

        EffectParameterCollection elements =
            BuildMGFXParameters(
                source.Elements ??
                Array.Empty<MGFXParameter>(),
                false
            );

        EffectParameterCollection members =
            BuildMGFXParameters(
                source.StructMembers ??
                Array.Empty<MGFXParameter>(),
                false
            );

        result.Add(
            new EffectParameter(
                source,

                // Los índices de ConstantBuffer apuntan
                // al array GLOBAL/top-level de Parameters.
                topLevel ? i : -1,

                elements,
                members,
                this
            )
        );
    }

    return new EffectParameterCollection(result);
}
		#endregion
	}
}
