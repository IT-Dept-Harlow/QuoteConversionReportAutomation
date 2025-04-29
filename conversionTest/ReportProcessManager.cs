// C# 10+ Features
using conversionTest;

namespace QuoteConversionReportAutomation
{
    // --- Using Statements ---
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using JR.Utils.GUI.Forms; // For FlexibleMessageBox

    /// <summary>
    /// Manages the lifecycle (checking, launching, terminating) of the external
    /// Crystal Report Wrapper process.
    /// </summary>
    public class ReportProcessManager
    {
        #region Fields

        /// <summary>
        /// The full path to the Crystal Report Wrapper executable.
        /// </summary>
        private readonly string _wrapperExePath;

        /// <summary>
        /// The process name of the wrapper executable (without the extension).
        /// </summary>
        private readonly string _wrapperProcessName;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ReportProcessManager class.
        /// </summary>
        /// <param name="wrapperExePath">The full file path to the wrapper executable.</param>
        /// <exception cref="ArgumentException">Thrown if wrapperExePath is null, empty, or invalid.</exception>
        public ReportProcessManager(string wrapperExePath)
        {
            if (string.IsNullOrWhiteSpace(wrapperExePath))
            {
                throw new ArgumentException("Wrapper executable path cannot be null or empty.", nameof(wrapperExePath));
            }

            // Basic validation - more robust checks happen in methods using the path
            _wrapperExePath = Path.GetFullPath(wrapperExePath); // Normalize the path
            _wrapperProcessName = Path.GetFileNameWithoutExtension(_wrapperExePath);

            if (string.IsNullOrEmpty(_wrapperProcessName))
            {
                throw new ArgumentException("Could not determine process name from the provided wrapper executable path.", nameof(wrapperExePath));
            }

            Logger.LogDebug($"ReportProcessManager initialized. Wrapper Path: '{_wrapperExePath}', Process Name: '{_wrapperProcessName}'");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Asynchronously ensures the Crystal Report Wrapper process is running, launching it if necessary.
        /// </summary>
        /// <param name="progressReporter">Optional progress reporter for status updates (e.g., "Starting report service...").</param>
        /// <param name="cancellationToken">Token to allow cancellation.</param>
        /// <returns>True if the wrapper is running or was successfully launched, false otherwise.</returns>
        public async Task<bool> EnsureWrapperIsRunningAsync(IProgress<string>? progressReporter = null, CancellationToken cancellationToken = default)
        {
            // Check if already running
            if (IsWrapperRunning())
            {
                Logger.LogInfo($"Wrapper process '{_wrapperProcessName}' is already running.");
                return true;
            }

            // If not running, attempt to launch
            Logger.LogWarning($"Wrapper process '{_wrapperProcessName}' not found. Attempting to launch...");
            progressReporter?.Report("Starting report service..."); // Report progress if reporter provided

            try
            {
                // Launch the process (run synchronous LaunchWrapper on background thread)
                await Task.Run(() => LaunchWrapper(), cancellationToken);

                // Wait briefly for the process to initialize its named pipe server
                await Task.Delay(3000, cancellationToken); // 3 seconds grace period

                // Check again if it's running after the launch attempt
                if (IsWrapperRunning())
                {
                    Logger.LogInfo($"Wrapper process '{_wrapperProcessName}' appears to be running after launch.");
                    progressReporter?.Report("Report service started.");
                    return true;
                }
                else // Launch attempt failed or process terminated quickly
                {
                    Logger.LogError($"Wrapper process '{_wrapperProcessName}' did not start successfully or terminated unexpectedly after launch attempt.");
                    progressReporter?.Report("Error: Failed to start report service.");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Operation cancelled during wrapper launch check.");
                progressReporter?.Report("Operation cancelled.");
                return false;
            }
            catch (FileNotFoundException fnfEx) // Catch specific error from LaunchWrapper
            {
                Logger.LogError($"Failed to launch wrapper: {fnfEx.Message}", fnfEx);
                FlexibleMessageBox.Show($"Could not start the required report service ({_wrapperProcessName}).\n" +
                                $"File not found: {fnfEx.FileName}\n\n" +
                                $"Please check the path in configuration and ensure the application exists.",
                                "Wrapper Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressReporter?.Report("Error: Report service executable not found.");
                return false;
            }
            catch (Exception launchEx) // Catch other errors during LaunchWrapper or Task.Run
            {
                Logger.LogError($"Failed to launch the Crystal Report Wrapper ('{_wrapperExePath}'): {launchEx.Message}", launchEx);
                FlexibleMessageBox.Show($"Could not start the required report service ({_wrapperProcessName}).\n" +
                                $"Please check the path in configuration ('{_wrapperExePath}') and ensure the application exists and has permissions to run.\n\n" +
                                $"Error: {launchEx.Message}",
                                "Wrapper Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                progressReporter?.Report("Error: Failed to start report service.");
                return false;
            }
        }

        /// <summary>
        /// Checks if the wrapper process (identified by its name) is currently running. Synchronous.
        /// </summary>
        /// <returns>True if at least one process with the name is running, false otherwise.</returns>
        public bool IsWrapperRunning()
        {
            try
            {
                // Get processes by name
                Process[] processes = Process.GetProcessesByName(_wrapperProcessName);
                bool isRunning = processes.Length > 0;
                // Dispose the process handles returned by GetProcessesByName
                foreach (var p in processes) p.Dispose();
                return isRunning;
            }
            catch (Exception ex) // Catch errors getting process list (e.g., permissions)
            {
                Logger.LogError($"Error checking for wrapper process '{_wrapperProcessName}': {ex.Message}");
                return false; // Assume not running if check fails
            }
        }

        /// <summary>
        /// Attempts to find and terminate the wrapper process. Synchronous.
        /// Uses Kill for forceful termination. Should ideally be called during application shutdown.
        /// </summary>
        public void TerminateWrapperProcess()
        {
            Logger.LogInfo($"Attempting to terminate wrapper process '{_wrapperProcessName}'...");
            Process[] processes;
            try
            {
                // Find running processes by name
                processes = Process.GetProcessesByName(_wrapperProcessName);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error finding wrapper processes '{_wrapperProcessName}' to terminate: {ex.Message}");
                return;
            }

            if (processes.Length == 0)
            {
                Logger.LogInfo("Wrapper process not found, likely already closed.");
                return;
            }

            // Iterate through found processes and attempt termination
            foreach (var process in processes)
            {
                using (process) // Ensure disposal of process object
                {
                    try
                    {
                        if (!process.HasExited) // Check if it's still running
                        {
                            Logger.LogInfo($"Terminating wrapper process ID: {process.Id}");
                            process.Kill(true); // Force kill process and descendants
                            process.WaitForExit(2000); // Wait briefly for termination
                            if (process.HasExited)
                                Logger.LogInfo($"Wrapper process {process.Id} terminated.");
                            else
                                Logger.LogWarning($"Wrapper process {process.Id} did not terminate after Kill.");
                        }
                    }
                    catch (Exception ex) // Catch errors during termination (permissions, process already exited)
                    {
                        // Log less severely as this often happens during shutdown if process exited quickly
                        Logger.LogWarning($"Error or expected condition terminating wrapper process ID {process.Id}: {ex.Message}");
                    }
                }
            }
            Logger.LogInfo("Finished attempting to terminate wrapper processes.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Launches the wrapper executable specified by _wrapperExePath. Synchronous.
        /// Intended to be called via Task.Run from async methods.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the executable path is invalid or file not found.</exception>
        /// <exception cref="Exception">Thrown if Process.Start fails for other reasons (permissions, etc.).</exception>
        private void LaunchWrapper()
        {
            if (!File.Exists(_wrapperExePath))
            {
                throw new FileNotFoundException($"Wrapper executable not found at the configured path: {_wrapperExePath}", _wrapperExePath);
            }

            try
            {
                Logger.LogInfo($"Launching wrapper: {_wrapperExePath}");
                // Configure start info: UseShellExecute is generally preferred for launching external EXEs.
                // Set WorkingDirectory in case the wrapper depends on files in its own folder.
                var startInfo = new ProcessStartInfo(_wrapperExePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(_wrapperExePath) ?? string.Empty,
                    UseShellExecute = true // Use OS shell to start (handles associations, UAC if needed)
                    // Consider CreateNoWindow = true if the wrapper is a console app you don't want visible
                };
                // Start the process (fire-and-forget, we don't wait for exit here)
                using Process? process = Process.Start(startInfo);
                if (process == null)
                {
                    // This case is rare with UseShellExecute=true but possible
                    throw new Exception($"Process.Start returned null for '{_wrapperExePath}'. The process might not have started correctly.");
                }
                Logger.LogInfo($"Wrapper launch command initiated for '{_wrapperExePath}'. Process ID (if available): {process.Id}");
            }
            catch (Exception ex) // Catch errors during Process.Start
            {
                Logger.LogError($"Failed to start wrapper process '{_wrapperExePath}': {ex.Message}", ex);
                // Re-throw wrapped exception for better context in calling method
                throw new Exception($"Failed to start the wrapper process '{_wrapperExePath}'. Check permissions and path.", ex);
            }
        }

        #endregion
    }
}
