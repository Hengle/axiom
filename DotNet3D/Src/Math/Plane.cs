#region LGPL License
/*
DotNet3D Library
Copyright (C) 2006 DotNet3D Project Team

The overall design, and a majority of the core engine and rendering code 
contained within this library is a derivative of the open source Object Oriented 
Graphics Engine OGRE, which can be found at http://ogre.sourceforge.net.  
Many thanks to the OGRE team for maintaining such a high quality project.

The math library included in this project, in addition to being a derivative of
the works of Ogre, also include derivative work of the free portion of the 
Wild Magic mathematics source code that is distributed with the excellent
book Game Engine Design.
http://www.wild-magic.com/

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

#region Namespace Declarations

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;

#endregion Namespace Declarations

namespace DotNet3D.Math
{
    /// <summary>
    /// Defines a plane in 3D space.
    /// </summary>
    /// <remarks>
    /// A plane is defined in 3D space by the equation
    /// Ax + By + Cz + D = 0
    ///
    /// This equates to a vector (the normal of the plane, whose x, y
    /// and z components equate to the coefficients A, B and C
    /// respectively), and a constant (D) which is the distance along
    /// the normal you have to go to move the plane back to the origin.
    /// </remarks>
    public struct Plane
    {
        /// <summary>
        /// The "positive side" of the plane is the half space to which the
        /// plane normal points. The "negative side" is the other half
        /// space. The flag "no side" indicates the plane itself.
        /// </summary>
        public enum Side
        {
            None,
            Positive,
            Negative
        }

        #region Fields and Properties

        /// <summary>
        ///		Direction the plane is facing.
        /// </summary>
        public Vector3 Normal;
        /// <summary>
        ///		Distance from the origin.
        /// </summary>
        public Real Distance;

        private static readonly Plane nullPlane = new Plane( Vector3.Zero, 0 );
        public static Plane Null
        {
            get
            {
                return nullPlane;
            }
        }

        #endregion Fields and Properties

        #region Constructors

        public Plane( Plane plane )
        {
            this.Normal = plane.Normal;
            this.Distance = plane.Distance;
        }

        /// <summary>
        ///		Construct a plane through a normal, and a distance to move the plane along the normal.
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="constant"></param>
        public Plane( Vector3 normal, float constant )
        {
            this.Normal = normal;
            this.Distance = -constant;
        }

        public Plane( Vector3 normal, Vector3 point )
        {
            this.Normal = normal;
            this.Distance = -normal.DotProduct( point );
        }

        /// <summary>
        ///		Construct a plane from 3 coplanar points.
        /// </summary>
        /// <param name="point0">First point.</param>
        /// <param name="point1">Second point.</param>
        /// <param name="point2">Third point.</param>
        public Plane( Vector3 point0, Vector3 point1, Vector3 point2 )
        {
            Vector3 edge1 = point1 - point0;
            Vector3 edge2 = point2 - point0;
            Normal = edge1.CrossProduct( edge2 );
            Normal.Normalize();
            Distance = -Normal.DotProduct( point0 );
        }

        #endregion

        #region Methods

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Side GetSide( Vector3 point )
        {
            float distance = GetDistance( point );

            if ( distance < 0.0f )
                return Side.Negative;

            if ( distance > 0.0f )
                return Side.Positive;

            return Side.None;
        }

        /// <summary>
        /// This is a pseudodistance. The sign of the return value is
        /// positive if the point is on the positive side of the plane,
        /// negative if the point is on the negative side, and zero if the
        ///	 point is on the plane.
        /// The absolute value of the return value is the true distance only
        /// when the plane normal is a unit length vector.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float GetDistance( Vector3 point )
        {
            return Normal.DotProduct( point ) + Distance;
        }

        /// <summary>
        ///		Construct a plane from 3 coplanar points.
        /// </summary>
        /// <param name="point0">First point.</param>
        /// <param name="point1">Second point.</param>
        /// <param name="point2">Third point.</param>
        public void Redefine( Vector3 point0, Vector3 point1, Vector3 point2 )
        {
            Vector3 edge1 = point1 - point0;
            Vector3 edge2 = point2 - point0;
            Normal = edge1.CrossProduct( edge2 );
            Normal.Normalize();
            Distance = -Normal.DotProduct( point0 );
        }

        #endregion Methods

        #region Object overrides

        /// <summary>
        ///		Object method for testing equality.
        /// </summary>
        /// <param name="obj">Object to test.</param>
        /// <returns>True if the 2 planes are logically equal, false otherwise.</returns>
        public override bool Equals( object obj )
        {
            return obj is Plane && this == (Plane)obj;
        }

        /// <summary>
        ///		Gets the hashcode for this Plane.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Distance.GetHashCode() ^ Normal.GetHashCode();
        }

        /// <summary>
        ///		Returns a string representation of this Plane.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format( "Distance: {0} Normal: {1}", Distance, Normal.ToString() );
        }

        #endregion

        #region Operator Overloads

        /// <summary>
        ///		Compares 2 Planes for equality.
        /// </summary>
        /// <param name="left">First plane.</param>
        /// <param name="right">Second plane.</param>
        /// <returns>true if equal, false if not equal.</returns>
        public static bool operator ==( Plane left, Plane right )
        {
            return ( left.Distance == right.Distance ) && ( left.Normal == right.Normal );
        }

        /// <summary>
        ///		Compares 2 Planes for inequality.
        /// </summary>
        /// <param name="left">First plane.</param>
        /// <param name="right">Second plane.</param>
        /// <returns>true if not equal, false if equal.</returns>
        public static bool operator !=( Plane left, Plane right )
        {
            return ( left.Distance != right.Distance ) || ( left.Normal != right.Normal );
        }

        #endregion
    }

    namespace Collections
    {

        using System.Collections.Generic;

        /// <summary>
        /// A Collection of Planes
        /// </summary>
        public class PlaneList : List<Plane>
        {
        }
    }
}
