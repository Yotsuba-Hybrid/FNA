using System;

namespace Microsoft.Xna.Framework.Graphics
{
	internal struct MGFXHeader
	{
		/// <summary>
		/// The MonoGame Effect file format header identifier ("MGFX").
		/// </summary>
		public static readonly int MGFXSignature = (BitConverter.IsLittleEndian) ? 0x5846474D: 0x4D474658;

		/// <summary>
		/// The current MonoGame Effect file format versions
		/// used to detect old packaged content.
		/// </summary>
		/// <remarks>
		/// We should avoid supporting old versions for very long if at all
		/// as users should be rebuilding content when packaging their game.
		/// </remarks>
		public const int MGFXVersion = 11;

		/// <summary>
		/// This is the minimum version of MGFX file we can support
		/// for cases when the changes are backwards compatible.
		/// </summary>
		public const int MGFXMinVersion = 10;

		public int Signature;
		public int Version;
		public int Profile;
		public int EffectKey;
		public int HeaderSize;
	}
}
