using Arnaoot.Core;
using Arnaoot.VectorGraphics.Abstractions;
using Arnaoot.VectorGraphics.Core;
using Arnaoot.VectorGraphics.Scene;
using Arnaoot.VectorGraphics.View;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using static Arnaoot.VectorGraphics.Abstractions.Abstractions;


namespace Arnaoot.VectorGraphics.Rendering
{

    //Contains methods For drawing shapes And the main painting logic.
    public interface IRenderManager : IDisposable
    {
        // Flags
        bool ShowGrid { get; set; }
        bool ShowAxes { get; set; }
        bool ShowScaleBar { get; set; }
        float GridSpacingReal { get; set; }

        object BackgroundImage { get; set; }

        // Main rasterization function
        void RasterizeIntoBuffer(
            object buffer,
            IViewSettings view,
            IReadOnlyCollection<IDrawElement> drawElements,
            Layer tempLayer,
            ArgbColor backColor);

        // Visibility check
        bool IsBoundsVisible(
            BoundingBox3D worldBounds,
            IViewSettings view);

        // Overlays and helpers

        void DrawScaleBar(
            IViewSettings view,
            IRenderTarget renderTarget,
            int scaleBarLengthPixels);

        // Export
        void SaveRegionAsImage(
            string filePath,
            IReadOnlyCollection<IDrawElement> drawElements,
            IViewSettings view,
            BoundingBox3D region,
            int pixelWidth,
            int pixelHeight,
            ImageFormat format = null,
            bool includeBackground = true,
            float padding = 0.05f);
    }

         public class RenderManager : IRenderManager, IDisposable
        {
            private readonly IRenderTarget _renderTarget;

            // Configuration
            public bool ShowGrid { get; set; }
            public bool ShowAxes { get; set; }
            public bool ShowScaleBar { get; set; }
            public int ScaleBarLengthPixels { get; set; }
            public float GridSpacingReal { get; set; }

            // Background image (platform-agnostic - stored as object)
            public object BackgroundImage { get; set; }

            public RenderManager(IRenderTarget renderTarget)
            {
                // Default values
                ScaleBarLengthPixels = 100;
                GridSpacingReal = 50;
                ShowGrid = false;
                ShowAxes = true;
                ShowScaleBar = true;

                _renderTarget = renderTarget ?? throw new ArgumentNullException(nameof(renderTarget));
            }
            public void Dispose()
            {
                // RenderManager doesn't own the render target
            }

            /// <summary>
            /// Render scene into the provided surface.
            /// Surface can be: System.Drawing.Bitmap, SKSurface, SKBitmap, or SKCanvas.
            /// </summary>
            public void RasterizeIntoBuffer(
                object surface,  // ✅ Platform-agnostic
                IViewSettings view,
                IReadOnlyCollection<IDrawElement> drawElements,
                Layer tempLayer,
                ArgbColor backColor)
            {
                if (surface == null) throw new ArgumentNullException(nameof(surface));
                if (view == null) throw new ArgumentNullException(nameof(view));

                _renderTarget.BeginFrame(surface);

                // Clear with background
                _renderTarget.Clear(BackgroundImage != null ? ArgbColor.White : backColor);

                // Draw background image if present
                if (BackgroundImage != null)
                {
                    // Get surface dimensions (platform-agnostic way)
                    var (width, height) = GetSurfaceDimensions(surface);
                    _renderTarget.DrawImage(BackgroundImage, 0, 0, width, height);
                }

                // Render scene
                Render(drawElements, _renderTarget, view);

                // Render temp layer
                if (tempLayer != null)
                {
                    foreach (var el in tempLayer.GetLayerElements())
                        el.EmitCommands(_renderTarget, view);
                }

                // Render overlays
                if (ShowScaleBar)
                    DrawScaleBar(view, _renderTarget, ScaleBarLengthPixels);

                if (ShowGrid)
                    DrawGrid(view, _renderTarget, GridSpacingReal);

                if (ShowAxes)
                    DrawAxes(view, _renderTarget);

                _renderTarget.EndFrame();
            }

