using System;
using System.Collections.Generic;

namespace Alchemy.Data
{
    [Serializable]
    public class SavedUpgrade
    {
        public string id;
        public int    level;
    }

    [Serializable]
    public class SaveData
    {
        public int  version = 1;
        public long gold;
        public int  gems;
        public long lastExitUnixTime;
        public List<SavedUpgrade> upgrades = new();
    }
}