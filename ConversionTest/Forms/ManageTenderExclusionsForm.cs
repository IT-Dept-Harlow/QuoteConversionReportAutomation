// QuoteConversionReportAutomation/Forms/ManageTenderExclusionsForm.cs

#region Using Directives

// System-related namespaces for core functionalities.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// Third-party namespaces for external libraries.
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Project-specific namespaces for application components.
using QuoteConversionReportAutomation.Configuration;
using QuoteConversionReportAutomation.Helpers;
using QuoteConversionReportAutomation.Managers;
using QuoteConversionReportAutomation.Services.Interfaces;
using QuoteConversionReportAutomation.Services.Logging;
using QuoteConversionReportAutomation.Theming;

#endregion

namespace QuoteConversionReportAutomation.Forms
{
    #region Class Definition
    /// <summary>
    /// Provides a user interface for managing the list of tender account posting codes
    /// that are to be excluded from report analysis.
    /// </summary>
    public partial class ManageTenderExclusionsForm : Form
    {
        #region Fields

        /// <summary>
        /// Provides read-only access to the application's configuration settings.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// The service responsible for providing the path to the appsettings.json file.
        /// </summary>
        private readonly IReportPathService _reportPathService;

        /// <summary>
        /// A lock object to ensure thread-safe read/write operations on the appsettings.json file.
        /// </summary>
        private static readonly object s_appSettingsFileLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// Initialises a new instance of the <see cref="ManageTenderExclusionsForm"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration settings.</param>
        /// <param name="reportPathService">The service for resolving application paths.</param>
        public ManageTenderExclusionsForm(IConfiguration configuration, IReportPathService reportPathService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _reportPathService = reportPathService ?? throw new ArgumentNullException(nameof(reportPathService));

            // Standard Windows Forms initialisation from the .Designer.cs file.
            InitializeComponent();
        }

        #endregion

        #region Form Events