            /// <summary>
            /// Get surface dimensions in a platform-agnostic way.
            /// </summary>
            private (float width, float height) GetSurfaceDimensions(object surface)
            {
                // Use reflection to get dimensions from any surface type
                var type = surface.GetType();

                // Try Width/Height properties
                var widthProp = type.GetProperty("Width");
                var heightProp = type.GetProperty("Height");

                if (widthProp != null && heightProp != null)
                {
                    var width = Convert.ToSingle(widthProp.GetValue(surface));
                    var height = Convert.ToSingle(heightProp.GetValue(surface));
                    return (width, height);
                }

                // Fallback to viewport if available
                return (800, 600);
            }

            private void Render(
                IEnumerable<IDrawElement> elements,
                IRenderTarget target,
                IViewSettings view)
            {
                foreach (var el in elements)
                {
                    // Frustum culling
                    if (!IsVisible(el.GetBounds(), view))
                        continue;

                    el.EmitCommands(target, view);
                }
            }
        public bool IsBoundsVisible(BoundingBox3D worldBounds, IViewSettings view)
        {
            return IsVisible( worldBounds,  view);
        }
            private static bool IsVisible(BoundingBox3D worldBounds, IViewSettings view)
            {
                if (worldBounds.IsEmpty())
                    return false;

                var min = view.RealToPict(worldBounds.Min, out _);
                var max = view.RealToPict(worldBounds.Max, out _);

                if (!min.IsValid || !max.IsValid)
                    return false;

                float left = Math.Min(min.X, max.X);
                float top = Math.Min(min.Y, max.Y);
                float right = Math.Max(min.X, max.X);
                float bottom = Math.Max(min.Y, max.Y);

                var screenAABB = new Rect2(left, top, right - left, bottom - top);
                return screenAABB.IntersectsWith(view.UsableViewport);
            }

