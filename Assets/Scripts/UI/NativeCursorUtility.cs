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
        SizeNeSw,
        Rotate
    }

    public static class NativeCursorUtility
    {
        private static Texture2D moveCursorTexture;
        private static Texture2D nwseCursorTexture;
        private static Texture2D neswCursorTexture;
        private static Texture2D rotateCursorTexture;
        private const float CursorScale = 0.7f;

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
            if (TryApplyTextureCursor(shape))
                return;

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

        private static bool TryApplyTextureCursor(NativeCursorShape shape)
        {
            Texture2D texture = null;
            Vector2 hotspot = Vector2.zero;

            switch (shape)
            {
                case NativeCursorShape.SizeAll:
                    if (moveCursorTexture == null)
                        moveCursorTexture = LoadReadableCursorTexture("EditIcons/p_edit_move", CursorScale);
                    texture = moveCursorTexture;
                    break;
                case NativeCursorShape.SizeNwSe:
                    if (nwseCursorTexture == null)
                        nwseCursorTexture = LoadReadableCursorTexture("EditIcons/p_leftTop_righDown", CursorScale);
                    texture = nwseCursorTexture;
                    break;
                case NativeCursorShape.SizeNeSw:
                    if (neswCursorTexture == null)
                        neswCursorTexture = LoadReadableCursorTexture("EditIcons/p_leftdown_righTop", CursorScale);
                    texture = neswCursorTexture;
                    break;
                case NativeCursorShape.Rotate:
                    if (rotateCursorTexture == null)
                        rotateCursorTexture = LoadReadableCursorTexture("EditIcons/p_edit_handline", CursorScale);
                    texture = rotateCursorTexture;
                    break;
            }

            if (texture == null)
                return false;

            hotspot = new Vector2(texture.width * 0.5f, texture.height * 0.5f);
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            return true;
        }

        private static Texture2D LoadReadableCursorTexture(string resourcePath, float scale)
        {
            Texture2D source = Resources.Load<Texture2D>(resourcePath);
            if (source == null)
                return null;

            try
            {
                int canvasWidth = source.width;
                int canvasHeight = source.height;
                int scaledWidth = Mathf.Max(8, Mathf.RoundToInt(source.width * scale));
                int scaledHeight = Mathf.Max(8, Mathf.RoundToInt(source.height * scale));

                // First render the source into a smaller temporary texture.
                RenderTexture rt = RenderTexture.GetTemporary(
                    scaledWidth,
                    scaledHeight,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);

                Graphics.Blit(source, rt);
                RenderTexture prev = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D scaledReadable = new Texture2D(scaledWidth, scaledHeight, TextureFormat.RGBA32, false);
                scaledReadable.ReadPixels(new Rect(0, 0, scaledWidth, scaledHeight), 0, 0);
                scaledReadable.Apply();

                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                // Then place the smaller cursor art into the center of a full-size,
                // transparent canvas. This makes the visible cursor glyph smaller
                // even on platforms that normalize cursor texture bounds.
                Texture2D finalTexture = new Texture2D(canvasWidth, canvasHeight, TextureFormat.RGBA32, false);
                Color[] clearPixels = new Color[canvasWidth * canvasHeight];
                for (int i = 0; i < clearPixels.Length; i++)
                    clearPixels[i] = new Color(0f, 0f, 0f, 0f);
                finalTexture.SetPixels(clearPixels);

                int x = Mathf.Max(0, (canvasWidth - scaledWidth) / 2);
                int y = Mathf.Max(0, (canvasHeight - scaledHeight) / 2);
                finalTexture.SetPixels(x, y, scaledWidth, scaledHeight, scaledReadable.GetPixels());
                finalTexture.Apply();

                return finalTexture;
            }
            catch
            {
                return null;
            }
        }
    }
}
