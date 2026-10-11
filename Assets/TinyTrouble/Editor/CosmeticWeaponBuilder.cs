using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Создаёт шесть разных мультяшных форм оружия с деталями на обеих сторонах.</summary>
    public static partial class CosmeticEquipmentBuilder
    {
        /// <summary>Выбирает собственную форму скина; бесплатное оружие сохраняет спокойный серый вид.</summary>
        /// <param name="root">Корень модели.</param>
        /// <param name="item">Выбранный товар.</param>
        /// <param name="body">Цвет корпуса.</param>
        /// <param name="dark">Цвет механики.</param>
        /// <param name="steel">Металл.</param>
        /// <param name="accent">Цвет украшений.</param>
        static void Weapon(GameObject root, CosmeticItem item, Material body, Material dark, Material steel, Material accent)
        {
            if (item.Id.EndsWith("-gray")) { BasicWeapon(root, item, body, dark, steel, accent); return; }
            var glow = Paint(item.Id, "Energy", item.Id.Contains("fire") ? new Color(1, .28f, .055f) : new Color(.25f, .85f, 1), .55f, true);
            var trim = Paint(item.Id, "Trim", item.Id.Contains("pop") ? new Color(1, .37f, .57f) : item.Id.Contains("lime") ? new Color(.14f, .27f, .21f) : new Color(.21f, .25f, .4f), .5f);
            if (item.Id == "pistol-pop") PopPistol(root, body, dark, steel, accent, trim);
            else if (item.Id == "pistol-ice") IcePistol(root, body, dark, steel, glow);
            else if (item.Id == "rifle-lime") SportRifle(root, body, dark, steel, accent, trim);
            else if (item.Id == "rifle-space") SpaceRifle(root, body, dark, steel, accent, glow);
            else if (item.Id == "shotgun-fire") FireShotgun(root, Paint(item.Id, "FireArmor", new Color(.7f, .09f, .055f), .45f), dark, steel, glow);
            else WaveShotgun(root, body, dark, steel, glow);
        }
        /// <summary>Лепит объёмный корпус по рисунку сбоку; обе стороны имеют одинаковую форму.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя детали.</param>
        /// <param name="outline">Контур в плоскости X и Y.</param>
        /// <param name="depth">Толщина.</param>
        /// <param name="position">Положение контура.</param>
        /// <param name="material">Поверхность.</param>
        /// <returns>Деталь для дополнительного поворота.</returns>
        static Transform Profile(GameObject root, string name, Vector2[] outline, float depth, Vector3 position, Material material)
            => MeshPart(root, name, CosmeticMeshBuilder.Extrude(outline, depth), position, material);
        /// <summary>Добавляет округлые крепления или декоративное кольцо из соединённых металлических сегментов.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя украшения.</param>
        /// <param name="center">Центр.</param>
        /// <param name="radius">Радиус.</param>
        /// <param name="width">Толщина металла.</param>
        /// <param name="material">Поверхность.</param>
        /// <param name="rotation">Наклон кольца.</param>
        static void Ring(GameObject root, string name, Vector3 center, float radius, float width, Material material, Quaternion rotation)
        {
            for (var i = 0; i < 24; i++)
            {
                var a = i * Mathf.PI / 12; var b = (i + 1) * Mathf.PI / 12;
                Rod(root, name + i, center + rotation * new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius,
                    center + rotation * new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0) * radius, width, material);
            }
        }
        /// <summary>Добавляет рукоять, спусковую скобу и рельеф обмотки на обеих сторонах.</summary>
        /// <param name="root">Корень оружия.</param>
        /// <param name="x">Положение рукояти.</param>
        /// <param name="body">Цвет рукояти.</param>
        /// <param name="dark">Скоба и обмотка.</param>
        /// <param name="steel">Спуск.</param>
        static void WeaponGrip(GameObject root, float x, Material body, Material dark, Material steel)
        {
            Profile(root, "Grip", new[] { new Vector2(-.12f, .03f), new Vector2(.09f, .02f), new Vector2(.16f, -.4f), new Vector2(-.11f, -.42f), new Vector2(-.16f, -.33f) }, .24f, new Vector3(x, -.12f, 0), body);
            Box(root, "GripHeel", new Vector3(x + .025f, -.54f, 0), new Vector3(.3f, .07f, .27f), .024f, dark);
            for (var side = -1; side <= 1; side += 2)
                for (var i = 0; i < 4; i++) Box(root, "GripStripe" + side + "-" + i, new Vector3(x, -.23f - i * .07f, side * .125f), new Vector3(.17f, .018f, .016f), .007f, dark);
            Rod(root, "GuardBottom", new Vector3(x + .11f, -.33f, 0), new Vector3(x + .42f, -.33f, 0), .045f, dark);
            Rod(root, "GuardFront", new Vector3(x + .42f, -.33f, 0), new Vector3(x + .4f, -.13f, 0), .045f, dark);
            Box(root, "Trigger", new Vector3(x + .24f, -.22f, 0), new Vector3(.035f, .12f, .08f), .012f, steel).localRotation = Quaternion.Euler(0, 0, -20);
        }
        /// <summary>Делает широкое цветное дульце с тёмным углублением, заметным при вращении.</summary>
        /// <param name="root">Корень.</param>
        /// <param name="name">Имя ствола.</param>
        /// <param name="from">Начало.</param>
        /// <param name="to">Конец.</param>
        /// <param name="width">Толщина.</param>
        /// <param name="metal">Обод.</param>
        /// <param name="dark">Глубина.</param>
        static void Muzzle(GameObject root, string name, Vector3 from, Vector3 to, float width, Material metal, Material dark)
        {
            Rod(root, name, from, to, width, metal);
            var axis = (to - from).normalized;
            Rod(root, name + "Bore", to + axis * .001f, to + axis * .005f, width * .67f, dark);
        }
        /// <summary>Создаёт округлый игрушечный пистолет с цветными вставками и конфетти.</summary>
        /// <param name="root">Корень.</param><param name="body">Корпус.</param><param name="dark">Механика.</param>
        /// <param name="steel">Металл.</param><param name="accent">Жёлтые детали.</param><param name="pink">Розовые вставки.</param>
        static void PopPistol(GameObject root, Material body, Material dark, Material steel, Material accent, Material pink)
        {
            Box(root, "RoundSlide", new Vector3(.02f, .08f, 0), new Vector3(.9f, .34f, .35f), .105f, body);
            Box(root, "PinkUnderFrame", new Vector3(-.02f, -.12f, 0), new Vector3(.75f, .15f, .28f), .055f, pink);
            WeaponGrip(root, -.28f, pink, dark, steel);
            Muzzle(root, "PopMuzzle", new Vector3(.38f, .07f, 0), new Vector3(.63f, .07f, 0), .26f, accent, dark);
            Box(root, "TopSight", new Vector3(.3f, .3f, 0), new Vector3(.11f, .12f, .1f), .03f, accent);
            Box(root, "BackSight", new Vector3(-.3f, .29f, 0), new Vector3(.13f, .1f, .12f), .03f, pink);
            for (var side = -1; side <= 1; side += 2)
            {
                Box(root, "CandyPanel" + side, new Vector3(-.05f, .08f, side * .18f), new Vector3(.56f, .19f, .025f), .07f, pink);
                MeshPart(root, "CandyStar" + side, CosmeticMeshBuilder.Star(.095f), new Vector3(-.08f, .08f, side * .208f), accent);
                for (var i = 0; i < 3; i++) Sphere(root, "Confetti" + side + "-" + i, new Vector3(.08f + i * .095f, .04f + (i % 2) * .075f, side * .21f), new Vector3(.055f, .055f, .025f), i % 2 == 0 ? steel : accent);
                for (var i = 0; i < 3; i++) Box(root, "SlideGroove" + side + "-" + i, new Vector3(-.34f + i * .055f, .11f, side * .178f), new Vector3(.022f, .15f, .015f), .008f, dark);
            }
        }
        /// <summary>Создаёт угловатый ледяной пистолет с кристаллами и длинным гранёным носом.</summary>
        /// <param name="root">Корень.</param><param name="body">Лёд.</param><param name="dark">Механика.</param>
        /// <param name="steel">Крепления.</param><param name="glow">Светящиеся грани.</param>
        static void IcePistol(GameObject root, Material body, Material dark, Material steel, Material glow)
        {
            Profile(root, "IceSlide", new[] { new Vector2(-.5f, -.07f), new Vector2(-.43f, .23f), new Vector2(-.16f, .3f), new Vector2(.38f, .18f), new Vector2(.69f, .02f), new Vector2(.51f, -.13f), new Vector2(-.3f, -.17f) }, .27f, Vector3.zero, body);
            WeaponGrip(root, -.32f, dark, body, steel);
            Muzzle(root, "IceMuzzle", new Vector3(.54f, .015f, 0), new Vector3(.72f, .015f, 0), .19f, steel, dark);
            for (var side = -1; side <= 1; side += 2)
            {
                Profile(root, "IceFacet" + side, new[] { new Vector2(-.38f, .11f), new Vector2(-.1f, .22f), new Vector2(.51f, .035f), new Vector2(-.02f, .07f) }, .016f, new Vector3(0, 0, side * .144f), glow);
                Line(root, "FrozenSeam" + side, new[] { new Vector3(-.3f, .02f, side * .155f), new Vector3(-.05f, -.06f, side * .155f), new Vector3(.19f, .005f, side * .155f), new Vector3(.45f, -.025f, side * .155f) }, .018f, steel);
            }
            Profile(root, "CrystalSight", new[] { new Vector2(-.14f, 0), new Vector2(-.055f, .2f), new Vector2(.035f, .05f), new Vector2(.12f, .1f), new Vector2(.15f, 0) }, .1f, new Vector3(-.23f, .27f, 0), glow);
        }
        /// <summary>Собирает спортивный автомат с изогнутым магазином и ажурным прикладом.</summary>
        /// <param name="root">Корень.</param><param name="body">Лайм.</param><param name="dark">Механика.</param>
        /// <param name="steel">Металл.</param><param name="accent">Отделка.</param><param name="trim">Тёмно-зелёные панели.</param>
        static void SportRifle(GameObject root, Material body, Material dark, Material steel, Material accent, Material trim)
        {
            Profile(root, "SportReceiver", new[] { new Vector2(-.65f, -.16f), new Vector2(-.63f, .18f), new Vector2(-.2f, .24f), new Vector2(.66f, .17f), new Vector2(.75f, -.06f), new Vector2(.24f, -.18f) }, .3f, Vector3.zero, body);
            WeaponGrip(root, -.4f, trim, dark, steel);
            Profile(root, "CurvedMagazine", new[] { new Vector2(.12f, -.12f), new Vector2(.35f, -.13f), new Vector2(.43f, -.42f), new Vector2(.58f, -.61f), new Vector2(.36f, -.71f), new Vector2(.22f, -.53f) }, .24f, Vector3.zero, dark);
            Box(root, "MagazineHeel", new Vector3(.47f, -.64f, 0), new Vector3(.27f, .09f, .27f), .025f, accent).localRotation = Quaternion.Euler(0, 0, 22);
            Rod(root, "StockTop", new Vector3(-.6f, .11f, 0), new Vector3(-1.18f, .12f, 0), .115f, trim);
            Rod(root, "StockBrace", new Vector3(-.62f, -.08f, 0), new Vector3(-1.18f, -.23f, 0), .09f, body);
            Box(root, "StockPad", new Vector3(-1.22f, -.04f, 0), new Vector3(.14f, .45f, .28f), .045f, dark);
            Rod(root, "SportBarrel", new Vector3(.58f, .04f, 0), new Vector3(1.18f, .04f, 0), .095f, steel);
            Muzzle(root, "SportBrake", new Vector3(1.12f, .04f, 0), new Vector3(1.31f, .04f, 0), .17f, trim, dark);
            Box(root, "SportRail", new Vector3(.05f, .23f, 0), new Vector3(.94f, .05f, .13f), .014f, dark);
            Ring(root, "OpenSight", new Vector3(-.1f, .38f, 0), .12f, .038f, trim, Quaternion.Euler(0, 90, 0));
            Box(root, "SightMount", new Vector3(-.1f, .29f, 0), new Vector3(.18f, .1f, .16f), .02f, dark);
            for (var side = -1; side <= 1; side += 2)
            {
                Profile(root, "SportPanel" + side, new[] { new Vector2(-.55f, -.07f), new Vector2(-.5f, .12f), new Vector2(.15f, .12f), new Vector2(.05f, -.08f) }, .018f, new Vector3(0, 0, side * .16f), trim);
                for (var i = 0; i < 4; i++) Box(root, "SportVent" + side + "-" + i, new Vector3(.3f + i * .09f, .06f, side * .16f), new Vector3(.045f, .13f, .025f), .015f, dark).localRotation = Quaternion.Euler(0, 0, -20);
                Line(root, "MagazineRib" + side, new[] { new Vector3(.22f, -.22f, side * .128f), new Vector3(.31f, -.47f, side * .128f), new Vector3(.44f, -.6f, side * .128f) }, .025f, body);
                Box(root, "SportBolt" + side, new Vector3(-.31f, .03f, side * .178f), new Vector3(.17f, .055f, .04f), .016f, steel);
            }
        }
        /// <summary>Создаёт космический автомат с открытым реактором между двумя рельсами.</summary>
        /// <param name="root">Корень.</param><param name="body">Корпус.</param><param name="dark">Механика.</param>
        /// <param name="steel">Металл.</param><param name="accent">Золотые кольца.</param><param name="glow">Энергия.</param>
        static void SpaceRifle(GameObject root, Material body, Material dark, Material steel, Material accent, Material glow)
        {
            Profile(root, "SpaceHousing", new[] { new Vector2(-.74f, -.16f), new Vector2(-.84f, .12f), new Vector2(-.63f, .31f), new Vector2(-.16f, .25f), new Vector2(.03f, .09f), new Vector2(-.05f, -.2f) }, .38f, Vector3.zero, body);
            WeaponGrip(root, -.46f, body, dark, steel);
            Profile(root, "SpaceStock", new[] { new Vector2(-.73f, .17f), new Vector2(-1.15f, .32f), new Vector2(-1.37f, .17f), new Vector2(-1.29f, -.25f), new Vector2(-1.05f, -.2f), new Vector2(-.8f, -.07f) }, .23f, Vector3.zero, dark);
            Box(root, "SpaceStockInset", new Vector3(-1.18f, .08f, 0), new Vector3(.13f, .26f, .26f), .04f, accent).localRotation = Quaternion.Euler(0, 0, 15);
            Sphere(root, "Reactor", new Vector3(.31f, .055f, 0), new Vector3(.36f, .29f, .29f), glow);
            for (var sign = -1; sign <= 1; sign += 2)
            {
                Rod(root, "ReactorRail" + sign, new Vector3(-.16f, sign * .21f, 0), new Vector3(.8f, sign * .21f, 0), .095f, body);
                Ring(root, "ReactorCage" + sign, new Vector3(.31f + sign * .22f, .055f, 0), .2f, .034f, accent, Quaternion.Euler(0, 90, 0));
                Box(root, "SpacePanel" + sign, new Vector3(-.46f, .065f, sign * .205f), new Vector3(.38f, .16f, .035f), .045f, dark);
                MeshPart(root, "GalaxyBadge" + sign, CosmeticMeshBuilder.Star(.09f), new Vector3(-.44f, .07f, sign * .233f), accent);
                Line(root, "EnergyCircuit" + sign, new[] { new Vector3(-.7f, -.02f, sign * .204f), new Vector3(-.57f, -.08f, sign * .204f), new Vector3(-.22f, -.08f, sign * .204f), new Vector3(-.13f, .04f, sign * .204f) }, .016f, glow);
            }
            Box(root, "SpaceMuzzleHousing", new Vector3(.89f, .055f, 0), new Vector3(.31f, .43f, .42f), .075f, dark);
            Muzzle(root, "PlasmaMuzzle", new Vector3(.92f, .055f, 0), new Vector3(1.17f, .055f, 0), .27f, accent, dark);
            Box(root, "ScopeStem", new Vector3(-.33f, .32f, 0), new Vector3(.13f, .15f, .16f), .025f, steel);
            Ring(root, "GalaxyScope", new Vector3(-.33f, .5f, 0), .15f, .045f, accent, Quaternion.Euler(0, 90, 0));
            Sphere(root, "ScopeCrystal", new Vector3(-.33f, .5f, 0), new Vector3(.12f, .11f, .11f), glow);
        }
        /// <summary>Собирает широкий двуствольный дробовик с тепловыми вставками и массивным цевьём.</summary>
        /// <param name="root">Корень.</param><param name="body">Красный корпус.</param><param name="dark">Механика.</param>
        /// <param name="steel">Металл.</param><param name="glow">Горячие вставки.</param>
        static void FireShotgun(GameObject root, Material body, Material dark, Material steel, Material glow)
        {
            Box(root, "FireReceiver", new Vector3(-.38f, .02f, 0), new Vector3(.75f, .39f, .47f), .085f, body);
            WeaponGrip(root, -.48f, dark, body, steel);
            Profile(root, "HeavyStock", new[] { new Vector2(-.69f, .17f), new Vector2(-1.13f, .14f), new Vector2(-1.37f, .03f), new Vector2(-1.36f, -.31f), new Vector2(-1.12f, -.26f), new Vector2(-.69f, -.08f) }, .33f, Vector3.zero, dark);
            Box(root, "HeavyStockPad", new Vector3(-1.37f, -.14f, 0), new Vector3(.07f, .36f, .35f), .025f, steel);
            for (var side = -1; side <= 1; side += 2)
            {
                Muzzle(root, "TwinBarrel" + side, new Vector3(-.13f, .055f, side * .14f), new Vector3(1.24f, .055f, side * .14f), .23f, steel, dark);
                Rod(root, "HotBand" + side, new Vector3(1.02f, .055f, side * .14f), new Vector3(1.09f, .055f, side * .14f), .255f, glow);
                Box(root, "FireSidePlate" + side, new Vector3(-.4f, .03f, side * .244f), new Vector3(.43f, .23f, .025f), .055f, dark);
                Profile(root, "FireMotif" + side, new[] { new Vector2(-.15f, -.08f), new Vector2(-.09f, .11f), new Vector2(-.035f, .01f), new Vector2(.04f, .14f), new Vector2(.13f, -.08f) }, .012f, new Vector3(-.4f, .01f, side * .265f), glow);
                for (var i = 0; i < 3; i++) Rod(root, "Shell" + side + "-" + i, new Vector3(-1.12f + i * .105f, -.19f, side * .182f), new Vector3(-1.12f + i * .105f, .05f, side * .182f), .064f, body);
            }
            Box(root, "HeavyPump", new Vector3(.48f, -.145f, 0), new Vector3(.7f, .25f, .55f), .07f, body);
            for (var i = 0; i < 5; i++) Box(root, "PumpWrap" + i, new Vector3(.22f + i * .13f, -.15f, 0), new Vector3(.035f, .28f, .575f), .012f, dark);
            Box(root, "BarrelBridge", new Vector3(.87f, .05f, 0), new Vector3(.065f, .27f, .53f), .02f, dark);
            Box(root, "FireSight", new Vector3(.94f, .24f, 0), new Vector3(.09f, .12f, .1f), .02f, glow);
        }
        /// <summary>Создаёт плавный морской дробовик с гребнем, плавниками и голубыми трубками.</summary>
        /// <param name="root">Корень.</param><param name="body">Морской корпус.</param><param name="dark">Механика.</param>
        /// <param name="steel">Крепления.</param><param name="glow">Голубые трубки.</param>
        static void WaveShotgun(GameObject root, Material body, Material dark, Material steel, Material glow)
        {
            Profile(root, "WaveShell", new[] { new Vector2(-.8f, -.17f), new Vector2(-.77f, .07f), new Vector2(-.53f, .27f), new Vector2(-.17f, .26f), new Vector2(.16f, .13f), new Vector2(.67f, .1f), new Vector2(.98f, -.02f), new Vector2(.73f, -.21f), new Vector2(-.1f, -.21f) }, .32f, Vector3.zero, body);
            WeaponGrip(root, -.43f, dark, body, steel);
            Profile(root, "WaveStock", new[] { new Vector2(-.72f, .1f), new Vector2(-1.23f, .25f), new Vector2(-1.44f, .15f), new Vector2(-1.37f, -.3f), new Vector2(-1.07f, -.16f), new Vector2(-.78f, -.13f) }, .24f, Vector3.zero, body);
            Profile(root, "DorsalFin", new[] { new Vector2(-.2f, 0), new Vector2(-.16f, .13f), new Vector2(.2f, .32f), new Vector2(.16f, .12f), new Vector2(.3f, 0) }, .065f, new Vector3(-.18f, .22f, 0), steel);
            Muzzle(root, "WaveMuzzle", new Vector3(.88f, -.02f, 0), new Vector3(1.17f, -.02f, 0), .255f, dark, dark);
            Ring(root, "WaveLip", new Vector3(1.11f, -.02f, 0), .135f, .027f, glow, Quaternion.Euler(0, 90, 0));
            for (var side = -1; side <= 1; side += 2)
            {
                Line(root, "WaterTube" + side, new[] { new Vector3(-1.24f, .1f, side * .135f), new Vector3(-.92f, -.02f, side * .18f), new Vector3(-.53f, .13f, side * .19f), new Vector3(-.12f, .1f, side * .19f), new Vector3(.26f, -.04f, side * .19f), new Vector3(.85f, -.03f, side * .17f) }, .04f, glow);
                for (var i = 0; i < 3; i++) Profile(root, "SideFin" + side + "-" + i, new[] { new Vector2(-.045f, -.05f), new Vector2(.045f, .13f), new Vector2(.095f, .19f), new Vector2(.07f, -.05f) }, .025f, new Vector3(.27f + i * .16f, -.15f, side * .18f), steel);
                Sphere(root, "WaterValve" + side, new Vector3(-.57f, .04f, side * .21f), new Vector3(.15f, .15f, .07f), dark);
                Sphere(root, "ValveLight" + side, new Vector3(-.57f, .04f, side * .251f), new Vector3(.075f, .075f, .025f), glow);
            }
        }
    }
}
