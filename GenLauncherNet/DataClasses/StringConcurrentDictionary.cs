using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenLauncherNet
{
    public class StringConcurrentDictionary<TValue>
    {
        public ConcurrentDictionary<string, TValue> concDict = new ConcurrentDictionary<string, TValue>();

        public bool ContainsKey(string key)
        {            
            return concDict.ContainsKey(key.ToUpper());
        }

        public bool TryGetValue(string key, out TValue value)
        {
            return concDict.TryGetValue(key.ToUpper(), out value);
        }

        public TValue this[string key]
        {
            get => concDict[key.ToUpper()];
            set => concDict[key.ToUpper()] = value;
        }

        public List<KeyValuePair<string, TValue>> ToList()
        {
            return concDict.ToList();
        }

        public bool TryAdd(string key, TValue value)
        {
            return concDict.TryAdd(key.ToUpper(), value);
        }

        public bool TryRemove(string key, out TValue value)
        {
            return concDict.TryRemove(key.ToUpper(), out value);
        }
    }
}
