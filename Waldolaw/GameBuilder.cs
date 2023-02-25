using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class GameBuilder
    {
        public GameBuilder() { }

        public Game Build(UserInputsJSON input)
        {
            List<Item> items = new List<Item>();
            foreach (var item in input.items)
            {
                Pos pos = new Pos();
                items.Add(new Item(item.name, pos, item.fuel));
            }
            // Add satellites and base
            items.Add(new Item("SAT_0", new Pos(0, 0), 0));
            items.Add(new Item("SAT_1", new Pos(input.mapsize - 1, 0), 0));
            items.Add(new Item("SAT_2", new Pos(0, input.mapsize - 1), 0));
            items.Add(new Item("SAT_3", new Pos(input.mapsize - 1, input.mapsize - 1), 0));
            items.Add(new Item("BASE", new Pos((input.mapsize - 1) / 2, input.mapsize - 1), 0));
            items.Add(new Item("SHIP", new Pos((input.mapsize - 1) / 2, input.mapsize - 1), input.fuel));

            return new Game(new Level(input.mapsize), items);
        }
    }
}
