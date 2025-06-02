// NamedPipeCommunicator.cs
// Handles communication with the Crystal Report Wrapper service via Named Pipes.
// Sends ReportRequest objects and receives ReportResponse objects.
// Uses length-prefixing for message framing.
// Configuration for pipe name, timeouts, and buffer sizes is read from appsettings.json.
// Utilises C# 10+ features.

#region Using Directives
// System related namespaces
using System;
using System.IO;
using System.IO.Pipes; // Required for NamedPipeClientStream
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Third-party namespaces
using Microsoft.Extensions.Configuration; // For IConfiguration
using Newtonsoft.Json; // For JSON serialization/deserialization

// Project specific namespaces
using QuoteConversionReportAutomation.Services.Logging; // For Logger
#endregion

namespace QuoteConversionReportAutomation.Services.Communication
{
    /// <summary>
    /// Facilitates communication with an external service (e.g., Crystal Report Wrapper)
    /// using named pipes. It handles sending <see cref="ReportRequest"/> objects and
    /// receiving <see cref="ReportResponse"/> objects, managing serialization,
    /// message framing (length-prefixing), connection timeouts, and response size validation.
    /// Communication parameters are configurable via `appsettings.json`.
    /// </summary>
    public class NamedPipeCommunicator
    {
        #region Fields
        /// <summary>
        /// The name of the named pipe used for communication.
        /// Read from "InterProcessCommunication:NamedPipeName" in `appsettings.json`.
        /// </summary>
        private readonly string _pipeName;

        /// <summary>
        /// Timeout in milliseconds for connecting to the named pipe server.
        /// Read from "InterProcessCommunication:PipeConnectTimeoutMs" in `appsettings.json`.
        /// </summary>
        private readonly int _connectTimeoutMs;

        /// <summary>
        /// Sanity check limit in bytes for the maximum expected response message size.
        /// This prevents allocating excessively large buffers if an invalid length is received from the pipe.
        /// Read from "InterProcessCommunication:MaxPipeResponseSizeBytes" in `appsettings.json`.
        /// </summary>
        private readonly int _maxResponseSizeBytes;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipeCommunicator"/> class.
        /// Reads named pipe communication parameters from the application configuration.
        /// </summary>
        /// <param name="configuration">The application configuration instance.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configuration"/> is null.</exception>
        public NamedPipeCommunicator(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));
            Logger.LogTrace("Initializing NamedPipeCommunicator...");

            // Read pipe name from configuration, with a default value.
            _pipeName = configuration.GetValue<string>("InterProcessCommunication:NamedPipeName", "CrystalReportPipe")!;
            if (string.IsNullOrWhiteSpace(_pipeName))
            {
                _pipeName = "CrystalReportPipe"; // Ensure a default if config value is empty.
                Logger.LogWarning($"Configuration key 'InterProcessCommunication:NamedPipeName' is missing or empty. Using default: '{_pipeName}'");
            }

            // Read connection timeout from configuration, with a default value.
            _connectTimeoutMs = configuration.GetValue<int>("InterProcessCommunication:PipeConnectTimeoutMs", 5000);
            if (_connectTimeoutMs <= 0)
            {
                Logger.LogWarning($"Invalid value for 'InterProcessCommunication:PipeConnectTimeoutMs' ({_connectTimeoutMs}). Using default: 5000ms.");
                _connectTimeoutMs = 5000;
            }

            // Read maximum response size from configuration, with a default value.
            _maxResponseSizeBytes = configuration.GetValue<int>("InterProcessCommunication:MaxPipeResponseSizeBytes", 10 * 1024 * 1024); // 10MB default
            if (_maxResponseSizeBytes <= 0)
            {
                Logger.LogWarning($"Invalid value for 'InterProcessCommunication:MaxPipeResponseSizeBytes' ({_maxResponseSizeBytes}). Using default: 10MB.");
                _maxResponseSizeBytes = 10 * 1024 * 1024;
            }

