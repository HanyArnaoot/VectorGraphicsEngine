using Arnaoot.Core;
using Arnaoot.VectorGraphics.Abstractions;
using Arnaoot.VectorGraphics.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Arnaoot.VectorGraphics.Abstractions.Abstractions;

namespace Arnaoot.VectorGraphics.Platform.WinForms
{
    public sealed class WinFormsRenderTarget : IRenderTarget
    {
        private Graphics _g;

        // Internal platform-specific caching
        private readonly Dictionary<string, Pen> _penCache = new Dictionary<string, Pen>();
        private readonly Dictionary<ArgbColor, SolidBrush> _brushCache = new Dictionary<ArgbColor, SolidBrush>();
        private readonly Dictionary<(string, float), Font> _fontCache = new Dictionary<(string, float), Font>();

        public WinFormsRenderTarget()
        {
        }
        public void BeginFrame(object surface)
        {
            if (surface is Bitmap bitmap)
            {
                _g?.Dispose();
                _g = Graphics.FromImage(bitmap);
                _g.SmoothingMode = SmoothingMode.AntiAlias;
                _g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            }
        }

        public void EndFrame()
        {
            _g?.Dispose();
            _g = null;
        }
        public void DrawImage(object image, float x, float y, float width, float height)
        {
            if (image is Image gdiImage)
            {
                _g.DrawImage(gdiImage, x, y, width, height);
            }
            else
            {
                throw new ArgumentException($"WinFormsRenderTarget expects System.Drawing.Image, got {image?.GetType()}");
            }
        }
        // Cleanup caches
        public void Dispose()
        {
            foreach (var pen in _penCache.Values)
                pen.Dispose();
            _penCache.Clear();

            foreach (var brush in _brushCache.Values)
                brush.Dispose();
            _brushCache.Clear();

            foreach (var font in _fontCache.Values)
                font.Dispose();
            _fontCache.Clear();
        }

        #region Internal Cache Methods
        private Pen GetCachedPen(ArgbColor color, int width, bool isSelected)
        {
            string key = $"{color.GetHashCode()}-{width}-{isSelected}";
            if (!_penCache.TryGetValue(key, out Pen pen))
            {
                pen = new Pen(color, width);
                if (isSelected)
                {
                    pen.Width = width + 1;
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                }
                _penCache[key] = pen;
            }
            return (Pen)pen.Clone();
        }

        private SolidBrush GetCachedBrush(ArgbColor color)
        {
            if (!_brushCache.TryGetValue(color, out SolidBrush brush))
            {
                brush = new SolidBrush(color);
                _brushCache[color] = brush;
            }
            return brush;
        }

        private Font GetCachedFont(string family, float size)
        {
            var key = (family, size);
            if (!_fontCache.TryGetValue(key, out Font font))
            {
                font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel);
                _fontCache[key] = font;
            }
            return font;
        }
        #endregion

        #region IRenderTarget Implementation
        public void Clear(ArgbColor color) => _g.Clear(color);

        public void DrawLine(Vector2D p1, Vector2D p2, ArgbColor color, float width, bool isSelected)
        {
            var pen = GetCachedPen(color, (int)width, isSelected);
            _g.DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);
        }

        public void DrawEllipse(Vector2D center, float radiusX, float radiusY, float angleRad,
                                ArgbColor stroke, float strokeWidth, ArgbColor? fill)
        {
            var state = _g.Save();
            try
            {
                if (radiusX < 1 || radiusY < 1)
                    return;

                _g.TranslateTransform(center.X, center.Y);
                _g.RotateTransform((float)(angleRad * 180.0 / Math.PI));
                var rect = new RectangleF(-radiusX, -radiusY, radiusX * 2, radiusY * 2);

                if (fill.HasValue)
                    _g.FillEllipse(GetCachedBrush(fill.Value), rect);

                var pen = GetCachedPen(stroke, (int)strokeWidth, false);
                _g.DrawEllipse(pen, rect);
            }
            finally { _g.Restore(state); }
        }

        public void DrawRectangle(Rect2 rect, ArgbColor stroke, float strokeWidth, ArgbColor? fill)
        {
            if (fill.HasValue)
                _g.FillRectangle(GetCachedBrush(fill.Value), rect.X, rect.Y, rect.Width, rect.Height);

            var pen = GetCachedPen(stroke, (int)strokeWidth, false);
            _g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawPolygon(ReadOnlySpan<Vector2D> points, ArgbColor stroke, float strokeWidth, ArgbColor? fill)
        {
            if (points.Length < 3) return;

            var rented = ArrayPool<PointF>.Shared.Rent(points.Length);
            try
            {
                for (int i = 0; i < points.Length; i++)
                    rented[i] = new PointF(points[i].X, points[i].Y);

                var exact = new PointF[points.Length];
                Array.Copy(rented, exact, points.Length);

                if (fill.HasValue)
                    _g.FillPolygon(GetCachedBrush(fill.Value), exact);

                var pen = GetCachedPen(stroke, (int)strokeWidth, false);
                _g.DrawPolygon(pen, exact);
            }
            finally
            {
                ArrayPool<PointF>.Shared.Return(rented);
            }
        }

        public void DrawString(string text, Vector2D position, ArgbColor color, string fontFamily, float size)
        {
            var font = GetCachedFont(fontFamily, size);
            _g.DrawString(text, font, GetCachedBrush(color), position.X, position.Y);
        }


        public void DrawPath(Path2D path, ArgbColor stroke, float strokeWidth, ArgbColor? fill)
        {
            if (path == null || path.SegmentCount == 0)
                return;

            var figures = path.GetFigures();
            var segments = path.GetSegments();

            if (figures.Count == 0)
                return;

            var pen = GetCachedPen(stroke, (int)strokeWidth, false);

            // Render each figure separately
            foreach (var figure in figures)
            {
                int pointCount = figure.SegmentCount + 1; // +1 for start point
                var rented = ArrayPool<PointF>.Shared.Rent(pointCount);
                try
                {
                    // First point is the figure's start point
                    rented[0] = new PointF(figure.StartPoint.X, figure.StartPoint.Y);

                    // Add all segment points for this figure
                    int index = 1;
                    for (int i = 0; i < figure.SegmentCount; i++)
                    {
                        var segment = segments[figure.SegmentStartIndex + i];
                        switch (segment.Type)
                        {
                            case PathSegmentType.LineTo:
                                rented[index++] = new PointF(segment.Point.X, segment.Point.Y);
                                break;
                            default:
                                throw new NotSupportedException($"Path segment type {segment.Type} is not yet supported");
                        }
                    }

                    // Create exact-size array for GDI+
                    var exact = new PointF[pointCount];
                    Array.Copy(rented, exact, pointCount);

                    // Fill if closed and fill color provided
                    if (figure.IsClosed && fill.HasValue)
                        _g.FillPolygon(GetCachedBrush(fill.Value), exact);

                    // Draw outline
                    if (figure.IsClosed)
                        _g.DrawPolygon(pen, exact);
                    else
                        _g.DrawLines(pen, exact);
                }
                finally
                {
                    ArrayPool<PointF>.Shared.Return(rented);
                }
            }
        }

        #endregion
    }

}
