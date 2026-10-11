namespace TinyTrouble
{
    /// <summary>Называет крупное движение героя; одновременно выполняется только одно.</summary>
    public enum CharacterGesture
    {
        /// <summary>Герой только дышит и смотрит на игрока.</summary>
        None,
        /// <summary>Первое приветствие после открытия лобби.</summary>
        Greeting,
        /// <summary>Перенос веса между ногами.</summary>
        WeightShift,
        /// <summary>Короткий взгляд по сторонам.</summary>
        LookAround,
        /// <summary>Весёлый взмах рукой.</summary>
        Wave,
        /// <summary>Взгляд на новую одежду и довольный кивок.</summary>
        ColorReaction,
        /// <summary>Танец с угловатыми движениями робота.</summary>
        DanceRobot,
        /// <summary>Пружинистый танец с небольшими подъёмами тела.</summary>
        DanceSpring,
        /// <summary>Танец победителя с поднятыми руками.</summary>
        DanceVictory
    }
}