            Logger.LogInfo($"NamedPipeCommunicator initialized. PipeName: '{_pipeName}', ConnectTimeout: {_connectTimeoutMs}ms, MaxResponseSize: {_maxResponseSizeBytes} bytes.");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Sends a <see cref="ReportRequest"/> object to the named pipe server and asynchronously awaits a <see cref="ReportResponse"/>.
        /// This method handles JSON serialization of the request, length-prefixing for message framing,
        /// connection timeouts, response size validation, and JSON deserialization of the response.
        /// </summary>
        /// <param name="request">The <see cref="ReportRequest"/> object to send to the server.</param>
        /// <param name="progressReporter">An optional <see cref="IProgress{T}"/> instance to report status updates.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that represents the asynchronous operation.
        /// The task result contains the deserialized <see cref="ReportResponse"/> object from the server.
        /// Returns null if the operation is cancelled, fails critically, or if the response cannot be processed.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
        /// <exception cref="TimeoutException">Thrown if connecting to the pipe server times out (as per configured <see cref="_connectTimeoutMs"/>).</exception>
        /// <exception cref="IOException">Thrown if there's an error reading/writing to the pipe, an invalid data length is received, or the pipe is closed prematurely.</exception>
        /// <exception cref="InvalidDataException">Thrown if the received response data cannot be deserialized into a <see cref="ReportResponse"/> object (e.g., malformed JSON).</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="Exception">Can be thrown for other unexpected errors during the communication process.</exception>
        public async Task<ReportResponse?> SendRequestReceiveResponseAsync(
            ReportRequest request,
            IProgress<string>? progressReporter = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            Logger.LogDebug($"Attempting to connect to named pipe server '{_pipeName}'...");
            progressReporter?.Report("Connecting to report service via named pipe...");

            // 'await using' ensures the pipeClient is properly disposed even if exceptions occur.
            await using var pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                // Attempt to connect to the pipe server with the configured timeout and cancellation token.
                await pipeClient.ConnectAsync(_connectTimeoutMs, cancellationToken);
                Logger.LogInfo($"Successfully connected to named pipe server '{_pipeName}'.");
                progressReporter?.Report("Connected to report service. Sending request...");

                // --- Send Request (Length-Prefixed) ---
                string requestJson = JsonConvert.SerializeObject(request); // Serialize request to JSON.
                byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson); // Encode JSON string to UTF-8 bytes.
                byte[] lengthPrefixBytes = BitConverter.GetBytes(requestBytes.Length); // Get 4-byte length prefix.

