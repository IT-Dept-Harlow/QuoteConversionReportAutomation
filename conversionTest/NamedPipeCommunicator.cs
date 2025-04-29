// C# 10+ Features
using conversionTest;

namespace QuoteConversionReportAutomation
{
    // --- Using Statements ---
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json; // For JSON serialization/deserialization
    using ReportWrapperCommon; // For ReportRequest and ReportResponse

    /// <summary>
    /// Handles communication with the Crystal Report Wrapper service via Named Pipes.
    /// Sends ReportRequest objects and receives ReportResponse objects.
    /// Uses length-prefixing for message framing.
    /// </summary>
    public class NamedPipeCommunicator
    {
        #region Constants

        /// <summary>
        /// The name of the named pipe used for communication.
        /// </summary>
        private const string PipeName = "CrystalReportPipe";

        /// <summary>
        /// Timeout in milliseconds for connecting to the named pipe server.
        /// </summary>
        private const int ConnectTimeoutMs = 5000; // 5 seconds

        /// <summary>
        /// Sanity check limit for the maximum expected response size (e.g., 10MB).
        /// Prevents allocating excessively large buffers if an invalid length is received.
        /// </summary>
        private const int MaxResponseSize = 10 * 1024 * 1024;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a request object via named pipe and awaits a response object.
        /// Handles serialization, length-prefixing, and timeouts.
        /// </summary>
        /// <param name="request">The ReportRequest object to send.</param>
        /// <param name="progressReporter">Optional progress reporter for status updates.</param>
        /// <param name="cancellationToken">Token to allow cancellation of the operation.</param>
        /// <returns>A Task that represents the asynchronous operation. The task result contains the ReportResponse object, or null on failure/cancellation.</returns>
        /// <exception cref="TimeoutException">Thrown if connecting to the pipe server times out.</exception>
        /// <exception cref="IOException">Thrown if there's an error reading/writing to the pipe or invalid data length received.</exception>
        /// <exception cref="InvalidDataException">Thrown if the received response cannot be deserialized.</exception>
        /// <exception cref="Exception">Thrown for other unexpected errors during communication.</exception>
        public async Task<ReportResponse?> SendRequestReceiveResponseAsync(ReportRequest request, IProgress<string>? progressReporter = null, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            Logger.LogDebug($"Connecting to named pipe '{PipeName}'...");
            progressReporter?.Report("Connecting to report service...");

            // Use await using for automatic disposal of the pipe client
            await using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

            try
            {
                // Connect with timeout and cancellation support
                await pipeClient.ConnectAsync(ConnectTimeoutMs, cancellationToken);
                Logger.LogInfo("Connected to pipe server.");
                progressReporter?.Report("Connected. Sending request...");

                // --- Send Request (Length-Prefixed) ---
                string requestJson = JsonConvert.SerializeObject(request);
                byte[] requestBytes = Encoding.UTF8.GetBytes(requestJson);
                byte[] lengthBytes = BitConverter.GetBytes(requestBytes.Length);

                // Write length, then message bytes
                await pipeClient.WriteAsync(lengthBytes, cancellationToken);
                await pipeClient.WriteAsync(requestBytes, cancellationToken);
                await pipeClient.FlushAsync(cancellationToken); // Ensure data is sent
                Logger.LogDebug($"Sent request ({requestBytes.Length} bytes): {requestJson}");
                progressReporter?.Report("Request sent. Waiting for response...");

                // --- Read Response (Length-Prefixed) ---
                // 1. Read the 4-byte length prefix
                byte[] responseLengthBuffer = new byte[4];
                int bytesRead = await ReadPipeAsync(pipeClient, responseLengthBuffer, 0, 4, cancellationToken);
                if (bytesRead < 4) throw new IOException("Failed to read full response length prefix from service.");

                // 2. Convert length bytes to integer and validate
                int responseLength = BitConverter.ToInt32(responseLengthBuffer, 0);
                if (responseLength <= 0 || responseLength > MaxResponseSize)
                {
                    throw new IOException($"Invalid response length received: {responseLength}. Must be between 1 and {MaxResponseSize}.");
                }
                Logger.LogDebug($"Expecting response length: {responseLength}");

                // 3. Read the actual response message bytes
                byte[] responseBuffer = new byte[responseLength];
                bytesRead = await ReadPipeAsync(pipeClient, responseBuffer, 0, responseLength, cancellationToken);
                if (bytesRead < responseLength) throw new IOException("Failed to read complete response message from service.");

                // 4. Decode bytes to string and deserialize JSON
                string responseJson = Encoding.UTF8.GetString(responseBuffer);
                Logger.LogDebug($"Received response ({responseLength} bytes): {responseJson}");
                var response = JsonConvert.DeserializeObject<ReportResponse>(responseJson);

                if (response == null)
                {
                    throw new InvalidDataException("Failed to deserialize response JSON from service. Response was null.");
                }

                progressReporter?.Report("Report created successfully.");
                return response;
            }
            catch (TimeoutException ex) // Catch specific timeout from ConnectAsync
            {
                Logger.LogError($"Timeout connecting to named pipe server '{PipeName}'. Is the service running?");
                // Re-throw with a more context
                throw new TimeoutException($"Connection to the report service timed out. Ensure the service is running.", ex);
            }
            catch (IOException ex) // Catch pipe-related read/write errors or invalid length
            {
                Logger.LogError($"IO Error communicating with named pipe server: {ex.Message}");
                // Re-throw with context
                throw new IOException($"Communication error with the report service: {ex.Message}", ex);
            }
            catch (OperationCanceledException) // Catch cancellation signal
            {
                Logger.LogWarning("Named pipe communication cancelled.");
                throw; // Re-throw cancellation exception
            }
            catch (JsonException jsonEx) // Catch JSON deserialization errors
            {
                Logger.LogError($"Error deserializing response from pipe: {jsonEx.Message}");
                throw new InvalidDataException($"Failed to understand the response from the report service: {jsonEx.Message}", jsonEx);
            }
            catch (Exception ex) // Catch other potential errors
            {
                Logger.LogError($"Unexpected error during named pipe communication: {ex}");
                throw new Exception($"An unexpected error occurred communicating with the report service: {ex.Message}", ex);
            }
            // pipeClient is automatically disposed by 'await using'
        }

