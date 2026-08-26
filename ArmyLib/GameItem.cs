using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmyLib
{
    public class GameItem
    {
        public string Name { get; set; }
        public string Platform { get; set; }
        public string AppIdOrPath { get; set; }
        public string InstallLocation { get; set; }
        public bool IsInstalled { get; set; }
    }
}
