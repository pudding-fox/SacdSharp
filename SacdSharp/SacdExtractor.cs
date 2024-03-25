using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace SacdSharp
{
    public class SacdExtractor
    {
        public SacdExtractor(Sacd sacd, SacdArea area, SacdTrack track)
        {
            this.Sacd = sacd;
            this.Area = area;
            this.Track = track;
        }

        public Sacd Sacd { get; private set; }

        public SacdArea Area { get; private set; }

        public SacdTrack Track { get; private set; }

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
            fileName = default(string);
            using (var process = Process.Start(startInfo))
            {
                var line = default(string);
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (line.StartsWith(Constants.PROCESSING, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('[', ']');
                        fileName = parts[1];
                    }
                }
                process.WaitForExit();
            }
            return File.Exists(fileName);
        }
    }
}
