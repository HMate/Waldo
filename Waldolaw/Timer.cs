using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waldolaw
{
    public class Timer
    {
        private Stopwatch _timer;

        public Timer()
        {
            _timer = System.Diagnostics.Stopwatch.StartNew();
        }

        public long TimeMs()
        {
            return _timer.ElapsedMilliseconds;
        }
    }
}
