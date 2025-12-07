using Arnaoot.Core;
using Arnaoot.VectorGraphics.Abstractions;
using Arnaoot.VectorGraphics.Commands;
using Arnaoot.VectorGraphics.Core;
using Arnaoot.VectorGraphics.Elements;
using Arnaoot.VectorGraphics.Platform.Skia;
using Arnaoot.VectorGraphics.Rendering;
using Arnaoot.VectorGraphics.Scene;
using Arnaoot.VectorGraphics.View;
using static Arnaoot.VectorGraphics.Abstractions.Abstractions;

namespace VectorDrawEngine
{
    public partial class VectorDrawEngine : UserControl
    {
        #region Local Variables
        #region enum
        public enum VectorGraphicsEngineMode
        {
            Normal,
            SelectingZoomRectangleFirstPoint,
            SelectingZoomRectangleSecondPoint,
            Panning,
            Rotating,
            DrawingLine,
            DrawingCircle,
            DrawingRectangle,
            DrawingLabel,
            ElementSelection,           // User can select elements
            DraggingControlPoint       // User is dragging a control point
        }
        #endregion
        #region Control fields
        //

        // Selection state
        private IDrawElement _selectedElement = null;
        private int _selectedControlPointIndex = -1;
        // private Vector3D? _pendingControlPointWorldPos;
        //
        private const float SELECTION_TOLERANCE_PIXELS = 8.0f;// Tolerance in pixels (convert to world units based on zoom)
        private ICommand _activeDragCommand; // stores the command being built
        private bool _isDraggingControlPoint;
        //
        private float _minZoomRegionSize = 1.0F; // Default value
        CommandManager _commandManager = new CommandManager(100);
        //
        private VectorGraphicsEngineMode CurrentMode; //normal , panning , zooming ,....
                                                      //
        private IDrawElement TemporaryElement; // For previewing the element being drawn
        // Selecting zoom Rectangle
        private DateTime VisibleRectangleStartTime;
        private Vector3D SelectingRectanglestartPoint; // Pixel coords of start
        private RectangleElement selectionRect; // Current selection in pixel coords
        //
        // Panning  
        private Vector2D PanStartPointPixel;
        // 
        private bool _pendingInvalidate = false;
        private DateTime _lastInvalidate = DateTime.MinValue;
        private const int THROTTLE_MS = 16; // ~60 FPS
                                            //
        private Bitmap _bufferBitmap;
        public  ArgbColor _backColor =  ArgbColor.White; // Default color for backcolor

        #endregion

        #region class declare
        //  private coordinate Usedcoordinate = new coordinate();
        private  IZooming UsedZooming = new Zooming();
        private IViewSettings _CurrentViewSettings;
        private IRenderTarget usedRenderTarget;//= new SkiaRenderTarget();
        private  IRenderManager UsedrRenderManager;
        #endregion
        #region Event handler
        /// <summary>
        /// Occurs adding draw element to a layer that has not been initialized.
        /// </summary>
        public event EventHandler< LayerNotInitializedEventArgs> LayerNotInitializedWarning;
        #endregion

        #endregion


        #region constructor
        /// <summary>
        /// Initializes a new instance of the VectorGraphicsEngine class.
        /// </summary>
        public VectorDrawEngine()
        {
            _CurrentViewSettings = new ViewSettings(
                new  Rect2(0, 0, this.Width, this.Height)
                , new Vector3D(1.0F, 1.0F, 1.0F), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));

            usedRenderTarget = new SkiaRenderTarget();
            UsedrRenderManager = new  RenderManager(usedRenderTarget);
            InitializeComponent();
            //InitializeContextMenu();


            UsedLayerManager.LayersChanged += OnLayersChanged;

        }
        #endregion

