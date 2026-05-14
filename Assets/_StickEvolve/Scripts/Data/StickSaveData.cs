using System;
using System.Collections.Generic;

namespace StickEvolve.Data
{
    [Serializable]
    public class StickSaveData
    {
        public int version = 3;
        public long gold;
        public int highestWaveCompleted;
        public int highestLevelCompleted;
        public int currentLevel = 1;
        public List<string> ownedCardIds = new();
        public List<CardLevelEntry> cardLevels = new();
        public int commonsStreak;
        public long lastExitUnixTime;
    }

    [Serializable]
    public class CardLevelEntry
    {
        public string cardId;
        public int level;
    }
}
