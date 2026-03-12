using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PocoRender.UI
{
    public enum NativeCursorShape
    {
        Arrow,
        SizeAll,
        SizeNwSe,
        SizeNeSw
    }

    public static class NativeCursorUtility
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static readonly IntPtr IDC_ARROW = new IntPtr(32512);
        private static readonly IntPtr IDC_SIZENWSE = new IntPtr(32642);
        private static readonly IntPtr IDC_SIZENESW = new IntPtr(32643);
        private static readonly IntPtr IDC_SIZEALL = new IntPtr(32646);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr hCursor);
#endif

        public static void Apply(NativeCursorShape shape)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            IntPtr cursorId = IDC_ARROW;
            switch (shape)
            {
                case NativeCursorShape.SizeAll:
                    cursorId = IDC_SIZEALL;
                    break;
                case NativeCursorShape.SizeNwSe:
                    cursorId = IDC_SIZENWSE;
                    break;
                case NativeCursorShape.SizeNeSw:
                    cursorId = IDC_SIZENESW;
                    break;
            }

            IntPtr hCursor = LoadCursor(IntPtr.Zero, cursorId);
            if (hCursor != IntPtr.Zero)
            {
                SetCursor(hCursor);
            }
#endif
        }

        public static void Reset()
        {
            Apply(NativeCursorShape.Arrow);
        }
    }
}
