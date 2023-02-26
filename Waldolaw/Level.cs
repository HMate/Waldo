using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class Level
    {
        public int Size { get; private set; }

        public Level(int size)
        {
            Size = size;
            _grid = Enumerable.Range(1, Size).Select(y => Enumerable.Range(1, Size).Select(x => new Cell()).ToArray()).ToArray();
        }

        public Item? ItemAt(int x, int y)
        {
            return _grid[y][x].Items.FirstOrDefault();
        }

        internal void PlaceItem(Pos position, Item item)
        {
            _grid[position.Y][position.X].Items.Add(item);
        }

        private class Cell
        {
            public List<Item> Items = new();
        }

        private readonly Cell[][] _grid;
    }
}