            #region grid , scale bar , axis draw
            #region Grid Path Generation
            private Path2D BuildGridPath(IViewSettings currentViewSettings, float GridSpacingDistanceReal)
            {
                Path2D gridPath = new Path2D(500);

                float gridSpacingDistancePixel = currentViewSettings.DIST_Real_to_Pict(GridSpacingDistanceReal);

                if (gridSpacingDistancePixel < 0.1f)
                    return gridPath;

                int gridExtent = 20;
                float maxGridDistance = gridExtent * GridSpacingDistanceReal;

                // XY Plane (Z = 0)
                for (int i = 0; i <= gridExtent; i++)
                {
                    float coord = i * GridSpacingDistanceReal;

                    // Lines parallel to X-axis
                    Vector2D p1 = currentViewSettings.RealToPict(
                          new Vector3D(0, coord, 0f), out _);
                    Vector2D p2 = currentViewSettings.RealToPict(
                          new Vector3D(maxGridDistance, coord, 0f), out _);

                    if (!float.IsNaN(p1.X) && !float.IsNaN(p1.Y) && !float.IsNaN(p2.X) && !float.IsNaN(p2.Y))
                    {
                        p1 = currentViewSettings.ClampToGdiRangePoint(p1);
                        p2 = currentViewSettings.ClampToGdiRangePoint(p2);
                        gridPath.MoveTo(p1);
                        gridPath.LineTo(p2);
                    }

                    // Lines parallel to Y-axis
                    Vector2D p3 = currentViewSettings.RealToPict(
                          new Vector3D(coord, 0, 0f), out _);
                    Vector2D p4 = currentViewSettings.RealToPict(
                          new Vector3D(coord, maxGridDistance, 0f), out _);

                    if (!float.IsNaN(p3.X) && !float.IsNaN(p3.Y) && !float.IsNaN(p4.X) && !float.IsNaN(p4.Y))
                    {
                        p3 = currentViewSettings.ClampToGdiRangePoint(p3);
                        p4 = currentViewSettings.ClampToGdiRangePoint(p4);
                        gridPath.MoveTo(p3);
                        gridPath.LineTo(p4);
                    }
                }

                // XZ Plane (Y = 0)
                for (int i = 0; i <= gridExtent; i++)
                {
                    float coord = i * GridSpacingDistanceReal;

                    Vector2D p1 = currentViewSettings.RealToPict(
                          new Vector3D(0, 0f, coord), out _);
                    Vector2D p2 = currentViewSettings.RealToPict(
                          new Vector3D(maxGridDistance, 0f, coord), out _);

                    if (!float.IsNaN(p1.X) && !float.IsNaN(p1.Y) && !float.IsNaN(p2.X) && !float.IsNaN(p2.Y))
                    {
                        p1 = currentViewSettings.ClampToGdiRangePoint(p1);
                        p2 = currentViewSettings.ClampToGdiRangePoint(p2);
                        gridPath.MoveTo(p1);
                        gridPath.LineTo(p2);
                    }

                    Vector2D p3 = currentViewSettings.RealToPict(
                          new Vector3D(coord, 0f, 0), out _);
                    Vector2D p4 = currentViewSettings.RealToPict(
                          new Vector3D(coord, 0f, maxGridDistance), out _);

                    if (!float.IsNaN(p3.X) && !float.IsNaN(p3.Y) && !float.IsNaN(p4.X) && !float.IsNaN(p4.Y))
                    {
                        p3 = currentViewSettings.ClampToGdiRangePoint(p3);
                        p4 = currentViewSettings.ClampToGdiRangePoint(p4);
                        gridPath.MoveTo(p3);
                        gridPath.LineTo(p4);
                    }
                }

                // YZ Plane (X = 0)
                for (int i = 0; i <= gridExtent; i++)
                {
                    float coord = i * GridSpacingDistanceReal;

                    Vector2D p1 = currentViewSettings.RealToPict(
                          new Vector3D(0f, 0, coord), out _);
                    Vector2D p2 = currentViewSettings.RealToPict(
                          new Vector3D(0f, maxGridDistance, coord), out _);

                    if (!float.IsNaN(p1.X) && !float.IsNaN(p1.Y) && !float.IsNaN(p2.X) && !float.IsNaN(p2.Y))
                    {
                        p1 = currentViewSettings.ClampToGdiRangePoint(p1);
                        p2 = currentViewSettings.ClampToGdiRangePoint(p2);
                        gridPath.MoveTo(p1);
                        gridPath.LineTo(p2);
                    }

                    Vector2D p3 = currentViewSettings.RealToPict(
                          new Vector3D(0f, coord, 0), out _);
                    Vector2D p4 = currentViewSettings.RealToPict(
                          new Vector3D(0f, coord, maxGridDistance), out _);

                    if (!float.IsNaN(p3.X) && !float.IsNaN(p3.Y) && !float.IsNaN(p4.X) && !float.IsNaN(p4.Y))
                    {
                        p3 = currentViewSettings.ClampToGdiRangePoint(p3);
                        p4 = currentViewSettings.ClampToGdiRangePoint(p4);
                        gridPath.MoveTo(p3);
                        gridPath.LineTo(p4);
                    }
                }

                return gridPath;
            }
            #endregion

