using BruTile;
using BruTile.Cache;
using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MBTilesPersistentCache
{
    public class MbTileCache : IPersistentCache<byte[]>, IDisposable
    {
        public static List<MbTileCache> openConnections = new List<MbTileCache>();
        SQLiteConnection sqlConn = null;

        public MbTileCache(string filename, string format)
        {
            sqlConn = new SQLiteConnection(filename);
            openConnections.Add(this);
            sqlConn.CreateTable<MBTiles.Domain.metadata>();
            sqlConn.CreateTable<MBTiles.Domain.tiles>();

            var metaList = new List<MBTiles.Domain.metadata>();

            metaList.Add(new MBTiles.Domain.metadata { name = "name", value = "Offline" });
            metaList.Add(new MBTiles.Domain.metadata { name = "type", value = "baselayer" });
            metaList.Add(new MBTiles.Domain.metadata { name = "version", value = "1" });
            metaList.Add(new MBTiles.Domain.metadata { name = "description", value = "Offline" });
            metaList.Add(new MBTiles.Domain.metadata { name = "format", value = format });

            foreach (var meta in metaList)
                sqlConn.InsertOrReplace(meta);

            double[] originalBounds = new double[4] { double.MaxValue, double.MaxValue, double.MinValue, double.MinValue }; // In WGS1984, the total extent of all bounds
            sqlConn.InsertOrReplace(new MBTiles.Domain.metadata { name = "bounds", value = string.Join(",", originalBounds) });
        }

        /// <summary>
        /// Flips the Y coordinate from OSM to TMS format and vice versa.
        /// </summary>
        /// <param name="level">zoom level</param>
        /// <param name="row">Y coordinate</param>
        /// <returns>inverted Y coordinate</returns>
        static int OSMtoTMS(int level, int row)
        {
            return (1 << level) - row - 1;
        }

        public void Add(TileIndex index, byte[] tile)
        {
            MBTiles.Domain.tiles mbtile = new MBTiles.Domain.tiles();
            mbtile.zoom_level = index.Level;
            mbtile.tile_column = index.Col;
            mbtile.tile_row = index.Row;
            mbtile.tile_data = tile;
            mbtile.createDate = DateTime.UtcNow;

            mbtile.tile_row = OSMtoTMS(mbtile.zoom_level, mbtile.tile_row);

            lock (sqlConn)
            {
                MBTiles.Domain.tiles oldTile = sqlConn.Table<MBTiles.Domain.tiles>().Where(x => x.zoom_level == mbtile.zoom_level && x.tile_column == mbtile.tile_column && x.tile_row == mbtile.tile_row).FirstOrDefault();

                if (oldTile != null)
                {
                    mbtile.id = oldTile.id;
                    sqlConn.Update(mbtile);
                }
                else
                    sqlConn.Insert(mbtile);
            }
        }

        public void Dispose()
        {
            if (sqlConn != null)
            {
                lock (sqlConn)
                {
                    sqlConn.Close();
                    sqlConn.Dispose();
                    sqlConn = null;
                }
            }
        }

        public byte[] Find(TileIndex index)
        {
            int level = index.Level;
            int rowNum = OSMtoTMS(level, index.Row);

            lock (sqlConn)
            {
                MBTiles.Domain.tiles oldTile = sqlConn.Table<MBTiles.Domain.tiles>().Where(x => x.zoom_level == level && x.tile_column == index.Col && x.tile_row == rowNum).FirstOrDefault();

                // You may also want to put a check here to 'age' the tile, i.e., if it is too old, return null so a new one is fetched.
                if (oldTile != null)
                    return oldTile.tile_data;
            }

            return null;
        }

        public void Remove(TileIndex index)
        {
            // We don't remove.
        }
    }
}
