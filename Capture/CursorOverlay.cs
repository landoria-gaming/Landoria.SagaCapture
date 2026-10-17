using System;
using UnityEngine;

namespace Landoria.SagaCapture
{
    // Draws a visible pointer into the recorded frame after screen capture.
    internal sealed class SagaCaptureCursorOverlay : IDisposable
    {
        private const int Width = 20;
        private const int Height = 27;
        private readonly Texture2D _texture;

        internal SagaCaptureCursorOverlay()
        {
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    _texture.SetPixel(x, Height - y - 1, Pixel(x, y));
                }
            }
            _texture.Apply(false, true);
        }

        // Positions the pointer using the game's display-space mouse coordinates.
        internal void Draw(RenderTexture output)
        {
            Vector3 mouse = Input.mousePosition;
            float x = mouse.x * output.width / Screen.width;
            float y = (Screen.height - mouse.y) * output.height / Screen.height;
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = output;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, output.width, output.height, 0);
                Graphics.DrawTexture(new Rect(x, y, Width, Height), _texture);
            }
            finally
            {
                GL.PopMatrix();
                RenderTexture.active = previous;
            }
        }

        // Generates a white arrow with a dark outline and transparent surroundings.
        private static Color Pixel(int x, int y)
        {
            bool triangle = y < 19 && x <= y / 2;
            bool stem = y >= 15 && y < 26 && x >= 6 && x <= 9;
            if (!triangle && !stem)
            {
                return Color.clear;
            }
            bool edge = x == 0 || x == y / 2 || y == 18 ||
                stem && (x == 6 || x == 9 || y == 25);
            return edge ? Color.black : Color.white;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_texture);
        }
    }
}