            #region Axes Path Generation
            public Path2D BuildAxesPath(IViewSettings currentViewSettings)
            {
                Path2D axesPath = new Path2D(20);

                Vector2D origin = currentViewSettings.RealToPict(
                      new Vector3D(0F, 0F, 0F), out _);
                origin = currentViewSettings.ClampToGdiRangePoint(origin);

                float axisLength = 80.0F / currentViewSettings.ZoomFactorAverage;
                float arrowLength = 20.0F / currentViewSettings.ZoomFactorAverage;
                float arrowWidth = 10.0F / currentViewSettings.ZoomFactorAverage;

                // X-axis
                Vector2D xEnd = currentViewSettings.RealToPict(
                      new Vector3D(axisLength, 0F, 0F), out _);
                xEnd = currentViewSettings.ClampToGdiRangePoint(xEnd);
                axesPath.MoveTo(origin);
                axesPath.LineTo(xEnd);

                Vector2D xArrowTip1 = currentViewSettings.RealToPict(
                      new Vector3D(axisLength - arrowLength, arrowWidth, 0F), out _);
                Vector2D xArrowTip2 = currentViewSettings.RealToPict(
                      new Vector3D(axisLength - arrowLength, -arrowWidth, 0F), out _);
                axesPath.MoveTo(xEnd);
                axesPath.LineTo(xArrowTip1);
                axesPath.MoveTo(xEnd);
                axesPath.LineTo(xArrowTip2);

                // Y-axis
                Vector2D yEnd = currentViewSettings.RealToPict(
                      new Vector3D(0F, axisLength, 0F), out _);
                yEnd = currentViewSettings.ClampToGdiRangePoint(yEnd);
                axesPath.MoveTo(origin);
                axesPath.LineTo(yEnd);

                Vector2D yArrowTip1 = currentViewSettings.RealToPict(
                      new Vector3D(arrowWidth, axisLength - arrowLength, 0F), out _);
                Vector2D yArrowTip2 = currentViewSettings.RealToPict(
                      new Vector3D(-arrowWidth, axisLength - arrowLength, 0F), out _);
                axesPath.MoveTo(yEnd);
                axesPath.LineTo(yArrowTip1);
                axesPath.MoveTo(yEnd);
                axesPath.LineTo(yArrowTip2);

                // Z-axis
                Vector2D zEnd = currentViewSettings.RealToPict(
                      new Vector3D(0F, 0F, axisLength), out _);
                zEnd = currentViewSettings.ClampToGdiRangePoint(zEnd);
                axesPath.MoveTo(origin);
                axesPath.LineTo(zEnd);

                Vector2D zArrowTip1 = currentViewSettings.RealToPict(
                      new Vector3D(arrowWidth, 0F, axisLength - arrowLength), out _);
                Vector2D zArrowTip2 = currentViewSettings.RealToPict(
                      new Vector3D(-arrowWidth, 0F, axisLength - arrowLength), out _);
                axesPath.MoveTo(zEnd);
                axesPath.LineTo(zArrowTip1);
                axesPath.MoveTo(zEnd);
                axesPath.LineTo(zArrowTip2);

                return axesPath;
            }
            #endregion
            #region Scale Bar Draw 

            /// <summary>
            /// Draws a scale bar in the bottom-left corner of the control.
            /// </summary>
            /// <param name="g">The Graphics object to draw on.</param>
            public void DrawScaleBar(IViewSettings CurrentViewSettings, IRenderTarget renderTarget, int _scaleBarLengthPixels)
            {
                const float margin = 10;
                const float barHeight = 5F;
                const float fontSize = 10F;

                int targetPixelLength = _scaleBarLengthPixels;
                float realDistance = targetPixelLength / CurrentViewSettings.ZoomFactorAverage;

                float niceDistance = GetProperDistance(realDistance);
                float pixelLength = CurrentViewSettings.DIST_Real_to_Pict(niceDistance);

                float xStart = margin;
                float xEnd = xStart + pixelLength;
                float yPos = CurrentViewSettings.UsableViewport.Height - margin - barHeight;

                renderTarget.DrawLine(new Vector2D(xStart, yPos), new Vector2D(xEnd, yPos), ArgbColor.Black, 1, false);
                renderTarget.DrawLine(new Vector2D(xStart, yPos - barHeight), new Vector2D(xStart, yPos + barHeight), ArgbColor.Black, 1, false);
                renderTarget.DrawLine(new Vector2D(xEnd, yPos - barHeight), new Vector2D(xEnd, yPos + barHeight), ArgbColor.Black, 1, false);
                //    
                string label = $"{niceDistance:F0} Units";
                renderTarget.DrawString(label, new Vector2D(xStart + margin, yPos - fontSize - 4), ArgbColor.Black, "Arial", fontSize);
            }



