using StickEvolve.Core;
using UnityEngine;

namespace StickEvolve.Cards
{
    public enum CardEffectKind
    {
        DamageMultiplier,
        FireRateMultiplier,
        RangeAdd,
        MaxHpMultiplier,
        CritChanceAdd,
        CritMultiplierAdd,
        BulletSpeedAdd,
        SpawnExtraHero,
        FullHeal,
        SpawnHeroOfClass,
        MultiShotAdd,     // +N доп.снарядов за выстрел (с разбросом)
        BulletPierceAdd,  // +N сквозных целей у пули
        LifestealAdd,     // +X% урона возвращается в HP стрелка
        ThornsAdd,        // +X фиксированного урона врагу при ближнем ударе по герою
        GoldGainMult,     // ×N коэффициент золота с убийств
    }

    public enum CardRarity { Common, Rare, Epic, Legendary }

    /// <summary>
    /// Описание карты апгрейда. В прототипе все карты создаются программно (CardCatalog),
    /// но класс совместим с ScriptableObject если позже захотим редактируемые ассеты.
    /// </summary>
    [CreateAssetMenu(fileName = "Card_New", menuName = "StickEvolve/Card", order = 1)]
    public class CardSO : ScriptableObject
    {
        public string id = "card_id";
        public string displayName = "Карта";
        public string description = "Описание";

        public CardEffectKind effect = CardEffectKind.DamageMultiplier;
        public float value = 1.1f;
        public float secondaryValue = 0f;
        public CardRarity rarity = CardRarity.Common;

        public Color frameColor = ColorPalette.CardFrame;
    }
}
