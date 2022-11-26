using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class SessionInformation
    {
        public bool Connected { get; set; }

        public Game GameMode { get; set; }
    }

    public enum Game
    {
        ZeroHour = 0,
        Generals = 1
    }
}
