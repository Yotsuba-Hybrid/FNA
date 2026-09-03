using System;
using Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

namespace Microsoft.Xna.Framework.Graphics;

public sealed partial class EffectParameter
{
	internal MGFXParameter MGFXParameter;
	internal object MGFXData;
	internal int MGFXParameterIndex = -1;

	internal bool IsMGFXParameter =>
		MGFXParameter != null;

	internal ulong MGFXStateKey;

	private static ulong NextMGFXStateKey;

	private void MarkMGFXDirty()
	{
		MGFXStateKey =
			unchecked(NextMGFXStateKey++);
	}
	internal EffectParameter(
		MGFXParameter parameter,
		int parameterIndex,
		EffectParameterCollection parameterElements,
		EffectParameterCollection structureMembers,
		Effect effect)
	{
		MGFXParameter = parameter;
		MGFXParameterIndex = parameterIndex;

		Name = parameter.Name;
		Semantic = parameter.Semantic ?? string.Empty;

		RowCount = parameter.RowCount;
		ColumnCount = parameter.ColumnCount;

		ParameterClass = parameter.Class;
		ParameterType = parameter.Type;

		elementCount =
			parameter.Elements?.Length ?? 0;

		elements =
			parameterElements;

		members =
			structureMembers;

		Annotations =
			EffectAnnotationCollection.Empty;

		outer = effect;

		// Cada Effect debe tener su propia copia mutable.
		if (parameter.Data is Array array)
		{
			MGFXData = array.Clone();
		}
		else
		{
			MGFXData = parameter.Data;
		}

		MGFXStateKey =
			unchecked(NextMGFXStateKey++);
	}

	private void MGFXSetValue(Texture value)
	{
		if (ParameterType != EffectParameterType.Texture &&
		    ParameterType != EffectParameterType.Texture1D &&
		    ParameterType != EffectParameterType.Texture2D &&
		    ParameterType != EffectParameterType.Texture3D &&
		    ParameterType != EffectParameterType.TextureCube)
		{
			throw new InvalidCastException();
		}

		texture = value;
		MGFXData = value;

		MarkMGFXDirty();
	}

	private void MGFXSetValue(int value)
	{
		if (ParameterType == EffectParameterType.Single)
		{
			MGFXSetValue((float)value);
			return;
		}

		if (ParameterClass != EffectParameterClass.Scalar ||
		    ParameterType != EffectParameterType.Int32)
		{
			throw new InvalidCastException();
		}

		((int[])MGFXData)[0] = value;

		MarkMGFXDirty();
	}

	private void MGFXSetValue(float value)
	{
		if (ParameterClass != EffectParameterClass.Scalar ||
		    ParameterType != EffectParameterType.Single)
		{
			throw new InvalidCastException();
		}

		((float[])MGFXData)[0] = value;

		MarkMGFXDirty();
	}

	private void MGFXSetValue(Vector2 value)
{
    if (ParameterClass != EffectParameterClass.Vector ||
        ParameterType != EffectParameterType.Single)
    {
        throw new InvalidCastException();
    }

    float[] data =
        (float[])MGFXData;

    data[0] = value.X;
    data[1] = value.Y;

    MarkMGFXDirty();
}

private void MGFXSetValue(Vector3 value)
{
    if (ParameterClass != EffectParameterClass.Vector ||
        ParameterType != EffectParameterType.Single)
    {
        throw new InvalidCastException();
    }

    float[] data =
        (float[])MGFXData;

    data[0] = value.X;
    data[1] = value.Y;
    data[2] = value.Z;

    MarkMGFXDirty();
}

private void MGFXSetValue(Vector4 value)
{
    if (ParameterClass != EffectParameterClass.Vector ||
        ParameterType != EffectParameterType.Single)
    {
        throw new InvalidCastException();
    }

    float[] data =
        (float[])MGFXData;

    data[0] = value.X;
    data[1] = value.Y;
    data[2] = value.Z;
    data[3] = value.W;

    MarkMGFXDirty();
}

private void MGFXSetValue(Quaternion value)
{
	if (ParameterType != EffectParameterType.Single)
		throw new InvalidCastException();

    float[] data =
        (float[])MGFXData;

    data[0] = value.X;
    data[1] = value.Y;
    data[2] = value.Z;
    data[3] = value.W;

    MarkMGFXDirty();
}
}
