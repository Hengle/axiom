﻿#region LGPL License
/*
Axiom Graphics Engine Library
Copyright © 2003-2011 Axiom Project Team

The overall design, and a majority of the core engine and rendering code
contained within this library is a derivative of the open source Object Oriented
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.
Many thanks to the OGRE team for maintaining such a high quality project.

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
*/
#endregion LGPL License

#region SVN Version Information
// <file>
//     <license see="http://axiom3d.net/wiki/index.php/license.txt"/>
//     <id value="$Id:$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections.Generic;
using System.Text;
using Axiom.Graphics;
using Axiom.Utilities;

#endregion Namespace Declarations

namespace Axiom.RenderSystems.OpenGL
{
	class GLFBOMultiRenderTarget : MultiRenderTarget
	{
		#region Fields and Properties

		protected GLFBORTTManager _manager;
		protected GLFrameBufferObject _fbo;

		#endregion Fields and Properties

		#region Construction and Destruction

		public GLFBOMultiRenderTarget( GLFBORTTManager manager, string name )
			: base( name )
		{
			this._manager = manager;
		}

		#endregion Construction and Destruction

		#region Methods

		/// <summary>
		/// Bind a surface to a certain attachment point.
		/// </summary>
		/// <param name="attachment">0 .. capabilities.MultiRenderTargetCount-1</param>
		/// <param name="target">RenderTexture to bind.</param>
		/// <remarks>
		/// It does not bind the surface and fails with an exception (ERR_INVALIDPARAMS) if:
		/// - Not all bound surfaces have the same size
		/// - Not all bound surfaces have the same internal format
		/// </remarks>
		public override void BindSurface( int attachment, RenderTexture target )
		{
			/// Check if the render target is in the rendertarget->FBO map
			GLFrameBufferObject fbObject = (GLFrameBufferObject)target[ "FBO" ];
			Proclaim.NotNull( fbObject );

			this._fbo.BindSurface( attachment, fbObject.SurfaceDesc );

			// Initialize?

			// Set width and height
			Width = this._fbo.Width;
			Height = this._fbo.Height;

		}

		/// <summary>
		/// Unbind Attachment
		/// </summary>
		/// <param name="attachment"></param>
		public override void UnbindSurface( int attachment )
		{
			this._fbo.UnbindSurface( attachment );
			Width = this._fbo.Width;
			Height = this._fbo.Height;
		}

		#endregion Methods

		#region RenderTarget Implementation

		public override object this[ string attribute ]
		{
			get
			{
				if ( attribute == "FBO" )
				{
					return this._fbo;
				}

				return base[ attribute ];
			}
		}

		public override bool RequiresTextureFlipping
		{
			get
			{
				return true;
			}
		}

		#endregion RenderTarget Implementation
	}
}