using UnityEngine;

namespace StickEvolve.Combat
{
    public enum CombatTeam { Heroes, Enemies }

    /// <summary>
    /// Помечает GameObject принадлежащим команде. Используется снарядами для фильтра целей —
    /// чтобы не править Unity Layers (TagManager.asset).
    /// </summary>
    public class TeamMember : MonoBehaviour
    {
        public CombatTeam team = CombatTeam.Heroes;
    }
}
