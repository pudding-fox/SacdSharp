using NUnit.Framework;
using System.IO;
using System.Xml.Schema;

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
            var fileName = Path.Combine(Media, "[SACD-R] Alan Parsons Project -1984.iso");
            var sacd = SacdFactory.Instance.Create(fileName);
            sacd.InitialiseComponent();
            Assert.That(sacd.Disc.Count == 9);
            Assert.That(sacd.Album.Count == 9);
            Assert.That(sacd.Areas.Count == 2);
            Assert.That(sacd.Areas[0].Info.Count == 6);
            Assert.That(sacd.Areas[0].Tracks.Count == 9);
            Assert.That(sacd.Areas[1].Info.Count == 6);
            Assert.That(sacd.Areas[1].Tracks.Count == 9);
        }

        [TestCase(0, 0, 642564450)]
        public void Test002(int areaIndex, int trackIndex, long length)
        {
            var fileName = Path.Combine(Media, "[SACD-R] Alan Parsons Project -1984.iso");
            var sacd = SacdFactory.Instance.Create(fileName);
            sacd.InitialiseComponent();
            var area = sacd.Areas[areaIndex];
            var track = area.Tracks[trackIndex];
            var extractor = SacdFactory.Instance.Create(sacd, area, track);
            var directoryName = Path.GetTempPath();
            var result = extractor.Extract(directoryName, out fileName);
            Assert.That(new FileInfo(fileName).Length == length);
        }
    }
}
