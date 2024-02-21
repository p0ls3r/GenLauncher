using System.Collections.Generic;

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
