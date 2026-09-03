using Microsoft.Xna.Framework.YotsubaFramework.Graphics.Effect.MGFX;

namespace Microsoft.Xna.Framework.Graphics;

public sealed partial class EffectPass
{
	internal MGFXPass MGFXPass;
	internal bool IsMGFXPass =>
		MGFXPass != null;
	public string Name
	{
		get;
		private set;
	}

	internal EffectPass(MGFXPass mgfxPass,
		Effect effect,
		uint index)
	{
		Name = mgfxPass.Name;
		MGFXPass = mgfxPass;
		parentEffect = effect;
		pass = index;
		Annotations = EffectAnnotationCollection.Empty;
	}
}
