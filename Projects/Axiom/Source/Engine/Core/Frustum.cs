#region LGPL License
/*
Axiom Graphics Engine Library
Copyright (C) 2003-2006 Axiom Project Team

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
#endregion

#region SVN Version Information
// <file>
//     <license see="http://axiomengine.sf.net/wiki/index.php/license.txt"/>
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

using System;
using System.Collections;
using System.Diagnostics;

using Axiom.Collections;
using Axiom.Core;
using Axiom.Graphics;
using Axiom.Math;

#endregion namespace Declarations

namespace Axiom.Core
{
    /// <summary>
    ///     A frustum represents a pyramid, capped at the near and far end which is
    ///     used to represent either a visible area or a projection area. Can be used
    ///     for a number of applications.
    /// </summary>
    // TODO: Review attaching object in the scene and making them no longer require a name.
    public class Frustum : MovableObject, IRenderable
    {
        #region Constants

        /// <summary>
        ///		Small constant used to reduce far plane projection to avoid inaccuracies.
        /// </summary>
        public const float InfiniteFarPlaneAdjust = 0.00001f;

        /// <summary>
        ///		Arbitrary large distance to use for the far plane when set to 0 (infinite far plane).
        /// </summary>
        public const float InfiniteFarPlaneDistance = 100000.0f;

        #endregion Constants

        #region Fields and Properties

		#region ProjectionType Property

		/// <summary>
		///		Perspective or Orthographic?
		/// </summary>
		private Projection _projectionType;
		/// <summary>
		///    Gets/Sets the type of projection to use (orthographic or perspective). Default is perspective.
		/// </summary>
		public Projection ProjectionType
		{
			get
			{
				return _projectionType;
			}
			set
			{
				_projectionType = value;
				InvalidateFrustum();
			}
		}

		#endregion ProjectionType Property

		#region FieldOfView Property

		/// <summary>
		///     y-direction field-of-view (default 45).
		/// </summary>
		private float _fieldOfView;
		/// <summary>
		///		Sets the Y-dimension Field Of View (FOV) of the camera.
		/// </summary>
		/// <remarks>
		///		Field Of View (FOV) is the angle made between the camera's position, and the left & right edges
		///		of the 'screen' onto which the scene is projected. High values (90+) result in a wide-angle,
		///		fish-eye kind of view, low values (30-) in a stretched, telescopic kind of view. Typical values
		///		are between 45 and 60.
		///		<p/>
		///		This value represents the HORIZONTAL field-of-view. The vertical field of view is calculated from
		///		this depending on the dimensions of the viewport (they will only be the same if the viewport is square).
		/// </remarks>
		public virtual float FieldOfView
		{
			get
			{
				return _fieldOfView;
			}
			set
			{
				_fieldOfView = value;
				InvalidateFrustum();
				InvalidateView();	//XEONX FIX: Now the IsObjectVisible() will work properly
			}
		}

		#endregion FieldOfView Property

		#region Far Property

		/// <summary>
		///     Far clip distance - default 10000.
		/// </summary>
		protected float _farDistance;
		/// <summary>
		///		Gets/Sets the distance to the far clipping plane.
		///	 </summary>
		///	 <remarks>
		///		The view frustrum is a pyramid created from the camera position and the edges of the viewport.
		///		This frustrum does not extend to infinity - it is cropped near to the camera and there is a far
		///		plane beyond which nothing is displayed. This method sets the distance for the far plane. Different
		///		applications need different values: e.g. a flight sim needs a much further far clipping plane than
		///		a first-person shooter. An important point here is that the larger the gap between near and far
		///		clipping planes, the lower the accuracy of the Z-buffer used to depth-cue pixels. This is because the
		///		Z-range is limited to the size of the Z buffer (16 or 32-bit) and the max values must be spread over
		///		the gap between near and far clip planes. The bigger the range, the more the Z values will
		///		be approximated which can cause artifacts when lots of objects are close together in the Z-plane. So
		///		make sure you clip as close to the camera as you can - don't set a huge value for the sake of
		///		it.
		/// </remarks>
		/// <value>
		///		The distance to the far clipping plane from the frustum in 
		///		world coordinates.  If you specify 0, this means an infinite view
		///		distance which is useful especially when projecting shadows; but
		///		be careful not to use a near distance too close.
		/// </value>
		public virtual float Far
		{
			get
			{
				return _farDistance;
			}
			set
			{
				_farDistance = value;
				InvalidateFrustum();
				InvalidateView();	//XEONX FIX: Now the IsObjectVisible() will work properly
			}
		}

		#endregion Far Property
			
		#region Near Property

		/// <summary>
		///     Near clip distance - default 100.
		/// </summary>
		private float _nearDistance;
		/// <summary>
		///		Gets/Sets the position of the near clipping plane.
		///	</summary>
		///	<remarks>
		///		The position of the near clipping plane is the distance from the cameras position to the screen
		///		on which the world is projected. The near plane distance, combined with the field-of-view and the
		///		aspect ratio, determines the size of the viewport through which the world is viewed (in world
		///		co-ordinates). Note that this world viewport is different to a screen viewport, which has it's
		///		dimensions expressed in pixels. The cameras viewport should have the same aspect ratio as the
		///		screen viewport it renders into to avoid distortion.
		/// </remarks>
		public virtual float Near
		{
			get
			{
				return _nearDistance;
			}
			set
			{
				Debug.Assert( value > 0, "Near clip distance must be greater than zero." );

				_nearDistance = value;
				InvalidateFrustum();
				InvalidateView();	//XEONX FIX: Now the IsObjectVisible() will work properly
			}
		}

		#endregion Near Property

		#region AspectRatio Property

		/// <summary>
		///     x/y viewport ratio - default 1.3333
		/// </summary>
		private float _aspectRatio;
		/// <summary>
		///		Gets/Sets the aspect ratio to use for the camera viewport.
		/// </summary>
		/// <remarks>
		///		The ratio between the x and y dimensions of the rectangular area visible through the camera
		///		is known as aspect ratio: aspect = width / height .
		///		<p/>
		///		The default for most fullscreen windows is 1.3333f - this is also assumed unless you
		///		use this property to state otherwise.
		/// </remarks>
		public virtual float AspectRatio
		{
			get
			{
				return _aspectRatio;
			}
			set
			{
				_aspectRatio = value;
				InvalidateFrustum();
			}
		}

		#endregion AspectRatio Property
			
		///<summary>
        /// Off-axis frustum center offset - default (0.0, 0.0)
        ///</summary>
        protected Vector2 _frustumOffset;
        ///<summary>
        /// Focal length of frustum (for stereo rendering, defaults to 1.0)
        ///</summary> 
        protected float _focalLength;

        /// <summary>
        ///     The 6 main clipping planes.
        /// </summary>
        protected Plane[] _planes = new Plane[6];
        /// <summary>
        ///     Stored versions of parent orientation.
        /// </summary>
        protected Quaternion _lastParentOrientation;
        /// <summary>
        ///     Stored versions of parent position.
        /// </summary>
        protected Vector3 _lastParentPosition;

		#region ProjectionMatrixRS Property

		/// <summary>
		/// Gets the projection matrix for this frustum adjusted for the current
		/// rendersystem specifics (may be right or left-handed, depth range
		/// may vary).
		/// </summary>
		/// <remarks>
		/// This method retrieves the rendering-API dependent version of the projection
		/// matrix. If you want a 'typical' projection matrix then use _projectionMatrix.
		/// </remarks>  
		protected Matrix4 _projectionMatrixRS;
		/// <summary>
		/// Gets the projection matrix for this frustum adjusted for the current
		/// rendersystem specifics (may be right or left-handed, depth range
		/// may vary).
		/// </summary>
		/// <remarks>
		/// This method retrieves the rendering-API dependent version of the projection
		/// matrix. If you want a 'typical' projection matrix then use ProjectionMatrix.
		/// </remarks>  
		public virtual Matrix4 ProjectionMatrixRS
		{
			get
			{
				UpdateFrustum();

				return _projectionMatrixRS;
			}
		}

		#endregion ProjectionMatrixRS Property

		#region ProjectionMatrixRSDepth Property

		/// <summary>
		///  The depth-adjusted projection matrix for the current rendersystem,
		///  but one which still conforms to right-hand rules.
		/// </summary>
		/// <remarks> 
		///     This differs from the rendering-API dependent getProjectionMatrix
		///     in that it always returns a right-handed projection matrix result 
		///     no matter what rendering API is being used - this is required for
		///     vertex and fragment programs for example. However, the resulting depth
		///     range may still vary between render systems since D3D uses [0,1] and 
		///     GL uses [-1,1], and the range must be kept the same between programmable
		///     and fixed-function pipelines.
		/// </remarks>
		protected Matrix4 _projectionMatrixRSDepth;
		/// <summary>
		///    Gets the 'standard' projection matrix for this camera, ie the 
		///    projection matrix which conforms to standard right-handed rules.
		/// </summary>
		/// <remarks>
		///    This differs from the rendering-API dependent ProjectionMatrix
		///    in that it always returns a right-handed projection matrix result 
		///    no matter what rendering API is being used - this is required for
		///    vertex and fragment programs for example. However, the resulting depth
		///    range may still vary between render systems since D3D uses [0,1] and 
		///    GL uses [-1,1], and the range must be kept the same between programmable
		///    and fixed-function pipelines.
		///    <para/>
		///    This corresponds to the Ogre mProjMatrixRSDepth and
		///    getProjectionMatrixWithRSDepth
		/// </remarks>
		public virtual Matrix4 ProjectionMatrixRSDepth
		{
			get
			{
				UpdateFrustum();

				return _projectionMatrixRSDepth;
			}
		}

		#endregion ProjectionMatrixRSDepth Property
			
		#region ProjectionMatrix Property

		/// <summary>
		/// The normal projection matrix for this frustum, ie the 
		/// projection matrix which conforms to standard right-handed rules and
		/// uses depth range [-1,+1].
		/// </summary>
		///<remarks>
		///    This differs from the rendering-API dependent getProjectionMatrixRS
		///    in that it always returns a right-handed projection matrix with depth
		///    range [-1,+1], result no matter what rendering API is being used - this
		///    is required for some uniform algebra for example.
		///</remarks>
		protected Matrix4 _projectionMatrix;
		/// <summary>
		/// Gets the projection matrix for this frustum.
		/// </summary>
		public virtual Matrix4 ProjectionMatrix
		{
			get
			{
				UpdateFrustum();

				return _projectionMatrix;
			}
			protected set
			{
				{
					_projectionMatrix = value;
				}
			}
		}

		#endregion ProjectionMatrix Property

		#region ViewMatrix Property

		/// <summary>
		///     Pre-calced view matrix.
		/// </summary>
		protected Matrix4 _viewMatrix;
		/// <summary>
		///     Gets the view matrix for this frustum.
		/// </summary>
		public virtual Matrix4 ViewMatrix
		{
			get
			{
				UpdateView();

				return _viewMatrix;
			}
			protected set
			{
				_viewMatrix = value;
			}
		}

		#endregion ViewMatrix Property
			
		/// <summary>
        ///     Something's changed in the frustum shape?
        /// </summary>
        protected bool _recalculateFrustum;
		/// <summary>
		///		Evaluates whether or not the view frustum is out of date.
		/// </summary>
		protected virtual bool IsFrustumOutOfDate
		{
			get
			{
				// deriving custom near plane from linked plane?
				bool returnVal = false;

				if ( _useObliqueDepthProjection )
				{
					// always out of date since plane needs to be in view space
					if ( IsViewOutOfDate )
					{
						returnVal = true;
					}

					// update derived plane
					if ( _linkedObliqueProjPlane != null &&
						!( _lastLinkedObliqueProjPlane == _linkedObliqueProjPlane.DerivedPlane ) )
					{

						_obliqueProjPlane = _linkedObliqueProjPlane.DerivedPlane;
						_lastLinkedObliqueProjPlane = _obliqueProjPlane;
						returnVal = true;
					}
				}

				return _recalculateFrustum || returnVal;
			}
		}
		
		/// <summary>
        ///     Something in the view pos has changed?
        /// </summary>
        protected bool _recalculateView;
		/// <summary>
		///		Gets whether or not the view matrix is out of date.
		/// </summary>
		protected virtual bool IsViewOutOfDate
		{
			get
			{
				bool returnVal = false;

				// are we attached to another node?
				if ( parentNode != null )
				{
					if ( !_recalculateView && parentNode.DerivedOrientation == _lastParentOrientation &&
						parentNode.DerivedPosition == _lastParentPosition )
					{
						returnVal = false;
					}
					else
					{
						// we are out of date with the parent scene node
						_lastParentOrientation = parentNode.DerivedOrientation;
						_lastParentPosition = parentNode.DerivedPosition;
						returnVal = true;
					}
				}

				// deriving direction from linked plane?
				if ( _isReflected && _linkedReflectionPlane != null &&
					!( _lastLinkedReflectionPlane == _linkedReflectionPlane.DerivedPlane ) )
				{

					_reflectionPlane = _linkedReflectionPlane.DerivedPlane;
					_reflectionMatrix = Utility.BuildReflectionMatrix( _reflectionPlane );
					_lastLinkedReflectionPlane = _linkedReflectionPlane.DerivedPlane;
					returnVal = true;
				}

				return _recalculateView || returnVal;
			}
		}

        ///<summary>
        /// Are we using a custom view matrix?
        /// </summary>
        bool _customViewMatrix;

        /// <summary>
        /// Are we using a custom projection matrix?
        /// </summary>
        bool _customProjMatrix;

        /// <summary>
        ///     Bounding box of this frustum.
        /// </summary>
        protected AxisAlignedBox _boundingBox = AxisAlignedBox.Null;
        /// <summary>
        ///     Vertex info for rendering this frustum.
        /// </summary>
        protected VertexData _vertexData = new VertexData();
        /// <summary>
        ///     Material to use when rendering this frustum.
        /// </summary>
        protected Material _material;
        /// <summary>
        ///		Frustum corners in world space.
        /// </summary>
        protected Vector3[] _worldSpaceCorners = new Vector3[8];

        /** Temp coefficient values calculated from a frustum change,
            used when establishing the frustum planes when the view changes. */
        protected float[] _coeffL = new float[2];
        protected float[] _coeffR = new float[2];
        protected float[] _coeffB = new float[2];
        protected float[] _coeffT = new float[2];

		#region IsRefelcted Property

		/// <summary>
		///		Is this frustum to act as a reflection of itself?
		/// </summary>
		private bool _isReflected;
		/// <summary>
		///     Gets a flag that specifies whether this camera is being reflected or not.
		/// </summary>
		public virtual bool IsReflected
		{
			get
			{
				return _isReflected;
			}
		}

		#endregion IsRefelcted Property

		#region ReflectionMatrix Property

		/// <summary>
		///		Derive reflection matrix.
		/// </summary>
		private Matrix4 _reflectionMatrix;
		/// <summary>
		///     Returns the reflection matrix of the camera if appropriate.
		/// </summary>
		public virtual Matrix4 ReflectionMatrix
		{
			get
			{
				return _reflectionMatrix;
			}
			protected set
			{
				_reflectionMatrix = value;
			}
		}

		#endregion ReflectionMatrix Property

		#region ReflectionPlane Property

		/// <summary>
		///		Fixed reflection.
		/// </summary>
		private Plane _reflectionPlane;
		/// <summary>
		///     Returns the reflection plane of the camera if appropriate.
		/// </summary>
		public virtual Plane ReflectionPlane
		{
			get
			{
				return _reflectionPlane;
			}
			protected set
			{
				_reflectionPlane = value;
			}
		}

		#endregion ReflectionPlane Property
			
        /// <summary>
        ///		Reference of a reflection plane (automatically updated).
        /// </summary>
        protected IDerivedPlaneProvider _linkedReflectionPlane;
        /// <summary>
        ///		Record of the last world-space reflection plane info used.
        /// </summary>
        protected Plane _lastLinkedReflectionPlane;
        /// <summary>
        ///		Is this frustum using an oblique depth projection?
        /// </summary>
        protected bool _useObliqueDepthProjection;
        /// <summary>
        ///		Fixed oblique projection plane.
        /// </summary>
        protected Plane _obliqueProjPlane;
        /// <summary>
        ///		Reference to oblique projection plane (automatically updated).
        /// </summary>
        protected IDerivedPlaneProvider _linkedObliqueProjPlane;
        /// <summary>
        ///		Record of the last world-space oblique depth projection plane info used.
        /// </summary>
        protected Plane _lastLinkedObliqueProjPlane;

        /// <summary>
        ///     Dummy list for IRenderable.Lights since we wont be lit.
        /// </summary>
        protected LightList _dummyLightList = new LightList();

        protected Hashtable _customParams = new Hashtable();

        #endregion Fields and Prperties

        #region Constructors

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public Frustum()
        {
            for (int i = 0; i < 6; i++)
            {
                _planes[i] = new Plane();
            }

            _fieldOfView = Utility.RadiansToDegrees(Utility.PI / 4.0f);
            _nearDistance = 100.0f;
            _farDistance = 100000.0f;
            _aspectRatio = 1.33333333333333f;

            _recalculateFrustum = true;
            _recalculateView = true;

            // Init matrices
            _viewMatrix = Matrix4.Zero;
            _projectionMatrix = Matrix4.Zero;

            _projectionType = Projection.Perspective;

            _lastParentPosition = Vector3.Zero;
            _lastParentOrientation = Quaternion.Identity;

            // init vertex data
            _vertexData.vertexDeclaration.AddElement(0, 0, VertexElementType.Float3, VertexElementSemantic.Position);
            _vertexData.vertexStart = 0;
            _vertexData.vertexCount = 32;
            _vertexData.vertexBufferBinding.SetBinding(0,
                HardwareBufferManager.Instance.CreateVertexBuffer(4 * 3, _vertexData.vertexCount, BufferUsage.DynamicWriteOnly));

            _material = (Material)MaterialManager.Instance["BaseWhite"];

            _customProjMatrix = false;
            _customViewMatrix = false;

            _frustumOffset = new Vector2(0.0f, 0.0f);
            _focalLength = 1.0f;

            UpdateView();
        }

        #endregion

        #region Methods

        /// <summary>
        ///		Disables any custom near clip plane.
        /// </summary>
        public virtual void DisableCustomNearClipPlane()
        {
            _useObliqueDepthProjection = false;
            _linkedObliqueProjPlane = null;
            InvalidateFrustum();
        }

        public void SetCustomViewMatrix(bool enable, Matrix4 viewMatrix)
        {
            _customViewMatrix = enable;
            if (enable)
            {
                Debug.Assert(viewMatrix.IsAffine);
                _viewMatrix = viewMatrix;
            }
            InvalidateView();
        }

	    public void SetCustomProjectionMatrix(bool enable, Matrix4 projMatrix)
	    {
		    _customProjMatrix = enable;
		    if (enable)
		    {
			    _projectionMatrix = projMatrix;
		    }

		    InvalidateFrustum();
	    }

        /// <summary>
        ///     Disables reflection modification previously turned on with <see cref="EnableReflection"/>.
        /// </summary>
        public virtual void DisableReflection()
        {
            _isReflected = false;
            _lastLinkedReflectionPlane.Normal = Vector3.Zero;
            InvalidateView();
        }

        /// <summary>
        ///		Links the frustum to a custom near clip plane, which can be used
        ///		to clip geometry in a custom manner without using user clip planes.
        /// </summary>
        /// <remarks>
        ///		<p>
        ///		There are several applications for clipping a scene arbitrarily by
        ///		a single plane; the most common is when rendering a reflection to 
        ///		a texture, and you only want to render geometry that is above the 
        ///		water plane (to do otherwise results in artefacts). Whilst it is
        ///		possible to use user clip planes, they are not supported on all
        ///		cards, and sometimes are not hardware accelerated when they are
        ///		available. Instead, where a single clip plane is involved, this
        ///		technique uses a 'fudging' of the near clip plane, which is 
        ///		available and fast on all hardware, to perform as the arbitrary
        ///		clip plane. This does change the shape of the frustum, leading 
        ///		to some depth buffer loss of precision, but for many of the uses of
        ///		this technique that is not an issue.</p>
        ///		<p>
        ///		This version of the method links to a plane, rather than requiring
        ///		a by-value plane definition, and therefore you can 
        ///		make changes to the plane (e.g. by moving / rotating the node it is
        ///		attached to) and they will automatically affect this object.
        ///		</p>
        ///		<p>This technique only works for perspective projection.</p>
        /// </remarks>
        /// <param name="plane">The plane to link to to perform the clipping.</param>
        public virtual void EnableCustomNearClipPlane(IDerivedPlaneProvider plane)
        {
            _useObliqueDepthProjection = true;
            _linkedObliqueProjPlane = plane;
            _obliqueProjPlane = plane.DerivedPlane;
            InvalidateFrustum();
        }

        /// <summary>
        ///		Links the frustum to a custom near clip plane, which can be used
        ///		to clip geometry in a custom manner without using user clip planes.
        /// </summary>
        /// <remarks>
        ///		<p>
        ///		There are several applications for clipping a scene arbitrarily by
        ///		a single plane; the most common is when rendering a reflection to 
        ///		a texture, and you only want to render geometry that is above the 
        ///		water plane (to do otherwise results in artefacts). Whilst it is
        ///		possible to use user clip planes, they are not supported on all
        ///		cards, and sometimes are not hardware accelerated when they are
        ///		available. Instead, where a single clip plane is involved, this
        ///		technique uses a 'fudging' of the near clip plane, which is 
        ///		available and fast on all hardware, to perform as the arbitrary
        ///		clip plane. This does change the shape of the frustum, leading 
        ///		to some depth buffer loss of precision, but for many of the uses of
        ///		this technique that is not an issue.</p>
        ///		<p>
        ///		This version of the method links to a plane, rather than requiring
        ///		a by-value plane definition, and therefore you can 
        ///		make changes to the plane (e.g. by moving / rotating the node it is
        ///		attached to) and they will automatically affect this object.
        ///		</p>
        ///		<p>This technique only works for perspective projection.</p>
        /// </remarks>
        /// <param name="plane">The plane to link to to perform the clipping.</param>
        public virtual void EnableCustomNearClipPlane(Plane plane)
        {
            _useObliqueDepthProjection = true;
            _linkedObliqueProjPlane = null;
            _obliqueProjPlane = plane;
            InvalidateFrustum();
        }

        /// <summary>
        ///     Modifies this camera so it always renders from the reflection of itself through the
        ///     plane specified.
        /// </summary>
        /// <remarks>
        ///     This is obviously useful for rendering planar reflections.
        /// </remarks>
        /// <param name="plane"></param>
        public virtual void EnableReflection(Plane plane)
        {
            _isReflected = true;
            _reflectionPlane = plane;
            _linkedReflectionPlane = null;
            _reflectionMatrix = Utility.BuildReflectionMatrix(plane);
            InvalidateView();
        }

        /// <summary>
        ///		Modifies this frustum so it always renders from the reflection of itself through the
        ///		plane specified. Note that this version of the method links to a plane
        ///		so that changes to it are picked up automatically.
        /// </summary>
        /// <remarks>This is obviously useful for performing planar reflections.</remarks>
        /// <param name="plane"></param>
        public virtual void EnableReflection(IDerivedPlaneProvider plane)
        {
            _isReflected = true;
            _linkedReflectionPlane = plane;
            _reflectionPlane = _linkedReflectionPlane.DerivedPlane;
            _reflectionMatrix = Utility.BuildReflectionMatrix(_reflectionPlane);
            _lastLinkedReflectionPlane = _reflectionPlane;
            InvalidateView();
        }

        /// <summary>
        ///		Get the derived position of this frustum.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector3 GetPositionForViewUpdate()
        {
            return _lastParentPosition;
        }

        /// <summary>
        ///		Get the derived orientation of this frustum.
        /// </summary>
        /// <returns></returns>
        protected virtual Quaternion GetOrientationForViewUpdate()
        {
            return _lastParentOrientation;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool IsObjectVisible(AxisAlignedBox box)
        {
            // this overload doesnt care about the clipping plane, but we gotta
            // pass in something to the out param anyway
            FrustumPlane dummy;
            return IsObjectVisible(box, out dummy);
        }

        /// <summary>
        ///		Tests whether the given box is visible in the Frustum.
        ///	 </summary>
        /// <param name="box"> Bounding box to be checked.</param>
        /// <param name="culledBy">
        ///		Optional FrustrumPlane params which will be filled by the plane which culled
        ///		the box if the result was false.
        ///	</param>
        /// <returns>True if the box is visible, otherwise false.</returns>
        public bool IsObjectVisible(AxisAlignedBox box, out FrustumPlane culledBy)
        {
            // Null boxes are always invisible
            if (box.IsNull)
            {
                culledBy = FrustumPlane.None;
                return false;
            }

            // Make any pending updates to the calculated frustum
            UpdateView();

            // Get corners of the box
            Vector3[] corners = box.Corners;

            // For each plane, see if all points are on the negative side
            // If so, object is not visible
            for (int plane = 0; plane < 6; plane++)
            {
                // skip far plane if infinite view frustum
                if (_farDistance == 0 && plane == (int)FrustumPlane.Far)
                {
                    continue;
                }

                if (_planes[plane].GetSide(corners[0]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[1]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[2]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[3]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[4]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[5]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[6]) == PlaneSide.Negative &&
                    _planes[plane].GetSide(corners[7]) == PlaneSide.Negative)
                {
                    // ALL corners on negative side therefore out of view
                    culledBy = (FrustumPlane)plane;
                    return false;
                }
            }

            // box is not culled
            culledBy = FrustumPlane.None;
            return true;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool IsObjectVisible(Sphere sphere)
        {
            // this overload doesnt care about the clipping plane, but we gotta
            // pass in something to the out param anyway
            FrustumPlane dummy;
            return IsObjectVisible(sphere, out dummy);
        }

        /// <summary>
        ///		Tests whether the given sphere is in the viewing frustum.
        /// </summary>
        /// <param name="sphere">Bounding sphere to be checked.</param>
        /// <param name="culledBy">
        ///		Optional FrustrumPlane params which will be filled by the plane which culled
        ///		the box if the result was false.
        ///	</param>
        /// <returns>True if the box is visible, otherwise false.</returns>
        public bool IsObjectVisible(Sphere sphere, out FrustumPlane culledBy)
        {
            // Make any pending updates to the calculated frustum
            UpdateView();

            // For each plane, see if sphere is on negative side
            // If so, object is not visible
            for (int plane = 0; plane < 6; plane++)
            {
                if (_farDistance == 0 && plane == (int)FrustumPlane.Far)
                {
                    continue;
                }

                // If the distance from sphere center to plane is negative, and 'more negative' 
                // than the radius of the sphere, sphere is outside frustum
                if (_planes[plane].GetDistance(sphere.Center) < -sphere.Radius)
                {
                    // ALL corners on negative side therefore out of view
                    culledBy = (FrustumPlane)plane;
                    return false;
                }
            }

            // sphere is not culled
            culledBy = FrustumPlane.None;
            return true;
        }

        /// <summary>
        ///		Overloaded method.
        /// </summary>
        /// <param name="box"></param>
        /// <returns></returns>
        public bool IsObjectVisible(Vector3 vertex)
        {
            // this overload doesnt care about the clipping plane, but we gotta
            // pass in something to the out param anyway
            FrustumPlane dummy;
            return IsObjectVisible(vertex, out dummy);
        }

        /// <summary>
        ///		Tests whether the given 3D point is in the viewing frustum.
        /// </summary>
        /// <param name="vector">3D point to check for frustum visibility.</param>
        /// <param name="culledBy">
        ///		Optional FrustrumPlane params which will be filled by the plane which culled
        ///		the box if the result was false.
        ///	</param>
        /// <returns>True if the box is visible, otherwise false.</returns>
        public bool IsObjectVisible(Vector3 vertex, out FrustumPlane culledBy)
        {
            // Make any pending updates to the calculated frustum
            UpdateView();

            // For each plane, see if all points are on the negative side
            // If so, object is not visible
            for (int plane = 0; plane < 6; plane++)
            {
                if (_farDistance == 0 && plane == (int)FrustumPlane.Far)
                {
                    continue;
                }

                if (_planes[plane].GetSide(vertex) == PlaneSide.Negative)
                {
                    // ALL corners on negative side therefore out of view
                    culledBy = (FrustumPlane)plane;
                    return false;
                }
            }

            // vertex is not culled
            culledBy = FrustumPlane.None;
            return true;
        }

        public virtual bool ProjectSphere(Sphere sphere, out float left, out float top, out float right, out float bottom)
        {
            // initialise
            left = bottom = -1.0f;
            right = top = 1.0f;

            // Transform light position into camera space
            Vector3 eyeSpacePos = this.ViewMatrix.TransformAffine(sphere.Center);

            if (eyeSpacePos.z < 0)
            {
                float r = sphere.Radius;
                // early-exit
                if (eyeSpacePos.LengthSquared <= r * r)
                    return false;

                Vector3 screenSpacePos = this.ProjectionMatrix * eyeSpacePos;

                // perspective attenuate
                Vector3 spheresize = new Vector3(r, r, eyeSpacePos.z);
                spheresize = this.ProjectionMatrixRSDepth * spheresize;

                float possLeft = screenSpacePos.x - spheresize.x;
                float possRight = screenSpacePos.x + spheresize.x;
                float possTop = screenSpacePos.y + spheresize.y;
                float possBottom = screenSpacePos.y - spheresize.y;

                left = Utility.Max(-1.0f, possLeft);
                right = Utility.Min(1.0f, possRight);
                top = Utility.Min(1.0f, possTop);
                bottom = Utility.Max(-1.0f, possBottom);
            }

            return (left != -1.0f) || (top != 1.0f) || (right != 1.0f) || (bottom != -1.0f);
        }

        /// <summary>
        ///     Signal to update frustum information.
        /// </summary>
        protected virtual void InvalidateFrustum()
        {
            _recalculateFrustum = true;
        }

        /// <summary>
        ///     Signal to update view information.
        /// </summary>
        protected virtual void InvalidateView()
        {
            _recalculateView = true;
        }

        protected void CalcProjectionParameters(ref float vpLeft, ref float vpRight, ref float vpBottom, ref float vpTop)
        {
            if (_customProjMatrix)
            {
                // Convert clipspace corners to camera space
                Matrix4 invProj = _projectionMatrix.Inverse();
                Vector3 topLeft = new Vector3(-0.5f, 0.5f, 0.0f);
                Vector3 bottomRight = new Vector3(0.5f, -0.5f, 0.0f);

                topLeft = invProj * topLeft;
                bottomRight = invProj * bottomRight;

                vpLeft = topLeft.x;
                vpTop = topLeft.y;
                vpRight = bottomRight.x;
                vpBottom = bottomRight.y;

            }
            else
            {
                // Calculate general projection parameters

                float thetaY = Utility.DegreesToRadians(_fieldOfView * 0.5f);
                float tanThetaY = Utility.Tan(thetaY);
                float tanThetaX = tanThetaY * _aspectRatio;

                // Unknown how to apply frustum offset to orthographic camera, just ignore here
                float nearFocal = (_projectionType == Projection.Perspective) ? _nearDistance / _focalLength : 0;
                float nearOffsetX = _frustumOffset.x * nearFocal;
                float nearOffsetY = _frustumOffset.y * nearFocal;
                float half_w = tanThetaX * _nearDistance;
                float half_h = tanThetaY * _nearDistance;

                vpLeft = -half_w + nearOffsetX;
                vpRight = +half_w + nearOffsetX;
                vpBottom = -half_h + nearOffsetY;
                vpTop = + half_h + nearOffsetY;
            }
        }

        /// <summary>
        ///		Updates the frustum data.
        /// </summary>
        protected virtual void UpdateFrustum()
        {
            if (IsFrustumOutOfDate)
            {
                float vpTop = 0.0f;
                float vpRight = 0.0f;
                float vpBottom = 0.0f;
                float vpLeft = 0.0f;

                CalcProjectionParameters(ref vpLeft, ref vpRight, ref vpBottom, ref vpTop);
 
                if (!_customViewMatrix)
                {
                    // The code below will dealing with general projection 
                    // parameters, similar glFrustum and glOrtho.
                    // Doesn't optimise manually except division operator, so the 
                    // code more self-explaining.

                    float inv_w = 1.0f / (vpRight - vpLeft);
                    float inv_h = 1.0f / (vpTop - vpBottom);
                    float inv_d = 1.0f / (_farDistance - _nearDistance);

                    // Recalc if frustum params changed
                    if (_projectionType == Projection.Perspective)
                    {
                        // Calc matrix elements
                        float A = 2.0f * _nearDistance * inv_w;
                        float B = 2.0f * _nearDistance * inv_h;
                        float C = (vpRight + vpLeft) * inv_w;
                        float D = (vpTop + vpBottom) * inv_h;
                        float q, qn;
                        if (_farDistance == 0.0f)
                        {
                            // Infinite far plane
                            q = Frustum.InfiniteFarPlaneAdjust - 1.0f;
                            qn = _nearDistance * (Frustum.InfiniteFarPlaneAdjust - 2.0f);
                        }
                        else
                        {
                            q = -(_farDistance + _nearDistance) * inv_d;
                            qn = -2.0f * (_farDistance * _nearDistance) * inv_d;
                        }

                        // NB: This creates 'uniform' perspective projection matrix,
                        // which depth range [-1,1], right-handed rules
                        //
                        // [ A   0   C   0  ]
                        // [ 0   B   D   0  ]
                        // [ 0   0   q   qn ]
                        // [ 0   0   -1  0  ]
                        //
                        // A = 2 * near / (right - left)
                        // B = 2 * near / (top - bottom)
                        // C = (right + left) / (right - left)
                        // D = (top + bottom) / (top - bottom)
                        // q = - (far + near) / (far - near)
                        // qn = - 2 * (far * near) / (far - near)

                        _projectionMatrix = Matrix4.Zero;
                        _projectionMatrix.m00 = A;
                        _projectionMatrix.m02 = C;
                        _projectionMatrix.m11 = B;
                        _projectionMatrix.m12 = D;
                        _projectionMatrix.m22 = q;
                        _projectionMatrix.m23 = qn;
                        _projectionMatrix.m32 = -1.0f;

                        if (_useObliqueDepthProjection)
                        {
                            // Translate the plane into view space

                            // Don't use getViewMatrix here, incase overrided by 
                            // camera and return a cull frustum view matrix
                            UpdateView();
                            
                            Plane plane = _viewMatrix * _obliqueProjPlane;

                            // Thanks to Eric Lenyel for posting this calculation 
                            // at www.terathon.com

                            // Calculate the clip-space corner point opposite the 
                            // clipping plane
                            // as (sgn(clipPlane.x), sgn(clipPlane.y), 1, 1) and
                            // transform it into camera space by multiplying it
                            // by the inverse of the projection matrix

                            /* generalised version
                            Vector4 q = matrix.inverse() * 
                            Vector4(Math::Sign(plane.normal.x), 
                            Math::Sign(plane.normal.y), 1.0f, 1.0f);
                            */
                            Vector4 q1 = new Vector4();
                            q1.x = (System.Math.Sign(plane.Normal.x) + _projectionMatrix.m02) / _projectionMatrix.m00;
                            q1.y = (System.Math.Sign(plane.Normal.y) + _projectionMatrix.m12) / _projectionMatrix.m11;
                            q1.z = -1.0f;
                            q1.w = (1.0f + _projectionMatrix.m22) / _projectionMatrix.m23;

                            // Calculate the scaled plane vector
                            Vector4 clipPlane4d = new Vector4(plane.Normal.x, plane.Normal.y, plane.Normal.z, plane.D);
                            Vector4 c = clipPlane4d * (2.0f / (clipPlane4d.Dot(q1)));

                            // Replace the third row of the projection matrix
                            _projectionMatrix.m20 = c.x;
                            _projectionMatrix.m21 = c.y;
                            _projectionMatrix.m22 = c.z + 1.0f;
                            _projectionMatrix.m23 = c.w;
                        }
                    } // perspective
                    else if (_projectionType == Projection.Orthographic)
                    {
                        float A = 2.0f * inv_w;
                        float B = 2.0f * inv_h;
                        float C = -(vpRight + vpLeft) * inv_w;
                        float D = -(vpTop + vpBottom) * inv_h;
                        float q, qn;
                        if (_farDistance == 0.0f)
                        {
                            // Can not do infinite far plane here, avoid divided zero only
                            q = -Frustum.InfiniteFarPlaneAdjust / _nearDistance;
                            qn = -Frustum.InfiniteFarPlaneAdjust - 1.0f;
                        }
                        else
                        {
                            q = -2.0f * inv_d;
                            qn = -(_farDistance + _nearDistance) * inv_d;
                        }

                        // NB: This creates 'uniform' orthographic projection matrix,
                        // which depth range [-1,1], right-handed rules
                        //
                        // [ A   0   0   C  ]
                        // [ 0   B   0   D  ]
                        // [ 0   0   q   qn ]
                        // [ 0   0   0   1  ]
                        //
                        // A = 2 * / (right - left)
                        // B = 2 * / (top - bottom)
                        // C = - (right + left) / (right - left)
                        // D = - (top + bottom) / (top - bottom)
                        // q = - 2 / (far - near)
                        // qn = - (far + near) / (far - near)

                        _projectionMatrix = Matrix4.Zero;
                        _projectionMatrix.m00 = A;
                        _projectionMatrix.m03 = C;
                        _projectionMatrix.m11 = B;
                        _projectionMatrix.m13 = D;
                        _projectionMatrix.m22 = q;
                        _projectionMatrix.m23 = qn;
                        _projectionMatrix.m33 = 1.0f;
                    } // ortho
                } // if !_customProjectionMatrix

                // grab a reference to the current render system
                RenderSystem renderSystem = Root.Instance.RenderSystem;

                // API specific
                _projectionMatrixRS = renderSystem.ConvertProjectionMatrix(_projectionMatrix);

                // API specific for Gpu Programs
                _projectionMatrixRSDepth = renderSystem.ConvertProjectionMatrix(_projectionMatrix, true);

                float farDist = (_farDistance == 0.0f) ? 100000.0f : _farDistance;
		
                // Near plane bounds
		        Vector3 min = new Vector3(vpLeft, vpBottom, -farDist);
		        Vector3 max = new Vector3(vpRight, vpTop, 0);

		        if (_customProjMatrix)
		        {
			        // Some custom projection matrices can have unusual inverted settings
			        // So make sure the AABB is the right way around to start with
			        Vector3 tmp = min;
			        min.Floor(max);
			        max.Ceil(tmp);
		        }

                float radio = 1.0f;
		        if (_projectionType == Projection.Perspective)
		        {
			        // Merge with far plane bounds
			        radio = _farDistance / _nearDistance;
			        min.Floor(new Vector3(vpLeft * radio, vpBottom * radio, -_farDistance));
			        max.Ceil(new Vector3(vpRight * radio, vpTop * radio, 0));
		        }

                _boundingBox.SetExtents(min, max);

                // Calc far palne corners
                float farLeft = vpLeft * radio;
                float farRight = vpRight * radio;
                float farBottom = vpBottom * radio;
                float farTop = vpTop * radio;

                // Calculate vertex positions
                // 0 is the origin
                // 1, 2, 3, 4 are the points on the near plane, top left first, clockwise
                // 5, 6, 7, 8 are the points on the far plane, top left first, clockwise
                HardwareVertexBuffer buffer = _vertexData.vertexBufferBinding.GetBuffer(0);

                IntPtr posPtr = buffer.Lock(BufferLocking.Discard);

                unsafe
                {
                    float* pPos = (float*)posPtr.ToPointer();

                    // near plane (remember frustum is going in -Z direction)
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    // far plane (remember frustum is going in -Z direction)
                    *pPos++ = farLeft;
                    *pPos++ = farTop;
                    *pPos++ = -farDist;
                    *pPos++ = farRight;
                    *pPos++ = farTop;
                    *pPos++ = -farDist;

                    *pPos++ = farRight;
                    *pPos++ = farTop;
                    *pPos++ = -farDist;
                    *pPos++ = farRight;
                    *pPos++ = farBottom;
                    *pPos++ = -farDist;

                    *pPos++ = farRight;
                    *pPos++ = farBottom;
                    *pPos++ = -farDist;
                    *pPos++ = farLeft;
                    *pPos++ = farBottom;
                    *pPos++ = -farDist;

                    *pPos++ = farLeft;
                    *pPos++ = farBottom;
                    *pPos++ = -farDist;
                    *pPos++ = farLeft;
                    *pPos++ = farTop;
                    *pPos++ = -farDist;

                    // Sides of the pyramid
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;

                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = 0.0f;
                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;

                    // Sides of the box
                    *pPos++ = vpLeft;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farLeft;
                    *pPos++ = farTop;
                    *pPos++ = -farDist;

                    *pPos++ = vpRight;
                    *pPos++ = vpTop;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farRight;
                    *pPos++ = farTop;
                    *pPos++ = -farDist;

                    *pPos++ = vpRight;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farRight;
                    *pPos++ = farBottom;
                    *pPos++ = -farDist;

                    *pPos++ = vpLeft;
                    *pPos++ = vpBottom;
                    *pPos++ = -_nearDistance;
                    *pPos++ = farLeft;
                    *pPos++ = farBottom;
                    *pPos++ = -farDist;
                }

                // don't forget to unlock!
                buffer.Unlock();

                _recalculateFrustum = false;
            }
        }


        /// <summary>
        ///		Updates the view matrix.
        /// </summary>
        protected virtual void UpdateView()
        {
            // check if the view is out of date
            if (IsViewOutOfDate)
            {
                // View matrix is:
                //
                //  [ Lx  Uy  Dz  Tx  ]
                //  [ Lx  Uy  Dz  Ty  ]
                //  [ Lx  Uy  Dz  Tz  ]
                //  [ 0   0   0   1   ]
                //
                // Where T = -(Transposed(Rot) * Pos)

                // This is most efficiently done using 3x3 Matrices

                // Get orientation from quaternion
                Quaternion orientation = GetOrientationForViewUpdate();
                Vector3 position = GetPositionForViewUpdate();
                Matrix3 rotation = orientation.ToRotationMatrix();

                if (!_customViewMatrix)
                {
                    // make the translation relative to the new axis
                    Matrix3 rotationT = rotation.Transpose();
                    Vector3 translation = -rotationT * position;

                    // Make final matrix
                    _viewMatrix = Matrix4.Identity;
                    _viewMatrix = rotationT; // initialize the upper 3x3 portion with the rotation
                    _viewMatrix.m03 = translation.x;
                    _viewMatrix.m13 = translation.y;
                    _viewMatrix.m23 = translation.z;

                    // deal with reflections
                    if (_isReflected)
                    {
                        _viewMatrix = _viewMatrix * _reflectionMatrix;
                    }
                }

                // update the frustum planes
                UpdateFrustum();

                Matrix4 combo = _projectionMatrix * _viewMatrix;

                _planes[(int)FrustumPlane.Left].Normal.x = combo.m30 + combo.m00;
                _planes[(int)FrustumPlane.Left].Normal.y = combo.m31 + combo.m01;
                _planes[(int)FrustumPlane.Left].Normal.z = combo.m32 + combo.m02;
                _planes[(int)FrustumPlane.Left].D = combo.m33 + combo.m03;

                _planes[(int)FrustumPlane.Right].Normal.x = combo.m30 - combo.m00;
                _planes[(int)FrustumPlane.Right].Normal.y = combo.m31 - combo.m01;
                _planes[(int)FrustumPlane.Right].Normal.z = combo.m32 - combo.m02;
                _planes[(int)FrustumPlane.Right].D = combo.m33 - combo.m03;

                _planes[(int)FrustumPlane.Top].Normal.x = combo.m30 - combo.m10;
                _planes[(int)FrustumPlane.Top].Normal.y = combo.m31 - combo.m11;
                _planes[(int)FrustumPlane.Top].Normal.z = combo.m32 - combo.m12;
                _planes[(int)FrustumPlane.Top].D = combo.m33 - combo.m13;

                _planes[(int)FrustumPlane.Bottom].Normal.x = combo.m30 + combo.m10;
                _planes[(int)FrustumPlane.Bottom].Normal.y = combo.m31 + combo.m11;
                _planes[(int)FrustumPlane.Bottom].Normal.z = combo.m32 + combo.m12;
                _planes[(int)FrustumPlane.Bottom].D = combo.m33 + combo.m13;

                _planes[(int)FrustumPlane.Near].Normal.x = combo.m30 + combo.m20;
                _planes[(int)FrustumPlane.Near].Normal.y = combo.m31 + combo.m21;
                _planes[(int)FrustumPlane.Near].Normal.z = combo.m32 + combo.m22;
                _planes[(int)FrustumPlane.Near].D = combo.m33 + combo.m23;

                _planes[(int)FrustumPlane.Far].Normal.x = combo.m30 - combo.m20;
                _planes[(int)FrustumPlane.Far].Normal.y = combo.m31 - combo.m21;
                _planes[(int)FrustumPlane.Far].Normal.z = combo.m32 - combo.m22;
                _planes[(int)FrustumPlane.Far].D = combo.m33 - combo.m23;

                // renormalize any normals which were not unit length
                for (int i = 0; i < 6; i++)
                {
                    float length = _planes[i].Normal.Normalize();
                    _planes[i].D /= length;
                }

                // Update worldspace corners
                Matrix4 eyeToWorld = _viewMatrix.Inverse();

                // Note: Even though we can dealing with general projection matrix here,
                //       but because it's incompatibly with infinite far plane, thus, we
                //       still need to working with projection parameters.

                // Calc near plane corners
                float nearLeft = 0.0f, nearRight = 0.0f, nearBottom = 0.0f, nearTop = 0.0f;
                CalcProjectionParameters(ref nearLeft, ref nearRight, ref nearBottom, ref nearTop);

                // Treat infinite fardist as some arbitrary far value
                float farDist = (_farDistance == 0.0f) ? 100000.0f : _farDistance;

                // Calc far plane corners
                float radio = (_projectionType == Projection.Perspective) ? farDist / _nearDistance : 1.0f;
                float farLeft = nearLeft * radio;
                float farRight = nearRight * radio;
                float farBottom = nearBottom * radio;
                float farTop = nearTop * radio;

                // near
                _worldSpaceCorners[0] = eyeToWorld.TransformAffine(new Vector3(nearRight, nearTop, -_nearDistance));
                _worldSpaceCorners[1] = eyeToWorld.TransformAffine(new Vector3(nearLeft, nearTop, -_nearDistance));
                _worldSpaceCorners[2] = eyeToWorld.TransformAffine(new Vector3(nearLeft, nearBottom, -_nearDistance));
                _worldSpaceCorners[3] = eyeToWorld.TransformAffine(new Vector3(nearRight, nearBottom, -_nearDistance));
                // far
                _worldSpaceCorners[4] = eyeToWorld.TransformAffine(new Vector3(farRight, farTop, -farDist));
                _worldSpaceCorners[5] = eyeToWorld.TransformAffine(new Vector3(farLeft, farTop, -farDist));
                _worldSpaceCorners[6] = eyeToWorld.TransformAffine(new Vector3(farLeft, farBottom, -farDist));
                _worldSpaceCorners[7] = eyeToWorld.TransformAffine(new Vector3(farRight, farBottom, -farDist));

                // update since we have now recalculated everything
                _recalculateView = false;
            }
        }

        #endregion Methods

        #region Overloaded operators

        /// <summary>
        ///		An indexer that accepts a FrustumPlane enum value and return the appropriate plane side of the Frustum.
        /// </summary>
        public Plane this[FrustumPlane plane]
        {
            get
            {
                // make any pending updates to the calculated frustum
                // TODO: Was causing a stack overflow, revisit
                UpdateView();

                // convert the incoming plan enum type to a int
                int index = (int)plane;

                // access the planes array by index
                return _planes[index];
            }
        }

        #endregion

        #region SceneObject Members

        /// <summary>
        ///    Local bounding radius of this camera.
        /// </summary>
        public override float BoundingRadius
        {
            get
            {
                return (_farDistance == 0) ? InfiniteFarPlaneDistance : _farDistance;
            }
        }

        /// <summary>
        ///     Returns the bounding box for this frustum.
        /// </summary>
        public override AxisAlignedBox BoundingBox
        {
            get
            {
                return _boundingBox;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="camera"></param>
        public override void NotifyCurrentCamera(Camera camera)
        {
            // do nothing
        }

        /// <summary>
        ///     Implemented to add outself to the rendering queue.
        /// </summary>
        /// <param name="queue"></param>
        public override void UpdateRenderQueue(RenderQueue queue)
        {
            if (isVisible)
            {
                queue.AddRenderable(this);
            }
        }

        #endregion SceneObject Members

        #region IRenderable Members

        public bool CastsShadows
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        ///     Returns the material to use when rendering this frustum.
        /// </summary>
        public Material Material
        {
            get
            {
                return _material;
            }
        }

        /// <summary>
        ///     Just returns the best technique for our material.
        /// </summary>
        public Technique Technique
        {
            get
            {
                return _material.GetBestTechnique();
            }
        }

        public void GetRenderOperation(RenderOperation op)
        {
            UpdateView();
            UpdateFrustum();

            op.operationType = OperationType.LineList;
            op.useIndices = false;
            op.vertexData = _vertexData;
        }

        public void GetWorldTransforms(Matrix4[] matrices)
        {
            if (parentNode != null)
            {
                parentNode.GetWorldTransforms(matrices);
            }
        }

        /// <summary>
        ///     Returns a dummy list since we won't be lit.
        /// </summary>
        public LightList Lights
        {
            get
            {
                return _dummyLightList;
            }
        }

        public bool NormalizeNormals
        {
            get
            {
                return false;
            }
        }

        public ushort NumWorldTransforms
        {
            get
            {
                return 1;
            }
        }

        public bool UseIdentityProjection
        {
            get
            {
                return false;
            }
        }

        public bool UseIdentityView
        {
            get
            {
                return false;
            }
        }

        public virtual bool PolygonModeOverrideable
        {
            get
            {
                return false;
            }
        }

        public Quaternion WorldOrientation
        {
            get
            {
                if (parentNode != null)
                {
                    return parentNode.DerivedOrientation;
                }
                else
                {
                    return Quaternion.Identity;
                }
            }
        }

        public Vector3 WorldPosition
        {
            get
            {
                if (parentNode != null)
                {
                    return parentNode.DerivedPosition;
                }
                else
                {
                    return Vector3.Zero;
                }
            }
        }

        public Vector3[] WorldSpaceCorners
        {
            get
            {
                UpdateView();

                return _worldSpaceCorners;
            }
        }

        public float GetSquaredViewDepth(Camera camera)
        {
            if (parentNode != null)
            {
                return (camera.DerivedPosition - parentNode.DerivedPosition).LengthSquared;
            }
            else
            {
                return 0;
            }
        }

        public Vector4 GetCustomParameter(int index)
        {
            if (_customParams[index] == null)
            {
                throw new Exception("A parameter was not found at the given index");
            }
            else
            {
                return (Vector4)_customParams[index];
            }
        }

        public void SetCustomParameter(int index, Vector4 val)
        {
            _customParams[index] = val;
        }

        public void UpdateCustomGpuParameter(GpuProgramParameters.AutoConstantEntry entry, GpuProgramParameters gpuParams)
        {
            if (_customParams[entry.data] != null)
            {
                gpuParams.SetConstant(entry.index, (Vector4)_customParams[entry.data]);
            }
        }

        #endregion
    }
}