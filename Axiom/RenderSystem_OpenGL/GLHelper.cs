#region LGPL License
/*
Axiom Game Engine Library
Copyright (C) 2003  Axiom Project Team

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

using System;
using System.Collections.Specialized;
using Axiom.Graphics;
using Tao.OpenGl;
using Tao.Platform.Windows;

namespace Axiom.RenderSystems.OpenGL {
    /// <summary>
    /// Summary description for GLHelper.
    /// </summary>
    public class GLHelper {
        private static StringCollection extensionList;
        private static string glVersion;
        private static string vendor;
        private static string videoCard;

        /// <summary>
        /// 
        /// </summary>
        public static StringCollection Extensions {
            get {
                return extensionList; 
            }
        }

        /// <summary>
        ///		Handy check to see if the current GL version is at least what is supplied.
        /// </summary>
        /// <param name="version">What you want to check for, i.e. "1.3" </param>
        /// <returns></returns>
        public static bool CheckMinVersion(string version) {
            return glVersion.StartsWith(version);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extention"></param>
        /// <returns></returns>
        public static bool SupportsExtension(string extention) {
            // check if the extension is supported
            return extensionList.Contains(extention);
        }

        /// <summary>
        /// 
        /// </summary>
        public static void InitializeExtensions() {
            if(extensionList == null) {
                // load GL extensions
                Ext.Init();

                // get the OpenGL version string and vendor name
                glVersion = Gl.glGetString(Gl.GL_VERSION);
                videoCard = Gl.glGetString(Gl.GL_RENDERER);
                vendor = Gl.glGetString(Gl.GL_VENDOR);

                // parse out the first piece of the vendor string if there are spaces in it
                if(vendor.IndexOf(" ") != -1) {
                    vendor = vendor.Substring(0, vendor.IndexOf(" "));
                }

                // create a new extension list
                extensionList = new StringCollection();

                string allExt = Gl.glGetString(Gl.GL_EXTENSIONS);
                string[] splitExt = allExt.Split(Char.Parse(" "));

                // store the parsed extension list
                for(int i = 0; i < splitExt.Length; i++)
                    extensionList.Add(splitExt[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usage"></param>
        /// <returns></returns>
        public static int ConvertEnum(BufferUsage usage) {
            switch(usage) {
                case BufferUsage.Static:
                    //case BufferUsage.StaticWriteOnly:
                    return (int)Gl.GL_STATIC_DRAW_ARB;

                case BufferUsage.Dynamic:
                case BufferUsage.DynamicWriteOnly:
                default:
                    return (int)Gl.GL_DYNAMIC_DRAW_ARB;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static int ConvertEnum(VertexElementType type) {
            switch(type) {
                case VertexElementType.Float1:
                case VertexElementType.Float2:
                case VertexElementType.Float3:
                case VertexElementType.Float4:
                    return Gl.GL_FLOAT;

                case VertexElementType.Short1:
                case VertexElementType.Short2:
                case VertexElementType.Short3:
                case VertexElementType.Short4:
                    return Gl.GL_SHORT;

                case VertexElementType.Color:
                    return Gl.GL_UNSIGNED_BYTE;
            }

            // should never reach this
            return 0;
        }

        /// <summary>
        ///		Find the GL int value for the CompareFunction enum.
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public static int ConvertEnum(CompareFunction func) {
            switch(func) {
                case CompareFunction.AlwaysFail:
                    return Gl.GL_NEVER;
                case CompareFunction.AlwaysPass:
                    return Gl.GL_ALWAYS;
                case CompareFunction.Less:
                    return Gl.GL_LESS;
                case CompareFunction.LessEqual:
                    return Gl.GL_LEQUAL;
                case CompareFunction.Equal:
                    return Gl.GL_EQUAL;
                case CompareFunction.NotEqual:
                    return Gl.GL_NOTEQUAL;
                case CompareFunction.GreaterEqual:
                    return Gl.GL_GEQUAL;
                case CompareFunction.Greater:
                    return Gl.GL_GREATER;
            } // switch

            // make the compiler happy
            return 0;
        }

        /// <summary>
        ///		Find the GL int value for the StencilOperation enum.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public static int ConvertEnum(StencilOperation op) {
            switch(op) {
                case StencilOperation.Keep:
                    return Gl.GL_KEEP;
                case StencilOperation.Zero:
                    return Gl.GL_ZERO;
                case StencilOperation.Replace:
                    return Gl.GL_REPLACE;
                case StencilOperation.Increment:
                    return Gl.GL_INCR;
                case StencilOperation.Decrement:
                    return Gl.GL_DECR;
                case StencilOperation.Invert:
                    return Gl.GL_INVERT;
            }

            // make the compiler happy
            return 0;
        }

        public static int ConvertEnum(GpuProgramType type) {
            switch(type) {
                case GpuProgramType.Vertex:
                    return Gl.GL_VERTEX_PROGRAM_ARB;

                case GpuProgramType.Fragment:
                    return Gl.GL_FRAGMENT_PROGRAM_ARB;
            }

            // make the compiler happy
            return 0;
        }

        public static string Vendor {
            get { 
                return vendor; 
            }
        }

        public static string VideoCard {
            get { 
                return videoCard; 
            }
        }

        public static string Version {
            get { 
                return glVersion; 
            }
        }
    }

    /// <summary>
    ///    Wrapper for Tao extension methods to cache the extension pointers and wrap methods eliminating
    ///    the need to pass them in manually.
    /// </summary>
    public class Ext {

        #region GL_ARB_multitexture

        private static IntPtr activeTextureARB;
        private static IntPtr clientActiveTextureARB;

        public static void glActiveTextureARB(int texture) {
            Gl.glActiveTextureARB(activeTextureARB, texture);
        }

        public static void glClientActiveTextureARB(int texture) {
            Gl.glClientActiveTextureARB(clientActiveTextureARB, texture);
        }

        #endregion GL_ARB_multitexture

        #region GL_ARB_vertex_buffer_object

        private static IntPtr bindBufferARBPtr;
        private static IntPtr bufferDataARBPtr;
        private static IntPtr bufferSubDataARBPtr;
        private static IntPtr deleteBuffersARBPtr;
        private static IntPtr genBuffersARBPtr;
        private static IntPtr getBufferSubDataARBPtr;      
        private static IntPtr mapBufferARBPtr;
        private static IntPtr unmapBufferARBPtr;

        public static void glBindBufferARB(int target, int buffer) {
            Gl.glBindBufferARB(bindBufferARBPtr, target, buffer);
        }

        public static void glBufferDataARB(int target, int size, IntPtr data, int usage) {
            Gl.glBufferDataARB(bufferDataARBPtr, target, size, data, usage);
        }

        public static void glBufferSubDataARB(int target, int offset, int size, IntPtr data) {
            Gl.glBufferSubDataARB(bufferSubDataARBPtr, target, offset, size, data);
        }

        public static void glDeleteBuffersARB(int number, ref int buffer) {
            // TODO: Fix, currently does nothing
            //Gl.glDeleteBuffersARB(deleteBuffersARBPtr, number, ref buffer);
        }

        public static void glGenBuffersARB(int number, out int buffer) {
            Gl.glGenBuffersARB(genBuffersARBPtr, number, out buffer);
        }

        public static void glGetBufferSubDataARB(int target, int offset, int size, IntPtr data) {
            Gl.glGetBufferSubDataARB(getBufferSubDataARBPtr, target, offset, size, data);
        }

        public static IntPtr glMapBufferARB(int target, int access) {
            return Gl.glMapBufferARB(mapBufferARBPtr, target, access);
        }

        public static void glUnmapBufferARB(int target) {
            Gl.glUnmapBufferARB(unmapBufferARBPtr, target);
        }

        #endregion GL_ARB_vertex_buffer_object

        #region GL_ARB_vertex_program/GL_ARB_fragment_program

        private static IntPtr bindProgramARBPtr;
        private static IntPtr deleteProgramsARBPtr;
        private static IntPtr genProgramsARBPtr;
        private static IntPtr programStringARBPtr;
        private static IntPtr isProgramARBPtr;
        private static IntPtr programLocalParameter4fvARBPtr;

        public static void glGenProgramsARB(int number, out int program) {
            Gl.glGenProgramsARB(genProgramsARBPtr, number, out program);
        }

        public static void glBindProgramARB(int type, int programId) {
            Gl.glBindProgramARB(bindProgramARBPtr, type, programId);
        }

        public static void glDeleteProgramsARB(int number, ref int program) {
            // TODO: Fix
            //Gl.glDeleteProgramsARB(deleteProgramsARBPtr, number, ref program);
        }

        public static void glProgramStringARB(int type, int format, int length, string source) {
            Gl.glProgramStringARB(programStringARBPtr, type, format, length, source);
        }

        public static bool glIsProgramARB(int programId) {
            return Gl.glIsProgramARB(isProgramARBPtr, programId) != 0;
        }

        public static void glProgramLocalParameter4vfARB(int type, int index, float[] values) {
            Gl.glProgramLocalParameter4fvARB(programLocalParameter4fvARBPtr, type, index, values);
        }

        #endregion GL_ARB_vertex_program/GL_ARB_fragment_program
        
        #region GL_EXT_texture3D
    
        private static IntPtr texImage3DEXTPtr;

        public static void glTexImage3DEXT(int target, int level, int internalformat, int width, int height, int depth, int border, int format, int type, byte[] pixels) {
            Gl.glTexImage3DEXT(texImage3DEXTPtr, target, level, internalformat, width, height, depth, border, format, type, pixels);
        }

        #endregion GL_EXT_texture3D

        #region GL_ARB_texture_compression

        private static IntPtr compressedTexImage2DARB;

        public static void glCompressedTexImage2DARB(int target, int level, int internalformat, int width, int height, int border, int imageSize, byte[] data)  {
            Gl.glCompressedTexImage2DARB(compressedTexImage2DARB, target, level, internalformat, width, height, border, imageSize, data);
        }

        #endregion 

        #region GL_ATI_fragment_shader

        private static IntPtr genFragmentShadersATIptr;
        private static IntPtr bindFragmentShaderATIptr;
        private static IntPtr deleteFragmentShaderATIptr;
        private static IntPtr beginFragmentShaderATIptr;
        private static IntPtr endFragmentShaderATIptr;
        private static IntPtr setFragmentShaderConstantATIptr;

        public static int glGenFragmentShadersATI(int range) {
            return Gl.glGenFragmentShadersATI(genFragmentShadersATIptr, range);
        }

        public static void glBindFragmentShaderATI(int id) {
            Gl.glBindFragmentShaderATI(bindFragmentShaderATIptr, id);
        }

        public static void glDeleteFragmentShaderATI(int id) {
            Gl.glDeleteFragmentShaderATI(deleteFragmentShaderATIptr, id);
        }

        public static void glBeginFragmentShaderATI() {
            Gl.glBeginFragmentShaderATI(beginFragmentShaderATIptr);
        }

        public static void glEndFragmentShaderATI() {
            Gl.glEndFragmentShaderATI(endFragmentShaderATIptr);
        }

        public static void glSetFragmentShaderConstantATI(int index, float[] values) {
            Gl.glSetFragmentShaderConstantATI(setFragmentShaderConstantATIptr, index, values);
        }

        #endregion GL_ATI_fragment_shader

        #region GL_NV_register_combiners

        private static IntPtr combinerStageParameterfvNVptr;

        public static void glCombinerStageParameterfvNV(int stage, int pname, float[] values) {
            Gl.glCombinerStageParameterfvNV(combinerStageParameterfvNVptr, stage, pname, values);
        }

        #endregion NV20 Crap

        #region GL_NV_fragment_program/GL_NV_vertex_program2

        private static IntPtr genProgramsNVptr;
        private static IntPtr bindProgramNVptr;
        private static IntPtr deleteProgramsNVptr;
        private static IntPtr loadProgramNVptr;
        private static IntPtr programNamedParameter4fNVptr;
        private static IntPtr programParameter4fvNVptr;

        public static void glGenProgramsNV(int num, out int id) {
            Gl.glGenProgramsNV(genProgramsNVptr, num, out id);
        }

        public static void glBindProgramNV(int target, int id) {
            Gl.glBindProgramNV(bindProgramNVptr, target, id);
        }

        public static void glDeleteProgramsNV(int num, ref int id) {
            Gl.glDeleteProgramsNV(deleteProgramsNVptr, num, ref id);
        }

        public static void glLoadProgramNV(int target, int id, int length, string program) {
            Gl.glLoadProgramNV(loadProgramNVptr, target, id, length, program);
        }

        public static void glProgramNamedParameter4fNV(int id, int length, string name, float x, float y, float z, float w) {
            Gl.glProgramNamedParameter4fNV(programNamedParameter4fNVptr, id, length, name, x, y, z, w);
        }
       
        public static void glProgramParameter4fvNV(int target, int index, float[] vals) {
            //Gl.gl
            Gl.glProgramParameter4fvNV(programParameter4fvNVptr, target, index, vals);
        }

        #endregion GL_NV_fragment_program/GL_NV_vertex_program2

        /// <summary>
        ///    Must be fired up after a GL context has been created.
        /// </summary>
        public static void Init() {
            // ARB_vertex_buffer_object
            bindBufferARBPtr = Wgl.wglGetProcAddress("glBindBufferARB");
            bufferDataARBPtr = Wgl.wglGetProcAddress("glBufferDataARB");        
            bufferSubDataARBPtr = Wgl.wglGetProcAddress("glBufferSubDataARB"); 
            deleteBuffersARBPtr = Wgl.wglGetProcAddress("glDeleteBuffersARB");       
            genBuffersARBPtr = Wgl.wglGetProcAddress("glGenBuffersARB");
            getBufferSubDataARBPtr = Wgl.wglGetProcAddress("glGetBufferSubDataARB");        
            mapBufferARBPtr = Wgl.wglGetProcAddress("glMapBufferARB");
            unmapBufferARBPtr = Wgl.wglGetProcAddress("glUnmapBufferARB");

            // ARB_multitexture
            activeTextureARB = Wgl.wglGetProcAddress("glActiveTextureARB");
            clientActiveTextureARB = Wgl.wglGetProcAddress("glClientActiveTextureARB");

            // ARB_vertex_program/ARB_fragment_program
            bindProgramARBPtr = Wgl.wglGetProcAddress("glBindProgramARB");
            genProgramsARBPtr = Wgl.wglGetProcAddress("glGenProgramsARB");
            deleteProgramsARBPtr = Wgl.wglGetProcAddress("glDeleteProgramsARB");
            programStringARBPtr = Wgl.wglGetProcAddress("glProgramStringARB");
            isProgramARBPtr = Wgl.wglGetProcAddress("glIsProgramARB");
            programLocalParameter4fvARBPtr = Wgl.wglGetProcAddress("glProgramLocalParameter4fvARB");

            // GL_EXT_texture3D
            texImage3DEXTPtr = Wgl.wglGetProcAddress("glTexImage3DEXT");

            // GL_ARB_texture_compression
            compressedTexImage2DARB = Wgl.wglGetProcAddress("glCompressedTexImage2DARB");

            // GL_ATI_fragment_shader
            genFragmentShadersATIptr = Wgl.wglGetProcAddress("glGenFragmentShadersATI");
            bindFragmentShaderATIptr = Wgl.wglGetProcAddress("glBindFragmentShaderATI");
            deleteFragmentShaderATIptr = Wgl.wglGetProcAddress("glDeleteFragmentShaderATI");
            beginFragmentShaderATIptr = Wgl.wglGetProcAddress("glBeginFragmentShaderATI");
            endFragmentShaderATIptr = Wgl.wglGetProcAddress("glEndFragmentShaderATI");
            setFragmentShaderConstantATIptr = Wgl.wglGetProcAddress("glSetFragmentShaderConstantATI");

            // GL_NV_register_combiner
            combinerStageParameterfvNVptr = Wgl.wglGetProcAddress("glCombinerStageParameterfvNV");

            // GL_NV_vertex_program2/GL_NV_fragment_program
            genProgramsNVptr = Wgl.wglGetProcAddress("glGenProgramsNV");
            bindProgramNVptr = Wgl.wglGetProcAddress("glBindProgramNV");
            deleteProgramsNVptr = Wgl.wglGetProcAddress("glDeleteProgramsNV");
            loadProgramNVptr = Wgl.wglGetProcAddress("glLoadProgramNV");
            programNamedParameter4fNVptr = Wgl.wglGetProcAddress("glProgramNamedParameter4fNV");
            programParameter4fvNVptr = Wgl.wglGetProcAddress("glProgramParameter4fvNV");
        }
    }

}
