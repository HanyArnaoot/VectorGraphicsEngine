using Arnaoot.Core;
using Arnaoot.VectorGraphics.Abstractions;
using Arnaoot.VectorGraphics.Commands;
using Arnaoot.VectorGraphics.Elements;
using Arnaoot.VectorGraphics.Scene;
 
using static Arnaoot.VectorGraphics.Abstractions.Abstractions;

namespace VectorDrawEngine
{
    public partial class VectorDrawEngine : UserControl
    {
        #region element draw wrapper
        void AddElement( Layer drawlayer, IDrawElement drawElement, bool ScheduleInvalidateNeed = true)
        {
            if (drawlayer != null && UsedLayerManager.Layers.Any(l => l.Id == drawlayer.Id))
            {
                drawlayer.AddElement(drawElement, ScheduleInvalidateNeed);
                if (ScheduleInvalidateNeed == true)
                {
                    ScheduleInvalidate();
                    _commandManager.ExecuteCommand(new   AddRemoveCommand(drawElement, drawlayer, true));
                }
            }
            else
            {
                System.Diagnostics.Debugger.Break();
                MessageBox.Show("can not draw to " + drawlayer.Name + "\r\n  Layer does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Draws a line on the specified layer.
        /// </summary>
        /// <param name="drawlayer">The layer number to draw on.</param>
        /// <param name="StartPoint">The coordinate of the start point.</param>
        /// <param name="EndPoint">The coordinate of the end point.</param>
        /// <param name="drawColor">The color of the line (defaults to Black).</param>
        /// <param name="relativeCoords">Indicates if coordinates are relative.</param>
        /// <param name="drawWidth">The width of the line in pixels.</param>
        public void DrawLine( Layer drawlayer,  Vector3D StartPoint,  Vector3D EndPoint, Color drawColor = default, bool relativeCoords = false, int drawWidth = 1, bool ScheduleInvalidateNeed = true)
        {
             LineElement element = new  LineElement(StartPoint, EndPoint, relativeCoords, drawWidth, ((drawColor == new Color()) ? Color.Black : drawColor));
            AddElement(drawlayer, element, ScheduleInvalidateNeed);
        }
        /// <summary>
        /// Draws a circle on the specified layer.
        /// </summary>
        /// <param name="drawlayer">The layer number to draw on.</param>
        /// <param name="CenterPosition">The  coordinate of the center.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="drawColor">The outline color (defaults to Black).</param>
        /// <param name="filled">Indicates if the circle should be filled.</param>
        /// <param name="fillColor">The fill color (defaults to Black).</param>
        /// <param name="fixedRadius">Indicates if the radius is fixed in pixels.</param>
        /// <param name="drawWidth">The width of the outline in pixels.</param>
        /// <param name="normal">the vector normal to the circle surface.</param>
        /// <param name="use3D">Draw as 3D circle or as normal 3D cirlce.</param>
        public void DrawCircle( Layer drawlayer, Vector3D CenterPosition, float radius, Color drawColor = default, bool filled = false, Color fillColor = default, bool fixedRadius = false, int drawWidth = 1, Vector3D? normal = null, bool use3D = false)
        {
             CircleElement element = new  CircleElement(CenterPosition, radius, fixedRadius, drawWidth, ((drawColor == new Color()) ? Color.Black : drawColor), ((fillColor == new Color()) ? Color.Black : fillColor), filled, normal, use3D);
            AddElement(drawlayer, element, true);
        }
        /// <summary>
        /// Draws a rectangle on the specified layer.
        /// </summary>
        /// <param name="layerNo">The layer number to draw on.</param>
        /// <param name="x1">The x-coordinate of the first corner.</param>
        /// <param name="y1">The y-coordinate of the first corner.</param>
        /// <param name="x2">The x-coordinate of the second corner.</param>
        /// <param name="y2">The y-coordinate of the second corner.</param>
        /// <param name="relativeCoords">Indicates if coordinates are relative.</param>
        /// <param name="drawWidth">The width of the outline in pixels.</param>
        /// <param name="drawColor">The outline color (defaults to Black).</param>
        /// <param name="fillColor">The fill color (defaults to White).</param>
        /// <param name="filled">Indicates if the rectangle should be filled.</param>
        public void DrawRectangle( Layer drawlayer, Vector3D StartPoint,  Vector3D EndPoint, bool relativeCoords = false, int drawWidth = 1, Color drawColor = default, Color fillColor = default, bool filled = false)
        {
             RectangleElement element = new RectangleElement(StartPoint, EndPoint, relativeCoords, drawWidth, ((drawColor == new Color()) ? Color.Black : drawColor), ((fillColor == new Color()) ? Color.White : fillColor), filled);
            AddElement(drawlayer, element, true);
        }
        /// <summary>
        /// Draws a text label on the specified layer.
        /// </summary>
        /// <param name="drawlayer">The layer number to draw on.</param>
        /// <param name="text">The text to display.</param>
        /// <param name="x1">The x-coordinate of the label's position.</param>
        /// <param name="y1">The y-coordinate of the label's position.</param>
        /// <param name="drawColor">The color of the text (defaults to Black).</param>
        /// <param name="fontSize">The size of the font in points (defaults to 8).</param>
        public void DrawLabel( Layer drawlayer, string text, Vector3D Position, Color drawColor = default, int fontSize = 8)
        {
             LabelElement element = new  LabelElement(Position, text, ((drawColor == new Color()) ? Color.Black : drawColor), fontSize);
            AddElement(drawlayer, element, true);
        }
        /// <summary>
        /// Draws a Cylinder on the specified layer.
        /// </summary>
        /// <param name="drawlayer">The layer number to draw on.</param>
        /// <param name="startCenter">The First coordinate of the center.</param>
        /// <param name="endCenter">The second-coordinate of the center.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="drawColor">The outline color (defaults to Black).</param>
        /// <param name="filled">Indicates if the circle should be filled.</param>
        /// <param name="fillColor">The fill color (defaults to Black).</param>
        /// <param name="fixedRadius">Indicates if the radius is fixed in pixels.</param>
        /// <param name="drawWidth">The width of the outline in pixels.</param>
        /// <param name="normal">the vector normal to the circle surface.</param>
        /// <param name="use3D">Draw as 3D circle or as normal 3D cirlce.</param>
        public void DrawCylinder( Layer drawlayer,  Vector3D startCenter,  Vector3D endCenter, float radius, bool fixedRadius, int drawWidth, Color drawColor,
                                  Color fillColor, bool filled, bool drawEndCaps, int detailLevel = 16)
        {
             CylinderElement element = new CylinderElement(startCenter, endCenter, radius, fixedRadius, drawWidth, drawColor,
                              fillColor, filled, drawEndCaps, detailLevel = 16);
            AddElement(drawlayer, element, true);
        }
        #endregion
        #region Constructor
        /// <summary>
        /// Gets or sets the minimum real-world size (width or height) for a zoom selection region.
        /// Prevents excessive zooming when selecting small areas.
        /// </summary>
        public float MinZoomRegionSize
        {
            get
            {
                return _minZoomRegionSize;
            }
            set
            {
                if (value <= 0F)
                {
                    throw new ArgumentException("MinZoomRegionSize must be positive.");
                }
                _minZoomRegionSize = value;
            }
        }



        private void InitializeInstanceFields()
        {
            _CurrentViewSettings = new  ViewSettings(GetUsableViewportwithControls(), new Vector3D(1.0F, 1.0F, 1.0F), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
        }
        #endregion
        #region zoom
        public void ZoomExtents(float PadingPercent)
        {
            _viewSettings = UsedZooming.ZoomExtents(_viewSettings, UsedLayerManager, PadingPercent);
        }
        #endregion
        #region Layers wrapper



        #endregion

        #region redo/undo
        public void Undo()
        {
            _commandManager.Undo();
        }
        public void Redo()
        {
            _commandManager.Redo();
        }
        #endregion
        #region Save/load
        public void LoadSVGFile(string filePath)
        {
            Arnaoot.VectorGraphics.Formats.Svg.SvgImporter importExport = new Arnaoot.VectorGraphics.Formats.Svg.SvgImporter();
            UsedLayerManager.RemoveAllLayers();
                        importExport.LoadFromSvg(filePath,   UsedLayerManager);
        }
        public void SaveSVGFile(string filePath)
        {
            Arnaoot.VectorGraphics.Formats.Svg.SvgExporter importExport = new Arnaoot.VectorGraphics.Formats.Svg.SvgExporter();
            importExport.SaveAsSvg(filePath, false, UsedLayerManager, _viewSettings);
        }

        #endregion
    }
}