        #region rasteraization
        /// <summary>
        /// Clears all drawing elements and resets the view settings.
        /// </summary>
        public void ClearDrawing(bool RemoveAllLayers)
        {
            UsedLayerManager.RemoveAllLayers();
            _viewSettings = new ViewSettings(GetUsableViewportwithControls(), new Vector3D(1.0F, 1.0F, 1.0F), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
            ScheduleInvalidate();
        }

        private void ScheduleInvalidate()
        {
            if (_pendingInvalidate || !this.IsHandleCreated) return;

            var elapsed = (DateTime.UtcNow - _lastInvalidate).TotalMilliseconds;
            if (elapsed < THROTTLE_MS)
            {
                _pendingInvalidate = true;
                var delay = THROTTLE_MS - (int)elapsed;

                System.Threading.Timer timer = null;
                timer = new System.Threading.Timer(_ =>
                {
                    timer?.Dispose();
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            _pendingInvalidate = false;
                            _lastInvalidate = DateTime.UtcNow;
                            RedrawToBuffer(); // Regen buffer
                            StatusZoom.Text = ((float)_viewSettings.ZoomFactorAverage * 100).ToString("0.00") + "%";
                            Invalidate(); // Trigger OnPaint with new buffer
                        }));
                    }
                }, null, delay, Timeout.Infinite);
            }
            else
            {
                _pendingInvalidate = true;
                this.BeginInvoke(new Action(() =>
                {
                    _pendingInvalidate = false;
                    _lastInvalidate = DateTime.UtcNow;
                    RedrawToBuffer(); // Regen
                    StatusZoom.Text = ((float)_viewSettings.ZoomFactorAverage * 100).ToString("0.00") + "%";
                    Invalidate(); // Trigger
                }));
            }
        }
        public void RedrawToBuffer()
        {
            if (this.Width <= 0 || this.Height <= 0) return;

            int newWidth = (int)_viewSettings.UsableViewport.Width;
            int newHeight = (int)_viewSettings.UsableViewport.Height;
            if (newWidth <= 0 || newHeight <= 0) return;

            // ✅ Reuse if size unchanged
            if (_bufferBitmap?.Width != newWidth || _bufferBitmap?.Height != newHeight)
            {
                _bufferBitmap?.Dispose();
                _bufferBitmap = new Bitmap(newWidth, newHeight);
            }

            // ✅ Cull elements early
            IReadOnlyCollection<IDrawElement> visibleElements = UsedLayerManager.GetVisibleElements()
                .Where(el => UsedrRenderManager.IsBoundsVisible(el.GetBounds(), _viewSettings))
                .ToList();

            // Add temp layer elements (assume few — no cull needed)
          Layer tempLayer = new  Layer();
            if (TemporaryElement != null) tempLayer.AddElement(TemporaryElement, false);
            if (selectionRect != null) tempLayer.AddElement(selectionRect, false);

            // Rasterize directly into reused buffer
            UsedrRenderManager.RasterizeIntoBuffer(_bufferBitmap, _viewSettings, visibleElements, tempLayer, _backColor);
            Invalidate();
        }
        #endregion

        #region  events  
        #region Main control events
        /// <summary>
        /// Paints the control, including background, grid, axes, and drawing elements.
        /// </summary>
        /// <param name="e">The PaintEventArgs containing event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_bufferBitmap != null)
            {
                e.Graphics.DrawImageUnscaled(_bufferBitmap, 0, 0);
            }
            else
            {
                // Fallback: Clear to your canvas color (masks any blank)
                e.Graphics.Clear(_backColor); // Or Color.White
            }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ScheduleInvalidate(); // triggers delayed or immediate RedrawToBuffer
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            //Debug.WriteLine("VectorGraphicsEngine lost focus at " + DateTime.Now.ToString("HH:mm:ss.fff"));
            CurrentMode = 0;
        }
        Rect2 GetUsableViewportwithControls()
        {
            // Null-safety: if any required control is null → return zero Rect2
            if (PanelRightTools == null ||
                ToolStripMain == null ||
                StatusStripMain == null)
            {
                return new Rect2(0, 0, 0, 0);
            }

            //int left = PanelRightTools.Visible ? PanelRightTools.Width : 0;
            int left = 0;
            int top = ToolStripMain.Height;
            //int width = ClientSize.Width - left;
            int width = PanelRightTools.Visible ? ClientSize.Width - PanelRightTools.Width : ClientSize.Width;
            int height = ClientSize.Height - StatusStripMain.Height;

            return new Rect2(left, top, width, height);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            //
            _CurrentViewSettings.UpdateUsableViewport(GetUsableViewportwithControls());
            //
            ScheduleInvalidate();
            //
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo();
            }
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                Redo();
            }
            else if (e.Control && e.KeyCode == Keys.Oemplus)
            {
                _viewSettings = UsedZooming.ZoomIn(_viewSettings, this.Width / 2, this.Height / 2);
            }
            else if (e.Control && e.KeyCode == Keys.OemMinus)
            {
                _viewSettings = UsedZooming.ZoomOut(_viewSettings, this.Width / 2, this.Height / 2);
            }
        }
        #endregion
        #region Mouse methods
        /// <summary>
        /// Handles the mouse wheel event to zoom in or out.
        /// </summary>
        /// <param name="e">The MouseEventArgs containing event data.</param>
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Vector2D PivotPointPixel = new Vector2D(e.X, e.Y);
            Vector3D ZoomFactor;
            //
            if (e.Delta > 0)
            {
                _viewSettings = UsedZooming.Zoom(_viewSettings, 1.1F, PivotPointPixel);
            }
            else if (e.Delta < 0)
            {
                _viewSettings = UsedZooming.Zoom(_viewSettings, 0.9F, PivotPointPixel);
            }
            //
        }
        /// <summary>
        /// Handles the mouse down event to initiate panning and raise the VectorMouseDown event.
        /// </summary>
        /// <param name="e">The MouseEventArgs containing event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            // Record time for click detection (existing functionality)
            VisibleRectangleStartTime = DateTime.Now;

            switch (e.Button)
            {
                case MouseButtons.Right:
                    CurrentMode = VectorGraphicsEngineMode.SelectingZoomRectangleSecondPoint;
                    SelectingRectanglestartPoint = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));
                    ScheduleInvalidate();
                    break;

                case MouseButtons.Left:
                    switch (CurrentMode)
                    {
                        case VectorGraphicsEngineMode.Normal:
                            CurrentMode = VectorGraphicsEngineMode.Panning;
                            PanStartPointPixel = new Vector2D(e.X, e.Y);
                            this.Cursor = Cursors.Hand;
                            break;

                        case VectorGraphicsEngineMode.SelectingZoomRectangleFirstPoint:
                            SelectingRectanglestartPoint = _viewSettings.PictToViewPlane(new Vector2D(e.X, e.Y));
                            CurrentMode = VectorGraphicsEngineMode.SelectingZoomRectangleSecondPoint;
                            break;

                        case VectorGraphicsEngineMode.ElementSelection:
                            Vector3D clickPoint = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));
                            float tolerance = _viewSettings.ZoomFactorAverage * SELECTION_TOLERANCE_PIXELS;

                            // First: Check if clicking a control point of the *currently selected* element
                            if (_selectedElement != null &&
                                _selectedElement.TryGetControlPointAt(clickPoint, tolerance, out int pointIndex))
                            {
                                // ✅ Start dragging control point — capture OLD state NOW
                                _selectedControlPointIndex = pointIndex;
                                //_dragStartWorldPos = clickPoint;

                                // Create drag command (captures pre-drag state)
                                _activeDragCommand = CreateControlPointCommand(_selectedElement, pointIndex);

                                _isDraggingControlPoint = true;
                                CurrentMode = VectorGraphicsEngineMode.DraggingControlPoint;
                                this.Cursor = Cursors.SizeAll;
                            }
                            else
                            {
                                // Try to select a *new* element
                                IDrawElement clickedElement = UsedLayerManager.FindElementAtPoint(clickPoint, tolerance);

                                if (clickedElement != null)
                                {
                                    // If same element but no CP hit → just select (no drag)
                                    if (clickedElement == _selectedElement)
                                    {
                                        // Maybe deselect CP, keep element selected
                                        _selectedControlPointIndex = -1;
                                    }
                                    else
                                    {
                                        // New element → full selection
                                        SelectElement(clickedElement);
                                        _selectedControlPointIndex = -1;
                                    }
                                }
                                else
                                {
                                    // Empty space → clear all
                                    ClearSelection();
                                }
                            }
                            break;
                        // =========================================

                        case VectorGraphicsEngineMode.DrawingLine:
                        case VectorGraphicsEngineMode.DrawingCircle:
                        case VectorGraphicsEngineMode.DrawingRectangle:
                            Vector3D realPoint = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));
                            switch (CurrentMode)
                            {
                                case VectorGraphicsEngineMode.DrawingLine:
                                    TemporaryElement = ( IDrawElement)new  LineElement(realPoint, realPoint, false, 1, DrawColor);
                                    break;
                                case VectorGraphicsEngineMode.DrawingCircle:
                                    TemporaryElement = ( IDrawElement)new  CircleElement(realPoint, 1F, false, 1, DrawColor, ArgbColor.Yellow, false, new Vector3D(0, 0, 0), false);
                                    break;
                                case VectorGraphicsEngineMode.DrawingRectangle:
                                    TemporaryElement = ( IDrawElement)new  RectangleElement(realPoint, realPoint, false, 1, DrawColor,  ArgbColor.Transparent, false);
                                    break;
                            }
                            break;

                        case VectorGraphicsEngineMode.DrawingLabel:
                            // Label will be handled on MouseUp
                            break;
                    }
                    break;
            }
        }
        /// <summary>
        /// Handles the mouse move event to update coordinates and enable panning.
        /// </summary>
        /// <param name="e">The MouseEventArgs containing event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Vector3D realPoint = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));
            StatusCoordinates.Text = $"Location: {realPoint.X:F1}, {realPoint.Y:F1}, {realPoint.Z:F1}";
            //

            if (_isDraggingControlPoint && _selectedElement != null)
            {
                Vector3D newPt = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));

                // ✅ Mutate immediately for visual feedback
                _selectedElement.MoveControlPoint(_selectedControlPointIndex, newPt);

                // ✅ Update command’s target value (for final commit)
                if (_activeDragCommand is  PropertyCommand<Vector3D> cmd)
                {
                    // Use reflection or add a method to update _newValue — simplest: recreate
                    // But better: add UpdateNewValue(T) to PropertyCommand (see below)
                    cmd.UpdateNewValue(newPt);
                }

                ScheduleInvalidate();
                return;
            }
            // ===============================================

            if (TemporaryElement != null)
            {
                switch (CurrentMode)
                {
                    case VectorGraphicsEngineMode.DrawingLine:
                        LineElement line = ( LineElement)TemporaryElement;
                        line.End = realPoint;
                        ScheduleInvalidate();
                        break;
                    case VectorGraphicsEngineMode.DrawingCircle:
                        CircleElement circle = ( CircleElement)TemporaryElement;
                        float dx = realPoint.X - circle.Center.X;
                        float dy = realPoint.Y - circle.Center.Y;
                        circle.Radius = Convert.ToSingle(Math.Sqrt(dx * dx + dy * dy));
                        ScheduleInvalidate();
                        break;
                    case VectorGraphicsEngineMode.DrawingRectangle:
                      RectangleElement rect = (RectangleElement)TemporaryElement;
                        rect.EndPoint = realPoint;
                        ScheduleInvalidate();
                        break;
                }
            }

            switch (CurrentMode)
            {
                case VectorGraphicsEngineMode.SelectingZoomRectangleSecondPoint:
                    Vector3D SelectingRectangleEndPoint = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));
                    selectionRect = new  RectangleElement(SelectingRectanglestartPoint, SelectingRectangleEndPoint, false, 1, Color.Blue, default, false);
                    selectionRect.IsSelected = true;
                    ScheduleInvalidate();
                    break;

                case VectorGraphicsEngineMode.Panning:
                    UsedZooming.PushToHistory(_viewSettings.Clone());

                    float deltaX = e.X - PanStartPointPixel.X;
                    float deltaY = e.Y - PanStartPointPixel.Y;

                    float worldDeltaX = deltaX / _viewSettings.ZoomFactor.X;
                    float worldDeltaY = -deltaY / _viewSettings.ZoomFactor.Y;

                    Vector3D NewShift = new Vector3D(
                        _viewSettings.ShiftWorld.X + worldDeltaX,
                        _viewSettings.ShiftWorld.Y + worldDeltaY,
                        _viewSettings.ShiftWorld.Z);
                    //
                    _viewSettings = new ViewSettings(
                        GetUsableViewportwithControls(),
                        _viewSettings.ZoomFactor,
                        NewShift,
                        _viewSettings.RotationAngle,
                        _viewSettings.RotateAroundPoint);

                    PanStartPointPixel = new Vector2D(e.X, e.Y);
                    ScheduleInvalidate();
                    break;
            }
        }


        /// <summary>
        /// Handles the mouse up event to end panning.
        /// </summary>
        /// <param name="e">The MouseEventArgs containing event data.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            Vector3D realPoint = _viewSettings.PictToReal(new Vector2D(e.X, e.Y));



            if (_isDraggingControlPoint)
            {
                if (_activeDragCommand != null)
                {
                    // Final newValue is already set — Execute() is NO-OP (already applied),
                    // but we still push to undo stack.
                    _commandManager.ExecuteCommand(_activeDragCommand);
                }

                _activeDragCommand = null;
                _isDraggingControlPoint = false;
                CurrentMode = VectorGraphicsEngineMode.Normal;
                this.Cursor = Cursors.Default;
            }
            // =======================================================

            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Middle:
                    break;

                case System.Windows.Forms.MouseButtons.Right:
                    if (CurrentMode == VectorGraphicsEngineMode.SelectingZoomRectangleSecondPoint)
                    {
                        if (DateTime.Now.Subtract(VisibleRectangleStartTime).TotalMilliseconds < 200)
                        {
                            CurrentMode = VectorGraphicsEngineMode.Normal;
                            ScheduleInvalidate();
                            return;
                        }
                        this.ContextMenuStrip = null;
                        ZoomToRectangle(SelectingRectanglestartPoint, _viewSettings.PictToReal(new Vector2D(e.X, e.Y)));
                        selectionRect = null;
                    }
                    break;

                case System.Windows.Forms.MouseButtons.Left:
                    switch (CurrentMode)
                    {
                        case VectorGraphicsEngineMode.Normal:
                            CurrentMode = VectorGraphicsEngineMode.Panning;
                            PanStartPointPixel = new Vector2D(e.X, e.Y);
                            this.Cursor = Cursors.Hand;
                            break;

                        case VectorGraphicsEngineMode.SelectingZoomRectangleSecondPoint:
                            ZoomToRectangle(SelectingRectanglestartPoint, _viewSettings.PictToViewPlane(new Vector2D(e.X, e.Y)));
                            break;

                        case VectorGraphicsEngineMode.Panning:
                            break;

                        case VectorGraphicsEngineMode.DrawingLine:
                        case VectorGraphicsEngineMode.DrawingCircle:
                        case VectorGraphicsEngineMode.DrawingRectangle:
                            if (TemporaryElement != null)
                            {
                                UsedLayerManager.AddElementsToActiveLayer(new[] { TemporaryElement });
                                _commandManager.ExecuteCommand(new  AddRemoveCommand(TemporaryElement, UsedLayerManager.ActiveLayer, true));
                                TemporaryElement = null;
                                ScheduleInvalidate();
                            }
                            break;

                        case VectorGraphicsEngineMode.DrawingLabel:
                            TxtLabelInput.Top = e.Y;
                            TxtLabelInput.Left = e.X;
                            TxtLabelInput.Text = "";
                            TxtLabelInput.Visible = true;
                            TxtLabelInput.Focus();
                            break;

                        // ====== NEW: Stay in ElementSelection mode after click ======
                        case VectorGraphicsEngineMode.ElementSelection:
                            // Don't change mode - stay in selection mode
                            UpdateUndoRedoButtons();
                            return;
                            // ===========================================================
                    }
                    break;
            }

            UpdateUndoRedoButtons();
            CurrentMode = VectorGraphicsEngineMode.Normal;
            this.Cursor = Cursors.Default;
        }
        #endregion
        #region Mouse CommandHelper

        private ICommand CreateControlPointCommand(IDrawElement element, int pointIndex)
        {
            if (element == null) return null;

            // Line: Start (0), End (1)
            if (element is  LineElement line)
            {
                if (pointIndex == (int) LinePointType.Start)
                    return new  PropertyCommand<Vector3D>(
                        line, "StartPoint",
                        () => line.Start ,
                        v => line.Start  = v,
                        line.Start);

                if (pointIndex == (int) LinePointType.End)
                    return new  PropertyCommand<Vector3D>(
                        line, "EndPoint",
                        () => line.End ,
                        v => line.End  = v,
                        line.End);
            }

            // Circle: Center (0), RadiusPoint (1) — assuming radius defined by center + perimeter point
            else if (element is  CircleElement circle)
            {
                var points = circle.GetControlPoints(); // [0]=Center, [1]=RadiusPoint
                if (pointIndex == 0)
                    return new  PropertyCommand<Vector3D>(
                        circle, "Center",
                        () => points[0],
                        v => circle.MoveControlPoint(0, v),
                        points[0]);

                if (pointIndex == 1)
                    return new  PropertyCommand<Vector3D>(
                        circle, "RadiusPoint",
                        () => points[1],
                        v => circle.MoveControlPoint(1, v),
                        points[1]);
            }

            // Rectangle: TopLeft (0), TopRight (1), BottomRight (2), BottomLeft (3)
            // Or: [0]=TopLeft, [1]=BottomRight (minimal)
            else if (element is  RectangleElement rect)
            {
                var points = rect.GetControlPoints();
                // Support 2-point (min/max) or 4-point corners — adapt indexing as needed
                if (pointIndex >= 0 && pointIndex < points.Length)
                    return new  PropertyCommand<Vector3D>(
                        rect, $"Corner{pointIndex}",
                        () => points[pointIndex],
                        v => rect.MoveControlPoint(pointIndex, v),
                        points[pointIndex]);
            }

            // Label: Anchor (0), maybe alignment handle (1)
            else if (element is LabelElement label)
            {
                var points = label.GetControlPoints();
                if (pointIndex == 0) // anchor/position
                    return new  PropertyCommand<Vector3D>(
                        label, "Position",
                        () => points[0],
                        v => label.MoveControlPoint(0, v),
                        points[0]);

                // Optional: second point for rotation/size if supported
                if (pointIndex == 1 && points.Length > 1)
                    return new  PropertyCommand<Vector3D>(
                        label, "Handle1",
                        () => points[1],
                        v => label.MoveControlPoint(1, v),
                        points[1]);
            }

            // Fallback: generic control-point command (safe for any IDrawElement)
            var initialPos = element.GetControlPoints()[pointIndex];
            return new  PropertyCommand<Vector3D>(
                element, $"ControlPoint{pointIndex}",
                () => element.GetControlPoints()[pointIndex],
                v => element.MoveControlPoint(pointIndex, v),
                initialPos);

            // Note: This fallback re-fetches points — less efficient, but works universally.
        }

        #endregion
        #endregion
        #region ToolStripMain command event

        private void BtnToggleGrid_Click(object sender, EventArgs e)
        {
            UsedrRenderManager.ShowGrid = !UsedrRenderManager.ShowGrid;
            BtnToggleGrid.Checked = UsedrRenderManager.ShowGrid ;
            ScheduleInvalidate();

        }
        private void BtnToggleAxes_Click(object sender, EventArgs e)
        {
            UsedrRenderManager.ShowAxes = !UsedrRenderManager.ShowAxes;
            BtnToggleAxes.Checked = UsedrRenderManager.ShowAxes;
            ScheduleInvalidate();
        }
        private void BtnOrbitPanel_Click(object sender, EventArgs e)
        {
            BtnOrbitPanel.Checked = !BtnOrbitPanel.Checked;
            PanelRightTools.Visible = BtnOrbitPanel.Checked;
            _viewSettings.UpdateUsableViewport(GetUsableViewportwithControls());
        }

        #region file
        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            //
            saveFileDialog1.Filter = "SVG files (*.SVG)|*.SVG|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            //
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {

                Arnaoot.VectorGraphics.Formats.Svg.SvgExporter  importExport = new Arnaoot.VectorGraphics.Formats.Svg.SvgExporter();
                importExport.SaveAsSvg(saveFileDialog1.FileName + ".SVG", false, UsedLayerManager, _viewSettings);
            }
        }
        private void ToolStripButton_New_file_Click(object sender, EventArgs e)
        {
            UsedLayerManager.RemoveAllLayers();
            UsedLayerManager.AddLayer("default");
            //
            _viewSettings = new ViewSettings(GetUsableViewportwithControls(), new Vector3D(1, 1, 1), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0), new Vector3D(0, 0, 0));
            ScheduleInvalidate();
        }
        private void BtnLoad_Click(object sender, EventArgs e)
        {
            // Create an OpenFileDialog instance.
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            // Set the filter to show only SVG files.  This is only visual;  the user can still select other files.
            openFileDialog1.Filter = "SVG Files (*.svg)|*.svg|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Title = "Select an SVG File";
            // Show the dialog and check the result.
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog1.FileName;
                // Basic file extension check.  More robust validation would be needed for production code.
                if (System.IO.Path.GetExtension(filePath).ToLower() == ".svg")
                {
                    try
                    {
                        UsedLayerManager.RemoveAllLayers();
                        //UsedLayerManager.AddLayer("Default");
                        //
                        Arnaoot.VectorGraphics.Formats.Svg.SvgImporter  importExport = new Arnaoot.VectorGraphics.Formats.Svg.SvgImporter();
                        importExport.LoadFromSvg(filePath,  UsedLayerManager);
                        //
                        ZoomExtents(5f);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error processing the SVG file: " + ex.Message);
                    }
                }
                else
                {
                    MessageBox.Show("Please select an SVG file.");
                }
            }
        }
        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                Vector3D TopLeftPoint = _viewSettings.PictToReal(new Vector2D(0F, ToolStripMain.Height));
                Vector3D BottomRightPoint = _viewSettings.PictToReal(new Vector2D(Width, Height - StatusStripMain.Height));
                BoundingBox3D ImageExtenst = new BoundingBox3D(TopLeftPoint, BottomRightPoint);
                //
                SaveFileDialog saveFileDialog1 = new SaveFileDialog();
                saveFileDialog1.Filter = "Png file (*.Png)|*.Png|All files (*.*)|*.*";
                saveFileDialog1.FilterIndex = 1;
                saveFileDialog1.RestoreDirectory = true;
                //
                // Show the dialog and check the result.
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    UsedrRenderManager.SaveRegionAsImage(saveFileDialog1.FileName, UsedLayerManager.GetVisibleElements(), _viewSettings, ImageExtenst, Width, Height, System.Drawing.Imaging.ImageFormat.Png, true);
                }
                //Stop
            }
            catch (System.Exception exc)
            {
                MessageBox.Show("Maze Image Save Error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion
        #region Draw
        private void BtnLineTool_Click(object sender, EventArgs e)
        {
            CurrentMode = VectorGraphicsEngineMode.DrawingLine;
        }

        private void BtnCircleTool_Click(object sender, EventArgs e)
        {
            CurrentMode = VectorGraphicsEngineMode.DrawingCircle;
        }

        private void BtnRectangleTool_Click(object sender, EventArgs e)
        {
            CurrentMode = VectorGraphicsEngineMode.DrawingRectangle;
        }

        private void BtnTextTool_Click(object sender, EventArgs e)
        {
            CurrentMode = VectorGraphicsEngineMode.DrawingLabel;
        }

        private void chooseColorToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                DrawColor = colorDialog1.Color;
                this.chooseColorToolStripMenuItem.BackColor = DrawColor;
                this.chooseColorToolStripMenuItem.ForeColor = GetContrastColor(DrawColor);
            }
        }
        // Helper function to determine contrasting text color
        private Color GetContrastColor(Color bgColor)
        {
            // Calculate luminance to choose black or white for text
            double luminance = (0.299 * bgColor.R + 0.587 * bgColor.G + 0.114 * bgColor.B) / 255;
            return ((luminance > 0.5) ? Color.Black : Color.White);
        }
        #endregion

        #region Redo Undo
        private void BtnUndo_Click(object sender, EventArgs e)
        {
            Undo();
            UpdateUndoRedoButtons();
            ScheduleInvalidate();
        }

        private void BtnRedo_Click(object sender, EventArgs e)
        {
            Redo();
            UpdateUndoRedoButtons();
            ScheduleInvalidate();
        }
        private void UpdateUndoRedoButtons()
        {
            BtnUndo.Enabled = _commandManager.CanUndo;
            BtnRedo.Enabled = _commandManager.CanRedo;
        }

        #endregion
        #region layer management
        private void OnLayersChanged()
        {
            // Example: refresh your ToolStrip layers menu
            UpdateLayerMenu(UsedLayerManager);

            // If the drawing area depends on visible layers
            Invalidate();
        }
        public void UpdateLayerMenu(ILayerManager layerManager)
        {
            // Update all layer-dependent menus
            UpdateVisibilityMenu(layerManager);
            UpdateActiveLayerMenu(layerManager);
            UpdateDeleteLayerMenu(layerManager);
        }

        private void UpdateVisibilityMenu(ILayerManager layerManager)
        {
            setLayerVisibilityToolStripMenuItem.DropDownItems.Clear();

            foreach (Layer layer in layerManager.Layers)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(layer.Name);
                item.Checked = layer.Visible;
                item.Tag = layer.Id;
                item.Click += (s, e) => ToggleLayerVisibility(layer);
                setLayerVisibilityToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void UpdateActiveLayerMenu( ILayerManager layerManager)
        {
            setActiveLayerToolStripMenuItem.DropDownItems.Clear();

            foreach ( Layer layer in layerManager.Layers)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(layer.Name);
                item.Checked = (layerManager.ActiveLayer == layer);
                item.Tag = layer.Id;
                item.Click += (s, e) => SetActiveLayer(layer);
                setActiveLayerToolStripMenuItem.DropDownItems.Add(item);
            }
        }

        private void UpdateDeleteLayerMenu( ILayerManager layerManager)
        {
            deleteLayerToolStripMenuItem.DropDownItems.Clear();

            foreach (Layer layer in layerManager.Layers)
            {
                // Don't allow deleting the last layer
                if (layerManager.Layers.Count > 1)
                {
                    ToolStripMenuItem item = new ToolStripMenuItem(layer.Name);
                    item.Tag = layer.Id;
                    item.Click += (s, e) => DeleteLayer(layer);
                    deleteLayerToolStripMenuItem.DropDownItems.Add(item);
                }
            }

            // Disable delete menu if only one layer exists
            deleteLayerToolStripMenuItem.Enabled = deleteLayerToolStripMenuItem.DropDownItems.Count > 0;
        }
        private void ToggleLayerVisibility(Layer layer)
        {
            layer.Visible = !layer.Visible;
            UpdateLayerMenu(UsedLayerManager);
            ScheduleInvalidate();
        }

        private void SetActiveLayer(Layer layer)
        {
            UsedLayerManager.SetActiveLayer(layer);
            UpdateLayerMenu(UsedLayerManager);
            ScheduleInvalidate();
        }

        private void DeleteLayer(Layer layer)
        {
            if (UsedLayerManager.Layers.Count <= 1) return;

            // Ask for confirmation
            DialogResult result = MessageBox.Show(
                $"Delete layer '{layer.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                UsedLayerManager.RemoveLayer(layer);
                UpdateLayerMenu(UsedLayerManager);
                ScheduleInvalidate();
            }
        }

        private void AddNewLayerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string layerName = $"Layer {UsedLayerManager.Layers.Count + 1}";
            //
            UsedLayerManager.AddLayer(layerName);
            UpdateLayerMenu(UsedLayerManager);
            ScheduleInvalidate();
        }
        #endregion
        #region zoom
        private void ZoomToRectangle(Vector3D RealStartPoint, Vector3D RealEndPoint)
        {
            // Convert selection to real-world coords
            //Vector3D RealStartPoint = CurrentViewSettings.PictToReal(FirstPointPixel);
          //  RealStartPoint =  ViewSettings.ClampToGdiRangePoint(RealStartPoint);
            //
            //Vector3D RealEndPoint = CurrentViewSettings.PictToReal(SecondPointPixel);
           // RealEndPoint = ViewSettings.ClampToGdiRangePoint(RealEndPoint);
            //}
            Vector3D RectRealMinPoint = new Vector3D(Math.Min(RealStartPoint.X, RealEndPoint.X), Math.Min(RealStartPoint.Y, RealEndPoint.Y), 0);
            Vector3D RectRealMaxPoint = new Vector3D(Math.Max(RealStartPoint.X, RealEndPoint.X), Math.Max(RealStartPoint.Y, RealEndPoint.Y), 0);
            //
            // Save current state before zooming
            UsedZooming.PushToHistory(_viewSettings.Clone());  //PushToHistory(CurrentViewSettings); //
            BoundingBox3D VisbileBox = new BoundingBox3D(RectRealMinPoint, RectRealMaxPoint);
             IViewSettings NewViewSettings = UsedZooming.GetRegionViewSettings(_viewSettings, VisbileBox, 5);
            _viewSettings = NewViewSettings;
            ScheduleInvalidate();
        }
        private void MenuItemZoomPrevious_Click(object sender, EventArgs e)
        {
            _viewSettings = UsedZooming.ZoomPrevious(_viewSettings);
        }

        private void BtnPan_Click(object sender, EventArgs e)
        {
            CurrentMode = VectorGraphicsEngineMode.Panning;
            PanStartPointPixel = new Vector2D(Convert.ToInt32(this.Width / 2.0), Convert.ToInt32(this.Height / 2.0));
        }
        public void CenterViewOnPosition(float Span, float realX, float realY)
        {
            BoundingBox3D VisbileBoxBounds = new BoundingBox3D(new Vector3D(realX - Span / 2, realY - Span / 2, 0), new Vector3D(realX + Span / 2, realY + Span / 2, 0));
            _viewSettings = UsedZooming.GetRegionViewSettings(_viewSettings, VisbileBoxBounds, 5F);
        }

        private void BtnZoomIn_Click(object sender, EventArgs e)
        {
            _viewSettings = UsedZooming.ZoomIn(_viewSettings, this.Width / 2, this.Height / 2);
        }
        private void BtnZoomOut_Click(object sender, EventArgs e)
        {
            _viewSettings = UsedZooming.ZoomOut(_viewSettings, this.Width / 2, this.Height / 2);
        }
        private void BtnZoomFit_Click(object sender, EventArgs e)
        {
            ResetViewSetting(false, false, true, false);
            _viewSettings = UsedZooming.ZoomExtents(_viewSettings, UsedLayerManager, 5f);

        }
        #endregion

        #endregion

        #region side menu methods
        private void BtnResetWorldShift_Click(object sender, EventArgs e)
        {
            ResetViewSetting(false, true, false, false);
        }
        private void btnWorldShift_Click(object sender, EventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null) return;
            Vector3D Shift = _viewSettings.ShiftWorld;
            int ShiftSize = (int)(5 / _viewSettings.ZoomFactorAverage);
             //
            switch (clickedButton.Name)
            {
                case nameof(btnWorldShiftXDec):
                    Shift.X = Shift.X - ShiftSize;
                    break;
                case nameof(btnWorldShiftXInc):
                    Shift.X = Shift.X + ShiftSize;
                    break;
                case nameof(btnWorldShiftYDec):
                    Shift.Y = Shift.Y - ShiftSize;
                    break;
                case nameof(btnWorldShiftYInc):
                    Shift.Y = Shift.Y + ShiftSize;
                    break;
                case nameof(btnWorldShiftZDec):
                    Shift.Z = Shift.Z - ShiftSize;
                    break;
                case nameof(btnWorldShiftZInc):
                    Shift.Z = Shift.Z + ShiftSize;
                    break;
                default:
                    break;
            }
            //
            lblworldShift.Text = "X: " + ((int)Shift.X).ToString() + ", Y: " + ((int)Shift.Y).ToString() + ",Z:" + ((int)Shift.Z).ToString();
            //
            _viewSettings.ShiftWorld = Shift;
            ScheduleInvalidate();
        }

        private void TrackBarRotation_Scroll(object sender, EventArgs e)
        {
            LblXRotation.Text = "X: " + ((float)TrackBarXRotation.Value * 1.8 / 3.14).ToString("0.00") + " °";
            LblYRotation.Text = "Y: " + ((float)TrackBarYRotation.Value * 1.8 / 3.14).ToString("0.00") + " °";
            LblZRotation.Text = "Z: " + ((float)TrackBarZRotation.Value * 1.8 / 3.14).ToString("0.00") + " °";

            Vector2D centerPixel = new Vector2D(this.Width / 2.0f, this.Height / 2.0f);

            // Use the new method that doesn't reverse rotation
            Vector3D rotatePoint = _viewSettings.PictToViewPlane(centerPixel, 0.0f);

            Vector3D RotationAngle = new Vector3D(
                (float)TrackBarXRotation.Value / 100f,
                (float)TrackBarYRotation.Value / 100f,
                (float)TrackBarZRotation.Value / 100f);

            _viewSettings = new ViewSettings(
                GetUsableViewportwithControls(),
                _viewSettings.ZoomFactor,
                _viewSettings.ShiftWorld,
                RotationAngle,
                rotatePoint);

            ScheduleInvalidate();
        }

        private void BtnResetRotation_Click(object sender, EventArgs e)
        {
            ResetViewSetting(false, false, true, false);
        }
        private void ResetViewSetting(bool ResetZoomFactor, bool ResetShiftWolrd, bool ResetRotationAngle, bool ResetRotateAroundPoint)
        {
            Vector3D ZoomFactor = _viewSettings.ZoomFactor;
            Vector3D ShiftWolrd = _viewSettings.ShiftWorld;
            Vector3D RotationAngle = _viewSettings.RotationAngle;
            Vector3D RotateAroundPoint = _viewSettings.RotateAroundPoint;
            //
            if (ResetZoomFactor == true)
            {
                ZoomFactor = new Vector3D(1, 1, 1);
            }
            //
            if (ResetShiftWolrd == true)
            {
                ShiftWolrd = new Vector3D(0, 0, 0);
            }
            //
            if (ResetRotationAngle == true)
            {
                RotationAngle = new Vector3D(0, 0, 0);
                //
                LblXRotation.Text = "X: 0°";
                LblYRotation.Text = "Y: 0°";
                LblZRotation.Text = "Z: 0°";
                //
                TrackBarXRotation.Value = 0;
                TrackBarYRotation.Value = 0;
                TrackBarZRotation.Value = 0;
                //
            }
            //
            if (ResetRotateAroundPoint == true)
            {
                RotateAroundPoint = new Vector3D(0, 0, 0);
            }
            //
            if (ResetRotationAngle == true || ResetShiftWolrd == true || ResetRotateAroundPoint == true || ResetZoomFactor == true)
            {
                _viewSettings = _viewSettings = new ViewSettings(GetUsableViewportwithControls(), _viewSettings.ZoomFactor, _viewSettings.ShiftWorld, RotationAngle, _CurrentViewSettings.RotateAroundPoint);
                ScheduleInvalidate();
            }
        }
        private void BtnSelectTool_Click(object sender, System.EventArgs e)
        {
            EnterSelectionMode();
        }
        private void TxtLabelInput_KeyDown(object sender, KeyEventArgs e)
        {
            Vector3D realPoint = _viewSettings.PictToReal(new Vector2D(TxtLabelInput.Left, TxtLabelInput.Top));
            if (e.KeyCode == Keys.Return)
            {
                LabelElement label = new LabelElement(realPoint, TxtLabelInput.Text, DrawColor, 8);
                UsedLayerManager.AddElementsToActiveLayer(new[] { label });
                TxtLabelInput.Text = "";
                TxtLabelInput.Visible = false;
            }
        }

        #endregion
        #region element selction and edit

        /// <summary>
        /// Attempts to find a control point near the click location on the selected element.
        /// </summary>
        private bool TrySelectControlPoint(Vector3D worldPoint, float worldTolerance, out int pointIndex)
        {
            if (_selectedElement != null)
            {
                return _selectedElement.TryGetControlPointAt(worldPoint, worldTolerance, out pointIndex);
            }

            pointIndex = -1;
            return false;
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        private void ClearSelection()
        {
            if (_selectedElement != null)
            {
                _selectedElement.IsSelected = false;
                _selectedElement = null;
                _selectedControlPointIndex = -1;
                ScheduleInvalidate();
            }
        }

        /// <summary>
        /// Selects an element and marks it as selected.
        /// </summary>
        private void SelectElement(IDrawElement element)
        {
            // Clear previous selection
            ClearSelection();

            if (element != null)
            {
                _selectedElement = element;
                _selectedElement.IsSelected = true;
                ScheduleInvalidate();
            }
        }
        /// <summary>
        /// Draws control point handles for the selected element.
        /// Call this after drawing all elements.
        /// </summary>
        private void DrawControlPointHandles(Graphics g)
        {
            if (_selectedElement == null || CurrentMode != VectorGraphicsEngineMode.ElementSelection)
                return;

            var controlPoints = _selectedElement.GetControlPoints();
            const float handleSize = 6f; // Size in pixels

            using (Brush fillBrush = new SolidBrush(Color.White))
            using (Pen outlinePen = new Pen(Color.Blue, 2f))
            {
                foreach (var worldPoint in controlPoints)
                {
                    // Convert world point to screen pixels
                    Vector2D screenPoint = _viewSettings.RealToPict(worldPoint, out float depth);

                    // Draw handle as a small square
                    Rect2 handleRect = new Rect2(
                        screenPoint.X - handleSize / 2,
                        screenPoint.Y - handleSize / 2,
                        handleSize,
                        handleSize);

                    g.FillRectangle(fillBrush, handleRect.ToRectangleF());
                    g.DrawRectangle(outlinePen,
                        handleRect.X, handleRect.Y,
                        handleRect.Width, handleRect.Height);
                }
            }
        }
        /// <summary>
        /// Enters element selection mode. Call this from a toolbar button or menu item.
        /// </summary>
        public void EnterSelectionMode()
        {
            ClearSelection();
            CurrentMode = VectorGraphicsEngineMode.ElementSelection;
            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Exits selection mode and returns to normal mode.
        /// </summary>
        public void ExitSelectionMode()
        {
            ClearSelection();
            CurrentMode = VectorGraphicsEngineMode.Normal;
            this.Cursor = Cursors.Default;
        }
        #endregion

    }
}
