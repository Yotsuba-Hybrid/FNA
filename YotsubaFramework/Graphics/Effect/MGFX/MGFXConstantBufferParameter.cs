namespace Microsoft.Xna.Framework.Graphics
{
	public struct MGFXConstantBufferParameter(int parameterIndex, ushort offset)
	{
		internal int ParameterIndex { get; set; } = parameterIndex;
		internal ushort Offset { get; set; } = offset;
	}
}

