using Arnaoot.Core;
using Arnaoot.VectorGraphics.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arnaoot.VectorGraphics.Abstractions
{
    public partial class Abstractions
    {
        // ========================================
        // IRenderTarget Interface (Cross-Platform)
        // ========================================
        public interface IRenderTarget
        {
            void Clear(ArgbColor color);
            void DrawLine(Vector2D p1, Vector2D p2, ArgbColor color, float width, bool isSelected = false);
            void DrawEllipse(Vector2D center, float radiusX, float radiusY, float angleRad,
                             ArgbColor stroke, float strokeWidth, ArgbColor? fill = null);
            void DrawRectangle(Rect2 rect, ArgbColor stroke, float strokeWidth, ArgbColor? fill = null);
            void DrawPolygon(ReadOnlySpan<Vector2D> points, ArgbColor stroke, float strokeWidth, ArgbColor? fill = null);
            void DrawString(string text, Vector2D position, ArgbColor color, string fontFamily = "Arial", float size = 12);
            void DrawPath(Path2D path, ArgbColor stroke, float strokeWidth, ArgbColor? fill = null);
            void BeginFrame(object surface); // surface = Bitmap for WinForms, SKSurface for Skia
            void EndFrame();
            void DrawImage(object image, float x, float y, float width, float height);
        }

    }
}
