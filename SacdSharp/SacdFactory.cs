namespace SacdSharp
{
    public class SacdFactory
    {
        public Sacd Create(string fileName)
        {
            return new Sacd(fileName);
        }

        public SacdExtractor Create(Sacd sacd, SacdArea area, SacdTrack track)
        {
            return new SacdExtractor(sacd, area, track);
        }

        public static readonly SacdFactory Instance = new SacdFactory();
    }
}
