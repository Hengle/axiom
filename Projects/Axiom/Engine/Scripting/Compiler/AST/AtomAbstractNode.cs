#region LGPL License
/*
Axiom Graphics Engine Library
Copyright � 2003-2011 Axiom Project Team

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
//     <id value="$Id$"/>
// </file>
#endregion SVN Version Information

#region Namespace Declarations

#endregion Namespace Declarations

namespace Axiom.Scripting.Compiler.AST
{
	/// <summary>
	///  This is an abstract node which cannot be broken down further
	/// </summary>
	public class AtomAbstractNode : AbstractNode
	{
		#region Fields and Properties

		private bool _parsed = false;
		private string _value;

		public uint Id;

		private bool _isNumber = false;

		public bool IsNumber
		{
			get
			{
				if ( !_parsed )
					_parse();
				return _isNumber;
			}
		}

		private float _number;

		public float Number
		{
			get
			{
				if ( !_parsed )
					_parse();
				return _number;
			}
		}
		#endregion Fields and Properties

		public AtomAbstractNode( AbstractNode parent )
			: base( parent )
		{
			Type = AbstractNodeType.Atom;
		}

		private void _parse()
		{
#if !(XBOX || XBOX360)
			_isNumber = float.TryParse( _value, out _number );
#else
			try
			{
				_number = float.Parse(_value);
				_isNumber = true;
			}
			catch (Exception)
			{
				_isNumber =  false;
			}
#endif
			_parsed = true;
		}

		#region AbstractNode Implementation

        /// <see cref="AbstractNode.Clone"/>
		public override AbstractNode Clone()
		{
			AtomAbstractNode node = new AtomAbstractNode( Parent );
			node.File = File;
			node.Line = Line;
			node.Id = this.Id;
			node.Type = Type;
			node._value = Value;
			return node;
		}

        /// <see cref="AbstractNode.Value"/>
		public override string Value
		{
			get
			{
				return _value;
			}

			set
			{
				_value = value;
			}
		}

		#endregion AbstractNode Implementation
	}
}