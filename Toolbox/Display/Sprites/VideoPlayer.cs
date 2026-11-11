using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Toolbox.Graphics;
using Toolbox.Logging;

namespace Toolbox.Display.Sprites
{
    public sealed class VideoPlayer : IDisposable
    {
        private readonly string _path;
        private readonly int _width;
        private readonly int _height;
        private readonly int _frameSize;
        private readonly bool _loop;
        private readonly object _frameLock = new();
        private Process? _process;
        private Task? _decodeTask;
        private Graphic? _currentFrame;
        private byte[] _decodeBuffer = Array.Empty<byte>();
        private int _frameVersion;
        private volatile bool _disposed;
        private volatile bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public int FrameVersion => Volatile.Read(ref _frameVersion);
        public Graphic? GetCurrentFrame() => Volatile.Read(ref _currentFrame);

        public void WithCurrentFrame(Action<Graphic> render)
        {
            lock (_frameLock)
            {
                if (_currentFrame is { } frame)
                    render(frame);
            }
        }

        private VideoPlayer(string path, bool loop, bool autoPlay)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("The skin video asset was not found.", path);

            _path = Path.GetFullPath(path);
            _loop = loop;
            (_width, _height) = ProbeDimensions(_path);

            long frameSize = checked((long)_width * _height * 4);
            if (_width <= 0 || _height <= 0 || frameSize > 64 * 1024 * 1024)
                throw new InvalidDataException($"The video has unsupported dimensions: {_width}x{_height}.");

            _frameSize = (int)frameSize;
            _decodeBuffer = new byte[_frameSize];
            StartDecoder(autoPlay);
        }

        public static VideoPlayer Load(string path, bool loop = true, bool autoPlay = true) =>
            new(path, loop, autoPlay);

