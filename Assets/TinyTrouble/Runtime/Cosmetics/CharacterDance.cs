using UnityEngine;

namespace TinyTrouble
{
    /// <summary>Передаёт просьбу о танце единому управлению движениями героя.</summary>
    public sealed class CharacterDance : MonoBehaviour
    {
        /// <summary>Единственный управляющий жестами компонент.</summary>
        [SerializeField] CharacterLife life;
        /// <summary>Подключает движения героя.</summary>
        /// <param name="life">Управление жестами.</param>
        public void Configure(CharacterLife life) { this.life = life; }
        /// <summary>Пробует показать танец по коду товара.</summary>
        /// <param name="id">Код танца каталога.</param>
        /// <returns>Начался ли танец.</returns>
        public bool Preview(string id)
        {
            if (!life) return false;
            switch (id)
            {
                case "robot": return life.TryStartDance(CharacterGesture.DanceRobot);
                case "spring": return life.TryStartDance(CharacterGesture.DanceSpring);
                case "victory": return life.TryStartDance(CharacterGesture.DanceVictory);
                default: return false;
            }
        }
        /// <summary>Плавно завершает показ танца.</summary>
        public void Stop() { if (life) life.StopDance(); }
    }
}
