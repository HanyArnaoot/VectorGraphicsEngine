using Arnaoot.Core;
using Arnaoot.VectorGraphics.Abstractions;
using Arnaoot.VectorGraphics.Scene;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VectorDrawEngine
{
    public partial class VectorDrawEngine : UserControl
    {
        #region Properties

        /// <summary>
        /// Gets or sets a value indicating whether to display the scale bar.
        /// </summary>
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Gets or sets the background color of the control.")]
        public Color BackgroundColor
        {
            get { return this.BackColor; }
            set
            {
                this.BackColor = value;
                //this.Invalidate();   // redraw control
            }
        }
        public Color DrawColor = Color.Black; // Default color for new elements
        public ILayerManager UsedLayerManager = new LayerManager();


        /// <summary>
        /// Gets or sets a value indicating whether to display the scale bar.
        /// </summary>
        public bool ShowScaleBar
        {
            get
            {
                return UsedrRenderManager.ShowScaleBar;
            }
            set
            {
                UsedrRenderManager.ShowScaleBar = value;
            }
        }

        /// <summary>
        /// Gets or sets the spacing between grid lines in real-world units.
        /// </summary>
        public float GridSpacing
        {
            get
            {
                return UsedrRenderManager.GridSpacingReal;
            }
            set
            {
                UsedrRenderManager.GridSpacingReal = value;
                ScheduleInvalidate();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display the axes.
        /// </summary>
        public bool ShowAxes
        {
            get
            {
                return UsedrRenderManager.ShowAxes;
            }
            set
            {
                UsedrRenderManager.ShowAxes = value;
                ScheduleInvalidate();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to display the grid.
        /// </summary>
        public bool ShowGrid
        {
            get
            {
                return UsedrRenderManager.ShowGrid;
            }
            set
            {
                UsedrRenderManager.ShowGrid = value;
                ScheduleInvalidate();
            }
        }

        /// <summary>
        /// Gets or sets the zoom factor for the VectorGraphicsEngine, 
        /// controlling the scale at which real-world coordinates are rendered in pixel space.
        /// </summary>
        /// <value>A single-precision value where smaller values zoom in (magnify) and larger values zoom out (shrink).</value>
        /// <remarks>
        /// Setting this property updates the <see cref="_viewSettings"/> ZoomFactor, 
        /// maintaining consistency with the control's view state. 
        /// The default is 1.0, representing no scaling. 
        /// After setting, call <see cref="ScheduleInvalidate"/> to refresh the display if not already triggered.
        /// </remarks>
        public  Vector3D ZoomFactor
        {
            get
            {
                return _viewSettings.ZoomFactor;
            }
            set
            {
                // Update CurrentViewSettings with the new ZoomFactor, preserving other values
                // Save current state before zooming
                UsedZooming.PushToHistory(_viewSettings.Clone());
                _viewSettings = new  ViewSettings(GetUsableViewportwithControls(), value, _viewSettings.ShiftWorld, _viewSettings.RotationAngle, _viewSettings.RotateAroundPoint);
                ScheduleInvalidate();
            }
        }
        /// <summary>
        /// Gets or sets the current view settings for the VectorGraphicsEngine, 
        /// including zoom factor, horizontal shift, and vertical shift.
        /// These settings determine how real-world coordinates are transformed 
        /// into pixel coordinates for rendering and interaction.
        /// </summary>
        /// <value>A <see cref="coordinate.ViewSettings"/> structure containing the current zoom and shift values.</value>
        /// <remarks>
        /// The default value is a zoom factor of 1 (no scaling), with no horizontal or vertical shift.
        /// Changes to this property should be followed by a call to <see cref="ScheduleInvalidate"/> 
        /// to update the display if the control is not already ScheduleInvalidated elsewhere.
        /// </remarks>
        /// 
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public  IViewSettings _viewSettings
        {
            get
            {
                return _CurrentViewSettings;
            }
            set
            {
                _CurrentViewSettings = value;
                ScheduleInvalidate();
            }
        }
        #endregion

    }
}
