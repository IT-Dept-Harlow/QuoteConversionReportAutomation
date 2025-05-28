// C# 10+ Features
namespace QuoteConversionReportAutomation.Managers
{
    // --- Standard and Third-Party Using Statements ---
    using Microsoft.Win32; // For Registry access to detect Windows theme.
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices; // For P/Invoke calls.
    using System.Threading.Tasks; // For Task.Delay.
    using System.Windows.Forms;

    // --- Project-Specific Using Statements ---
    using QuoteConversionReportAutomation.Services.Logging; // Assuming a custom logging service.
    using QuoteConversionReportAutomation.Services.Excel;

    #region Custom Menu Renderer for Dark Mode
    // This region contains classes responsible for rendering the MenuStrip and its items
    // in a dark theme, providing a custom look and feel.

    /// <summary>
    /// Custom renderer to handle dark mode menu item highlighting, text color, and background appearance for ToolStrip controls.
    /// It inherits from <see cref="ToolStripProfessionalRenderer"/> to customise the rendering behavior.
    /// </summary>
    public class DarkModeMenuRenderer : ToolStripProfessionalRenderer
    {
        // --- Private Static Readonly Fields for Colors ---
        // These colors are defined once and shared across all instances of DarkModeMenuRenderer.
        private static readonly Color _staticMenuItemHoverColor = Color.FromArgb(85, 85, 95);    // Background color when a menu item is hovered/selected.
        private static readonly Color _staticMenuBorderColor = Color.FromArgb(85, 85, 90);       // Border color for menus and dropdowns.
        private static readonly Color _staticMenuBackColor = Color.FromArgb(45, 45, 48);         // Background color for menu dropdowns and non-hovered items in dropdowns.

        // --- Private Readonly Fields for Colors ---
        // This color is specific to each instance, though currently set to a static value.
        private readonly Color _instanceMenuForeColor = Color.FromArgb(220, 220, 220); // Text color for menu items.

        /// <summary>
        /// Initializes a new instance of the <see cref="DarkModeMenuRenderer"/> class.
        /// Sets up the custom color table for dark mode rendering.
        /// </summary>
        public DarkModeMenuRenderer() : base(new DarkModeColorTable(_staticMenuItemHoverColor, Color.FromArgb(100, 100, 110), _staticMenuBorderColor, _staticMenuBackColor)) { }

