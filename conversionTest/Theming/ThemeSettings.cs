// ThemeSettings.cs
// Provides a centralized definition for UI color palettes and theming control.
// Corrected internal call to IsWindowsDarkModeEnabled.

#region Using Directives
using System.Drawing;
using System.Windows.Forms; // For SystemColors
using Microsoft.Win32; // For Registry access
using System; // For Exception, Environment
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Theming
{
    /// <summary>
    /// Defines the available theme modes for the application.
    /// </summary>
    public enum ApplicationThemeMode
    {
        Light,
        Dark,
        System
    }

    /// <summary>
    /// Represents a collection of theme-specific colors.
    /// </summary>
    public struct ThemePalette
    {
        // General Form & Text
        public Color FormBackColor { get; init; }
        public Color FormForeColor { get; init; }
        public Color DisabledControlBackColor { get; init; }

        // Input Controls
        public Color ControlBackColor { get; init; }
        public Color ControlForeColor { get; init; }
        public Color ControlBorderColor { get; init; }

        // Buttons
        public Color ButtonBackColor { get; init; }
        public Color ButtonForeColor { get; init; }
        public Color ButtonBorderColor { get; init; }

        // Labels & GroupBox Text
        public Color LabelForeColor { get; init; }
        public Color GroupBoxForeColor { get; init; }

        // MenuStrip & ToolStripDropDownMenu
        public Color MenuStripBackColor { get; init; }
        public Color MenuStripForeColor { get; init; }
        public Color MenuDropDownBackColor { get; init; }
        public Color MenuItemSelectedColor { get; init; }
        public Color MenuItemPressedColor { get; init; }
        public Color MenuBorderColor { get; init; }
        public Color MenuSeparatorColor { get; init; }

        // StatusStrip
        public Color StatusStripBackColor { get; init; }
        public Color StatusStripForeColor { get; init; }

        // DataGridView
        public Color DataGridViewHeaderBackColor { get; init; }
        public Color DataGridViewHeaderForeColor { get; init; }
        public Color DataGridViewCellBackColor { get; init; }
        public Color DataGridViewCellForeColor { get; init; }
        public Color DataGridViewSelectionBackColor { get; init; }
        public Color DataGridViewSelectionForeColor { get; init; }
        public Color DataGridViewGridColor { get; init; }
        public Color DataGridViewRowHeaderBackColor { get; init; }

        // Specific UI Elements
        public Color EmphasisColor { get; init; }
        public Color CodeSnippetColor { get; init; }
        public Color AccentColorWarning { get; init; }
        public Color AutoRunEnabledButtonBackColor { get; init; }
        public Color AutoRunDisabledButtonBackColor { get; init; }
        public Color AutoRunButtonForeColor { get; init; }
        public Color SuccessStatusColor { get; init; }
        public Color ErrorStatusColor { get; init; }
    }

    /// <summary>
    /// Provides static access to theme settings and color palettes for the application.
    /// </summary>
    public static class ThemeSettings
    {
        public static bool EnableCustomTheming { get; set; } = true;
        public static ApplicationThemeMode CurrentThemeMode { get; set; } = ApplicationThemeMode.Light;


        // --- Define Color Palettes ---
        public static ThemePalette LightPalette { get; } = new ThemePalette
        {
            FormBackColor = SystemColors.Control,
            FormForeColor = SystemColors.ControlText,
            DisabledControlBackColor = SystemColors.ControlLight,
            ControlBackColor = SystemColors.Window,
            ControlForeColor = SystemColors.WindowText,
            ControlBorderColor = SystemColors.ControlDark,
            ButtonBackColor = SystemColors.Control,
            ButtonForeColor = SystemColors.ControlText,
            ButtonBorderColor = SystemColors.ControlDarkDark,
            LabelForeColor = SystemColors.ControlText,
            GroupBoxForeColor = SystemColors.ControlText,
            MenuStripBackColor = Color.FromArgb(220, 220, 225),
            MenuStripForeColor = SystemColors.MenuText,
            MenuDropDownBackColor = SystemColors.Menu,
            MenuItemSelectedColor = SystemColors.Highlight,
            MenuItemPressedColor = SystemColors.MenuHighlight,
            MenuBorderColor = SystemColors.ControlDark,
            MenuSeparatorColor = SystemColors.ControlDark,
            StatusStripBackColor = Color.FromArgb(210, 210, 215),
            StatusStripForeColor = SystemColors.ControlText,
            DataGridViewHeaderBackColor = SystemColors.Control,
            DataGridViewHeaderForeColor = SystemColors.WindowText,
            DataGridViewCellBackColor = SystemColors.Window,
            DataGridViewCellForeColor = SystemColors.WindowText,
            DataGridViewSelectionBackColor = SystemColors.Highlight,
            DataGridViewSelectionForeColor = SystemColors.HighlightText,
            DataGridViewGridColor = SystemColors.ControlDark,
            DataGridViewRowHeaderBackColor = SystemColors.Control,
            EmphasisColor = Color.SaddleBrown,
            CodeSnippetColor = Color.FromArgb(40, 100, 40),
            AccentColorWarning = Color.FromArgb(200, 0, 0),
            AutoRunEnabledButtonBackColor = Color.LightGreen,
            AutoRunDisabledButtonBackColor = Color.LightCoral,
            AutoRunButtonForeColor = Color.Black,
            SuccessStatusColor = Color.Green,
            ErrorStatusColor = Color.Red
        };

        public static ThemePalette DarkPalette { get; } = new ThemePalette
        {
            FormBackColor = Color.FromArgb(45, 45, 48),
            FormForeColor = Color.WhiteSmoke,
            DisabledControlBackColor = Color.FromArgb(50, 50, 50),
            ControlBackColor = Color.FromArgb(60, 60, 63),
            ControlForeColor = Color.WhiteSmoke,
            ControlBorderColor = Color.FromArgb(90, 90, 90),
            ButtonBackColor = Color.FromArgb(80, 80, 80),
            ButtonForeColor = Color.WhiteSmoke,
            ButtonBorderColor = Color.FromArgb(100, 100, 100),
            LabelForeColor = Color.FromArgb(200, 200, 200),
            GroupBoxForeColor = Color.WhiteSmoke,
            MenuStripBackColor = Color.FromArgb(55, 55, 58),
            MenuStripForeColor = Color.FromArgb(220, 220, 220),
            MenuDropDownBackColor = Color.FromArgb(45, 45, 48),
            MenuItemSelectedColor = Color.FromArgb(85, 85, 95),
            MenuItemPressedColor = Color.FromArgb(100, 100, 110),
            MenuBorderColor = Color.FromArgb(85, 85, 90),
            MenuSeparatorColor = Color.FromArgb(85, 85, 90),
            StatusStripBackColor = Color.FromArgb(35, 35, 38),
            StatusStripForeColor = Color.FromArgb(190, 190, 190),
            DataGridViewHeaderBackColor = Color.FromArgb(55, 55, 58),
            DataGridViewHeaderForeColor = Color.WhiteSmoke,
            DataGridViewCellBackColor = Color.FromArgb(60, 60, 63),
            DataGridViewCellForeColor = Color.WhiteSmoke,
            DataGridViewSelectionBackColor = Color.FromArgb(0, 120, 215),
            DataGridViewSelectionForeColor = Color.White,
            DataGridViewGridColor = Color.FromArgb(80, 80, 80),
            DataGridViewRowHeaderBackColor = Color.FromArgb(55, 55, 58),
            EmphasisColor = Color.FromArgb(255, 210, 100),
            CodeSnippetColor = Color.FromArgb(180, 210, 180),
            AccentColorWarning = Color.FromArgb(255, 160, 160),
            AutoRunEnabledButtonBackColor = Color.DarkSeaGreen,
            AutoRunDisabledButtonBackColor = Color.IndianRed,
            AutoRunButtonForeColor = Color.Black,
            SuccessStatusColor = Color.LightGreen,
            ErrorStatusColor = Color.LightCoral
        };

        public static ThemePalette CurrentPalette
        {
            get
            {
                if (!EnableCustomTheming)
                {
                    // When custom theming is disabled, UIManager should primarily use SystemColors.
                    // This palette provides a fallback if direct SystemColors access isn't feasible in some styling logic.
                    // For a true system theme, many properties here would defer to SystemColors.
                    return LightPalette; // Or define a dedicated "SystemPalette" that uses SystemColors for all properties.
                }
                return CurrentThemeMode == ApplicationThemeMode.Dark ? DarkPalette : LightPalette;
            }
        }

        /// <summary>
        /// Checks the Windows Registry to determine if the system-wide "Apps" theme is set to dark mode.
        /// </summary>
        /// <returns>True if system apps use light theme is 0 (dark mode), false otherwise.</returns>
        public static bool IsWindowsDarkModeEnabled()
        {
            try
            {
                const string keyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                const string valueName = "AppsUseLightTheme";
                object? registryValue = Registry.GetValue(keyPath, valueName, 1);
                return registryValue is int intValue && intValue == 0;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error reading Windows theme setting from registry: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines if the application should currently be in dark mode visuals,
        /// considering both the `EnableCustomTheming` flag and the `CurrentThemeMode`.
        /// If custom theming is off, this reflects the actual OS dark mode state for title bar theming.
        /// </summary>
        public static bool IsCurrentlyDark()
        {
            if (!EnableCustomTheming)
            {
                return IsWindowsDarkModeEnabled(); // Reflect OS state for title bar if custom theming is off
            }
            return CurrentThemeMode == ApplicationThemeMode.Dark;
        }

        /// <summary>
        /// Updates the CurrentThemeMode based on the system's dark mode setting.
        /// This method should be called at startup and potentially when the system theme changes,
        /// if the application is set to follow the system theme automatically.
        /// </summary>
        public static void SyncThemeWithSystem()
        {
            // Call the local IsWindowsDarkModeEnabled method
            if (IsWindowsDarkModeEnabled())
            {
                CurrentThemeMode = ApplicationThemeMode.Dark;
            }
            else
            {
                CurrentThemeMode = ApplicationThemeMode.Light;
            }
            Logger.LogInfo($"ThemeSettings.SyncThemeWithSystem: Set CurrentThemeMode to {CurrentThemeMode}");
        }
    }
}