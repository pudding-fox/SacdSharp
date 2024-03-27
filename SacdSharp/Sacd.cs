using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;

namespace SacdSharp
{
    public class Sacd
    {
        public static string Executable
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(Sacd).Assembly.Location
                );
                return Path.Combine(directory, "x86\\sacd_extract.exe");
            }
        }

        private Sacd()
        {
            this.Disc = new SacdMetaData();
            this.Album = new SacdMetaData();
            this.Areas = new SacdAreas();
            this.Errors = new SacdErrors();
        }

        public Sacd(string fileName) : this()
        {
            this.FileName = fileName;
        }

        public string FileName { get; private set; }

        public SacdMetaData Disc { get; private set; }

        public SacdMetaData Album { get; private set; }

        public SacdAreas Areas { get; private set; }

        public SacdErrors Errors { get; private set; }

        public virtual void InitialiseComponent()
        {
            var arguments = string.Format("-i \"{0}\" -P", this.FileName);
            var startInfo = new ProcessStartInfo(Executable, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var data = new List<string>();
            using (var process = Process.Start(startInfo))
            {
                var line = default(string);
                while ((line = process.StandardOutput.ReadLine()) != null)
                {
                    if (!this.IsEOF(data))
                    {
                        data.Add(line.Trim());
                    }
                }
                process.WaitForExit();
            }
            this.InitialiseComponent(data);
        }

        public virtual void InitialiseComponent(IEnumerable<string> data)
        {
            using (var sequence = data.GetEnumerator())
            {
                var handler = default(Action<string>);
                while (sequence.MoveNext())
                {
                    var element = sequence.Current;
                    if (this.GetHandler(element, ref handler))
                    {
                        continue;
                    }
                    if (handler != null)
                    {
                        handler(element);
                    }
                }
            }
        }

        protected virtual bool IsEOF(IList<string> data)
        {
            //Two blank lines means everything is done.
            return data.Count > 1 && string.IsNullOrEmpty(data[data.Count - 2]) && string.IsNullOrEmpty(data[data.Count - 1]);
        }

        protected virtual bool GetHandler(string data, ref Action<string> handler)
        {
            if (data.StartsWith(Constants.DISC_INFORMATION))
            {
                handler = this.ReadDiscInformation;
                return true;
            }
            if (data.StartsWith(Constants.ALBUM_INFORMATION))
            {
                handler = this.ReadAlbumInformation;
                return true;
            }
            if (data.StartsWith(Constants.AREA_INFORMATION))
            {
                handler = this.ReadAreaInformation;
                return true;
            }
            if (data.StartsWith(Constants.TRACK_LIST))
            {
                handler = this.ReadTrack;
                return true;
            }
            return false;
        }

        protected virtual void ReadDiscInformation(string data)
        {
            var pair = default(KeyValuePair<string, string>);
            if (!this.GetParts(data, out pair))
            {
                return;
            }
            this.Disc[pair.Key] = pair.Value;
        }

        protected virtual void ReadAlbumInformation(string data)
        {
            var pair = default(KeyValuePair<string, string>);
            if (!this.GetParts(data, out pair))
            {
                return;
            }
            this.Album[pair.Key] = pair.Value;
        }

        protected virtual void ReadAreaInformation(string data)
        {
            var pair = default(KeyValuePair<string, string>);
            if (!this.GetParts(data, out pair))
            {
                this.Areas.Current = null;
                return;
            }
            if (this.Areas.Current == null)
            {
                this.Areas.Add(this.Areas.Current = new SacdArea());
            }
            this.Areas.Current.Info[pair.Key] = pair.Value;
        }

        protected virtual void ReadTrack(string data)
        {
            var pair = default(KeyValuePair<string, string>);
            if (!this.GetParts(data, out pair))
            {
                this.Areas.Current.Tracks.Current = null;
                return;
            }
            if (this.Areas.Current.Tracks.Current == null)
            {
                this.Areas.Current.Tracks.Add(this.Areas.Current.Tracks.Current = new SacdTrack());
            }
            this.Areas.Current.Tracks.Current.Info[pair.Key] = pair.Value;
        }

        protected virtual bool GetParts(string data, out KeyValuePair<string, string> pair)
        {
            var parts = data.Split(new[] { ":" }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                parts[0] = parts[0].Split('[')[0];
                pair = new KeyValuePair<string, string>(
                    parts[0].Trim(),
                    parts[1].Trim()
                );
                return true;
            }
            pair = default(KeyValuePair<string, string>);
            return false;
        }
    }

    public class SacdMetaData : Dictionary<string, string>
    {
        public SacdMetaData() : base(StringComparer.OrdinalIgnoreCase)
        {

        }

        public string Title
        {
            get
            {
                var title = default(string);
                this.TryGetValue(Constants.TITLE, out title);
                return title;
            }
        }

        public string Artist
        {
            get
            {
                var title = default(string);
                this.TryGetValue(Constants.ARTIST, out title);
                return title;
            }
        }

        public string AreaDescription
        {
            get
            {
                var title = default(string);
                this.TryGetValue(Constants.AREA_DESCRIPTION, out title);
                return title;
            }
        }
    }

    public class SacdAreas : List<SacdArea>
    {
        public SacdArea this[bool isStereo]
        {
            get
            {
                return this.FirstOrDefault(area => area.IsStereo == isStereo);
            }
        }

        internal SacdArea Current { get; set; }
    }

    public class SacdArea
    {
        public SacdArea()
        {
            this.Info = new SacdMetaData();
            this.Tracks = new SacdTracks();
        }

        public SacdMetaData Info { get; private set; }

        public SacdTracks Tracks { get; private set; }

        internal bool IsStereo
        {
            get
            {
                var description = default(string);
                if (this.Info.TryGetValue(Constants.AREA_DESCRIPTION, out description))
                {
                    if (string.Equals(description, Constants.STEREO, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal bool IsEmpty
        {
            get
            {
                return this.Info.Count == 0 && this.Tracks.Count == 0;
            }
        }
    }

    public class SacdTracks : List<SacdTrack>
    {
        internal SacdTrack Current { get; set; }
    }

    public class SacdTrack
    {
        public SacdTrack()
        {
            this.Info = new SacdMetaData();
        }

        public SacdMetaData Info { get; private set; }

        internal bool IsEmpty
        {
            get
            {
                return this.Info.Count == 0;
            }
        }
    }

    public class SacdErrors : List<SacdError>
    {

    }

    public class SacdError
    {
        public SacdError(string message)
        {
            this.Message = message;
        }

        public string Message { get; private set; }
    }
}