        /// <summary>
        /// Renders the text of a <see cref="ToolStripItem"/>.
        /// Overridden to set a custom text color for dark mode.
        /// </summary>
        /// <param name="e">A <see cref="ToolStripItemTextRenderEventArgs"/> that contains the event data.</param>
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item != null)
            {
                e.TextColor = _instanceMenuForeColor; // Apply custom foreground color for text.
            }
            base.OnRenderItemText(e); // Call base method to perform default text rendering.
        }

        /// <summary>
        /// Renders the background of a <see cref="ToolStripItem"/>.
        /// Overridden to customise background colors for selected, hovered, and disabled states in dark mode.
        /// </summary>
        /// <param name="e">A <see cref="ToolStripItemRenderEventArgs"/> that contains the event data.</param>
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item == null) return; // Do nothing if the item is null.

            // Handle disabled menu items.
            if (!e.Item.Enabled)
            {
                using (SolidBrush brush = new SolidBrush(_staticMenuBackColor)) // Use static menu back color for disabled items.
                {
                    e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
                }
                // Optionally, render disabled text if needed, though OnRenderItemText might handle this.
                if (!string.IsNullOrEmpty(e.Item.Text))
                {
                    TextRenderer.DrawText(e.Graphics, e.Item.Text, e.Item.Font, e.Item.ContentRectangle, SystemColors.GrayText, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }
                return;
            }

            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size); // Define the rectangle for the item's background.

            // Handle selected/hovered items or items whose dropdown is visible.
            if (e.Item.Selected || (e.Item is ToolStripMenuItem tsmi && tsmi.DropDown.Visible && !tsmi.IsOnDropDown))
            {
                using (SolidBrush brush = new SolidBrush(_staticMenuItemHoverColor)) // Use hover color for selected/active items.
                {
                    e.Graphics.FillRectangle(brush, rc);
                }
            }
            else // Handle normal (non-selected, non-hovered) items.
            {
                Color itemBackColorToUse;
                // Check if the item is on a dropdown menu.
                if (e.Item.IsOnDropDown)
                {
                    // Items on a dropdown should use the darker dropdown background (_staticMenuBackColor).
                    itemBackColorToUse = _staticMenuBackColor;
                }
                else
                {
                    // Top-level items on the MenuStrip should use their own BackColor.
                    // This BackColor is set in UIManager.ApplyTheme -> UpdateMenuItemsTheme
                    // to match the MenuStrip's distinct background color.
                    itemBackColorToUse = e.Item.BackColor;
                }
                using (SolidBrush brush = new SolidBrush(itemBackColorToUse))
                {
                    e.Graphics.FillRectangle(brush, rc);
                }
            }
        }

        /// <summary>
        /// Renders the background of a <see cref="ToolStrip"/>.
        /// Overridden to apply a custom background color to <see cref="ToolStripDropDown"/> elements in dark mode.
        /// </summary>
        /// <param name="e">A <see cref="ToolStripRenderEventArgs"/> that contains the event data.</param>
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown) // Apply custom background only to dropdowns.
            {
                using (SolidBrush brush = new SolidBrush(_staticMenuBackColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }
            else // For other ToolStrip types (e.g., the main MenuStrip), let the base renderer handle it or apply form background.
            {
                // The MenuStrip's BackColor is set directly in ApplyTheme.
                // If the base method interferes, this could be removed or made conditional.
                base.OnRenderToolStripBackground(e);
            }
        }

        /// <summary>
        /// Renders the border of a <see cref="ToolStrip"/>.
        /// Overridden to apply a custom border color to <see cref="ToolStripDropDown"/> elements in dark mode.
        /// </summary>
        /// <param name="e">A <see cref="ToolStripRenderEventArgs"/> that contains the event data.</param>
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown) // Apply custom border only to dropdowns.
            {
                using (Pen pen = new Pen(_staticMenuBorderColor))
                {
                    e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1));
                }
            }
            else // For other ToolStrip types, let the base renderer handle the border.
            {
                base.OnRenderToolStripBorder(e);
            }
        }
    }

    /// <summary>
    /// Custom <see cref="ProfessionalColorTable"/> to define specific colors for the <see cref="ToolStripProfessionalRenderer"/>
    /// when in dark mode. This class provides the color palette used by <see cref="DarkModeMenuRenderer"/>.
    /// </summary>
    public class DarkModeColorTable : ProfessionalColorTable
    {
        // --- Private Readonly Fields for Color Palette ---
        private readonly Color _hoverColor;             // Color for selected/hovered menu items.
        private readonly Color _pressedColor;           // Color for pressed menu items.
        private readonly Color _borderColor;            // General border color.
        private readonly Color _menuBackColor;          // Background color for dropdown menus and image margins.
        private readonly Color _statusStripBackColor;   // Background color for StatusStrip (though distinct colors are now used directly).

        /// <summary>
        /// Initializes a new instance of the <see cref="DarkModeColorTable"/> class with specified colors.
        /// </summary>
        /// <param name="hover">The color for hovered or selected menu items.</param>
        /// <param name="pressed">The color for pressed menu items.</param>
        /// <param name="border">The color for borders.</param>
        /// <param name="menuBack">The background color for menus.</param>
        public DarkModeColorTable(Color hover, Color pressed, Color border, Color menuBack)
        {
            _hoverColor = hover;
            _pressedColor = pressed;
            _borderColor = border;
            _menuBackColor = menuBack;
            _statusStripBackColor = menuBack; // This is less relevant now as StatusStrip gets explicit colors.
        }

        // --- Overridden Color Properties ---
        // These properties return the custom dark mode colors.
        public override Color MenuItemSelected => _hoverColor;
        public override Color MenuItemSelectedGradientBegin => _hoverColor; // No gradient, solid color.
        public override Color MenuItemSelectedGradientEnd => _hoverColor;   // No gradient, solid color.
        public override Color MenuItemPressedGradientBegin => _pressedColor; // No gradient, solid color.
        public override Color MenuItemPressedGradientEnd => _pressedColor;   // No gradient, solid color.
        public override Color MenuItemBorder => _borderColor;               // Border for individual menu items.
        public override Color MenuBorder => _borderColor;                   // Border for the entire MenuStrip or ToolStripDropDown.
        public override Color ToolStripDropDownBackground => _menuBackColor; // Background of dropdown menus.
        public override Color ImageMarginGradientBegin => _menuBackColor;    // Background of the image margin in dropdowns.
        public override Color ImageMarginGradientMiddle => _menuBackColor;   // Background of the image margin in dropdowns.
        public override Color ImageMarginGradientEnd => _menuBackColor;      // Background of the image margin in dropdowns.
        public override Color SeparatorDark => _borderColor;                // Color for dark part of a separator.
        public override Color SeparatorLight => Color.Transparent;          // Color for light part of a separator (transparent for dark mode).

        // These are less critical if StatusStrip.BackColor is set directly.
        public override Color StatusStripGradientBegin => _statusStripBackColor;
        public override Color StatusStripGradientEnd => _statusStripBackColor;

        // These are less critical if MenuStrip.BackColor is set directly.
        public override Color MenuStripGradientBegin => _menuBackColor;
        public override Color MenuStripGradientEnd => _menuBackColor;
    }
    #endregion Custom Menu Renderer for Dark Mode

    /// <summary>
    /// Manages UI updates, theme application (dark/light mode), and control state for the main application form.
    /// This class centralizes UI logic to keep the main form class cleaner.
    /// </summary>
    public class UIManager
    {
        #region Fields and Control References
        // --- UI Control References ---
        // References to the UI controls managed by this UIManager instance.
        private readonly Form _parentForm; // Only used by instance methods of UIManager
        private readonly MenuStrip _menuStrip;
        private readonly StatusStrip _statusStrip;
        private readonly ToolStripStatusLabel _statusLabel;
        private readonly ToolStripStatusLabel _autoRunStatusLabel;
        private readonly ToolStripMenuItem _darkModeMenuItem;
        private readonly Button _createReportButton;
        private readonly Button _processEmailButton;
        private readonly Button _oneClickProcessButton;
        private readonly Button _toggleAutoRunButton;
        private readonly Button _viewReportButton;
        private readonly Button _viewAnalysisButton;
        private readonly ComboBox _reportTypeComboBox;
        private readonly DateTimePicker _startDatePicker;
        private readonly DateTimePicker _endDatePicker;
        private readonly ComboBox _financialYearComboBox;
        private readonly Label _financialYearLabel;
        private readonly CheckBox _sendToFemiOnlyCheckBox;
        private readonly CheckBox _skipEmailCheckBox;
        private readonly Label _emailRecipientLabel;
        private readonly ToolTip _toolTip;

        // --- Internal State ---
        private bool _isDarkModeField; // Tracks the currently applied theme (true for dark, false for light).
        private DarkModeMenuRenderer? _darkModeRenderer; // Custom renderer instance for dark mode menus.
        private int _currentAutoRunHour = 8; // Default hour for auto-run, can be updated.
        #endregion Fields and Control References

        #region P/Invoke Declarations and Constants
        // This region contains P/Invoke declarations for interacting with Windows APIs,
        // primarily for custom window theming (title bar) and system messages.

        // --- DWM API for Immersive Dark Mode Title Bar ---
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        // DWM attribute constants for enabling dark mode on the title bar.
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903 = 19; // For Win10 builds 18362-19040
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;                 // For Win10 build 19041+ and Win11

        // --- User32 API for Redrawing Window ---
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        // RedrawWindow flags.
        private const uint RDW_INVALIDATE = 0x0001;     // Invalidates lprcUpdate or hrgnUpdate (or entire window if both are null).
        private const uint RDW_ERASE = 0x0004;          // Causes the window to receive a WM_ERASEBKGND message.
        private const uint RDW_UPDATENOW = 0x0100;      // Causes a WM_PAINT message to be posted to the window's message queue.
        private const uint RDW_ERASENOW = 0x0200;       // Causes the background to be erased immediately.
        private const uint RDW_FRAME = 0x0400;          // Causes the non-client area to be redrawn.

        // --- User32 API for Sending Messages with Timeout ---
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            UIntPtr wParam,
            string? lParam, // Can be null if not needed by the message.
            SendMessageTimeoutFlags fuFlags,
            uint uTimeout,
            out UIntPtr lpdwResult);

        /// <summary>
        /// Flags for the SendMessageTimeout function, controlling its behavior.
        /// </summary>
        [Flags]
        public enum SendMessageTimeoutFlags : uint
        {
            SMTO_NORMAL = 0x0,              // Default behavior.
            SMTO_BLOCK = 0x1,               // Blocks the calling thread until the message is processed or timeout occurs.
            SMTO_ABORTIFHUNG = 0x2,         // Returns if the receiving window is hung.
            SMTO_NOTIMEOUTIFNOTHUNG = 0x8,  // Does not time out if the receiving window is not hung.
            SMTO_ERRORONEXIT = 0x0020       // Returns an error if the receiving thread terminates without processing the message.
        }

        // Window Messages & System-wide Constants.
        public const uint WM_SETTINGCHANGE = 0x001A;         // Message indicating a system-wide setting has changed.
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xFFFF); // Target for broadcasting messages to all top-level windows.
        #endregion P/Invoke Declarations and Constants

        #region Theme Colors
        // Defines color palettes for Dark Mode (DM) and Light Mode (LM).
        // These are used to style various UI elements.

        // --- Dark Mode Colors ---
        private static readonly Color DM_BackColor = Color.FromArgb(45, 45, 48);          // General background color for forms/panels.
        private static readonly Color DM_ForeColor = Color.White;                         // General text color.
        private static readonly Color DM_ControlBackColor = Color.FromArgb(60, 60, 63);   // Background for input controls (TextBox, ComboBox).
        private static readonly Color DM_ButtonBackColor = Color.FromArgb(80, 80, 80);      // Background for buttons.
        private static readonly Color DM_Menu_ItemText_ForeColor = Color.FromArgb(220, 220, 220); // Text color for menu items (used by renderer).

        // --- Light Mode Colors ---
        private static readonly Color LM_BackColor = SystemColors.Control;                // Standard system control background.
        private static readonly Color LM_ForeColor = SystemColors.ControlText;            // Standard system control text color.
        private static readonly Color LM_ControlBackColor = SystemColors.Window;          // Standard system window background (for inputs).
        private static readonly Color LM_ButtonBackColor = SystemColors.Control;          // Standard system button background.
                                                                                          // LM_MenuForeColor will typically be SystemColors.MenuText via the default renderer.

        // --- Distinct MenuStrip Colors ---
        private static readonly Color DM_MenuStrip_Distinct_BackColor = Color.FromArgb(55, 55, 58); // Slightly lighter dark for MenuStrip bar
        private static readonly Color DM_MenuStrip_Distinct_ForeColor = DM_Menu_ItemText_ForeColor; // Text color for top-level MenuStrip items in dark mode

        private static readonly Color LM_MenuStrip_Distinct_BackColor = Color.FromArgb(220, 220, 225); // Distinct light gray for MenuStrip bar
        private static readonly Color LM_MenuStrip_Distinct_ForeColor = SystemColors.MenuText; // Standard menu text for light mode

        // --- Distinct StatusStrip Colors ---
        private static readonly Color DM_StatusStrip_Distinct_BackColor = Color.FromArgb(35, 35, 38); // Slightly different dark for StatusStrip
        private static readonly Color DM_StatusStrip_Distinct_ForeColor = Color.FromArgb(190, 190, 190); // Slightly subdued white for StatusStrip text

        private static readonly Color LM_StatusStrip_Distinct_BackColor = Color.FromArgb(210, 210, 215); // Darker gray for Light Mode StatusStrip
        private static readonly Color LM_StatusStrip_Distinct_ForeColor = Color.Black;                   // Standard black text for StatusStrip

        // --- AutoRun Specific Colors ---
        private static readonly Color AutoRunEnabledColor = Color.LightGreen;             // Background for AutoRun button when enabled.
        private static readonly Color AutoRunDisabledColor = Color.LightCoral;            // Background for AutoRun button when disabled.
        private static readonly Color AutoRunButtonForeColor = Color.Black;               // Text color for AutoRun button (consistent across themes).
        #endregion Theme Colors

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="UIManager"/> class.
        /// </summary>
        /// <param name="parentForm">The main form whose UI is being managed.</param>
        /// <param name="menuStrip">The main menu strip of the form.</param>
        /// <param name="statusStrip">The status strip at the bottom of the form.</param>
        /// <param name="statusLabel">The primary status label on the status strip.</param>
        /// <param name="autoRunStatusLabel">The status label for the auto-run feature.</param>
        /// <param name="darkModeMenuItem">The menu item used to toggle dark mode.</param>
        /// <param name="createReportButton">The button to create a report.</param>
        /// <param name="processEmailButton">The button to process and email a report.</param>
        /// <param name="oneClickProcessButton">The button for one-click report generation and processing.</param>
        /// <param name="toggleAutoRunButton">The button to enable/disable auto-run.</param>
        /// <param name="viewReportButton">The button to view the generated raw report.</param>
        /// <param name="viewAnalysisButton">The button to view the generated analysis file.</param>
        /// <param name="reportTypeComboBox">The combo box for selecting the report type.</param>
        /// <param name="startDatePicker">The date picker for the report start date.</param>
        /// <param name="endDatePicker">The date picker for the report end date.</param>
        /// <param name="financialYearComboBox">The combo box for selecting the financial year.</param>
        /// <param name="financialYearLabel">The label associated with the financial year combo box.</param>
        /// <param name="sendToFemiOnlyCheckBox">The checkbox to restrict email recipients.</param>
        /// <param name="skipEmailCheckBox">The checkbox to skip sending emails.</param>
        /// <param name="emailRecipientLabel">The label indicating email recipients for daily reports.</param>
        /// <param name="toolTip">The ToolTip component for providing hints.</param>
        public UIManager(
            Form parentForm, MenuStrip menuStrip, StatusStrip statusStrip,
            ToolStripStatusLabel statusLabel, ToolStripStatusLabel autoRunStatusLabel,
            ToolStripMenuItem darkModeMenuItem, Button createReportButton, Button processEmailButton,
            Button oneClickProcessButton,
            Button toggleAutoRunButton, Button viewReportButton, Button viewAnalysisButton,
            ComboBox reportTypeComboBox, DateTimePicker startDatePicker, DateTimePicker endDatePicker,
            ComboBox financialYearComboBox, Label financialYearLabel, CheckBox sendToFemiOnlyCheckBox,
            CheckBox skipEmailCheckBox,
            Label emailRecipientLabel, ToolTip toolTip
            )
        {
            // Assign all control references, ensuring none are null.
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _menuStrip = menuStrip ?? throw new ArgumentNullException(nameof(menuStrip));
            _statusStrip = statusStrip ?? throw new ArgumentNullException(nameof(statusStrip));
            _statusLabel = statusLabel ?? throw new ArgumentNullException(nameof(statusLabel));
            _autoRunStatusLabel = autoRunStatusLabel ?? throw new ArgumentNullException(nameof(autoRunStatusLabel));
            _darkModeMenuItem = darkModeMenuItem ?? throw new ArgumentNullException(nameof(darkModeMenuItem));
            _createReportButton = createReportButton ?? throw new ArgumentNullException(nameof(createReportButton));
            _processEmailButton = processEmailButton ?? throw new ArgumentNullException(nameof(processEmailButton));
            _oneClickProcessButton = oneClickProcessButton ?? throw new ArgumentNullException(nameof(oneClickProcessButton));
            _toggleAutoRunButton = toggleAutoRunButton ?? throw new ArgumentNullException(nameof(toggleAutoRunButton));
            _viewReportButton = viewReportButton ?? throw new ArgumentNullException(nameof(viewReportButton));
            _viewAnalysisButton = viewAnalysisButton ?? throw new ArgumentNullException(nameof(viewAnalysisButton));
            _reportTypeComboBox = reportTypeComboBox ?? throw new ArgumentNullException(nameof(reportTypeComboBox));
            _startDatePicker = startDatePicker ?? throw new ArgumentNullException(nameof(startDatePicker));
            _endDatePicker = endDatePicker ?? throw new ArgumentNullException(nameof(endDatePicker));
            _financialYearComboBox = financialYearComboBox ?? throw new ArgumentNullException(nameof(financialYearComboBox));
            _financialYearLabel = financialYearLabel ?? throw new ArgumentNullException(nameof(financialYearLabel));
            _sendToFemiOnlyCheckBox = sendToFemiOnlyCheckBox ?? throw new ArgumentNullException(nameof(sendToFemiOnlyCheckBox));
            _skipEmailCheckBox = skipEmailCheckBox ?? throw new ArgumentNullException(nameof(skipEmailCheckBox));
            _emailRecipientLabel = emailRecipientLabel ?? throw new ArgumentNullException(nameof(emailRecipientLabel));
            _toolTip = toolTip ?? throw new ArgumentNullException(nameof(toolTip));
        }
        #endregion Constructor

        #region Theme Management
        // This region handles the application of visual themes (Dark/Light Mode) to the UI.

        /// <summary>
        /// Applies the specified theme (dark or light) to the UIManager's parent form and its controls.
        /// This includes styling the title bar, menus, and other standard UI elements.
        /// </summary>
        /// <param name="isDarkModeRequested">True to apply dark mode, false to apply light mode.</param>
        public void ApplyTheme(bool isDarkModeRequested)
        {
            _isDarkModeField = isDarkModeRequested; // Update the internal state tracking the current theme.

            // Determine the color palette based on the requested theme.
            Color formBackColor = isDarkModeRequested ? DM_BackColor : LM_BackColor;
            Color formForeColor = isDarkModeRequested ? DM_ForeColor : LM_ForeColor;
            Color controlBackColor = isDarkModeRequested ? DM_ControlBackColor : LM_ControlBackColor;
            Color buttonBackColor = isDarkModeRequested ? DM_ButtonBackColor : LM_ButtonBackColor;

            // Determine distinct MenuStrip colors
            Color menuStripDistinctBackColor = isDarkModeRequested ? DM_MenuStrip_Distinct_BackColor : LM_MenuStrip_Distinct_BackColor;
            Color menuStripDistinctForeColor = isDarkModeRequested ? DM_MenuStrip_Distinct_ForeColor : LM_MenuStrip_Distinct_ForeColor;

            // Determine distinct StatusStrip colors
            Color statusStripBackColor = isDarkModeRequested ? DM_StatusStrip_Distinct_BackColor : LM_StatusStrip_Distinct_BackColor;
            Color statusStripForeColor = isDarkModeRequested ? DM_StatusStrip_Distinct_ForeColor : LM_StatusStrip_Distinct_ForeColor;


            // Perform UI updates on the UI thread safely.
            SafeControlUpdate(_parentForm, () =>
            {
                // Apply base colors to the parent form.
                _parentForm.BackColor = formBackColor;
                _parentForm.ForeColor = formForeColor;

                // Apply title bar theme directly for the _parentForm of this UIManager instance
                bool titleBarSuccess = UseImmersiveDarkModeInternal(_parentForm.Handle, isDarkModeRequested);
                Logger.LogInfo($"UIManager.ApplyTheme: Attempted to set title bar dark mode for '{_parentForm.Name}' to {isDarkModeRequested}. Success: {titleBarSuccess}");
                if (titleBarSuccess)
                {
                    // Request a redraw of the frame to apply title bar changes
                    RedrawWindow(_parentForm.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);
                }


                // Recursively apply theme to all child controls.
                UpdateControlThemeRecursive(_parentForm, formBackColor, formForeColor, controlBackColor, buttonBackColor, isDarkModeRequested);

                // Apply theme to MenuStrip (if it exists for this UIManager instance)
                if (_menuStrip != null)
                {
                    _menuStrip.BackColor = menuStripDistinctBackColor;
                    _menuStrip.ForeColor = menuStripDistinctForeColor;
                    // Set the appropriate menu renderer (dark or light).
                    if (isDarkModeRequested)
                    {
                        _darkModeRenderer ??= new DarkModeMenuRenderer();
                        _menuStrip.Renderer = _darkModeRenderer;
                    }
                    else
                    {
                        _menuStrip.Renderer = new ToolStripProfessionalRenderer(new ProfessionalColorTable());
                    }
                    // Re-apply theme to menu items after renderer change.
                    UpdateMenuItemsTheme(_menuStrip.Items, menuStripDistinctBackColor, menuStripDistinctForeColor, isDarkModeRequested);
                }


                // Apply theme to StatusStrip and its labels (if they exist for this UIManager instance)
                if (_statusStrip != null)
                {
                    _statusStrip.BackColor = statusStripBackColor;
                    _statusStrip.ForeColor = statusStripForeColor;
                }
                if (_statusLabel != null)
                {
                    _statusLabel.ForeColor = statusStripForeColor;
                    _statusLabel.BackColor = Color.Transparent; // Or statusStripBackColor
                }
                if (_autoRunStatusLabel != null)
                {
                    _autoRunStatusLabel.ForeColor = statusStripForeColor;
                    _autoRunStatusLabel.BackColor = Color.Transparent; // Or statusStripBackColor
                }

                _parentForm.Refresh(); // Refresh client area after all changes
            });

            // Update AutoRun UI elements based on the new theme and current auto-run state.
            // This part is specific to Form1's UIManager instance which has these controls.
            if (_toggleAutoRunButton != null && _autoRunStatusLabel != null)
            {
                bool isTimerCurrentlyEnabled = false;
                SafeControlUpdate(_toggleAutoRunButton, () => isTimerCurrentlyEnabled = _toggleAutoRunButton.Text.StartsWith("Disable"));
                bool isAutoRunStatusFinal = (_autoRunStatusLabel.Text?.Contains("Completed") ?? false) ||
                                            (_autoRunStatusLabel.Text?.Contains("Done for") ?? false) ||
                                            (_autoRunStatusLabel.Text?.Contains("FAILED") ?? false);
                UpdateAutoRunUI(isTimerCurrentlyEnabled, isAutoRunStatusFinal, _isDarkModeField, _autoRunStatusLabel.Text ?? "");
            }
            Logger.LogInfo($"Theme applied by UIManager instance for '{_parentForm.Name}': {(isDarkModeRequested ? "Dark Mode" : "Light Mode")}");
        }

        /// <summary>
        /// Applies window frame theming (title bar, basic background/foreground) to an external form.
        /// This method centralizes the P/Invoke calls for theming any Form.
        /// It does NOT handle child controls recursively.
        /// </summary>
        /// <param name="formToTheme">The Form instance to apply the theme to.</param>
        /// <param name="isDarkModeEnabled">True to apply dark mode, false for light mode.</param>
        public static void ApplyThemeToExternalForm(Form formToTheme, bool isDarkModeEnabled)
        {
            if (formToTheme == null || formToTheme.IsDisposed)
            {
                Logger.LogWarning("ApplyThemeToExternalForm: Attempted to theme a null or disposed form.");
                return;
            }

            Logger.LogDebug($"ApplyThemeToExternalForm: Applying FRAME theme to '{formToTheme.Name}'. DarkMode: {isDarkModeEnabled}");

            Color formBackColor = isDarkModeEnabled ? DM_BackColor : LM_BackColor;
            Color formForeColor = isDarkModeEnabled ? DM_ForeColor : LM_ForeColor;

            // Apply basic form colors directly
            formToTheme.BackColor = formBackColor;
            formToTheme.ForeColor = formForeColor;

            // Apply title bar and frame theme
            bool titleBarSuccess = UseImmersiveDarkModeInternal(formToTheme.Handle, isDarkModeEnabled);
            Logger.LogInfo($"ApplyThemeToExternalForm: Attempted to set title bar dark mode for '{formToTheme.Name}' to {isDarkModeEnabled}. Success: {titleBarSuccess}");

            if (titleBarSuccess)
            {
                // Redraw the frame to apply title bar changes.
                // Avoid FormBorderStyle changes here as they can be problematic if called mid-operation.
                RedrawWindow(formToTheme.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASENOW);

                // Send WM_SETTINGCHANGE to notify the window and its children of theme changes.
                // This can help some standard controls pick up theme changes.
                UIntPtr settingChangeResult;
                SendMessageTimeout(
                    formToTheme.Handle,
                    WM_SETTINGCHANGE,
                    UIntPtr.Zero,
                    "ImmersiveColorSet", // Standard string for this type of change
                    SendMessageTimeoutFlags.SMTO_ABORTIFHUNG | SendMessageTimeoutFlags.SMTO_NOTIMEOUTIFNOTHUNG,
                    500, // Shorter timeout, this is a notification
                    out settingChangeResult
                );
                Logger.LogDebug($"ApplyThemeToExternalForm: WM_SETTINGCHANGE sent to '{formToTheme.Name}'. Result: {settingChangeResult}, LastError: {Marshal.GetLastWin32Error()}");
            }
            // A single Refresh at the end of all theming (including children) is usually better.
            // formToTheme.Refresh(); // This might be deferred to the caller if children are themed separately.
        }


        /// <summary>
        /// Recursively applies theme colors to a control and its child controls.
        /// </summary>
        /// <param name="parentControl">The parent control to start theming from.</param>
        /// <param name="formBackColor">The background color for the form/panel areas.</param>
        /// <param name="formForeColor">The general foreground (text) color.</param>
        /// <param name="controlBackColor">The background color for input-type controls.</param>
        /// <param name="buttonBackColor">The background color for buttons.</param>
        /// <param name="isCurrentlyDark">A boolean indicating if dark mode is currently being applied.</param>
        private void UpdateControlThemeRecursive(Control parentControl, Color formBackColor, Color formForeColor, Color controlBackColor, Color buttonBackColor, bool isCurrentlyDark)
        {
            // Check if parentControl itself is one of the specific controls handled by the UIManager instance
            // This is to avoid re-theming controls that are directly managed by UIManager fields if _parentForm is complex.
            // However, for a generic recursive call, this might not be necessary if the UIManager instance is specific to _parentForm.

            foreach (Control control in parentControl.Controls)
            {
                SafeControlUpdate(control, () => // Ensure updates happen on the UI thread.
                {
                    // Handle specific control types with custom styling.
                    if (control == _toggleAutoRunButton && _toggleAutoRunButton != null) // Check if it's the instance's button
                    {
                        // AutoRun button has its own color logic managed by UpdateAutoRunUI for BackColor.
                        control.ForeColor = AutoRunButtonForeColor;
                    }
                    else if (control is Button button)
                    {
                        button.BackColor = buttonBackColor;
                        button.ForeColor = formForeColor;
                        button.FlatStyle = FlatStyle.Flat; // Flat style often looks better with custom colors.
                        if (isCurrentlyDark)
                        {
                            button.FlatAppearance.BorderColor = DM_ControlBackColor; // Subtle border in dark mode.
                            button.FlatAppearance.BorderSize = 1;
                        }
                        else
                        {
                            button.FlatAppearance.BorderColor = SystemColors.ControlDark; // Standard border in light mode.
                            button.FlatAppearance.BorderSize = 1;
                        }
                    }
                    else if (control is TextBox tb)
                    {
                        tb.BackColor = controlBackColor;
                        tb.ForeColor = formForeColor;
                        tb.BorderStyle = isCurrentlyDark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                    else if (control is RichTextBox rtb)
                    {
                        rtb.BackColor = controlBackColor;
                        rtb.ForeColor = formForeColor;
                        rtb.BorderStyle = isCurrentlyDark ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                    }
                    else if (control is ComboBox cb)
                    {
                        cb.BackColor = controlBackColor;
                        cb.ForeColor = formForeColor;
                        cb.FlatStyle = FlatStyle.Flat; // Or Standard/System depending on desired look.
                    }
                    else if (control is DateTimePicker dtp)
                    {
                        dtp.BackColor = controlBackColor;
                        dtp.ForeColor = formForeColor;
                        // Basic calendar theming for DateTimePicker.
                        dtp.CalendarForeColor = formForeColor;
                        dtp.CalendarMonthBackground = controlBackColor;
                        dtp.CalendarTitleBackColor = buttonBackColor;
                        dtp.CalendarTitleForeColor = formForeColor;
                        dtp.CalendarTrailingForeColor = Color.Gray; // For days not in the current month.
                    }
                    else if (control is CheckBox chkBox)
                    {
                        chkBox.BackColor = formBackColor; // Match form background for transparency.
                        chkBox.ForeColor = formForeColor;
                        chkBox.FlatStyle = FlatStyle.Standard;
                    }
                    else if (control is Label || control is GroupBox)
                    {
                        // For labels and groupbox backgrounds, match the form's back color for a blended look.
                        // GroupBox text color will be the form's forecolor.
                        control.BackColor = formBackColor; // Or Color.Transparent for Labels if parent is a Panel/GroupBox
                        control.ForeColor = formForeColor;
                        if (control is GroupBox gb)
                        {
                            // Recursively theme controls within the GroupBox.
                            UpdateControlThemeRecursive(gb, formBackColor, formForeColor, controlBackColor, buttonBackColor, isCurrentlyDark);
                        }
                    }
                    else if (control is Panel panel)
                    {
                        panel.BackColor = formBackColor; // Match form background.
                        panel.ForeColor = formForeColor;
                        // Recursively theme controls within the Panel.
                        UpdateControlThemeRecursive(panel, formBackColor, formForeColor, controlBackColor, buttonBackColor, isCurrentlyDark);
                    }
                    else if (control is TabControl tabControl)
                    {
                        tabControl.BackColor = formBackColor;
                        tabControl.ForeColor = formForeColor;
                        foreach (TabPage tabPage in tabControl.TabPages)
                        {
                            tabPage.BackColor = formBackColor; // Theme each tab page
                            tabPage.ForeColor = formForeColor;
                            UpdateControlThemeRecursive(tabPage, formBackColor, formForeColor, controlBackColor, buttonBackColor, isCurrentlyDark);
                        }
                    }
                    else if (control is TableLayoutPanel tlp)
                    {
                        tlp.BackColor = formBackColor;
                        tlp.ForeColor = formForeColor;
                        UpdateControlThemeRecursive(tlp, formBackColor, formForeColor, controlBackColor, buttonBackColor, isCurrentlyDark);
                    }
                    // Skip MenuStrip, StatusStrip, and ToolStrip as they are handled separately or by renderers.
                    else if (!(control is MenuStrip || control is StatusStrip || control is ToolStrip))
                    {
                        // For other container controls, recurse.
                        if (control.HasChildren)
                        {
                            UpdateControlThemeRecursive(control, formBackColor, formForeColor, controlBackColor, buttonBackColor, isCurrentlyDark);
                        }
                        else // For simple non-container controls not explicitly handled.
                        {
                            control.BackColor = formBackColor; // Default to formBackColor
                            control.ForeColor = formForeColor;
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Updates the theme for a collection of <see cref="ToolStripItem"/> objects and their dropdowns.
        /// </summary>
        /// <param name="items">The collection of menu items to theme.</param>
        /// <param name="menuStripBackColor">The background color for the MenuStrip bar itself.</param>
        /// <param name="menuStripForeColor">The foreground (text) color for top-level items on the MenuStrip bar.</param>
        /// <param name="isCurrentlyDark">A boolean indicating if dark mode is currently being applied.</param>
        private void UpdateMenuItemsTheme(ToolStripItemCollection items, Color menuStripBackColor, Color menuStripForeColor, bool isCurrentlyDark)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.IsDisposed) continue;

                // Only apply distinct MenuStrip bar colors to items directly on the _menuStrip instance.
                // Dropdown items get their colors from the renderer.
                if (item.Owner == _menuStrip)
                {
                    item.BackColor = menuStripBackColor;
                    item.ForeColor = menuStripForeColor;
                }
                // For ToolStripMenuItems, their DropDown items are handled by the DarkModeMenuRenderer (or default renderer)
                // No explicit ForeColor/BackColor needed here for dropdown items themselves.
            }
        }

        /// <summary>
        /// Checks the Windows Registry to determine if the system-wide "Apps" theme is set to dark mode.
        /// </summary>
        /// <returns>True if dark mode for apps is enabled in Windows settings; otherwise, false.</returns>
        public static bool IsWindowsDarkModeEnabled()
        {
            try
            {
                const string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string valueName = "AppsUseLightTheme";
                object? registryValue = Registry.GetValue(keyPath, valueName, 1); // Default to 1 (light mode) if value not found
                return registryValue is int intValue && intValue == 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading Windows theme setting from registry: {ex.Message}");
                return false; // Default to light mode on error
            }
        }
        #endregion Theme Management

        #region UI State Management (Status, Buttons, Controls)
        // This region includes methods for updating the state and appearance of various UI elements
        // like status labels, buttons, and input controls, often based on application state.

        /// <summary>
        /// Updates the text of the main status label on the status strip.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void UpdateStatusMain(string message)
        {
            if (_statusLabel == null) return;
            SafeToolStripItemUpdate(_statusLabel, () => { _statusLabel.Text = message; });
        }

        /// <summary>
        /// Gets the current text of the main status label.
        /// </summary>
        /// <returns>The current text of the status label.</returns>
        public string GetCurrentStatusMain()
        {
            if (_statusLabel == null) return string.Empty;
            string currentStatus = string.Empty;
            SafeToolStripItemUpdate(_statusLabel, () => { currentStatus = _statusLabel.Text ?? string.Empty; });
            return currentStatus;
        }

        /// <summary>
        /// Updates the text of the auto-run status label on the status strip.
        /// </summary>
        /// <param name="message">The message to display for auto-run status.</param>
        public void UpdateStatusRight(string message)
        {
            if (_autoRunStatusLabel == null) return;
            SafeToolStripItemUpdate(_autoRunStatusLabel, () => { _autoRunStatusLabel.Text = message; });
        }

        /// <summary>
        /// Enables or disables the main action buttons (Create Report, Process Email, 1-Click Process).
        /// </summary>
        /// <param name="enable">True to enable the buttons, false to disable them.</param>
        public void SetActionButtonsEnabled(bool enable)
        {
            if (_createReportButton != null) SafeControlUpdate(_createReportButton, () => { _createReportButton.Enabled = enable; });
            if (_processEmailButton != null) SafeControlUpdate(_processEmailButton, () => { _processEmailButton.Enabled = enable; });
            if (_oneClickProcessButton != null) SafeControlUpdate(_oneClickProcessButton, () => { _oneClickProcessButton.Enabled = enable; });
        }

        /// <summary>
        /// Enables or disables other input controls on the form.
        /// </summary>
        /// <param name="enable">True to enable the controls, false to disable them.</param>
        /// <param name="isFinancialYearVisible">Indicates if the financial year ComboBox should be considered for enabling/disabling.</param>
        public void SetOtherControlsEnabled(bool enable, bool isFinancialYearVisible)
        {
            if (_reportTypeComboBox != null) SafeControlUpdate(_reportTypeComboBox, () => { _reportTypeComboBox.Enabled = enable; });
            if (_startDatePicker != null) SafeControlUpdate(_startDatePicker, () => { _startDatePicker.Enabled = enable; });
            if (_endDatePicker != null) SafeControlUpdate(_endDatePicker, () => { _endDatePicker.Enabled = enable; });
            if (_financialYearComboBox != null) SafeControlUpdate(_financialYearComboBox, () => { _financialYearComboBox.Enabled = enable && isFinancialYearVisible; });
            if (_sendToFemiOnlyCheckBox != null) SafeControlUpdate(_sendToFemiOnlyCheckBox, () => { _sendToFemiOnlyCheckBox.Enabled = enable; });
            if (_skipEmailCheckBox != null) SafeControlUpdate(_skipEmailCheckBox, () => { _skipEmailCheckBox.Enabled = enable; });
        }

        /// <summary>
        /// Resets the UI to an initial or error state.
        /// </summary>
        /// <param name="button1Text">The text for the primary action button (e.g., "Create Report").</param>
        /// <param name="configValid">Indicates if the application configuration is valid.</param>
        /// <param name="rawReportExists">Indicates if a raw report file exists.</param>
        /// <param name="analysisExists">Indicates if an analysis file exists.</param>
        /// <param name="isDailySelected">Indicates if the "Daily" report type is selected.</param>
        /// <param name="isTimerEnabled">Indicates if the auto-run timer is enabled.</param>
        /// <param name="isDarkModeActive">Indicates if dark mode is currently active.</param>
        /// <param name="isFinalStatusForToday">Indicates if auto-run has reached a final status for the day.</param>
        /// <param name="currentAutoRunStatusText">The current text of the auto-run status label.</param>
        public void ResetUIOnError(string button1Text, bool configValid, bool rawReportExists, bool analysisExists, bool isDailySelected, bool isTimerEnabled, bool isDarkModeActive, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            if (_parentForm == null) return; // Should not happen if constructor ran
            SafeControlUpdate(_parentForm, () =>
            {
                Logger.LogDebug($"UIManager: Resetting UI state. Button 1 text (fallback): '{button1Text}'");

                // Set text and enabled state for primary action buttons based on config validity.
                if (_createReportButton != null)
                {
                    _createReportButton.Text = configValid ? button1Text : "Config Error";
                    _createReportButton.Enabled = configValid;
                }
                if (_processEmailButton != null)
                {
                    _processEmailButton.Text = "Process && Email";
                    _processEmailButton.Enabled = rawReportExists; // Only enable if raw report exists.
                }
                if (_oneClickProcessButton != null)
                {
                    _oneClickProcessButton.Enabled = configValid; // 1-Click button also depends on config.
                }
                if (_toggleAutoRunButton != null)
                {
                    _toggleAutoRunButton.Enabled = true; // Always re-enable the auto-run toggle.
                }


                SetOtherControlsEnabled(true, _financialYearComboBox?.Visible ?? false); // Re-enable other input controls.

                // Set visibility and enabled state for "View Report" and "View Analysis" buttons.
                if (_viewReportButton != null)
                {
                    _viewReportButton.Visible = rawReportExists;
                    _viewReportButton.Enabled = rawReportExists;
                }
                if (_viewAnalysisButton != null)
                {
                    _viewAnalysisButton.Visible = analysisExists;
                    _viewAnalysisButton.Enabled = analysisExists;
                }


                // Update the AutoRun UI elements.
                if (_toggleAutoRunButton != null && _autoRunStatusLabel != null) // Ensure these controls are managed by this UIManager instance
                {
                    UpdateAutoRunUI(isTimerEnabled, isFinalStatusForToday, isDarkModeActive, currentAutoRunStatusText);
                }

                // Logic to reset the main status label to "Ready" after a delay if it's showing a transient message.
                if (_statusLabel != null)
                {
                    string currentMainStatus = _statusLabel.Text ?? string.Empty;
                    if (currentMainStatus != "Ready" &&
                        !currentMainStatus.StartsWith("Auto Run:") &&
                        !currentMainStatus.StartsWith("Configuration O") && // Specific config messages
                        !currentMainStatus.StartsWith("Configuration E") && // Specific config messages
                        !currentMainStatus.Contains("Successfully") &&
                        !currentMainStatus.Contains("Sent"))
                    {
                        _ = Task.Delay(5000).ContinueWith(t => // Non-blocking delay.
                        {
                            SafeToolStripItemUpdate(_statusLabel, () =>
                            {
                                // Check if the status is still the same transient message before resetting.
                                if (_statusLabel.Text == currentMainStatus &&
                                    !(_statusLabel.Text ?? string.Empty).StartsWith("Auto Run:") &&
                                    !(_statusLabel.Text ?? string.Empty).StartsWith("Configuration") &&
                                    !(_statusLabel.Text ?? string.Empty).Contains("Successfully") &&
                                    !(_statusLabel.Text ?? string.Empty).Contains("Sent"))
                                {
                                    _statusLabel.Text = "Ready";
                                }
                            });
                        }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure update runs on UI thread.
                    }
                    else if (string.IsNullOrEmpty(currentMainStatus) || currentMainStatus.Contains("in progress") || currentMainStatus.Contains("Validating") || currentMainStatus.Contains("Starting"))
                    {
                        // If status is empty or clearly an in-progress message, reset to "Ready" immediately.
                        UpdateStatusMain("Ready");
                    }
                }
            });
        }

        /// <summary>
        /// Sets the UI state after a process has completed successfully.
        /// </summary>
        /// <param name="configValid">Indicates if the application configuration is valid.</param>
        /// <param name="isDailySelected">Indicates if the "Daily" report type is selected.</param>
        /// <param name="isTimerEnabled">Indicates if the auto-run timer is enabled.</param>
        /// <param name="isDarkModeActive">Indicates if dark mode is currently active.</param>
        /// <param name="isFinalStatusForToday">Indicates if auto-run has reached a final status for the day.</param>
        /// <param name="currentAutoRunStatusText">The current text of the auto-run status label.</param>
        public void SetUICompleted(bool configValid, bool isDailySelected, bool isTimerEnabled, bool isDarkModeActive, bool isFinalStatusForToday, string currentAutoRunStatusText)
        {
            UpdateStatusMain("Process Completed Successfully.");
            // Reset general UI state, Form1 will handle specific button texts.
            string rawPath = string.Empty, analysisPath = string.Empty;
            if (_viewReportButton != null) SafeControlUpdate(_viewReportButton, () => rawPath = _viewReportButton.Tag?.ToString() ?? "");
            if (_viewAnalysisButton != null) SafeControlUpdate(_viewAnalysisButton, () => analysisPath = _viewAnalysisButton.Tag?.ToString() ?? "");

            ResetUIOnError("Create Report", configValid, File.Exists(rawPath), File.Exists(analysisPath), isDailySelected, isTimerEnabled, isDarkModeActive, isFinalStatusForToday, currentAutoRunStatusText);

            // Ensure main action buttons are re-enabled based on config validity.
            if (_createReportButton != null) SafeControlUpdate(_createReportButton, () => _createReportButton.Enabled = configValid);
            if (_processEmailButton != null) SafeControlUpdate(_processEmailButton, () => _processEmailButton.Enabled = false); // Typically disabled after completion until new raw report.
            if (_oneClickProcessButton != null) SafeControlUpdate(_oneClickProcessButton, () => _oneClickProcessButton.Enabled = configValid);
        }

        /// <summary>
        /// Resets the state of action buttons when the report type changes.
        /// </summary>
        /// <param name="configValid">Indicates if the application configuration is valid.</param>
        public void ResetButtonStatesAfterTypeChange(bool configValid)
        {
            if (_parentForm == null) return;
            SafeControlUpdate(_parentForm, () =>
            {
                Logger.LogDebug("UIManager: Resetting button states due to report type change.");
                // Set default texts and enabled states. Form1 might override text for 1-click mode.
                if (_createReportButton != null)
                {
                    _createReportButton.Text = configValid ? "Create Report" : "Config Error";
                    _createReportButton.Enabled = configValid;
                }
                if (_processEmailButton != null)
                {
                    _processEmailButton.Text = "Process && Email";
                    _processEmailButton.Enabled = false; // Disabled until a raw report is created.
                }
                if (_oneClickProcessButton != null)
                {
                    _oneClickProcessButton.Text = configValid ? "Generate, Process && Email Report" : "Config Error";
                    _oneClickProcessButton.Enabled = configValid;
                }


                // Hide and disable view buttons as the context has changed.
                if (_viewReportButton != null)
                {
                    _viewReportButton.Visible = false;
                    _viewReportButton.Enabled = false;
                    _viewReportButton.Tag = null; // Clear file path tags.
                }
                if (_viewAnalysisButton != null)
                {
                    _viewAnalysisButton.Visible = false;
                    _viewAnalysisButton.Enabled = false;
                    _viewAnalysisButton.Tag = null;
                }


                UpdateStatusMain("Ready"); // Reset main status.
            });
        }

        /// <summary>
        /// Shows or hides the "View Analysis" button and sets its associated file path.
        /// </summary>
        /// <param name="show">True to show the button, false to hide it.</param>
        /// <param name="filePath">The path to the analysis file to be opened when the button is clicked.</param>
        public void ShowViewAnalysisButton(bool show, string? filePath = null)
        {
            if (_viewAnalysisButton == null) return;
            SafeControlUpdate(_viewAnalysisButton, () =>
            {
                _viewAnalysisButton.Visible = show;
                _viewAnalysisButton.Enabled = show;
                _viewAnalysisButton.Tag = filePath; // Store file path in Tag property.
            });
        }

        /// <summary>
        /// Shows or hides the "View Report" button and sets its associated file path.
        /// </summary>
        /// <param name="show">True to show the button, false to hide it.</param>
        /// <param name="filePath">The path to the raw report file to be opened when the button is clicked.</param>
        public void ShowViewReportButton(bool show, string? filePath = null)
        {
            if (_viewReportButton == null) return;
            SafeControlUpdate(_viewReportButton, () =>
            {
                _viewReportButton.Visible = show;
                _viewReportButton.Enabled = show;
                _viewReportButton.Tag = filePath; // Store file path in Tag property.
            });
        }
        #endregion UI State Management

        #region Auto Run UI Management
        // This region contains methods specifically for updating UI elements related to the auto-run feature.

        /// <summary>
        /// Sets the current auto-run hour used for UI display purposes.
        /// </summary>
        /// <param name="hour">The hour (0-23) for the auto-run check.</param>
        public void SetAutoRunHour(int hour)
        {
            if (hour >= 0 && hour <= 23) // Validate hour range.
            {
                _currentAutoRunHour = hour;
                Logger.LogDebug($"UIManager: Auto-run hour set to {_currentAutoRunHour}");
            }
            else
            {
                Logger.LogWarning($"UIManager: Invalid hour ({hour}) passed to SetAutoRunHour. Keeping current value ({_currentAutoRunHour}).");
            }
        }

        /// <summary>
        /// Updates the UI elements related to the auto-run feature (button text, color, status label).
        /// </summary>
        /// <param name="isTimerEnabled">Indicates if the auto-run timer is currently enabled.</param>
        /// <param name="isFinalStatusForToday">Indicates if auto-run has reached a final status for the day (e.g., completed, failed).</param>
        /// <param name="isDarkModeActive">Indicates if dark mode is currently active, for text color decisions.</param>
        /// <param name="statusText">Optional specific status text to display; otherwise, it's determined by other parameters.</param>
        public void UpdateAutoRunUI(bool isTimerEnabled, bool isFinalStatusForToday, bool isDarkModeActive, string statusText = "")
        {
            if (_toggleAutoRunButton == null || _autoRunStatusLabel == null || _toolTip == null) return;

            // Update the toggle button's text and appearance.
            SafeControlUpdate(_toggleAutoRunButton, () =>
            {
                if (_toggleAutoRunButton.IsDisposed) return;

                _toggleAutoRunButton.Text = isTimerEnabled ? $"Disable Daily Auto Run @ {_currentAutoRunHour}:00"
                                                   : $"Enable Daily Auto Run @ {_currentAutoRunHour}:00";
                _toggleAutoRunButton.BackColor = isTimerEnabled ? AutoRunEnabledColor : AutoRunDisabledColor;
                _toggleAutoRunButton.ForeColor = AutoRunButtonForeColor; // Consistent text color.
                _toolTip.SetToolTip(_toggleAutoRunButton, $"Enable or disable the automated daily report generation. The report runs around {_currentAutoRunHour}:00 for the previous workday.");
            });

            // Update the auto-run status label.
            SafeToolStripItemUpdate(_autoRunStatusLabel, () =>
            {
                if (_autoRunStatusLabel.IsDisposed) return;

                string textToShow = statusText; // Use provided status text if available.
                if (string.IsNullOrEmpty(textToShow)) // Otherwise, determine status text.
                {
                    textToShow = isTimerEnabled
                        ? (isFinalStatusForToday ? (_autoRunStatusLabel.Text ?? $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)") // If final status, keep it.
                                                 : $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)")
                        : (isFinalStatusForToday ? (_autoRunStatusLabel.Text ?? "Auto Run: Disabled") // If final status, keep it.
                                                 : "Auto Run: Disabled");
                }
                // If enabled, not final, and no specific error/time in text, ensure next check time is shown.
                else if (isTimerEnabled && !isFinalStatusForToday && !textToShow.Contains("FAILED") && !textToShow.Contains("ERROR") && !textToShow.Contains(":"))
                {
                    textToShow = $"Auto Run: Enabled (Next check ~{_currentAutoRunHour}:00)";
                }

                _autoRunStatusLabel.Text = textToShow;

                // Set text color based on status and current theme.
                if (textToShow.Contains("FAILED") || textToShow.Contains("ERROR"))
                {
                    _autoRunStatusLabel.ForeColor = Color.Red; // Error color.
                }
                else if (isTimerEnabled && !isFinalStatusForToday)
                { // Timer enabled and not yet run/failed today.
                    _autoRunStatusLabel.ForeColor = Color.Green; // Success/active color.
                }
                else
                { // Default color based on theme, using distinct StatusStrip foreground colors.
                    _autoRunStatusLabel.ForeColor = isDarkModeActive ? DM_StatusStrip_Distinct_ForeColor : LM_StatusStrip_Distinct_ForeColor;
                }
            });
        }

        /// <summary>
        /// Disables primary UI controls during automated report execution.
        /// </summary>
        public void DisableControlsForAutoRun()
        {
            Logger.LogDebug("Disabling controls for Auto Run.");
            SetActionButtonsEnabled(false);
            SetOtherControlsEnabled(false, _financialYearComboBox?.Visible ?? false);
            if (_toggleAutoRunButton != null) SafeControlUpdate(_toggleAutoRunButton, () => _toggleAutoRunButton.Enabled = false);
            if (_viewReportButton != null) SafeControlUpdate(_viewReportButton, () => _viewReportButton.Enabled = false);
            if (_viewAnalysisButton != null) SafeControlUpdate(_viewAnalysisButton, () => _viewAnalysisButton.Enabled = false);
            UpdateProgress("Auto Run in progress..."); // Update main status.
        }
        #endregion Auto Run UI Management

        #region Progress Reporting (Status Label)
        // This region handles updating the main status label for progress reporting.
        // ProgressBar functionality is not included in this version.

        /// <summary>
        /// Updates the main status label with a progress message.
        /// </summary>
        /// <param name="message">The progress message to display.</param>
        public void UpdateProgress(string message)
        {
            if (_statusLabel == null) return;
            SafeToolStripItemUpdate(_statusLabel, () => { _statusLabel.Text = message; });
        }

        /// <summary>
        /// Updates the main status label with a progress message from a <see cref="ProgressReport"/> object.
        /// Assumes ProgressReport is a custom class/struct with a 'Message' property.
        /// </summary>
        /// <param name="report">The progress report object containing the message.</param>
        public void UpdateProgress(ProgressReport report) // Replace ProgressReport with actual type if different
        {
            if (_statusLabel == null) return;
            SafeToolStripItemUpdate(_statusLabel, () => { _statusLabel.Text = report.Message; });
        }
        #endregion Progress Reporting

        #region ToolTip Management
        // This region provides utility methods for setting ToolTip text on controls and ToolStripItems.

        /// <summary>
        /// Sets the ToolTip text for a specified <see cref="Control"/>.
        /// </summary>
        /// <param name="control">The control to set the ToolTip for.</param>
        /// <param name="tipText">The ToolTip text.</param>
        public void SetToolTip(Control control, string tipText)
        {
            if (_toolTip == null) return;
            SafeControlUpdate(control, () => { _toolTip.SetToolTip(control, tipText); });
        }

        /// <summary>
        /// Sets the ToolTip text for a specified <see cref="ToolStripItem"/>.
        /// Note: ToolStripItems use their own ToolTipText property, not the ToolTip component directly.
        /// </summary>
        /// <param name="item">The ToolStripItem to set the ToolTip for.</param>
        /// <param name="tipText">The ToolTip text.</param>
        public void SetToolTip(ToolStripItem item, string tipText)
        {
            SafeToolStripItemUpdate(item, () => { item.ToolTipText = tipText; });
        }
        #endregion ToolTip Management

        #region Safe UI Update Utilities
        // This region contains static utility methods for safely updating UI elements from any thread.

        /// <summary>
        /// Safely updates a standard <see cref="Control"/>'s property or state by executing an action,
        /// marshalling the call to the UI thread if necessary.
        /// </summary>
        /// <param name="ctrl">The control to update.</param>
        /// <param name="action">The action to perform on the control.</param>
        public static void SafeControlUpdate(Control ctrl, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (ctrl == null || ctrl.IsDisposed) return; // Exit if control is null or disposed.

            // Check if control handle is created and control is not disposing.
            if (ctrl.IsHandleCreated && !ctrl.Disposing)
            {
                if (ctrl.InvokeRequired) // If called from a non-UI thread.
                {
                    try
                    {
                        ctrl.BeginInvoke(action); // Asynchronously invoke the action on the UI thread.
                    }
                    catch (ObjectDisposedException) { /* Control was disposed before action executed. */ }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Invoke") || ex.Message.Contains("Handle"))
                    {
                        // Handle cases where Invoke/BeginInvoke fails due to handle issues.
                        Logger.LogWarning($"SafeControlUpdate ignored invoke/handle error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Unexpected error during SafeControlUpdate Invoke/BeginInvoke: {ex}");
                    }
                }
                else // Called from the UI thread.
                {
                    try
                    {
                        action(); // Execute action directly.
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Unexpected error during SafeControlUpdate direct action: {ex}");
                    }
                }
            }
            else // Handle not created or control is disposing.
            {
                if (!ctrl.IsHandleCreated) Logger.LogTrace($"SafeControlUpdate skipped for control '{ctrl.Name}' as handle is not created.");
                if (ctrl.Disposing) Logger.LogTrace($"SafeControlUpdate skipped for control '{ctrl.Name}' as it is disposing.");
            }
        }

        /// <summary>
        /// Safely updates a <see cref="ToolStripItem"/>'s property or state by executing an action,
        /// marshalling the call to the UI thread of its owner <see cref="ToolStrip"/> if necessary.
        /// </summary>
        /// <param name="item">The ToolStripItem to update.</param>
        /// <param name="action">The action to perform on the item.</param>
        public static void SafeToolStripItemUpdate(ToolStripItem item, Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            if (item == null || item.IsDisposed) return; // Exit if item is null or disposed.

            ToolStrip? owner = item.Owner; // Get the owner ToolStrip.
            // Check if owner exists, its handle is created, and it's not disposed/disposing.
            if (owner != null && owner.IsHandleCreated && !owner.IsDisposed && !owner.Disposing)
            {
                if (owner.InvokeRequired) // If called from a non-UI thread (relative to the owner ToolStrip).
                {
                    try
                    {
                        owner.BeginInvoke(action); // Asynchronously invoke action on the owner's UI thread.
                    }
                    catch (ObjectDisposedException) { /* Owner or item was disposed. */ }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Invoke") || ex.Message.Contains("Handle"))
                    {
                        Logger.LogWarning($"SafeToolStripItemUpdate ignored invoke/handle error: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Unexpected error during SafeToolStripItemUpdate Invoke/BeginInvoke: {ex}");
                    }
                }
                else // Called from the owner's UI thread.
                {
                    try
                    {
                        action(); // Execute action directly.
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Unexpected error during SafeToolStripItemUpdate direct action: {ex}");
                    }
                }
            }
            else // Owner is null, handle not created, or owner is disposing.
            {
                string ownerName = owner?.Name ?? "null";
                if (owner == null) Logger.LogWarning($"SafeToolStripItemUpdate skipped for item '{item.Name}' as owner is null.");
                else if (!owner.IsHandleCreated) Logger.LogTrace($"SafeToolStripItemUpdate skipped for item '{item.Name}' on '{ownerName}' as owner handle not created.");
                else if (owner.IsDisposed || owner.Disposing) Logger.LogTrace($"SafeToolStripItemUpdate skipped for item '{item.Name}' on '{ownerName}' as owner is disposed/disposing.");
            }
        }
        #endregion Safe UI Update Utilities

        #region Windows API Helpers for Theming
        // This region contains private static helper methods that interact with Windows APIs
        // for advanced theming capabilities, such as dark mode title bars.

        /// <summary>
        /// Attempts to apply the immersive dark mode attribute to the specified window handle.
        /// Renamed to Internal to distinguish from the public static method if one were added for direct calls.
        /// </summary>
        /// <param name="handle">The window handle (HWND).</param>
        /// <param name="enabled">True to enable dark mode, false to disable it.</param>
        /// <returns>True if the attribute was set successfully; otherwise, false.</returns>
        private static bool UseImmersiveDarkModeInternal(IntPtr handle, bool enabled) // Renamed for clarity
        {
            if (handle == IntPtr.Zero) // Validate window handle.
            {
                Logger.LogError("UseImmersiveDarkModeInternal: Window handle is Zero. Cannot set attribute.");
                return false;
            }

            int attribute; // The DWM attribute constant to use.
            Version osVersion = Environment.OSVersion.Version; // Get current OS version.

            Logger.LogDebug($"UseImmersiveDarkModeInternal: OS Version: Major={osVersion.Major}, Minor={osVersion.Minor}, Build={osVersion.Build}");

            // Determine the correct DWM attribute based on the Windows build number.
            if (osVersion.Major >= 10 && osVersion.Build >= 19041)
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                Logger.LogDebug($"UseImmersiveDarkModeInternal: Using attribute DWMWA_USE_IMMERSIVE_DARK_MODE (20) for OS Build {osVersion.Build}.");
            }
            else if (osVersion.Major >= 10 && osVersion.Build >= 18362)
            {
                attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903;
                Logger.LogDebug($"UseImmersiveDarkModeInternal: Using attribute DWMWA_USE_IMMERSIVE_DARK_MODE_WINDOWS_10_1903 (19) for OS Build {osVersion.Build}.");
            }
            else
            {
                Logger.LogWarning($"UseImmersiveDarkModeInternal: OS Version (Build {osVersion.Build}) does not support DWMWA_USE_IMMERSIVE_DARK_MODE attributes 19 or 20. Title bar theming may not work.");
                return false;
            }

            int useImmersiveDarkMode = enabled ? 1 : 0;
            Logger.LogDebug($"UseImmersiveDarkModeInternal: Calling DwmSetWindowAttribute with handle {handle}, attribute {attribute}, value {useImmersiveDarkMode}");

            int result = DwmSetWindowAttribute(handle, attribute, ref useImmersiveDarkMode, sizeof(int));

            if (result == 0)
            {
                Logger.LogInfo($"UseImmersiveDarkModeInternal: DwmSetWindowAttribute successful for enabled={enabled}.");
                return true;
            }
            else
            {
                Logger.LogError($"UseImmersiveDarkModeInternal: DwmSetWindowAttribute FAILED with result code 0x{result:X8} for enabled={enabled}. Error: {new System.ComponentModel.Win32Exception(result).Message}");
                return false;
            }
        }
        #endregion Windows API Helpers for Theming
    }
}