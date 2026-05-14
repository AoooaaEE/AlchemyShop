using UnityEngine;

namespace StickEvolve.Levels
{
    /// <summary>
    /// Visual theme for a biome: sky gradient, ground/grass colors, tree tints, etc.
    /// Used by the bootstrapper to build the procedural background.
    /// </summary>
    public class BiomeTheme
    {
        public Color[] skyGradient;
        public Color groundColor;
        public Color grassColor;
        public Color grassTuftColor;
        public Color treeFarColor;
        public Color mountainFarColor;
        public Color mountainNearColor;
        public Color cameraBackground;
        public Color sunColor;
        public Color sunHaloColor;
        public Color cloudColor;
        public Color[] flowerColors;

        public static BiomeTheme Plains() => new BiomeTheme
        {
            skyGradient = new[]
            {
                new Color(0.30f, 0.55f, 0.90f),
                new Color(0.45f, 0.70f, 0.95f),
                new Color(0.62f, 0.82f, 0.98f),
                new Color(0.80f, 0.92f, 1.00f),
                new Color(0.95f, 0.96f, 0.90f),
            },
            groundColor = new Color(0.42f, 0.30f, 0.18f),
            grassColor = new Color(0.42f, 0.66f, 0.28f),
            grassTuftColor = new Color(0.32f, 0.55f, 0.22f),
            treeFarColor = new Color(0.22f, 0.42f, 0.28f),
            mountainFarColor = new Color(0.55f, 0.62f, 0.78f),
            mountainNearColor = new Color(0.38f, 0.46f, 0.62f),
            cameraBackground = new Color(0.55f, 0.80f, 0.98f),
            sunColor = new Color(1f, 0.95f, 0.75f),
            sunHaloColor = new Color(1f, 0.95f, 0.75f, 0.35f),
            cloudColor = new Color(1f, 1f, 1f),
            flowerColors = new[]
            {
                new Color(0.95f, 0.85f, 0.30f),
                new Color(0.95f, 0.45f, 0.55f),
                new Color(0.85f, 0.45f, 0.90f),
                new Color(0.95f, 0.95f, 0.95f),
            },
        };

        public static BiomeTheme Ice() => new BiomeTheme
        {
            skyGradient = new[]
            {
                new Color(0.15f, 0.20f, 0.45f),
                new Color(0.25f, 0.35f, 0.60f),
                new Color(0.40f, 0.55f, 0.75f),
                new Color(0.60f, 0.72f, 0.85f),
                new Color(0.80f, 0.88f, 0.95f),
            },
            groundColor = new Color(0.85f, 0.90f, 0.95f),
            grassColor = new Color(0.75f, 0.85f, 0.92f),
            grassTuftColor = new Color(0.70f, 0.82f, 0.90f),
            treeFarColor = new Color(0.30f, 0.50f, 0.55f),
            mountainFarColor = new Color(0.70f, 0.78f, 0.90f),
            mountainNearColor = new Color(0.55f, 0.65f, 0.80f),
            cameraBackground = new Color(0.35f, 0.50f, 0.75f),
            sunColor = new Color(0.90f, 0.92f, 1.0f),
            sunHaloColor = new Color(0.80f, 0.88f, 1.0f, 0.25f),
            cloudColor = new Color(0.90f, 0.92f, 0.98f),
            flowerColors = new[]
            {
                new Color(0.80f, 0.90f, 1.0f),
                new Color(0.70f, 0.85f, 0.95f),
                new Color(0.60f, 0.75f, 0.90f),
                new Color(0.90f, 0.95f, 1.0f),
            },
        };

        public static BiomeTheme Get(BiomeType biome) => biome switch
        {
            BiomeType.Ice => Ice(),
            _ => Plains(),
        };
    }
}
