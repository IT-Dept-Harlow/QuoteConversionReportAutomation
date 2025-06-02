// ReportProcessManager.cs
// Manages the lifecycle (checking, launching, terminating) of the external
// Crystal Report Wrapper process. This class is responsible for interacting
// with the specified wrapper executable.
// Utilises C# 10+ features.

#region Using Directives
// System related namespaces
using System;
using System.Diagnostics; // Required for Process class
using System.IO;          // Required for Path and File operations
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel; // Required for Win32Exception

// Project specific namespaces
// Note: QuoteConversionReportAutomation.Helpers.FlexibleMessageBox is removed from here.
// UI interactions should be handled by the calling UI layer.
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Managers
{
    /// <summary>
    /// Manages the lifecycle of an external report processing executable (the "wrapper").
    /// This includes checking if the wrapper process is running, launching it if necessary,
    /// and providing a method to terminate it.
    /// </summary>
    public class ReportProcessManager
    {
        #region Fields
        /// <summary>
        /// The full, absolute path to the wrapper executable file.
        /// </summary>
        private readonly string _wrapperExePath;

        /// <summary>
        /// The name of the wrapper process, derived from the executable filename (without the extension).
        /// Used for checking if the process is already running.
        /// </summary>
        private readonly string _wrapperProcessName;

        /// <summary>
        /// A brief delay in milliseconds to allow the wrapper process to initialize,
        /// particularly its named pipe server, after being launched.
        /// Consider making this configurable if different environments require different startup times.
        /// </summary>
        private const int WrapperLaunchGracePeriodMs = 3000; // 3 seconds
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ReportProcessManager"/> class.
        /// </summary>
        /// <param name="wrapperExePath">The full file path to the wrapper executable. This path must be valid and point to an existing file.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="wrapperExePath"/> is null, empty, whitespace, or if a process name cannot be derived from it.</exception>
        /// <exception cref="PathTooLongException">Thrown if the resolved path exceeds the system-defined maximum length.</exception>
        /// <exception cref="System.Security.SecurityException">Thrown if the caller does not have the required permissions to access path information.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="wrapperExePath"/> contains a colon (:) that is not part of a volume identifier (e.g., "c:\").</exception>
        public ReportProcessManager(string wrapperExePath)
        {
            if (string.IsNullOrWhiteSpace(wrapperExePath))
            {
                throw new ArgumentException("Wrapper executable path cannot be null, empty, or whitespace.", nameof(wrapperExePath));
            }

            try
            {
                // Normalize the path to an absolute path. This can throw various exceptions.
                _wrapperExePath = Path.GetFullPath(wrapperExePath);
                _wrapperProcessName = Path.GetFileNameWithoutExtension(_wrapperExePath);

                if (string.IsNullOrEmpty(_wrapperProcessName))
                {
                    // This case should be rare if GetFullPath and GetFileNameWithoutExtension succeed.
                    throw new ArgumentException("Could not derive a valid process name from the provided wrapper executable path.", nameof(wrapperExePath));
                }
            }
            catch (Exception ex) // Catch exceptions from Path operations
            {
                Logger.LogCritical($"ReportProcessManager: Error initializing with wrapper path '{wrapperExePath}': {ex.Message}", ex);
                throw new ArgumentException($"Invalid wrapper executable path provided ('{wrapperExePath}'): {ex.Message}", nameof(wrapperExePath), ex);
            }

            Logger.LogDebug($"ReportProcessManager initialized. Wrapper Path: '{_wrapperExePath}', Process Name: '{_wrapperProcessName}'");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Asynchronously ensures the Crystal Report Wrapper process is running.
        /// If the process is not found, it attempts to launch the executable specified during initialization.
        /// </summary>
        /// <param name="progressReporter">An optional <see cref="IProgress{T}"/> instance to report status updates (e.g., "Starting report service...").</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        /// The task result is true if the wrapper process is running or was successfully launched; otherwise, false.
        /// </returns>
        /// <exception cref="FileNotFoundException">Can be propagated if <see cref="LaunchWrapper"/> fails because the executable is not found.</exception>
        /// <exception cref="InvalidOperationException">Can be propagated if <see cref="LaunchWrapper"/> fails to start the process for other reasons (e.g., permissions).</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
        public async Task<bool> EnsureWrapperIsRunningAsync(IProgress<string>? progressReporter = null, CancellationToken cancellationToken = default)
        {
            if (IsWrapperRunning())
            {
                Logger.LogInfo($"Wrapper process '{_wrapperProcessName}' is already running.");
                progressReporter?.Report("Report service is active."); // Report current state
                return true;
            }

            Logger.LogWarning($"Wrapper process '{_wrapperProcessName}' not found. Attempting to launch from: '{_wrapperExePath}'");
            progressReporter?.Report("Starting report service...");

            try
            {
                // Launch the wrapper process. LaunchWrapper will throw if it fails critically.
                // Run the synchronous LaunchWrapper on a background thread to keep this method async.
                await Task.Run(() => LaunchWrapper(), cancellationToken).ConfigureAwait(false);

                // Allow a brief period for the process to initialize, especially its named pipe server.
                await Task.Delay(WrapperLaunchGracePeriodMs, cancellationToken).ConfigureAwait(false);

                if (IsWrapperRunning()) // Check again after launch attempt and grace period.
                {
                    Logger.LogInfo($"Wrapper process '{_wrapperProcessName}' successfully launched and is now running.");
                    progressReporter?.Report("Report service started successfully.");
                    return true;
                }
                else
                {
                    Logger.LogError($"Wrapper process '{_wrapperProcessName}' did not start successfully or terminated unexpectedly after launch attempt from '{_wrapperExePath}'.");
                    progressReporter?.Report("Error: Failed to start or confirm report service activity.");
                    return false; // Launch failed or process exited quickly.
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Operation to ensure wrapper '{_wrapperProcessName}' is running was cancelled.");
                progressReporter?.Report("Report service startup cancelled.");
                throw; // Re-throw to be handled by the caller.
            }
            // FileNotFoundException and InvalidOperationException (from LaunchWrapper) will propagate up.
            // Other exceptions from Task.Run or Task.Delay will also propagate.
        }

        /// <summary>
        /// Checks if the wrapper process (identified by its derived process name) is currently running on the system.
        /// This method is synchronous.
        /// </summary>
        /// <returns>True if at least one instance of the wrapper process is found running; otherwise, false.</returns>
        public bool IsWrapperRunning()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(_wrapperProcessName);
                bool isRunning = processes.Length > 0;
                // It's important to dispose of the process objects returned by GetProcessesByName.
                foreach (var p in processes)
                {
                    p.Dispose();
                }
                Logger.LogTrace($"IsWrapperRunning check for '{_wrapperProcessName}': {isRunning} (Found {processes.Length} instances).");
                return isRunning;
            }
            catch (InvalidOperationException ex) // Can occur if process name is invalid (should be caught in constructor)
            {
                Logger.LogError($"Error checking for wrapper process '{_wrapperProcessName}' (InvalidOperationException): {ex.Message}", ex);
                return false;
            }
            catch (Exception ex) // Catch other potential errors (e.g., permissions to query processes).
            {
                Logger.LogError($"Error checking for wrapper process '{_wrapperProcessName}': {ex.Message}", ex);
                return false; // Assume not running if the check itself fails.
            }
        }

        /// <summary>
        /// Attempts to find and terminate all running instances of the wrapper process.
        /// This method uses a forceful kill and should ideally be called during application shutdown
        /// or when a clean restart of the wrapper is required. This method is synchronous.
        /// </summary>
        public void TerminateWrapperProcess()
        {
            Logger.LogInfo($"Attempting to terminate all instances of wrapper process '{_wrapperProcessName}'...");
            Process[] processes;
            try
            {
                processes = Process.GetProcessesByName(_wrapperProcessName);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error finding wrapper processes '{_wrapperProcessName}' to terminate: {ex.Message}", ex);
                return; // Cannot proceed if process enumeration fails.
            }

            if (processes.Length == 0)
            {
                Logger.LogInfo($"No running instances of wrapper process '{_wrapperProcessName}' found to terminate.");
                return;
            }

            Logger.LogInfo($"Found {processes.Length} instance(s) of '{_wrapperProcessName}'. Attempting termination...");
            foreach (var process in processes)
            {
                using (process) // Ensure the Process object is disposed.
                {
                    try
                    {
                        if (!process.HasExited) // Check if the process is still running before attempting to kill.
                        {
                            Logger.LogInfo($"Terminating wrapper process ID: {process.Id}, Name: '{process.ProcessName}', StartTime: {TryGetProcessStartTime(process)}");
                            process.Kill(true); // Forcefully kill the process and its descendants.
                            if (process.WaitForExit(2000)) // Wait up to 2 seconds for termination.
                            {
                                Logger.LogInfo($"Wrapper process ID {process.Id} terminated successfully.");
                            }
                            else
                            {
                                Logger.LogWarning($"Wrapper process ID {process.Id} did not confirm termination within 2 seconds after Kill command.");
                            }
                        }
                        else
                        {
                            Logger.LogInfo($"Wrapper process ID {process.Id} had already exited before termination attempt.");
                        }
                    }
                    catch (Win32Exception ex) when (ex.NativeErrorCode == 5) // NativeErrorCode 5 is Access Denied.
                    {
                        Logger.LogWarning($"Access denied attempting to terminate wrapper process ID {process.Id}. It may require higher privileges or be a protected process. Error: {ex.Message}");
                    }
                    catch (InvalidOperationException ex) // Can occur if process has already exited or no process associated.
                    {
                        Logger.LogWarning($"Invalid operation terminating wrapper process ID {process.Id} (likely already exited): {ex.Message}");
                    }
                    catch (Exception ex) // Catch other errors during termination.
                    {
                        Logger.LogError($"Error terminating wrapper process ID {process.Id}: {ex.Message}", ex);
                    }
                }
            }
            Logger.LogInfo($"Finished attempting to terminate wrapper process(es) '{_wrapperProcessName}'.");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Launches the wrapper executable specified by <see cref="_wrapperExePath"/>.
        /// This method is synchronous and intended to be called via `Task.Run` from asynchronous contexts.
        /// </summary>
        /// <exception cref="FileNotFoundException">Thrown if the wrapper executable path is invalid or the file is not found.</exception>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="Process.Start(ProcessStartInfo)"/> fails to start the process for other reasons (e.g., permissions, invalid executable).</exception>
        /// <exception cref="Win32Exception">Can be thrown by <see cref="Process.Start(ProcessStartInfo)"/> for various system-level errors.</exception>
        private void LaunchWrapper()
        {
            if (!File.Exists(_wrapperExePath)) // Pre-check for file existence.
            {
                Logger.LogError($"Wrapper executable not found at the configured path: {_wrapperExePath}");
                throw new FileNotFoundException($"The report service executable ('{Path.GetFileName(_wrapperExePath)}') was not found at the configured path.", _wrapperExePath);
            }

            try
            {
                Logger.LogInfo($"Attempting to launch wrapper executable: '{_wrapperExePath}'");
                var startInfo = new ProcessStartInfo(_wrapperExePath)
                {
                    // Set WorkingDirectory to the directory of the executable.
                    // This can be important if the wrapper relies on relative paths for its own resources.
                    WorkingDirectory = Path.GetDirectoryName(_wrapperExePath) ?? string.Empty,
                    UseShellExecute = true // UseShellExecute = true is generally preferred for launching .exe files.
                                           // It allows the OS to handle elevation (UAC) if required by the .exe's manifest.
                                           // If set to false, you might need to handle elevation manually or run into issues.
                                           // CreateNoWindow = true; // Consider if the wrapper is a console app and you don't want its window shown.
                };

                using Process? process = Process.Start(startInfo); // Attempt to start the process.

                if (process == null)
                {
                    // This scenario is rare with UseShellExecute = true but is a possible failure point.
                    Logger.LogError($"Process.Start returned null for '{_wrapperExePath}'. The wrapper process might not have started correctly.");
                    throw new InvalidOperationException($"Failed to start the report service ('{Path.GetFileName(_wrapperExePath)}'). Process.Start returned null.");
                }
                // Process started, but it might exit immediately if there's an issue within the wrapper itself.
                // We don't wait for exit here; IsWrapperRunning will check its status after a grace period.
                Logger.LogInfo($"Wrapper launch command initiated for '{_wrapperExePath}'. Process ID (if available and not exited quickly): {TryGetProcessId(process)}");
            }
            catch (Win32Exception w32Ex) // Catch specific system-level errors from Process.Start.
            {
                Logger.LogError($"Failed to start wrapper process '{_wrapperExePath}' due to a system error (Win32Exception): {w32Ex.Message} (NativeErrorCode: 0x{w32Ex.NativeErrorCode:X})", w32Ex);
                throw new InvalidOperationException($"Failed to start the report service ('{Path.GetFileName(_wrapperExePath)}') due to a system error. Please check event logs or permissions. Error: {w32Ex.Message}", w32Ex);
            }
            catch (Exception ex) // Catch other general errors from Process.Start.
            {
                Logger.LogError($"Failed to start wrapper process '{_wrapperExePath}': {ex.Message}", ex);
                throw new InvalidOperationException($"Failed to start the report service ('{Path.GetFileName(_wrapperExePath)}'). Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Safely tries to get the Process ID.
        /// </summary>
        private static int TryGetProcessId(Process process)
        {
            try { return process.Id; } catch { return -1; }
        }

        /// <summary>
        /// Safely tries to get the Process StartTime.
        /// </summary>
        private static string TryGetProcessStartTime(Process process)
        {
            try { return process.StartTime.ToString("o"); } catch { return "N/A"; }
        }
        #endregion
    }
}