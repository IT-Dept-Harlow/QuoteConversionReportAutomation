using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReportWrapperCommon; // Namespace for ReportRequest/Response
using Newtonsoft.Json; // Or System.Web.Script.Serialization
using System.Diagnostics;
using System.Drawing; // For Process

namespace CrystalReportWrapper // Your wrapper's namespace
{
    public class TrayApplicationContext : ApplicationContext
    {
        private NotifyIcon _trayIcon;
        private CancellationTokenSource _cancellationTokenSource;
        private const string PipeName = "CrystalReportPipe"; // Must match client
        private Task _serverTask;

        public TrayApplicationContext()
        {
            InitializeTrayIcon();
            StartPipeServer();
            Console.WriteLine("Crystal Report Wrapper started. Listening for requests...");
            Logger.LogInfo("Crystal Report Wrapper started.");
        }

        private void InitializeTrayIcon()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, OnExit);
            _trayIcon = new NotifyIcon()
            {
                Icon = SystemIcons.Application, // Load from resources or default
                ContextMenuStrip = contextMenu,
                Text = "Crystal Report Wrapper",
                Visible = true
            };
        }

        private void StartPipeServer()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            _serverTask = Task.Run(() => ListenForConnections(token), token);
        }

        private async Task ListenForConnections(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                NamedPipeServerStream pipeServer = null;
                try
                {
                    pipeServer = new NamedPipeServerStream(
                        PipeName, PipeDirection.InOut, 1,
                        // *** Use PipeTransmissionMode.Byte for length prefixing ***
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    Console.WriteLine($"Pipe server waiting for connection on '{PipeName}'...");
                    await pipeServer.WaitForConnectionAsync(token);
                    Console.WriteLine("Client connected.");

                    await HandleClientConnection(pipeServer, token);
                }
                catch (OperationCanceledException) { Console.WriteLine("Pipe server cancellation requested."); break; }
                catch (IOException ioEx) { Console.WriteLine($"Pipe server IO error (client likely disconnected): {ioEx.Message}"); await Task.Delay(100, token); }
                catch (Exception ex) { Console.WriteLine($"Pipe server error: {ex}"); await Task.Delay(1000, token); }
                finally
                {
                    try { pipeServer?.Dispose(); } catch { }
                    Console.WriteLine("Pipe server instance disposed/closed.");
                }
            }
            Console.WriteLine("Pipe server stopped listening.");
        }

        private async Task HandleClientConnection(NamedPipeServerStream pipeServer, CancellationToken token)
        {
            var response = new ReportResponse { Success = false, ErrorMessage = "Unknown error occurred." };
            string requestJson = "[No request received]";

            try
            {
                // *** Read Request using Length Prefix ***
                // 1. Read the 4-byte length prefix
                byte[] lengthBuffer = new byte[4];
                int bytesRead = await pipeServer.ReadAsync(lengthBuffer, 0, 4, token);
                if (bytesRead < 4) throw new IOException("Failed to read message length prefix from client.");
                int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                if (messageLength <= 0) throw new IOException("Invalid message length received from client.");

                // 2. Read the actual message bytes
                byte[] messageBuffer = new byte[messageLength];
                bytesRead = await pipeServer.ReadAsync(messageBuffer, 0, messageLength, token);
                if (bytesRead < messageLength) throw new IOException("Failed to read complete message from client.");

                // 3. Decode and Deserialize
                requestJson = Encoding.UTF8.GetString(messageBuffer);
                Console.WriteLine($"Received request ({messageLength} bytes): {requestJson}");
                // *** End Read Request ***

                var request = JsonConvert.DeserializeObject<ReportRequest>(requestJson);
                if (request == null) throw new InvalidDataException("Failed to deserialize request.");

                try // Separate try for report generation
                {
                    Console.WriteLine("Starting report generation...");
                    RunCrystalReportClass reportRunner = new RunCrystalReportClass(0);
                    reportRunner.RunReport(
                        request.CrystalReportLocation, request.ReportOutputLocation,
                        request.ReportDateFrom, request.ReportDateTo, null);

                    response.Success = true;
                    response.OutputPath = request.ReportOutputLocation;
                    response.ErrorMessage = null;
                    Console.WriteLine("Report generated successfully.");
                }
                catch (Exception reportEx)
                {
                    Console.WriteLine($"Error generating report: {reportEx}");
                    response.Success = false;
                    response.ErrorMessage = $"Report generation failed: {reportEx.GetType().Name} - {reportEx.Message}";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client request (Request JSON might be partial/invalid): {ex}");
                response.Success = false;
                response.ErrorMessage = $"Error processing request: {ex.Message}";
            }
            finally
            {
                // *** Send Response using Length Prefix ***
                try
                {
                    if (pipeServer.IsConnected)
                    {
                        // 1. Serialize response and get bytes
                        string responseJson = JsonConvert.SerializeObject(response);
                        byte[] responseBytes = Encoding.UTF8.GetBytes(responseJson);
                        int responseLength = responseBytes.Length;
                        byte[] lengthBytes = BitConverter.GetBytes(responseLength);

                        // 2. Write length prefix (4 bytes)
                        await pipeServer.WriteAsync(lengthBytes, 0, 4, token);

                        // 3. Write message bytes
                        await pipeServer.WriteAsync(responseBytes, 0, responseLength, token);

                        // 4. Flush the pipe (important!)
                        await pipeServer.FlushAsync(token);

                        Console.WriteLine($"Sent response ({responseLength} bytes): {responseJson}");
                        // Give client a moment to receive before disconnecting? Not usually needed with byte mode.
                        // await Task.Delay(100, token);
                    }
                    else { Console.WriteLine($"Could not send response, pipe not connected."); }
                }
                catch (OperationCanceledException) { Console.WriteLine("Operation cancelled while sending response."); }
                catch (Exception respEx) { Console.WriteLine($"Error sending response: {respEx}"); }
                // *** End Send Response ***

                // Disconnection/Disposal happens in the outer finally block
            }
        }


        private async void OnExit(object sender, EventArgs e) // Make async for await
        {
            Console.WriteLine("Exit requested. Shutting down wrapper...");
            Logger.LogInfo("Exit requested. Shutting down wrapper.");
            if (_trayIcon != null) _trayIcon.Visible = false; // Hide icon immediately

            _cancellationTokenSource?.Cancel(); // Signal cancellation

            try
            {
                // Wait briefly for the server task to potentially finish cleanly
                if (_serverTask != null) await Task.WhenAny(_serverTask, Task.Delay(1000));
            }
            catch (OperationCanceledException) { /* Expected */ }
            catch (Exception ex) { Console.WriteLine($"Error during shutdown wait: {ex.Message}"); /* Optional: Logger.LogError(...) */ }
            finally
            {
                _cancellationTokenSource?.Dispose();
                Application.Exit();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                // Wait slightly for task completion? Optional.
                // _serverTask?.Wait(500);
            }
            base.Dispose(disposing);
        }
    }
}
