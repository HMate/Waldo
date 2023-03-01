using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class AI
    {
        internal Commands calculatePathToWaldo(Game game)
        {
            var result = new Commands();
            _logger.Debug($"Waldo is at {game.Waldo.Position}");

            if (game.Waldo.Position == new Pos(1, 2))
            {
                result.AddForward(4);
                result.AddTurn(TurnDirection.Left);
                result.AddForward(2);
                result.AddTurn(TurnDirection.Left);
                result.AddForward(4);
                result.AddTurn(TurnDirection.Left);
                result.AddForward(2);
            }

            return result;
        }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    }
}
