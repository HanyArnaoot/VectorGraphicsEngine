using Arnaoot.Core;
using Arnaoot.VectorGraphics.Abstractions;
using Arnaoot.VectorGraphics.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Arnaoot.VectorGraphics.Abstractions.Abstractions;

namespace Arnaoot.VectorGraphics.Platform.Skia
{

     
  
    public sealed class SkiaRenderTarget : IRenderTarget, IDisposable
    {
        private SKCanvas _canvas;
        private SKSurface _ownedSurface; // Only if we create it from System.Drawing.Bitmap
        private object _targetGdiBitmap; // Target GDI+ bitmap to copy back to (stored as object to avoid type loading)

        // Internal platform-specific caching
        private readonly Dictionary<string, SKPaint> _paintCache = new Dictionary<string, SKPaint>();
        private readonly Dictionary<(string, float), SKTypeface> _typefaceCache = new Dictionary<(string, float), SKTypeface>();

        public SkiaRenderTarget()
        {
        }

        public void BeginFrame(object surface)
        {
            // Clean up previous surface if we owned it
            _ownedSurface?.Dispose();
            _ownedSurface = null;
            _canvas = null;
            _targetGdiBitmap = null;

            if (surface is SKCanvas canvas)
            {
                _canvas = canvas;
            }
            else if (surface is SKSurface skSurface)
            {
                _canvas = skSurface.Canvas;
            }
            else if (surface is SKBitmap skBitmap)
            {
                _ownedSurface = SKSurface.Create(new SKImageInfo(skBitmap.Width, skBitmap.Height, skBitmap.ColorType, skBitmap.AlphaType));
                _canvas = _ownedSurface.Canvas;
            }
            else
            {
                // Check if it's System.Drawing.Bitmap using reflection to avoid hard dependency
                var surfaceType = surface.GetType();
                if (surfaceType.FullName == "System.Drawing.Bitmap")
                {
                    try
                    {
                        // Use reflection to get Width and Height properties
                        var widthProp = surfaceType.GetProperty("Width");
                        var heightProp = surfaceType.GetProperty("Height");

                        if (widthProp != null && heightProp != null)
                        {
                            int width = (int)widthProp.GetValue(surface);
                            int height = (int)heightProp.GetValue(surface);

                            // Create SkiaSharp surface for rendering
                            var imageInfo = new SKImageInfo(
                                width,
                                height,
                                SKColorType.Bgra8888,
                                SKAlphaType.Premul);

                            _ownedSurface = SKSurface.Create(imageInfo);
                            _canvas = _ownedSurface.Canvas;
                            _targetGdiBitmap = surface as dynamic;
                        }
                    }
                    catch
                    {
                        throw new ArgumentException($"Failed to extract dimensions from System.Drawing.Bitmap");
                    }
                }
                else
                {
                    throw new ArgumentException($"SkiaRenderTarget expects SKCanvas, SKSurface, SKBitmap, or System.Drawing.Bitmap, got {surface?.GetType()}");
                }
            }

            // Enable anti-aliasing by default
            _canvas.Clear(SKColors.Transparent);
        }

        public void EndFrame()
        {
            _canvas?.Flush();

            // Copy back to System.Drawing.Bitmap if needed
            if (_targetGdiBitmap != null && _ownedSurface != null)
            {
                CopySkiaSurfaceToGdiBitmap(_ownedSurface, _targetGdiBitmap);
            }

            _canvas = null;
            _targetGdiBitmap = null;
        }

