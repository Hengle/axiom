#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Threading;

namespace Axiom.Text
{
	/// <summary>
	/// Summary description for TextManager.
	/// </summary>
	public class TextManager
	{
		static TextManager()
		{
		}

		/// <summary>
		/// Used to retreive a text resource by passing a key.  
		/// </summary>
		/// <param name="keyName"></param>
		/// <returns></returns>
		public static String GetString(String keyName)
		{
			return keyName;
		}
	}
}