        public void Play()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(VideoPlayer));
            if (_isPlaying)
                return;

            StartDecoder(continuePlayback: true);
        }

        public void Stop()
        {
            _isPlaying = false;
            var process = Interlocked.Exchange(ref _process, null);
            if (process == null)
                return;

            var decodeTask = _decodeTask;
            StopProcess(process);
            decodeTask?.GetAwaiter().GetResult();
            process.Dispose();
            _decodeTask = null;
        }

        private static (int width, int height) ProbeDimensions(string path)
        {
            var startInfo = CreateProcessStartInfo(ResolveExecutable("ffprobe"));
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("error");
            startInfo.ArgumentList.Add("-select_streams");
            startInfo.ArgumentList.Add("v:0");
            startInfo.ArgumentList.Add("-show_entries");
            startInfo.ArgumentList.Add("stream=width,height");
            startInfo.ArgumentList.Add("-of");
            startInfo.ArgumentList.Add("csv=s=x:p=0");
            startInfo.ArgumentList.Add(path);

            using var process = StartProcess(startInfo, "ffprobe");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            string output = outputTask.GetAwaiter().GetResult().Trim();
            string error = errorTask.GetAwaiter().GetResult().Trim();
            if (process.ExitCode != 0)
                throw new InvalidDataException($"ffprobe could not read video '{path}': {error}");

            var dimensions = output.Split('x');
            if (dimensions.Length != 2 ||
                !int.TryParse(dimensions[0], out int width) ||
                !int.TryParse(dimensions[1], out int height))
                throw new InvalidDataException($"ffprobe returned invalid video dimensions for '{path}': {output}");

            return (width, height);
        }

        private void StartDecoder(bool continuePlayback)
        {
            var startInfo = CreateProcessStartInfo(ResolveExecutable("ffmpeg"));
            startInfo.ArgumentList.Add("-v");
            startInfo.ArgumentList.Add("error");
            startInfo.ArgumentList.Add("-nostdin");
            if (continuePlayback && _loop)
            {
                startInfo.ArgumentList.Add("-stream_loop");
                startInfo.ArgumentList.Add("-1");
            }

            if (continuePlayback)
                startInfo.ArgumentList.Add("-re");

            startInfo.ArgumentList.Add("-noautorotate");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(_path);
            startInfo.ArgumentList.Add("-an");
            startInfo.ArgumentList.Add("-sn");
            startInfo.ArgumentList.Add("-dn");
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("rawvideo");
            startInfo.ArgumentList.Add("-pix_fmt");
            startInfo.ArgumentList.Add("bgra");
            startInfo.ArgumentList.Add("pipe:1");

            var process = StartProcess(startInfo, "ffmpeg");
            _process = process;
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            try
            {
                var firstFrame = _decodeBuffer;
                if (!ReadFrame(process.StandardOutput.BaseStream, firstFrame))
                {
                    process.WaitForExit();
                    string error = errorTask.GetAwaiter().GetResult().Trim();
                    throw new InvalidDataException($"ffmpeg could not decode a frame from '{_path}': {error}");
                }

                PublishFrame(firstFrame);
                if (continuePlayback)
                {
                    _isPlaying = true;
                    _decodeTask = Task.Run(() => DecodeFrames(process, errorTask));
                }
                else
                {
                    StopProcess(process);
                    _process = null;
                }
            }
            catch
            {
                if (ReferenceEquals(_process, process))
                    _process = null;
                StopProcess(process);
                process.Dispose();
                throw;
            }
        }

        private void DecodeFrames(Process process, Task<string> errorTask)
        {
            try
            {
                var frame = _decodeBuffer;
                while (!_disposed && ReadFrame(process.StandardOutput.BaseStream, frame))
                {
                    PublishFrame(frame);
                    frame = _decodeBuffer;
                }

                process.WaitForExit();
                string error = errorTask.GetAwaiter().GetResult().Trim();
                if (!_disposed && process.ExitCode != 0)
                    Log.Error(new InvalidDataException($"ffmpeg failed to decode '{_path}': {error}"));
            }
            catch (Exception ex) when (_disposed || !ReferenceEquals(_process, process) || ex is IOException || ex is InvalidOperationException)
            {
                if (!_disposed && ReferenceEquals(_process, process))
                    Log.Error(ex);
            }
            finally
            {
                if (ReferenceEquals(_process, process))
                {
                    _isPlaying = false;
                    _process = null;
                    _decodeTask = null;
                }
                process.Dispose();
            }
        }

        private void PublishFrame(byte[] pixels)
        {
            lock (_frameLock)
            {
                var previousFrame = _currentFrame;
                Volatile.Write(ref _currentFrame, new Graphic(_width, _height, pixels));
                _decodeBuffer = previousFrame?.Pixels ?? new byte[_frameSize];
                Interlocked.Increment(ref _frameVersion);
            }
        }

        private static bool ReadFrame(Stream stream, byte[] frame)
        {
            int offset = 0;
            while (offset < frame.Length)
            {
                int read = stream.Read(frame, offset, frame.Length - offset);
                if (read == 0)
                {
                    if (offset == 0)
                        return false;

                    throw new EndOfStreamException("The video decoder returned a partial frame.");
                }

                offset += read;
            }

            return true;
        }

        private static Process StartProcess(ProcessStartInfo startInfo, string executable)
        {
            try
            {
                return Process.Start(startInfo)
                    ?? throw new InvalidOperationException($"Could not start {executable}.");
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                throw new InvalidOperationException(
                    $"Unable to start {executable}. Install FFmpeg (including ffprobe) or place both executables beside the application.",
                    ex);
            }
        }

        private static ProcessStartInfo CreateProcessStartInfo(string executable) => new(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        private static string ResolveExecutable(string executable)
        {
            string fileName = OperatingSystem.IsWindows() ? executable + ".exe" : executable;
            string besideApp = Path.Combine(AppContext.BaseDirectory, fileName);
            return File.Exists(besideApp) ? besideApp : executable;
        }

        private static void StopProcess(Process process)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                process.WaitForExit();
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                Log.Error(ex);
            }
        }

        public void Dispose()
        {
            _disposed = true;
            Stop();
        }
    }
}
