using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class LevelBuilder
    {
        public LevelBuilder() { }

        public Level Build(UserInputsJSON input)
        {
            return new Level(input.mapsize);
        }
    }
}