        #endregion

        #region Private Static Helper Methods

        /// <summary>
        /// Helper method to reliably read an exact number of bytes from a PipeStream asynchronously.
        /// Handles cases where ReadAsync might return fewer bytes than requested in a single call.
        /// </summary>
        /// <param name="pipe">The PipeStream to read from.</param>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The starting position in the buffer.</param>
        /// <param name="count">The exact number of bytes to read.</param>
        /// <param name="cancellationToken">Token to allow cancellation.</param>
        /// <returns>The total number of bytes read (should equal count on success).</returns>
        /// <exception cref="EndOfStreamException">Thrown if the pipe closes before the requested number of bytes are read.</exception>
        /// <exception cref="OperationCanceledException">Thrown if cancellation is requested.</exception>
        private static async Task<int> ReadPipeAsync(PipeStream pipe, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (count == 0) return 0; // Nothing to read

            int totalBytesRead = 0;
            while (totalBytesRead < count)
            {
                cancellationToken.ThrowIfCancellationRequested(); // Check for cancellation before each read

                // ReadAsync can return 0 if the pipe is closed gracefully.
                // Use AsMemory() for efficiency with modern .NET versions.
                int bytesRead = await pipe.ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), cancellationToken);

                if (bytesRead == 0)
                {
                    // Pipe closed before we could read the expected amount
                    throw new EndOfStreamException($"The pipe connection was closed prematurely while reading data. Expected {count} bytes, got {totalBytesRead}.");
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        #endregion
    }
}
