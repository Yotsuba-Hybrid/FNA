#region License
/* FNA - XNA4 Reimplementation for Desktop Platforms
 * Copyright 2009-2024 Ethan Lee and the MonoGame Team
 *
 * Released under the Microsoft Public License.
 * See LICENSE for details.
 */
#endregion

#region Using Statements
using System;
using System.ComponentModel;
#endregion

namespace Microsoft.Xna.Framework
{
	public class FileDropEventArgs : EventArgs
	{
		public string[] Files { get; private set; }
		public FileDropEventArgs(string[] files)
		{
			Files = files;
		}
	}

	/* MonoGame exposes this as a value type with public fields. Matching that
	 * layout is required for binary compatibility with MonoGame libraries.
	 */
	public struct TextInputEventArgs
	{
		public char Character;
		public Input.Keys Key;
		public TextInputEventArgs(char character, Input.Keys key = Input.Keys.None)
		{
			Character = character;
			Key = key;
		}
	}

	public abstract class GameWindow
	{
		#region Public Properties

		[DefaultValue(false)]
		public abstract bool AllowUserResizing
		{
			get;
			set;
		}

		public abstract Rectangle ClientBounds
		{
			get;
		}

		public abstract DisplayOrientation CurrentOrientation
		{
			get;
			internal set;
		}

		public abstract IntPtr Handle
		{
			get;
		}

		public abstract string ScreenDeviceName
		{
			get;
		}

		public string Title
		{
			get
			{
				return _title;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value", "The title name cannot be null.  Use an empty string instead.");
				}
				if (_title != value)
				{
					SetTitle(value);
					_title = value;
				}
			}
		}

		/// <summary>
		/// Determines whether the border of the window is visible.
		/// </summary>
		/// <exception cref="System.NotImplementedException">
		/// Thrown when trying to use this property on an unsupported platform.
		/// </exception>
		public virtual bool IsBorderlessEXT
		{
			get
			{
				return false;
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public virtual bool IsBorderless
		{
			get { return IsBorderlessEXT; }
			set { IsBorderlessEXT = value; }
		}

		#endregion

		#region Internal Variables

		internal string _title;

		#endregion

		#region Protected Constructors

		protected GameWindow()
		{
		}

		#endregion

		#region Events

		public event EventHandler<EventArgs> ClientSizeChanged;
		public event EventHandler<EventArgs> OrientationChanged;
		public event EventHandler<EventArgs> ScreenDeviceNameChanged;
		public event EventHandler<FileDropEventArgs> FileDrop;
		public event EventHandler<TextInputEventArgs> TextInput;

		#endregion

		#region Public Methods

		public abstract void BeginScreenDeviceChange(bool willBeFullScreen);

		public abstract void EndScreenDeviceChange(
			string screenDeviceName,
			int clientWidth,
			int clientHeight
		);

		public void EndScreenDeviceChange(string screenDeviceName)
		{
			EndScreenDeviceChange(
				screenDeviceName,
				ClientBounds.Width,
				ClientBounds.Height
			);
		}

		#endregion

		#region Protected Methods

		protected void OnActivated()
		{
		}

		protected void OnClientSizeChanged()
		{
			if (ClientSizeChanged != null)
			{
				ClientSizeChanged(this, EventArgs.Empty);
			}
		}

		protected void OnDeactivated()
		{
		}

		protected void OnOrientationChanged()
		{
			if (OrientationChanged != null)
			{
				OrientationChanged(this, EventArgs.Empty);
			}
		}

		protected void OnPaint()
		{
		}

		protected void OnFileDrop(FileDropEventArgs e)
		{
			if (FileDrop != null)
			{
				FileDrop(this, e);
			}
		}

		internal void OnTextInput(TextInputEventArgs e)
		{
			if (TextInput != null)
			{
				TextInput(this, e);
			}
		}

		protected void OnScreenDeviceNameChanged()
		{
			if (ScreenDeviceNameChanged != null)
			{
				ScreenDeviceNameChanged(this, EventArgs.Empty);
			}
		}

		protected internal abstract void SetSupportedOrientations(
			DisplayOrientation orientations
		);

		protected abstract void SetTitle(string title);

		#endregion
	}
}
