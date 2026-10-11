using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Собирает ледяную, неоновую и космическую кирки с украшениями со всех сторон.</summary>
    public static partial class CosmeticEquipmentBuilder
    {
        /// <summary>Добавляет обмотанную рукоять, металлические крепления и особую голову выбранной кирки.</summary>
        /// <param name="root">Корень кирки.</param><param name="item">Товар.</param><param name="body">Основной цвет.</param>
        /// <param name="dark">Тёмная обмотка.</param><param name="steel">Металл.</param><param name="accent">Украшения.</param>
        static void Pickaxe(GameObject root, CosmeticItem item, Material body, Material dark, Material steel, Material accent)
        {
            if (item.Id.EndsWith("-gray")) { BasicPickaxe(root, item, body, dark, steel, accent); return; }
            var glow = Paint(item.Id, "Energy", item.Id.Contains("space") ? new Color(.6f, .3f, 1) : new Color(.2f, .84f, 1), .55f, true);
            var pale = Paint(item.Id, "Highlight", item.Id.Contains("space") ? new Color(1, .87f, .48f) : new Color(.7f, .94f, 1), .65f);
            Rod(root, "Shaft", new Vector3(0, .07f, 0), new Vector3(0, 1.38f, 0), .095f, dark);
            Rod(root, "LowerGrip", new Vector3(0, .12f, 0), new Vector3(0, .66f, 0), .135f, body);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * 2.1f;
                Rod(root, "DiagonalWrap" + i, new Vector3(Mathf.Sin(angle) * .07f, .15f + i * .06f, Mathf.Cos(angle) * .07f),
                    new Vector3(Mathf.Sin(angle + 2.1f) * .07f, .21f + i * .06f, Mathf.Cos(angle + 2.1f) * .07f), .026f, dark);
            }
            for (var i = 0; i < 3; i++) Rod(root, "ShaftCollar" + i, new Vector3(0, .7f + i * .26f, 0), new Vector3(0, .745f + i * .26f, 0), .145f, steel);
            Box(root, "HeadSocket", new Vector3(0, 1.35f, 0), new Vector3(.24f, .28f, .27f), .05f, steel);
            Rod(root, "Pommel", Vector3.zero, new Vector3(0, .1f, 0), .17f, pale);
            if (item.Id.Contains("ice")) CrystalPickaxe(root, body, dark, steel, glow, pale);
            else if (item.Id.Contains("neon")) NeonPickaxe(root, body, dark, steel, glow, pale);
            else CosmicPickaxe(root, body, dark, accent, glow, pale);
        }
        /// <summary>Создаёт крупные ледяные грани, острые кристаллы, трещины и стальные зажимы.</summary>
        /// <param name="root">Корень.</param><param name="body">Лёд.</param><param name="dark">Обмотка.</param>
        /// <param name="steel">Зажимы.</param><param name="glow">Трещины.</param><param name="pale">Светлые грани.</param>
        static void CrystalPickaxe(GameObject root, Material body, Material dark, Material steel, Material glow, Material pale)
        {
            Profile(root, "IceSpine", new[] { new Vector2(-.83f, -.25f), new Vector2(-.57f, .19f), new Vector2(-.22f, .35f), new Vector2(.19f, .28f), new Vector2(.55f, .1f), new Vector2(.82f, -.27f), new Vector2(.38f, -.04f), new Vector2(-.05f, .09f), new Vector2(-.4f, .01f) }, .23f, new Vector3(0, 1.42f, 0), body);
            for (var side = -1; side <= 1; side += 2)
            {
                Profile(root, "LargeFacet" + side, new[] { new Vector2(-.73f, -.12f), new Vector2(-.2f, .28f), new Vector2(.65f, -.11f), new Vector2(.12f, .07f), new Vector2(-.36f, .09f) }, .018f, new Vector3(0, 1.42f, side * .13f), pale);
                Line(root, "CrystalCrack" + side, new[] { new Vector3(-.46f, 1.51f, side * .15f), new Vector3(-.25f, 1.55f, side * .15f), new Vector3(-.1f, 1.48f, side * .15f), new Vector3(.07f, 1.59f, side * .15f), new Vector3(.39f, 1.51f, side * .15f) }, .019f, glow);
                Box(root, "ClampFace" + side, new Vector3(0, 1.47f, side * .16f), new Vector3(.18f, .31f, .055f), .025f, steel);
                Sphere(root, "ClampRivet" + side, new Vector3(0, 1.47f, side * .195f), new Vector3(.075f, .075f, .025f), dark);
            }
            for (var i = 0; i < 3; i++)
            {
                var crystal = Profile(root, "IceSpike" + i, new[] { new Vector2(-.1f, 0), new Vector2(-.065f, .26f), new Vector2(.025f, .43f), new Vector2(.12f, .16f), new Vector2(.1f, -.04f) }, .14f, new Vector3(-.32f + i * .29f, 1.64f, 0), i % 2 == 0 ? body : pale);
                crystal.localRotation = Quaternion.Euler(0, 0, (1 - i) * 24);
            }
            Ring(root, "GripCharmRing", new Vector3(.1f, .78f, 0), .085f, .018f, steel, Quaternion.identity);
            Profile(root, "IceCharm", new[] { new Vector2(0, -.13f), new Vector2(-.065f, 0), new Vector2(0, .09f), new Vector2(.065f, 0) }, .045f, new Vector3(.18f, .65f, 0), pale);
        }
        /// <summary>Создаёт изгиб неонового лезвия, видимый аккумулятор и световые дорожки с обеих сторон.</summary>
        /// <param name="root">Корень.</param><param name="body">Корпус.</param><param name="dark">Лезвие.</param>
        /// <param name="steel">Крепления.</param><param name="glow">Дорожки.</param><param name="pale">Острая кромка.</param>
        static void NeonPickaxe(GameObject root, Material body, Material dark, Material steel, Material glow, Material pale)
        {
            var outline = new[] { new Vector2(-.82f, -.32f), new Vector2(-.71f, .12f), new Vector2(-.46f, .39f), new Vector2(-.07f, .46f), new Vector2(.4f, .33f), new Vector2(.79f, -.04f), new Vector2(.43f, .08f), new Vector2(.05f, .18f), new Vector2(-.32f, .13f), new Vector2(-.58f, -.06f) };
            Profile(root, "NeonBlade", outline, .2f, new Vector3(0, 1.43f, 0), dark);
            Box(root, "Battery", new Vector3(0, 1.44f, 0), new Vector3(.28f, .42f, .34f), .06f, body);
            for (var side = -1; side <= 1; side += 2)
            {
                Line(root, "NeonEdge" + side, new[] { new Vector3(-.76f, 1.18f, side * .116f), new Vector3(-.65f, 1.56f, side * .116f), new Vector3(-.43f, 1.78f, side * .116f), new Vector3(-.06f, 1.84f, side * .116f), new Vector3(.37f, 1.72f, side * .116f), new Vector3(.7f, 1.42f, side * .116f) }, .03f, pale);
                Line(root, "BladeCircuit" + side, new[] { new Vector3(-.52f, 1.59f, side * .12f), new Vector3(-.36f, 1.67f, side * .12f), new Vector3(.06f, 1.67f, side * .12f), new Vector3(.32f, 1.61f, side * .12f), new Vector3(.47f, 1.52f, side * .12f) }, .025f, glow);
                Box(root, "BatteryScreen" + side, new Vector3(0, 1.44f, side * .18f), new Vector3(.19f, .28f, .02f), .035f, dark);
                for (var i = 0; i < 3; i++) Box(root, "ChargeBar" + side + "-" + i, new Vector3(0, 1.36f + i * .075f, side * .195f), new Vector3(.12f, .035f, .015f), .01f, glow);
                Line(root, "HandleCircuit" + side, new[] { new Vector3(0, 1.22f, side * .059f), new Vector3(0, .76f, side * .059f) }, .023f, glow);
                for (var i = 0; i < 2; i++) Sphere(root, "BladeRivet" + side + "-" + i, new Vector3(-.23f + i * .48f, 1.69f, side * .13f), new Vector3(.055f, .055f, .025f), steel);
            }
            Box(root, "BatteryTop", new Vector3(0, 1.66f, 0), new Vector3(.31f, .055f, .35f), .015f, steel);
        }
        /// <summary>Собирает полумесяц вокруг звёздного ядра, золотую орбиту и украшенную рукоять.</summary>
        /// <param name="root">Корень.</param><param name="body">Космический корпус.</param><param name="dark">Обмотка.</param>
        /// <param name="gold">Золотая отделка.</param><param name="glow">Энергия.</param><param name="pale">Свет звезды.</param>
        static void CosmicPickaxe(GameObject root, Material body, Material dark, Material gold, Material glow, Material pale)
        {
            var moon = new[] { new Vector2(-.67f, -.32f), new Vector2(-.79f, .02f), new Vector2(-.69f, .39f), new Vector2(-.42f, .64f), new Vector2(-.07f, .72f), new Vector2(.29f, .58f), new Vector2(.55f, .25f), new Vector2(.34f, .39f), new Vector2(.02f, .48f), new Vector2(-.32f, .36f), new Vector2(-.48f, .07f), new Vector2(-.47f, -.15f) };
            Profile(root, "MoonBlade", moon, .18f, new Vector3(.12f, 1.28f, 0), body);
            Ring(root, "StarOrbit", new Vector3(.05f, 1.6f, 0), .3f, .044f, gold, Quaternion.Euler(0, 25, 0));
            MeshPart(root, "StarCore", CosmeticMeshBuilder.Star(.22f), new Vector3(.05f, 1.6f, 0), pale);
            for (var side = -1; side <= 1; side += 2)
            {
                Line(root, "MoonEdge" + side, new[] { new Vector3(-.51f, .99f, side * .106f), new Vector3(-.6f, 1.3f, side * .106f), new Vector3(-.5f, 1.66f, side * .106f), new Vector3(-.26f, 1.87f, side * .106f), new Vector3(.05f, 1.94f, side * .106f), new Vector3(.35f, 1.82f, side * .106f) }, .022f, gold);
                for (var i = 0; i < 3; i++) MeshPart(root, "SmallStar" + side + "-" + i, CosmeticMeshBuilder.Star(.045f + i * .007f), new Vector3(-.48f + i * .18f, 1.57f + i * .13f, side * .11f), pale);
                MeshPart(root, "HandleStar" + side, CosmeticMeshBuilder.Star(.065f), new Vector3(0, .88f, side * .066f), gold);
            }
            Rod(root, "OrbitBrace", new Vector3(0, 1.32f, 0), new Vector3(.05f, 1.39f, 0), .085f, gold);
            Sphere(root, "OrbitGem", new Vector3(.31f, 1.72f, -.12f), new Vector3(.1f, .1f, .1f), glow);
            Sphere(root, "PommelGem", new Vector3(0, -.06f, 0), new Vector3(.16f, .18f, .16f), glow);
            Ring(root, "HandleCrown", new Vector3(0, 1.06f, 0), .085f, .025f, gold, Quaternion.Euler(90, 0, 0));
        }
    }
}
