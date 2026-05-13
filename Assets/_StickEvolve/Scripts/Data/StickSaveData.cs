using System;
using System.Collections.Generic;

namespace StickEvolve.Data
{
    [Serializable]
    public class StickSaveData
    {
        public int version = 1;
        public long gold;
        public int highestWaveCompleted;
        public List<string> ownedCardIds = new();
        public long lastExitUnixTime;
    }
}
