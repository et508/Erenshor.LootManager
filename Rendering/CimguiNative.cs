using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LootManager
{
    public static class CimguiNative
    {
        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetForegroundDrawList_Nil();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetBackgroundDrawList_Nil();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr igGetWindowDrawList();

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImDrawList_AddLine(IntPtr self, Vec2 p1, Vec2 p2, uint col, float thickness);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ImDrawList_AddRectFilled(IntPtr self, Vec2 pMin, Vec2 pMax, uint col, float rounding, int flags);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        public unsafe static extern void ImDrawList_AddText_Vec2(IntPtr self, Vec2 pos, uint col, byte* textBegin, byte* textEnd);

        [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl)]
        private unsafe static extern void igCalcTextSize(Vec2* pOut, byte* text, byte* textEnd, byte wrapWidth, float maxWidth);

        private static int WriteUtf8(string text)
        {
            int byteCount = Encoding.UTF8.GetByteCount(text);
            if (byteCount > _utf8Buf.Length)
                _utf8Buf = new byte[byteCount * 2];
            return Encoding.UTF8.GetBytes(text, 0, text.Length, _utf8Buf, 0);
        }

        public unsafe static Vec2 CalcTextSize(string text)
        {
            if (string.IsNullOrEmpty(text)) return new Vec2(0f, 0f);
            int num = WriteUtf8(text);
            Vec2 vec;
            fixed (byte* ptr = _utf8Buf)
            {
                byte* p = (_utf8Buf == null || _utf8Buf.Length == 0) ? null : ptr;
                igCalcTextSize(&vec, p, p + num, 0, -1f);
            }
            return vec;
        }

        public unsafe static void AddText(IntPtr drawList, float x, float y, uint color, string text)
        {
            if (drawList == IntPtr.Zero || string.IsNullOrEmpty(text)) return;
            int num = WriteUtf8(text);
            fixed (byte* ptr = _utf8Buf)
            {
                byte* p = (_utf8Buf == null || _utf8Buf.Length == 0) ? null : ptr;
                ImDrawList_AddText_Vec2(drawList, new Vec2(x, y), color, p, p + num);
            }
        }

        private static byte[] _utf8Buf = new byte[256];

        public struct Vec2
        {
            public Vec2(float x, float y) { X = x; Y = y; }
            public float X;
            public float Y;
        }

        public struct Vec4
        {
            public Vec4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
            public float X;
            public float Y;
            public float Z;
            public float W;
        }
    }
}