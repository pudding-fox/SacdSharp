﻿using NUnit.Framework;
using System.IO;
using System.Threading;

namespace SacdSharp.Tests
{
    [TestFixture]
    public class SacdTests
    {
        public static string Media
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(SacdTests).Assembly.Location
                );
                return Path.Combine(directory, "Media");
            }
        }

        [Test]
        public void Test001()
        {
            var sacd = SacdFactory.Instance.Create(Path.Combine(Media, "[SACD-R] Alan Parsons Project -1984.iso"));
            sacd.InitialiseComponent();
            Assert.That(sacd.Disc.Count == 9);
            Assert.That(sacd.Album.Count == 9);
            Assert.That(sacd.Areas.Count == 2);
            Assert.That(sacd.Areas[0].Info.Count == 6);
            Assert.That(sacd.Areas[0].Tracks.Count == 9);
            Assert.That(sacd.Areas[1].Info.Count == 6);
            Assert.That(sacd.Areas[1].Tracks.Count == 9);
        }

        [TestCase(true, 0, 214188386)]
        [TestCase(false, 0, 642564450)]
        public void Test002(bool isStereo, int trackIndex, long length)
        {
            var sacd = SacdFactory.Instance.Create(Path.Combine(Media, "[SACD-R] Alan Parsons Project -1984.iso"));
            sacd.InitialiseComponent();
            var area = sacd.Areas[isStereo];
            var track = area.Tracks[trackIndex];
            var extractor = SacdFactory.Instance.Create(sacd, area, track);
            var directoryName = Path.GetTempPath();
            var fileName = extractor.GetFileName(directoryName);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            var result = extractor.Extract(directoryName, out fileName);
            Assert.That(result);
            Assert.That(new FileInfo(fileName).Length == length);
            Assert.That(extractor.IsExtracted(directoryName, out fileName));
        }

        [Test]
        public void Test003()
        {
            var sacd = SacdFactory.Instance.Create(Path.Combine(Media, "[SACD-R] Alan Parsons Project -1984.iso"));
            sacd.InitialiseComponent();
            var area = sacd.Areas[0];
            var track = area.Tracks[0];
            var extractor = SacdFactory.Instance.Create(sacd, area, track);
            var directoryName = Path.GetTempPath();
            var fileName = extractor.GetFileName(directoryName);
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            var thread = new Thread(() => extractor.Extract(directoryName, out fileName));
            thread.Start();
            extractor.Cancel();
            thread.Join();
            Assert.That(!File.Exists(fileName));
        }
    }
}