        private void CopySkiaSurfaceToGdiBitmap(SKSurface skSurface, object gdiBitmap)
        {
            // This method only works when System.Drawing is available
            // Use reflection to avoid hard dependency
            try
            {
                var bitmapType = gdiBitmap.GetType();
                var widthProp = bitmapType.GetProperty("Width");
                var heightProp = bitmapType.GetProperty("Height");

                if (widthProp == null || heightProp == null)
                    return;

                int width = (int)widthProp.GetValue(gdiBitmap);
                int height = (int)heightProp.GetValue(gdiBitmap);

                // Get pixel data from SkiaSharp surface
                using (var image = skSurface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = new System.IO.MemoryStream())
                {
                    data.SaveTo(stream);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);

                    // Load into temporary GDI+ bitmap using reflection
                    var bitmapCtor = bitmapType.GetConstructor(new[] { typeof(System.IO.Stream) });
                    if (bitmapCtor != null)
                    {
                        var tempBitmap = bitmapCtor.Invoke(new object[] { stream });

                        // Get Graphics.FromImage method
                        var graphicsType = bitmapType.Assembly.GetType("System.Drawing.Graphics");
                        var fromImageMethod = graphicsType?.GetMethod("FromImage", new[] { bitmapType });

                        if (fromImageMethod != null)
                        {
                            var graphics = fromImageMethod.Invoke(null, new[] { gdiBitmap });
                            var drawImageMethod = graphicsType.GetMethod("DrawImage", new[] { bitmapType, typeof(int), typeof(int), typeof(int), typeof(int) });

                            if (drawImageMethod != null)
                            {
                                drawImageMethod.Invoke(graphics, new object[] { tempBitmap, 0, 0, width, height });
                            }

                            // Dispose graphics
                            ((IDisposable)graphics).Dispose();
                        }

                        // Dispose temp bitmap
                        ((IDisposable)tempBitmap).Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to copy to GDI bitmap: {ex.Message}");
            }
        }

        public void DrawImage(object image, float x, float y, float width, float height)
        {
            SKImage skImage = null;
            SKBitmap skBitmap = null;

            try
            {
                if (image is SKImage img)
                {
                    skImage = img;
                }
                else if (image is SKBitmap bmp)
                {
                    skBitmap = bmp;
                    skImage = SKImage.FromBitmap(bmp);
                }
                else
                {
                    throw new ArgumentException($"SkiaRenderTarget expects SKImage or SKBitmap, got {image?.GetType()}");
                }

                var destRect = SKRect.Create(x, y, width, height);
                _canvas.DrawImage(skImage, destRect);
            }
            finally
            {
                // Only dispose if we created it from bitmap
                if (image is SKBitmap && skImage != null)
                {
                    skImage.Dispose();
                }
            }
        }

        // Cleanup caches
        public void Dispose()
        {
            foreach (var paint in _paintCache.Values)
                paint.Dispose();
            _paintCache.Clear();

            foreach (var typeface in _typefaceCache.Values)
                typeface?.Dispose();
            _typefaceCache.Clear();

            _ownedSurface?.Dispose();
            _ownedSurface = null;
        }

        #region Internal Cache Methods
        private SKPaint GetCachedPaint(ArgbColor color, float width, bool isStroke, bool isSelected = false, SKPaintStyle? forceStyle = null)
        {
            string key = $"{color.GetHashCode()}-{width}-{isStroke}-{isSelected}-{forceStyle}";
            if (!_paintCache.TryGetValue(key, out SKPaint paint))
            {
                paint = new SKPaint
                {
                    Color = new SKColor(color.R, color.G, color.B, color.A),
                    IsAntialias = true,
                    Style = forceStyle ?? (isStroke ? SKPaintStyle.Stroke : SKPaintStyle.Fill),
                    StrokeWidth = width
                };

                if (isSelected && isStroke)
                {
                    paint.StrokeWidth = width + 1;
                    paint.PathEffect = SKPathEffect.CreateDash(new float[] { 10, 5 }, 0);
                }

                _paintCache[key] = paint;
            }
            return paint;
        }

        private SKTypeface GetCachedTypeface(string family)
        {
            var key = (family, 0f); // Size doesn't matter for typeface
            if (!_typefaceCache.TryGetValue(key, out SKTypeface typeface))
            {
                typeface = SKTypeface.FromFamilyName(family, SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
                _typefaceCache[key] = typeface;
            }
            return typeface;
        }
        #endregion

        #region IRenderTarget Implementation
        public void Clear(ArgbColor color)
        {
            _canvas.Clear(new SKColor(color.R, color.G, color.B, color.A));
        }

        public void DrawLine(Vector2D p1, Vector2D p2, ArgbColor color, float width, bool isSelected = false)
        {
            var paint = GetCachedPaint(color, width, true, isSelected);
            _canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, paint);
        }

        public void DrawEllipse(Vector2D center, float radiusX, float radiusY, float angleRad,
                                ArgbColor stroke, float strokeWidth, ArgbColor? fill = null)
        {
            if (radiusX < 1 || radiusY < 1)
                return;

            _canvas.Save();
            try
            {
                _canvas.Translate(center.X, center.Y);
                _canvas.RotateRadians(angleRad);

                var rect = SKRect.Create(-radiusX, -radiusY, radiusX * 2, radiusY * 2);

                if (fill.HasValue)
                {
                    var fillPaint = GetCachedPaint(fill.Value, 0, false);
                    _canvas.DrawOval(rect, fillPaint);
                }

                var strokePaint = GetCachedPaint(stroke, strokeWidth, true);
                _canvas.DrawOval(rect, strokePaint);
            }
            finally
            {
                _canvas.Restore();
            }
        }

        public void DrawRectangle(Rect2 rect, ArgbColor stroke, float strokeWidth, ArgbColor? fill = null)
        {
            var skRect = SKRect.Create(rect.X, rect.Y, rect.Width, rect.Height);

            if (fill.HasValue)
            {
                var fillPaint = GetCachedPaint(fill.Value, 0, false);
                _canvas.DrawRect(skRect, fillPaint);
            }

            var strokePaint = GetCachedPaint(stroke, strokeWidth, true);
            _canvas.DrawRect(skRect, strokePaint);
        }

        public void DrawPolygon(ReadOnlySpan<Vector2D> points, ArgbColor stroke, float strokeWidth, ArgbColor? fill = null)
        {
            if (points.Length < 3) return;

            using (var path = new SKPath())
            {
                path.MoveTo(points[0].X, points[0].Y);
                for (int i = 1; i < points.Length; i++)
                {
                    path.LineTo(points[i].X, points[i].Y);
                }
                path.Close();

                if (fill.HasValue)
                {
                    var fillPaint = GetCachedPaint(fill.Value, 0, false);
                    _canvas.DrawPath(path, fillPaint);
                }

                var strokePaint = GetCachedPaint(stroke, strokeWidth, true);
                _canvas.DrawPath(path, strokePaint);
            }
        }

        public void DrawString(string text, Vector2D position, ArgbColor color, string fontFamily = "Arial", float size = 12)
        {
            var typeface = GetCachedTypeface(fontFamily);
            using (var paint = new SKPaint())
            {
                paint.Color = new SKColor(color.R, color.G, color.B, color.A);
                paint.IsAntialias = true;
                paint.TextSize = size;
                paint.Typeface = typeface;

                _canvas.DrawText(text, position.X, position.Y + size, paint); // Adjust Y for baseline
            }
        }

        public void DrawPath(Path2D path, ArgbColor stroke, float strokeWidth, ArgbColor? fill = null)
        {
            if (path == null || path.SegmentCount == 0)
                return;

            var figures = path.GetFigures();
            var segments = path.GetSegments();

            if (figures.Count == 0)
                return;

            var strokePaint = GetCachedPaint(stroke, strokeWidth, true);

            // Render each figure separately
            foreach (var figure in figures)
            {
                using (var skPath = new SKPath())
                {
                    // Start at figure's start point
                    skPath.MoveTo(figure.StartPoint.X, figure.StartPoint.Y);

                    // Add all segments for this figure
                    for (int i = 0; i < figure.SegmentCount; i++)
                    {
                        var segment = segments[figure.SegmentStartIndex + i];
                        switch (segment.Type)
                        {
                            case PathSegmentType.LineTo:
                                skPath.LineTo(segment.Point.X, segment.Point.Y);
                                break;

                            // Future support for curves:
                            // case PathSegmentType.QuadraticBezier:
                            //     skPath.QuadTo(segment.ControlPoint1.X, segment.ControlPoint1.Y, 
                            //                   segment.Point.X, segment.Point.Y);
                            //     break;
                            // case PathSegmentType.CubicBezier:
                            //     skPath.CubicTo(segment.ControlPoint1.X, segment.ControlPoint1.Y,
                            //                    segment.ControlPoint2.X, segment.ControlPoint2.Y,
                            //                    segment.Point.X, segment.Point.Y);
                            //     break;
                            // case PathSegmentType.Arc:
                            //     // Handle arc segments
                            //     break;

                            default:
                                throw new NotSupportedException($"Path segment type {segment.Type} is not yet supported");
                        }
                    }

                    // Close path if needed
                    if (figure.IsClosed)
                    {
                        skPath.Close();
                    }

                    // Fill if closed and fill color provided
                    if (figure.IsClosed && fill.HasValue)
                    {
                        var fillPaint = GetCachedPaint(fill.Value, 0, false);
                        _canvas.DrawPath(skPath, fillPaint);
                    }

                    // Draw outline
                    _canvas.DrawPath(skPath, strokePaint);
                }
            }
        }
        #endregion
    }

}
