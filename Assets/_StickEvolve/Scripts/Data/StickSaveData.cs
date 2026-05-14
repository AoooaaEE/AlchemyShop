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

        // Мета-прогресс (сохраняется между раннами):
        public long metaGems;
        public List<MetaUpgradeEntry> metaUpgrades = new();
        public bool tutorialShown;
        public int highestActReached;
        public long totalEnemiesKilled;
    }

    [Serializable]
    public class CardLevelEntry
    {
        public string cardId;
        public int level;
    }

    [Serializable]
    public class MetaUpgradeEntry
    {
        public string upgradeId;
        public int level;
    }
}
