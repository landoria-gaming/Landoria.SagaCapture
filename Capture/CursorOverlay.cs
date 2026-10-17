using System;
using UnityEngine;
using UnityEngine.UI;

namespace Landoria.SagaCapture
{
    // Draws Valheim's loaded cursor texture into recorded gameplay frames.
    internal sealed class SagaCaptureCursorOverlay : IDisposable
    {
        private RawImage _cursorImage;
        private readonly Vector3[] _corners = new Vector3[4];

        // Positions the game's cursor image at its display-space mouse position.
        internal void Draw(RenderTexture output)
        {
            if (_cursorImage == null)
            {
                _cursorImage = FindCursorImage();
            }
            if (_cursorImage == null)
            {
                return;
            }

            Vector3 mouse = Input.mousePosition;
            float scaleX = (float)output.width / Screen.width;
            float scaleY = (float)output.height / Screen.height;
            _cursorImage.rectTransform.GetWorldCorners(_corners);
            float width = Mathf.Abs(_corners[2].x - _corners[0].x);
            float height = Mathf.Abs(_corners[2].y - _corners[0].y);
            var bounds = new Rect(mouse.x * scaleX,
                (Screen.height - mouse.y) * scaleY,
                width * scaleX, height * scaleY);
            RenderTexture previous = RenderTexture.active;
            try
            {
                RenderTexture.active = output;
                GL.PushMatrix();
                GL.LoadPixelMatrix(0, output.width, output.height, 0);
                Graphics.DrawTexture(bounds, _cursorImage.texture);
            }
            finally
            {
                GL.PopMatrix();
                RenderTexture.active = previous;
            }
        }

        // Finds the cursor RawImage already present in Valheim's UI prefabs.
        private static RawImage FindCursorImage()
        {
            foreach (RawImage image in Resources.FindObjectsOfTypeAll<RawImage>())
            {
                if (image.gameObject.scene.IsValid() &&
                    image.name == "Cursor" && image.texture != null &&
                    image.texture.name == "cursor")
                {
                    SagaCapturePlugin.Log.LogInfo(
                        "Using Valheim's displayed cursor size in recordings.");
                    return image;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _cursorImage = null;
        }
    }
}
