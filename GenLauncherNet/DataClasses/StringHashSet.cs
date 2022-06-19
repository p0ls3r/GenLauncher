using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class StringHashSet
    {
        private HashSet<string> sHashSt = new HashSet<string>();

        public bool Contains(string key) => sHashSt.Contains(key.ToUpper());

        public bool Remove(string key) => sHashSt.Remove(key.ToUpper());

        public bool Add(string key) => sHashSt.Add(key.ToUpper());
    }
}
