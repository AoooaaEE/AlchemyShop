using System.Collections.Generic;
using StickEvolve.Wave;

namespace StickEvolve.Core
{
    /// <summary>
    /// Описание одного уровня кампании. Один уровень = 10 волн, последняя — босс.
    /// Создаётся CampaignBuilder-ом для каждого из 40 уровней (4 биома × 10).
    /// </summary>
    public class LevelDefinition
    {
        public int levelNumber;      // 1..40
        public int biomeIndex;       // 0=Forest, 1=Snow, 2=Desert, 3=Castle
        public int levelInBiome;     // 1..10
        public string biomeName;
        public string levelName;
        public List<WaveConfig> waves = new();
        public bool hasMiniBoss;     // на 5-й волне уровня 5 каждого биома
        public bool hasFinalBoss;    // на 10-й волне уровня 10 каждого биома
    }
}