        /// <summary>
        /// Adjusts a real-world distance to a "Proper" round number (e.g., 1, 5, 10, 50).
        /// </summary>
        /// <param name="distance">The raw real-world distance.</param>
        /// <returns>A rounded, user-friendly distance.</returns>
        private float GetProperDistance(float distance)
            {
                int magnitude = Convert.ToInt32(Math.Floor(Math.Log10(distance)));
                float normalized = distance / Convert.ToSingle(Math.Pow(10, magnitude));
                float niceValue = 0F;

                if (normalized >= 5F)
                {
                    niceValue = 10F;
                }
                else if (normalized >= 2F)
                {
                    niceValue = 5F;
                }
                else
                {
                    niceValue = 1F;
                }

                return niceValue * Convert.ToSingle(Math.Pow(10, magnitude));
            }
            #endregion
            #region Drawing Helpers
            void DrawGrid(IViewSettings currentViewSettings, IRenderTarget renderTarget, float GridSpacingDistanceReal)
            {
                Path2D gridPath = BuildGridPath(currentViewSettings, GridSpacingDistanceReal);
                renderTarget.DrawPath(gridPath, ArgbColor.LightGray, 1F);
            }

            void DrawAxes(IViewSettings currentViewSettings, IRenderTarget renderTarget)
            {
                Path2D axesPath = BuildAxesPath(currentViewSettings);
                renderTarget.DrawPath(axesPath, ArgbColor.Black, 2F);
            }
            #endregion







            #endregion


            /// <summary>
            /// Saves a specific region of the view as an image file.
            /// </summary>
            /// <param name="filePath">The path to save the image file.</param>
            /// <param name="region">The region in real-world coordinates to save.</param>
            /// <param name="pixelWidth">The width of the output image in pixels.</param>
            /// <param name="pixelHeight">The height of the output image in pixels.</param>
            /// <param name="format">The image format (defaults to PNG if not specified).</param>
            /// <param name="includeBackground">Indicates whether to include the background image.</param>
            /// <param name="Padding">The padding percentage around the region.</param>
            public void SaveRegionAsImage(string filePath, IReadOnlyCollection<IDrawElement> DrawElements, IViewSettings CurrentViewSettings, BoundingBox3D region, int pixelWidth, int pixelHeight, System.Drawing.Imaging.ImageFormat format = null, bool includeBackground = true, float Padding = 0.05F)
            {
                //although it is used only to rasterize the image displayed which is already rasterized and can be save directly 
                //i am using this code to allow the feature of selecting with a rectangle a specific part of the screen and save it
                //if speed is importatnt and no need to get a specific part of the view , just save the buffer image directly
                //
                if (!region.IsValid())
                {
                    throw new ArgumentException("Region dimensions and pixel size must be positive.");
                }
                System.Drawing.Imaging.ImageFormat imageFormat = format ?? System.Drawing.Imaging.ImageFormat.Png;
                //
                IZooming UsedZooming = new Zooming();
                IViewSettings RegionViewSettings = UsedZooming.GetRegionViewSettings(CurrentViewSettings, region, Padding);
                Bitmap bmp = new Bitmap((int)CurrentViewSettings.UsableViewport.Width, (int)CurrentViewSettings.UsableViewport.Height);
                RasterizeIntoBuffer(bmp, CurrentViewSettings, DrawElements, new Layer(), Color.White);
                bmp.Save(filePath, imageFormat);
            }
        }

    }