        /// <summary>
        /// Handles the Load event of the form. This is called once when the form is first displayed.
        /// It applies the visual theme and loads the current exclusion list into the ListView.
        /// </summary>
        private void ManageTenderExclusionsForm_Load(object sender, EventArgs e)
        {
            // Apply the current theme (light or dark) to this form and its controls.
            UIManager.ApplyThemeToExternalForm(this, ThemeSettings.IsCurrentlyDark());
            ApplyChildControlTheme();

            // Load the existing list of codes from the configuration.
            LoadExclusionList();
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Handles the Click event for the "Add" button.
        /// Adds the posting code from the textbox to the configuration and refreshes the list.
        /// </summary>
        private void btnAdd_Click(object sender, EventArgs e)
        {
            string newCode = txtPostingCode.Text.Trim();

            // Validate that the user has entered a code.
            if (string.IsNullOrWhiteSpace(newCode))
            {
                FlexibleMessageBox.Show(this, "Please enter a posting code to add.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPostingCode.Focus();
                return;
            }

            // Get the current list and check for duplicates.
            var currentCodes = GetCurrentExclusionList();
            if (currentCodes.Contains(newCode, StringComparer.OrdinalIgnoreCase))
            {
                FlexibleMessageBox.Show(this, $"The posting code '{newCode}' is already in the exclusion list.", "Duplicate Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Add the new code and save the updated list.
            currentCodes.Add(newCode);
            if (SaveChanges(currentCodes))
            {
                LoadExclusionList(); // Refresh the list view.
                txtPostingCode.Clear();
                txtPostingCode.Focus();
            }
        }

        /// <summary>
        /// Handles the Click event for the "Remove Selected" button.
        /// Removes the selected posting code from the configuration and refreshes the list.
        /// </summary>
        private void btnRemove_Click(object sender, EventArgs e)
        {
            // Check if an item is selected in the list.
            if (lstExclusionCodes.SelectedItems.Count == 0)
            {
                FlexibleMessageBox.Show(this, "Please select a posting code from the list to remove.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string codeToRemove = lstExclusionCodes.SelectedItems[0].Text;

            // Confirm the removal with the user.
            if (FlexibleMessageBox.Show(this, $"Are you sure you want to remove the posting code '{codeToRemove}' from the exclusion list?",
                                        "Confirm Removal", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var currentCodes = GetCurrentExclusionList();
                currentCodes.RemoveAll(code => code.Equals(codeToRemove, StringComparison.OrdinalIgnoreCase));

                if (SaveChanges(currentCodes))
                {
                    LoadExclusionList(); // Refresh the list view.
                }
            }
        }

        /// <summary>
        /// Handles the Click event for the "Close" button.
        /// Closes the form.
        /// </summary>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Loads the current list of tender exclusion codes from the configuration and populates the ListView.
        /// </summary>
        private void LoadExclusionList()
        {
            lstExclusionCodes.Items.Clear();
            var codes = GetCurrentExclusionList();

            // Sort the codes alphabetically before displaying them.
            foreach (var code in codes.OrderBy(c => c))
            {
                lstExclusionCodes.Items.Add(new ListViewItem(code));
            }
        }

        /// <summary>
        /// Retrieves the current list of tender exclusion posting codes from the application configuration.
        /// </summary>
        /// <returns>A new list containing the string values of the posting codes.</returns>
        private List<string> GetCurrentExclusionList()
        {
            // Use the GetSection method with the configured key to retrieve the list.
            // The .Get<List<string>>() extension method automatically deserialises the JSON array.
            return _configuration.GetSection(AppConfigKeys.OperationalParameters.TenderAccountPostingCodesToExclude)
                                 .Get<List<string>>() ?? new List<string>();
        }

        /// <summary>
        /// Saves the provided list of exclusion codes back to the appsettings.json file.
        /// </summary>
        /// <param name="codesToSave">The complete list of codes to be written to the configuration file.</param>
        /// <returns>True if the save operation was successful; otherwise, false.</returns>
        private bool SaveChanges(List<string> codesToSave)
        {
            try
            {
                string appSettingsPath = Path.Combine(_reportPathService.AppSettingsDirectory, "appsettings.json");
                if (!File.Exists(appSettingsPath))
                {
                    throw new FileNotFoundException("appsettings.json could not be found.", appSettingsPath);
                }

                string currentJson;
                // Use a lock to prevent file access conflicts.
                lock (s_appSettingsFileLock)
                {
                    currentJson = File.ReadAllText(appSettingsPath);
                }

                var rootObject = JObject.Parse(currentJson);

                // Navigate to the correct location in the JSON structure.
                var operationalParams = rootObject.SelectToken("OperationalParameters") as JObject;
                if (operationalParams == null)
                {
                    operationalParams = new JObject();
                    rootObject["OperationalParameters"] = operationalParams;
                }

                // Create a JArray from the list of codes and assign it to the correct key.
                var codesArray = new JArray(codesToSave.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(c => c));
                operationalParams[AppConfigKeys.OperationalParameters.TenderAccountPostingCodesToExclude.Split(':').Last()] = codesArray;

                string updatedJson = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

                lock (s_appSettingsFileLock)
                {
                    File.WriteAllText(appSettingsPath, updatedJson);
                }

                // IMPORTANT: Tell the application's configuration to reload its values from the modified file.
                if (_configuration is IConfigurationRoot configRoot)
                {
                    configRoot.Reload();
                    Logger.LogInfo("Saved new tender exclusion list and reloaded configuration.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save tender exclusion list to appsettings.json.", ex);
                FlexibleMessageBox.Show(this, $"An error occurred while saving the changes:\n\n{ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Applies the current theme from ThemeSettings specifically to the child controls of this form.
        /// </summary>
        private void ApplyChildControlTheme()
        {
            if (!ThemeSettings.EnableCustomTheming) return;

            bool isDarkMode = ThemeSettings.IsCurrentlyDark();
            ThemePalette palette = ThemeSettings.CurrentPalette;

            // This recursive helper method applies the theme to all controls within a given parent.
            UpdateControlThemeRecursive(this, palette, isDarkMode);
        }

        /// <summary>
        /// Recursive helper to apply theme colours to child controls using the provided ThemePalette.
        /// </summary>
        private void UpdateControlThemeRecursive(Control parentControl, ThemePalette palette, bool isDarkMode)
        {
            if (parentControl is GroupBox)
            {
                parentControl.BackColor = this.BackColor;
            }

            foreach (Control control in parentControl.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = palette.ButtonBackColor;
                    button.ForeColor = palette.ButtonForeColor;
                    button.FlatStyle = FlatStyle.Flat;
                    button.FlatAppearance.BorderColor = palette.ButtonBorderColor;
                    button.FlatAppearance.BorderSize = 1;
                }
                else if (control is TextBox tb)
                {
                    tb.BackColor = palette.ControlBackColor;
                    tb.ForeColor = palette.ControlForeColor;
                    tb.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is ListView lv)
                {
                    lv.BackColor = palette.ControlBackColor;
                    lv.ForeColor = palette.ControlForeColor;
                    lv.BorderStyle = isDarkMode ? BorderStyle.FixedSingle : BorderStyle.Fixed3D;
                }
                else if (control is Label label)
                {
                    label.BackColor = Color.Transparent;
                    label.ForeColor = palette.LabelForeColor;
                }
                else if (control is GroupBox gb)
                {
                    gb.ForeColor = palette.GroupBoxForeColor;
                    UpdateControlThemeRecursive(gb, palette, isDarkMode);
                }
            }
        }

        #endregion
    }
    #endregion
}
