using BruTile.Predefined;
using BruTile.Web;
using System;

namespace MBTilesPersistentCache
{
    class Program
    {
        static MbTileCache mbStreetTileCache;

        static BruTile.ITileSource GetOSMBasemap(string cacheFilename)
        {
            if (mbStreetTileCache == null)
                mbStreetTileCache = new MbTileCache(cacheFilename, "png");

            HttpTileSource src = new HttpTileSource(new GlobalSphericalMercator(),
                "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
                new[] { "a", "b", "c" }, name: "OpenStreetMap",
                persistentCache: mbStreetTileCache,
                attribution: new BruTile.Attribution("(c) OpenStreetMap contributors", "https://www.openstreetmap.org/copyright"));

            return src;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Initialize our tile source
            var tileSource = GetOSMBasemap("myOfflineDb.mbtiles");

            // Add it to BruTile here

            // Cleanup
            foreach (var conn in MbTileCache.openConnections)
                conn.Dispose();

            MbTileCache.openConnections.Clear();
        }
    }
}
