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
        public List<string> ownedCardIds = new();
        public List<CardLevelEntry> cardLevels = new();
        public int commonsStreak;
        public long lastExitUnixTime;

        // Прогресс кампании
        public int highestUnlockedLevel = 1;
        public List<int> levelStars = new();
    }

    [Serializable]
    public class CardLevelEntry
    {
        public string cardId;
        public int level;
    }
}
