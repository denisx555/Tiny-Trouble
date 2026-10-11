using UnityEditor;
using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Собирает мультяшное снаряжение с собственными силуэтами, крупными деталями и цветными акцентами.</summary>
    public static partial class CosmeticEquipmentBuilder
    {
        /// <summary>Создаёт объёмный предмет, не изменяя каталог и покупки игрока.</summary>
        /// <param name="root">Корень будущего шаблона.</param>
        /// <param name="item">Описание предмета.</param>
        /// <param name="basis">Основной сохранённый материал.</param>
        public static void Build(GameObject root, CosmeticItem item, Material basis)
        {
            var gray = item.Id.EndsWith("-gray");
            var dark = Paint(item.Id, "Dark", new Color(.12f, .16f, .22f), .38f);
            var steel = Paint(item.Id, "Steel", new Color(.63f, .7f, .78f), .6f);
            var accentColor = gray ? new Color(.72f, .76f, .8f) : item.Id.Contains("clouds") ? new Color(.93f, .97f, 1) : item.Id.Contains("ice") || item.Id.Contains("neon") ? new Color(.25f, .95f, 1) : item.Id.Contains("space") || item.Id.Contains("stars") ? new Color(1, .72f, .22f) : new Color(1, .88f, .32f);
            var accent = Paint(item.Id, "Accent", accentColor, .32f, !gray && (item.Id.Contains("neon") || item.Id.Contains("stars")));
            if (item.Category == CosmeticCategory.Weapon) Weapon(root, item, basis, dark, steel, accent);
            else if (item.Category == CosmeticCategory.Pickaxe) Pickaxe(root, item, basis, dark, steel, accent);
            else if (item.Category == CosmeticCategory.Parachute) Parachute(root, item, basis, dark, accent);
            else if (item.Category == CosmeticCategory.Spray) Spray(root, item, basis, dark);
            else if (item.Category == CosmeticCategory.Trail) Trail(root, item, basis, accent);
        }
        /// <summary>Создаёт бесплатное серое оружие со скруглённым корпусом и механическими деталями.</summary>
        /// <param name="root">Корень оружия.</param>
        /// <param name="item">Тип и расцветка оружия.</param>
        /// <param name="body">Цвет корпуса.</param>
        /// <param name="dark">Тёмная механика.</param>
        /// <param name="steel">Светлый металл.</param>
        /// <param name="accent">Крупные декоративные детали.</param>
        static void BasicWeapon(GameObject root, CosmeticItem item, Material body, Material dark, Material steel, Material accent)
        {
            var pistol = item.Slot == CosmeticSlot.Pistol; var shotgun = item.Slot == CosmeticSlot.Shotgun;
            var length = pistol ? .78f : shotgun ? 1.28f : 1.35f;
            Box(root, "Receiver", Vector3.zero, new Vector3(length, .28f, .29f), .05f, body);
            Box(root, "LowerFrame", new Vector3(-.08f, -.17f, 0), new Vector3(pistol ? .5f : .73f, .12f, .26f), .035f, dark);
            var grip = Box(root, "Grip", new Vector3(pistol ? -.23f : -.29f, -.37f, 0), new Vector3(.22f, .4f, .25f), .045f, body); grip.localRotation = Quaternion.Euler(0, 0, -14);
            for (var i = 0; i < 3; i++) Box(root, "GripRib" + i, new Vector3(pistol ? -.2f : -.26f, -.28f - i * .075f, -.135f), new Vector3(.14f, .017f, .012f), .006f, dark);
            var barrelStart = length * .5f;
            Rod(root, "Barrel", new Vector3(barrelStart - .08f, .035f, 0), new Vector3(barrelStart + (pistol ? .08f : .43f), .035f, 0), shotgun ? .15f : .09f, dark);
            Rod(root, "Muzzle", new Vector3(barrelStart + (pistol ? .04f : .38f), .035f, 0), new Vector3(barrelStart + (pistol ? .1f : .48f), .035f, 0), shotgun ? .18f : .13f, steel);
            Rod(root, "MuzzleBore", new Vector3(barrelStart + (pistol ? .101f : .481f), .035f, 0), new Vector3(barrelStart + (pistol ? .105f : .485f), .035f, 0), shotgun ? .115f : .065f, dark);
            Box(root, "TopRail", new Vector3(0, .167f, 0), new Vector3(length * .82f, .05f, .18f), .018f, dark);
            Box(root, "FrontSight", new Vector3(length * .37f, .23f, 0), new Vector3(.065f, .08f, .075f), .016f, accent);
            Box(root, "RearSight", new Vector3(-length * .36f, .225f, 0), new Vector3(.09f, .075f, .12f), .015f, steel);
            Box(root, "TriggerGuardBase", new Vector3(.015f, -.32f, 0), new Vector3(.29f, .047f, .17f), .018f, dark);
            Box(root, "TriggerGuardFront", new Vector3(.16f, -.255f, 0), new Vector3(.04f, .15f, .17f), .012f, dark);
            Box(root, "Trigger", new Vector3(.015f, -.24f, 0), new Vector3(.035f, .09f, .08f), .01f, steel).localRotation = Quaternion.Euler(0, 0, -15);
            for (var side = -1; side <= 1; side += 2)
            {
                Box(root, "SidePlate" + side, new Vector3(-.08f, .01f, side * .151f), new Vector3(length * .53f, .14f, .014f), .012f, accent);
                Box(root, "EjectionPort" + side, new Vector3(.14f, .085f, side * .165f), new Vector3(.17f, .045f, .02f), .01f, dark);
                for (var i = 0; i < 3; i++) Box(root, "Vent" + side + "-" + i, new Vector3(-length * .38f + i * .055f, .08f, side * .167f), new Vector3(.016f, .11f, .016f), .006f, dark);
                var badge = MeshPart(root, "Badge" + side, CosmeticMeshBuilder.Star(.075f), new Vector3(-.13f, 0, side * .168f), body);
                if (side > 0) badge.localRotation = Quaternion.Euler(0, 180, 0);
            }
            if (!pistol)
            {
                Box(root, "StockNeck", new Vector3(-length * .55f, -.035f, 0), new Vector3(.3f, .14f, .2f), .03f, steel);
                Box(root, "Stock", new Vector3(-length * .72f, -.065f, 0), new Vector3(.4f, .32f, .28f), .06f, body);
                Box(root, "StockPad", new Vector3(-length * .87f, -.065f, 0), new Vector3(.07f, .35f, .3f), .025f, dark);
                if (shotgun)
                {
                    Box(root, "Pump", new Vector3(.47f, -.12f, 0), new Vector3(.47f, .22f, .35f), .06f, accent);
                    for (var i = 0; i < 4; i++) Box(root, "PumpRib" + i, new Vector3(.32f + i * .085f, -.12f, -.18f), new Vector3(.025f, .14f, .018f), .007f, dark);
                }
                else
                {
                    Box(root, "Magazine", new Vector3(.24f, -.37f, 0), new Vector3(.23f, .38f, .23f), .035f, dark).localRotation = Quaternion.Euler(0, 0, 10);
                    Box(root, "MagazineBase", new Vector3(.28f, -.54f, 0), new Vector3(.28f, .055f, .26f), .02f, accent);
                    Box(root, "Optic", new Vector3(-.05f, .28f, 0), new Vector3(.23f, .15f, .18f), .03f, body);
                    Box(root, "OpticGlass", new Vector3(.071f, .28f, 0), new Vector3(.012f, .09f, .12f), .004f, accent);
                }
            }
        }
        /// <summary>Собирает бесплатную стальную кирку с обмоткой и креплениями на обеих сторонах.</summary>
        /// <param name="root">Корень кирки.</param>
        /// <param name="item">Выбранный стиль.</param>
        /// <param name="body">Основной цвет.</param>
        /// <param name="dark">Обмотка рукояти.</param>
        /// <param name="steel">Металлические крепления.</param>
        /// <param name="accent">Цвет кромки и украшений.</param>
        static void BasicPickaxe(GameObject root, CosmeticItem item, Material body, Material dark, Material steel, Material accent)
        {
            Rod(root, "Shaft", new Vector3(0, .03f, 0), new Vector3(0, 1.3f, 0), .095f, body);
            Rod(root, "Grip", new Vector3(0, .1f, 0), new Vector3(0, .55f, 0), .125f, dark);
            for (var i = 0; i < 5; i++) Rod(root, "Wrap" + i, new Vector3(0, .13f + i * .085f, 0), new Vector3(0, .15f + i * .085f, 0), .137f, accent);
            Box(root, "HeadSocket", new Vector3(0, 1.3f, 0), new Vector3(.23f, .23f, .26f), .05f, steel);
            var blade = new[] { new Vector2(-.72f, -.22f), new Vector2(-.48f, .14f), new Vector2(0, .22f), new Vector2(.47f, .13f), new Vector2(.74f, -.17f), new Vector2(.42f, -.02f), new Vector2(0, .025f), new Vector2(-.42f, -.05f) };
            MeshPart(root, "ForgedBlade", CosmeticMeshBuilder.Extrude(blade, .19f), new Vector3(0, 1.4f, 0), steel);
            for (var side = -1; side <= 1; side += 2)
            {
                Box(root, "BladeInlay" + side, new Vector3(0, 1.48f, side * .105f), new Vector3(.85f, .055f, .018f), .012f, accent);
                Sphere(root, "SocketBolt" + side, new Vector3(0, 1.32f, side * .14f), new Vector3(.09f, .09f, .035f), dark);
            }
            Rod(root, "Pommel", Vector3.zero, new Vector3(0, .065f, 0), .15f, steel);
        }
        /// <summary>Создаёт купол из двенадцати секций ткани, швов и строп с подвесом.</summary>
        /// <param name="root">Корень парашюта.</param>
        /// <param name="item">Расцветка купола.</param>
        /// <param name="body">Основная ткань.</param>
        /// <param name="dark">Подвес и тёмные секции.</param>
        /// <param name="accent">Контрастная ткань и швы.</param>
        static void Parachute(GameObject root, CosmeticItem item, Material body, Material dark, Material accent)
        {
            var thread = Paint(item.Id, "Thread", new Color(.85f, .89f, .94f), .15f);
            var alternate = item.Id.Contains("neon") ? Paint(item.Id, "Pink", new Color(1, .25f, .65f), .25f, true) : body;
            for (var i = 0; i < 12; i++)
            {
                MeshPart(root, "CanopyPanel" + i, CosmeticMeshBuilder.Canopy(i, 12), Vector3.up * 1.4f, i % 3 == 0 ? accent : i % 3 == 1 ? alternate : body);
                var seam = new Vector3[13]; var angle = i / 12f * Mathf.PI * 2;
                for (var p = 0; p < seam.Length; p++)
                { var theta = p / 12f * Mathf.PI * .48f; seam[p] = new Vector3(Mathf.Cos(angle) * Mathf.Sin(theta) * 1.145f, 1.4f + Mathf.Cos(theta) * .65f, Mathf.Sin(angle) * Mathf.Sin(theta) * .825f); }
                Line(root, "Seam" + i, seam, .012f, thread);
                Rod(root, "Suspension" + i, seam[12], new Vector3(Mathf.Cos(angle) * .17f, .15f, Mathf.Sin(angle) * .12f), .014f, thread);
            }
            Box(root, "Harness", new Vector3(0, .13f, 0), new Vector3(.4f, .16f, .3f), .055f, dark);
            for (var side = -1; side <= 1; side += 2) Box(root, "Buckle" + side, new Vector3(side * .2f, .12f, -.12f), new Vector3(.065f, .08f, .08f), .012f, accent);
            if (item.Id.Contains("space"))
                for (var side = -1; side <= 1; side += 2) MeshPart(root, "CanopyStar" + side, CosmeticMeshBuilder.Star(.15f), new Vector3(side * .45f, 1.78f, -.72f), accent).localRotation = Quaternion.Euler(30, 0, 0);
            if (item.Id.Contains("clouds"))
                for (var side = -1; side <= 1; side += 2) for (var p = 0; p < 3; p++)
                    Sphere(root, "Cloud" + side + "-" + p, new Vector3(side * .42f + (p - 1) * .12f, 1.7f + (p == 1 ? .03f : 0), -.73f), new Vector3(.21f, .13f, .06f), thread);
        }
        /// <summary>Показывает прозрачный рисунок на небольшом образце кирпичной стены.</summary>
        /// <param name="root">Корень образца.</param>
        /// <param name="item">Спрей.</param>
        /// <param name="art">Материал прозрачного рисунка.</param>
        /// <param name="mortar">Швы между кирпичами.</param>
        static void Spray(GameObject root, CosmeticItem item, Material art, Material mortar)
        {
            Box(root, "WallBacking", Vector3.zero, new Vector3(1.68f, 1.6f, .12f), .035f, mortar);
            var brick = Paint(item.Id, "Brick", new Color(.28f, .35f, .44f), .1f);
            for (var row = 0; row < 5; row++) for (var column = 0; column < 4; column++)
            {
                var x = -.6f + column * .4f + (row % 2 == 0 ? 0 : .12f);
                Box(root, "Brick" + row + "-" + column, new Vector3(x, -.62f + row * .31f, -.067f), new Vector3(.38f, .285f, .04f), .012f, brick);
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.name = "SprayArtwork"; quad.transform.SetParent(root.transform, false);
            quad.transform.localPosition = new Vector3(0, 0, -.15f); quad.transform.localScale = new Vector3(1.35f, 1.35f, 1);
            quad.GetComponent<Renderer>().sharedMaterial = art; Object.DestroyImmediate(quad.GetComponent<Collider>());
        }
        /// <summary>Создаёт четыре потока от рук и обуви; просмотр прикрепляет их к позе падающего героя.</summary>
        /// <param name="root">Корень следа.</param>
        /// <param name="item">Стиль следа.</param>
        /// <param name="body">Основной материал.</param>
        /// <param name="accent">Материал свечения.</param>
        static void Trail(GameObject root, CosmeticItem item, Material body, Material accent)
        {
            var gray = item.Id.EndsWith("-gray");
            var ribbons = item.Id.Contains("rainbow") || gray;
            var names = new[] { "LeftHand", "RightHand", "LeftSneaker", "RightSneaker" };
            for (var source = 0; source < names.Length; source++)
            {
                var flow = new GameObject("Flow-" + names[source]); flow.transform.SetParent(root.transform, false);
                flow.transform.localPosition = new Vector3(source % 2 == 0 ? -.5f : .5f, -.55f, source < 2 ? -.3f : .35f);
                if (ribbons)
                    for (var lane = 0; lane < (gray ? 2 : 3); lane++)
                    {
                        var color = gray ? new Color(.67f, .73f, .8f) : Color.HSVToRGB((source * 2 + lane) % 6 / 6f, .65f, 1);
                        var material = FlowPaint(item.Id, "Ribbon" + (gray ? lane : (source * 2 + lane) % 6), color);
                        var points = new Vector3[28];
                        for (var p = 0; p < points.Length; p++)
                        {
                            var t = p / (float)(points.Length - 1);
                            points[p] = new Vector3((lane - 1) * .1f * t + Mathf.Sin(t * 5 + source) * .1f * t, t * 1.65f, Mathf.Cos(t * 4 + source) * .09f * t);
                        }
                        var line = Line(flow, "Ribbon" + lane, points, gray ? .04f : .085f, material);
                        line.widthCurve = new AnimationCurve(new Keyframe(0, .25f), new Keyframe(.2f, 1), new Keyframe(1, 0));
                        var gradient = new Gradient(); gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                            new[] { new GradientAlphaKey(gray ? .3f : .85f, 0), new GradientAlphaKey(gray ? .4f : 1, .15f), new GradientAlphaKey(0, 1) });
                        line.colorGradient = gradient;
                    }
                else
                    for (var i = 0; i < 10; i++)
                    {
                        var pos = new Vector3(Mathf.Sin(i * 2.4f) * .22f, i / 10f * 1.65f, Mathf.Cos(i * 2.4f) * .18f);
                        var material = FlowPaint(item.Id, "Flow" + i % 2, i % 2 == 0 ? body.color : accent.color);
                        if (item.Id.Contains("stars"))
                            MeshPart(flow, "Star" + source + "-" + i, CosmeticMeshBuilder.Star(.06f + i % 3 * .015f), pos, material).localRotation = Quaternion.Euler(i * 27, i * 41, i * 19);
                        else
                        {
                            var streak = new GameObject("Spark" + source + "-" + i); streak.transform.SetParent(flow.transform, false); streak.transform.localPosition = pos;
                            var line = Line(streak, "Streak", new[] { Vector3.zero, Vector3.up * .15f }, .028f, material);
                            line.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                        }
                    }
            }
        }
        /// <summary>Сохраняет прозрачный материал потока, учитывающий затухание цветных линий и частиц.</summary>
        /// <param name="id">Код следа.</param>
        /// <param name="role">Имя цвета.</param>
        /// <param name="color">Цвет ленты или частицы.</param>
        /// <returns>Материал, который остаётся доступен после перезапуска Unity.</returns>
        static Material FlowPaint(string id, string role, Color color)
        {
            var material = Paint(id, role, color, 0);
            material.shader = Shader.Find("Sprites/Default"); material.color = color;
            EditorUtility.SetDirty(material); return material;
        }
        /// <summary>Сохраняет цвет детали, чтобы шаблон не зависел от временного материала.</summary>
        /// <param name="id">Код предмета.</param>
        /// <param name="role">Название цвета внутри предмета.</param>
        /// <param name="color">Основной цвет.</param>
        /// <param name="shine">Гладкость поверхности.</param>
        /// <param name="glow">Нужно ли лёгкое свечение.</param>
        /// <returns>Сохраняемый материал.</returns>
        static Material Paint(string id, string role, Color color, float shine, bool glow = false)
        {
            var path = CosmeticAssetBuilder.Root + "/Materials/" + id + "-" + role + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; material.SetFloat("_Glossiness", shine); material.SetFloat("_Metallic", role == "Steel" ? .3f : 0);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .45f); }
            else { material.DisableKeyword("_EMISSION"); material.SetColor("_EmissionColor", Color.black); }
            EditorUtility.SetDirty(material); return material;
        }
        /// <summary>Добавляет скруглённую деталь заданного размера.</summary>
        /// <param name="root">Корень предмета.</param>
        /// <param name="name">Имя детали.</param>
        /// <param name="position">Местное положение.</param>
        /// <param name="size">Размер.</param>
        /// <param name="bevel">Ширина скругления.</param>
        /// <param name="material">Цвет.</param>
        /// <returns>Положение и поворот детали.</returns>
        static Transform Box(GameObject root, string name, Vector3 position, Vector3 size, float bevel, Material material)
            => MeshPart(root, name, CosmeticMeshBuilder.RoundedBox(size, bevel), position, material);
        /// <summary>Добавляет сетку собственной формы без игрового столкновения.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя формы.</param>
        /// <param name="mesh">Сетка.</param>
        /// <param name="position">Положение.</param>
        /// <param name="material">Цвет.</param>
        /// <returns>Положение и поворот детали.</returns>
        static Transform MeshPart(GameObject root, string name, Mesh mesh, Vector3 position, Material material)
        { var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); part.transform.SetParent(root.transform, false); part.transform.localPosition = position; mesh.name = name; part.GetComponent<MeshFilter>().sharedMesh = mesh; part.GetComponent<Renderer>().sharedMaterial = material; return part.transform; }
        /// <summary>Соединяет две точки круглой деталью: стволом, рукоятью или стропой.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя.</param>
        /// <param name="a">Первая точка.</param>
        /// <param name="b">Вторая точка.</param>
        /// <param name="width">Толщина.</param>
        /// <param name="material">Цвет.</param>
        static void Rod(GameObject root, string name, Vector3 a, Vector3 b, float width, Material material)
        { var part = Primitive(root, name, PrimitiveType.Cylinder, (a + b) / 2, new Vector3(width, Vector3.Distance(a, b) / 2, width), material); part.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); }
        /// <summary>Добавляет мягкую круглую деталь.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя.</param>
        /// <param name="position">Положение.</param>
        /// <param name="size">Размер.</param>
        /// <param name="material">Цвет.</param>
        static void Sphere(GameObject root, string name, Vector3 position, Vector3 size, Material material)
            => Primitive(root, name, PrimitiveType.Sphere, position, size, material);
        /// <summary>Создаёт простую круглую форму и убирает ненужное столкновение.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя.</param>
        /// <param name="shape">Форма.</param>
        /// <param name="position">Положение.</param>
        /// <param name="size">Размер.</param>
        /// <param name="material">Цвет.</param>
        /// <returns>Положение и поворот.</returns>
        static Transform Primitive(GameObject root, string name, PrimitiveType shape, Vector3 position, Vector3 size, Material material)
        { var part = GameObject.CreatePrimitive(shape); part.name = name; part.transform.SetParent(root.transform, false); part.transform.localPosition = position; part.transform.localScale = size; part.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(part.GetComponent<Collider>()); return part.transform; }
        /// <summary>Создаёт гладкую линию ткани, шва или следа.</summary>
        /// <param name="root">Родитель.</param>
        /// <param name="name">Имя линии.</param>
        /// <param name="points">Точки пути.</param>
        /// <param name="width">Ширина.</param>
        /// <param name="material">Цвет.</param>
        /// <returns>Линия для настройки окончания.</returns>
        static LineRenderer Line(GameObject root, string name, Vector3[] points, float width, Material material)
        { var part = new GameObject(name, typeof(LineRenderer)); part.transform.SetParent(root.transform, false); var line = part.GetComponent<LineRenderer>(); line.useWorldSpace = false; line.positionCount = points.Length; line.SetPositions(points); line.widthMultiplier = width; line.sharedMaterial = material; line.generateLightingData = true; line.numCornerVertices = 4; line.numCapVertices = 4; return line; }
    }
}