                // Write the length prefix, then the message bytes to the pipe.
                await pipeClient.WriteAsync(lengthPrefixBytes, 0, lengthPrefixBytes.Length, cancellationToken);
                await pipeClient.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken);
                await pipeClient.FlushAsync(cancellationToken); // Ensure all data is sent immediately.
                Logger.LogDebug($"Sent request ({requestBytes.Length} bytes) to '{_pipeName}': {requestJson}");
                progressReporter?.Report("Request sent. Waiting for response from report service...");

                // --- Read Response (Length-Prefixed) ---
                // 1. Read the 4-byte length prefix for the incoming response.
                byte[] responseLengthBuffer = new byte[4];
                int bytesReadForLength = await ReadPipeAsync(pipeClient, responseLengthBuffer, 0, 4, cancellationToken);
                if (bytesReadForLength < 4)
                {
                    throw new IOException("Failed to read the full response length prefix from the report service. Pipe may have closed unexpectedly.");
                }

                // 2. Convert length prefix bytes to an integer and validate it.
                int responseDataLength = BitConverter.ToInt32(responseLengthBuffer, 0);
                if (responseDataLength <= 0 || responseDataLength > _maxResponseSizeBytes)
                {
                    throw new IOException($"Invalid response data length received from report service: {responseDataLength} bytes. Expected 1 to {_maxResponseSizeBytes} bytes.");
                }
                Logger.LogDebug($"Expecting response data length: {responseDataLength} bytes from '{_pipeName}'.");

                // 3. Read the actual response message bytes based on the received length.
                byte[] responseDataBuffer = new byte[responseDataLength];
                int bytesReadForData = await ReadPipeAsync(pipeClient, responseDataBuffer, 0, responseDataLength, cancellationToken);
                if (bytesReadForData < responseDataLength)
                {
                    throw new IOException("Failed to read the complete response message from the report service. Pipe may have closed after sending partial data.");
                }

                // 4. Decode response bytes to string and deserialize JSON to ReportResponse object.
                string responseJson = Encoding.UTF8.GetString(responseDataBuffer);
                Logger.LogDebug($"Received response ({responseDataLength} bytes) from '{_pipeName}': {responseJson}");
                var response = JsonConvert.DeserializeObject<ReportResponse>(responseJson);

                if (response == null) // Check if deserialization was successful.
                {
                    throw new InvalidDataException("Failed to deserialize JSON response from the report service. The response was null or malformed.");
                }

                progressReporter?.Report(response.Success ? "Report service processed request successfully." : "Report service indicated an error.");
                return response;
            }
            catch (TimeoutException ex) // Connection timeout.
            {
                Logger.LogError($"Timeout connecting to named pipe server '{_pipeName}'. Is the report service running and accessible?", ex);
                throw new TimeoutException($"Connection to the report service ('{_pipeName}') timed out after {_connectTimeoutMs}ms. Please ensure the service is running.", ex);
            }
            catch (IOException ioEx) // Pipe I/O errors or framing issues.
            {
                Logger.LogError($"IO Error during named pipe communication with '{_pipeName}': {ioEx.Message}", ioEx);
                throw new IOException($"Communication error with the report service ('{_pipeName}'): {ioEx.Message}", ioEx);
            }
            catch (OperationCanceledException opEx) // Operation cancelled.
            {
                Logger.LogWarning($"Named pipe communication with '{_pipeName}' was cancelled.", opEx);
                throw; // Re-throw to allow caller to handle cancellation.
            }
            catch (JsonException jsonEx) // JSON deserialization errors.
            {
                Logger.LogError($"Error deserializing response from named pipe '{_pipeName}': {jsonEx.Message}. Received JSON might be malformed.", jsonEx);
                throw new InvalidDataException($"Failed to understand the response from the report service ('{_pipeName}'). Ensure the service returns valid JSON: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex) // Catch other unexpected errors.
            {
                Logger.LogCritical($"Unexpected error during named pipe communication with '{_pipeName}': {ex.Message}", ex);
                throw new Exception($"An unexpected error occurred while communicating with the report service ('{_pipeName}'): {ex.Message}", ex);
            }
            // pipeClient is automatically disposed by 'await using'.
        }
        #endregion

        #region Private Static Helper Methods
        /// <summary>
        /// Asynchronously reads an exact number of bytes from a <see cref="PipeStream"/>.
        /// This helper method ensures that the requested number of bytes is read, handling cases
        /// where <see cref="PipeStream.ReadAsync(byte[], int, int, CancellationToken)"/> might return fewer bytes
        /// than requested in a single call.
        /// </summary>
        /// <param name="pipe">The <see cref="PipeStream"/> to read data from.</param>
        /// <param name="buffer">The byte array buffer to store the read data into.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data.</param>
        /// <param name="count">The exact number of bytes to read from the pipe.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
        /// <returns>A <see cref="Task{TResult}"/> that represents the asynchronous read operation.
        /// The task result is the total number of bytes read, which should equal <paramref name="count"/> on success.</returns>
        /// <exception cref="EndOfStreamException">Thrown if the pipe is closed or the end of the stream is reached before <paramref name="count"/> bytes are read.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the read operation is cancelled via the <paramref name="cancellationToken"/>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="pipe"/> or <paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="offset"/> or <paramref name="count"/> are invalid for the buffer.</exception>
        private static async Task<int> ReadPipeAsync(PipeStream pipe, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(pipe, nameof(pipe));
            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
            if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset and count for the provided buffer size.");

            if (count == 0) return 0; // Nothing to read.

            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation before each read attempt.

                // ReadAsync can return 0 if the pipe is closed gracefully from the other end.
                // Use AsMemory() for potentially better performance with modern .NET versions.
                int bytesReadThisCall = await pipe.ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), cancellationToken).ConfigureAwait(false);

                if (bytesReadThisCall == 0) // Pipe closed before all expected bytes were read.
                {
                    throw new EndOfStreamException($"The pipe connection was closed prematurely while reading data. Expected {count} bytes, but received only {totalBytesRead} before the stream ended.");
                }
                totalBytesRead += bytesReadThisCall;
            }
            return totalBytesRead; // Should be equal to 'count' if successful.
        }
        #endregion
    }
}