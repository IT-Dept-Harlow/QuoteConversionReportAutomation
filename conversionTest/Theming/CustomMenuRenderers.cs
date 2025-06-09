// CustomMenuRenderers.cs
// Contains custom ToolStripProfessionalRenderer and ProfessionalColorTable
// implementations for theming MenuStrip and ToolStripDropDownMenu controls.
// These classes use ThemePalette for color definitions and respect the
// global custom theming enabled flag.

#region Using Directives
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Theming; // For ThemeSettings and ThemePalette
using System.Drawing;
using System.Windows.Forms;
// Logger might be needed if you add logging within these renderers, but currently not used directly.
// using QuoteConversionReportAutomation.Services.Logging; 
#endregion

namespace QuoteConversionReportAutomation.Theming // Or another appropriate namespace like .Rendering
{
    /// <summary>
    /// Custom renderer to handle menu item highlighting, text color, and background appearance 
    /// for ToolStrip controls based on the provided ThemePalette and custom theming state.
    /// </summary>
    public class CustomThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly ThemePalette _palette;
        private readonly bool _isCustomThemeEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomThemeMenuRenderer"/> class.
        /// </summary>
        /// <param name="palette">The theme palette containing colors for rendering if custom theming is enabled.</param>
        /// <param name="isCustomThemeEnabled">Indicates if custom theming is active for the renderer.</param>
        public CustomThemeMenuRenderer(ThemePalette palette, bool isCustomThemeEnabled)
            : base(new CustomThemeColorTable(palette, isCustomThemeEnabled))
        {
            _palette = palette;
            _isCustomThemeEnabled = isCustomThemeEnabled;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item != null)
            {
                if (_isCustomThemeEnabled)
                {
                    e.TextColor = _palette.MenuStripForeColor;
                }
                base.OnRenderItemText(e);
            }
            else
            {
                base.OnRenderItemText(e);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item == null) return;

            if (!_isCustomThemeEnabled)
            {
                base.OnRenderMenuItemBackground(e);
                return;
            }

            if (!e.Item.Enabled)
            {
                using (SolidBrush brush = new SolidBrush(_palette.MenuStripBackColor))
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
                if (!string.IsNullOrEmpty(e.Item.Text))
                {
                    TextRenderer.DrawText(e.Graphics, e.Item.Text, e.Item.Font, e.Item.ContentRectangle, SystemColors.GrayText, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                return;
            }

            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);

            if (e.Item.Selected || (e.Item is ToolStripMenuItem tsmi && tsmi.DropDown.Visible && !tsmi.IsOnDropDown))
            {
                using (SolidBrush brush = new SolidBrush(_palette.MenuItemSelectedColor))
                {
                    e.Graphics.FillRectangle(brush, rc);
                }
            }
            else
            {
                Color itemBackColorToUse = e.Item.IsOnDropDown ? _palette.MenuDropDownBackColor : _palette.MenuStripBackColor;
                using (SolidBrush brush = new SolidBrush(itemBackColorToUse))
                {
                    e.Graphics.FillRectangle(brush, rc);
                }
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (!_isCustomThemeEnabled)
            {
                base.OnRenderToolStripBackground(e);
                return;
            }

            if (e.ToolStrip is ToolStripDropDown)
            {
                using (SolidBrush brush = new SolidBrush(_palette.MenuDropDownBackColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }
            else
            {
                using (SolidBrush brush = new SolidBrush(_palette.MenuStripBackColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (!_isCustomThemeEnabled)
            {
                base.OnRenderToolStripBorder(e);
                return;
            }

            if (e.ToolStrip is ToolStripDropDown)
            {
                using (Pen pen = new Pen(_palette.MenuBorderColor))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1));
                }
            }
            // else base.OnRenderToolStripBorder(e); // Usually no border for main MenuStrip
        }
    }

    /// <summary>
    /// Custom <see cref="ProfessionalColorTable"/> to define specific colors for the <see cref="ToolStripProfessionalRenderer"/>
    /// when using custom themes. This class provides the color palette used by <see cref="CustomThemeMenuRenderer"/>.
    /// </summary>
    public class CustomThemeColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _palette;
        private readonly bool _isCustomThemeEnabled;

        public CustomThemeColorTable(ThemePalette palette, bool isCustomThemeEnabled)
        {
            _palette = palette;
            _isCustomThemeEnabled = isCustomThemeEnabled;
        }

        public override Color MenuItemSelected => _isCustomThemeEnabled ? _palette.MenuItemSelectedColor : base.MenuItemSelected;
        public override Color MenuItemSelectedGradientBegin => _isCustomThemeEnabled ? _palette.MenuItemSelectedColor : base.MenuItemSelectedGradientBegin;
        public override Color MenuItemSelectedGradientEnd => _isCustomThemeEnabled ? _palette.MenuItemSelectedColor : base.MenuItemSelectedGradientEnd;
        public override Color MenuItemPressedGradientBegin => _isCustomThemeEnabled ? _palette.MenuItemPressedColor : base.MenuItemPressedGradientBegin;
        public override Color MenuItemPressedGradientEnd => _isCustomThemeEnabled ? _palette.MenuItemPressedColor : base.MenuItemPressedGradientEnd;
        public override Color MenuItemBorder => _isCustomThemeEnabled ? _palette.MenuBorderColor : base.MenuItemBorder;
        public override Color MenuBorder => _isCustomThemeEnabled ? _palette.MenuBorderColor : base.MenuBorder;
        public override Color ToolStripDropDownBackground => _isCustomThemeEnabled ? _palette.MenuDropDownBackColor : base.ToolStripDropDownBackground;
        public override Color ImageMarginGradientBegin => _isCustomThemeEnabled ? _palette.MenuDropDownBackColor : base.ImageMarginGradientBegin;
        public override Color ImageMarginGradientMiddle => _isCustomThemeEnabled ? _palette.MenuDropDownBackColor : base.ImageMarginGradientMiddle;
        public override Color ImageMarginGradientEnd => _isCustomThemeEnabled ? _palette.MenuDropDownBackColor : base.ImageMarginGradientEnd;
        public override Color SeparatorDark => _isCustomThemeEnabled ? _palette.MenuSeparatorColor : base.SeparatorDark;
        public override Color SeparatorLight => _isCustomThemeEnabled ? Color.Transparent : base.SeparatorLight;
        public override Color StatusStripGradientBegin => _isCustomThemeEnabled ? _palette.StatusStripBackColor : base.StatusStripGradientBegin;
        public override Color StatusStripGradientEnd => _isCustomThemeEnabled ? _palette.StatusStripBackColor : base.StatusStripGradientEnd;
        public override Color MenuStripGradientBegin => _isCustomThemeEnabled ? _palette.MenuStripBackColor : base.MenuStripGradientBegin;
        public override Color MenuStripGradientEnd => _isCustomThemeEnabled ? _palette.MenuStripBackColor : base.MenuStripGradientEnd;
    }
}