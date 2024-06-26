﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace SacdSharp
{
    public class SacdExtractor
    {
        private SacdExtractor()
        {
            this.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
        }

        public CancellationTokenSource CancellationTokenSource { get; private set; }

        public SacdExtractor(Sacd sacd, SacdArea area, SacdTrack track) : this()
        {
            this.Sacd = sacd;
            this.Area = area;
            this.Track = track;
        }

        public Sacd Sacd { get; private set; }

        public SacdArea Area { get; private set; }

        public SacdTrack Track { get; private set; }


        public string GetFileName(string directoryName)
        {
            var fileName = string.Concat(
                this.Sacd.Album.Title,
                Path.DirectorySeparatorChar,
                this.Area.IsStereo ? Constants.STEREO : Constants.MULTI_CHANNEL,
                Path.DirectorySeparatorChar,
                string.Format("{0:00}", this.Area.Tracks.IndexOf(this.Track) + 1),
                " - ",
                this.Track.Info.Title,
                ".dsf"
            );
            return Path.Combine(directoryName, fileName);
        }

        public bool IsExtracted(string directoryName, out string fileName)
        {
            fileName = this.GetFileName(directoryName);
            return File.Exists(fileName);
        }

        public bool Extract(string directoryName, out string fileName)
        {
            var flag = this.Area.IsStereo ? "2" : "m";
            var track = this.Area.Tracks.IndexOf(this.Track) + 1;
            var arguments = string.Format("-i \"{0}\" -s -{1} -t {2} -o \"{3}\"", this.Sacd.FileName, flag, track, directoryName);
            var startInfo = new ProcessStartInfo(Sacd.Executable, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(startInfo))
            {
                if (!this.Extract(process, out fileName))
                {
                    return false;
                }
            }
            return File.Exists(fileName);
        }

        protected virtual bool Extract(Process process, out string fileName)
        {
            var line = default(string);
            fileName = default(string);
            try
            {
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith(Constants.PROCESSING, StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('[', ']');
                        fileName = parts[1];
                        break;
                    }
                }
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith(Constants.COMPLETED, StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split(':', '%');
                        if (parts.Length >= 2)
                        {
                            var value = default(int);
                            if (int.TryParse(parts[1], out value))
                            {
                                this.OnProgress(value);
                            }
                        }
                    }
                    if (this.CancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch
                        {
                            //Nothing can be done.
                        }
                    }
                }
                process.WaitForExit();
                return true;
            }
            finally
            {
                if (this.CancellationTokenSource.IsCancellationRequested)
                {
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            File.Delete(fileName);
                        }
                        catch
                        {
                            //Nothing can be done.
                        }
                    }
                }
            }
        }

        public void Cancel()
        {
            this.CancellationTokenSource.Cancel();
        }

        public class Int32EventArgs : EventArgs
        {
            public Int32EventArgs(int value)
            {
                this.Value = value;
            }

            public int Value { get; private set; }
        }

        public event EventHandler<Int32EventArgs> Progress;

        protected virtual void OnProgress(int value)
        {
            if (this.Progress == null)
            {
                return;
            }
            this.Progress(this, new Int32EventArgs(value));
        }
    }
}
