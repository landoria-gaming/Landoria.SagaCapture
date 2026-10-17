using System;
using UnityEngine;
using UnityEngine.UI;

namespace Landoria.SagaCapture
{
    // Draws Valheim's loaded cursor texture into recorded gameplay frames.
    internal sealed class SagaCaptureCursorOverlay : IDisposable
    {
        private Texture _texture;

        // Positions the game's cursor image at its display-space mouse position.
        internal void Draw(RenderTexture output)
        {
            if (_texture == null)
            {
                _texture = FindCursorTexture();
            }
            if (_texture == null)
            {
                return;
            }

            Vector3 mouse = Input.mousePosition;
            float scaleX = (float)output.width / Screen.width;
            float scaleY = (float)output.height / Screen.height;
            var bounds = new Rect(mouse.x * scaleX,
                (Screen.height - mouse.y) * scaleY,
                _texture.width * scaleX * Preference.CursorScale,
                _texture.height * scaleY * Preference.CursorScale);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = output;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, output.width, output.height, 0);
                Graphics.DrawTexture(bounds, _texture);
            }
            finally
            {
                GL.PopMatrix();
                RenderTexture.active = previous;
            }
        }

        // Finds the cursor RawImage already present in Valheim's UI prefabs.
        private static Texture FindCursorTexture()
        {
            foreach (RawImage image in Resources.FindObjectsOfTypeAll<RawImage>())
            {
                if (image.name == "Cursor" && image.texture != null &&
                    image.texture.name == "cursor")
                {
                    SagaCapturePlugin.Log.LogInfo(
                        "Using Valheim's cursor texture in gameplay recordings.");
                    return image.texture;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _texture = null;
        }
    }
}
