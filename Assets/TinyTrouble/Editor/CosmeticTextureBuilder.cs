using UnityEngine;

namespace TinyTrouble.EditorTools
{
    /// <summary>Рисует собственные узоры товаров без скачивания чужих картинок.</summary>
    public static class CosmeticTextureBuilder
    {
        /// <summary>Выбирает главный цвет товара по его постоянному коду.</summary>
        /// <param name="item">Товар каталога.</param>
        /// <returns>Цвет основы рисунка.</returns>
        public static Color Tint(CosmeticItem item)
        {
            if (item.BasicColorIndex >= 0) return new[] { new Color(1, .78f, .12f), new Color(.12f, .65f, 1), new Color(1, .24f, .32f), new Color(.62f, .32f, 1) }[item.BasicColorIndex];
            if (item.Id.Contains("space") || item.Id.Contains("stars")) return new Color(.16f, .13f, .42f);
            if (item.Id.Contains("neon")) return new Color(.09f, .12f, .2f);
            if (item.Id.Contains("ice") || item.Id.Contains("clouds") || item.Id.Contains("wave")) return new Color(.16f, .7f, .94f);
            if (item.Id.Contains("lime")) return new Color(.4f, .85f, .16f);
            if (item.Id.Contains("kitten")) return new Color(.94f, .55f, .27f);
            if (item.Id.Contains("puppy")) return new Color(.72f, .45f, .26f);
            if (item.Id.Contains("robot")) return new Color(.24f, .75f, .82f);
            return new Color(1, .32f, .45f);
        }

        /// <summary>Собирает полосы, звёзды, облака или рисунок спрея в квадратной текстуре.</summary>
        /// <param name="item">Товар с названием рисунка.</param>
        /// <returns>Временная текстура; вызывающий код сохраняет её и удаляет из памяти.</returns>
        public static Texture2D Draw(CosmeticItem item)
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            var basis = Tint(item);
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var color = basis;
                    if (item.Id == "stripes" || item.Id.Contains("wave")) color = y % 48 < 22 ? basis : new Color(1, .88f, .25f);
                    else if (item.Id.Contains("space") || item.Id.Contains("stars"))
                    {
                        var dx = x % 64 - 32; var dy = y % 64 - 32;
                        if (Mathf.Abs(dx) + Mathf.Abs(dy) < 10 || (Mathf.Abs(dx) < 2 && Mathf.Abs(dy) < 14)) color = new Color(1, .91f, .5f);
                    }
                    else if (item.Id.Contains("neon"))
                    { if ((x + y) % 64 < 10) color = Color.cyan; else if ((x - y + 512) % 96 < 8) color = new Color(1, .25f, .8f); }
                    else if (item.Id.Contains("clouds"))
                    { var dx = x % 80 - 40; var dy = y % 64 - 32; if (dx * dx / 2 + dy * dy < 260) color = Color.white; }
                    else if (item.Id.Contains("ice"))
                    { if ((x + y * 2) % 72 < 6 || (x * 2 - y + 512) % 72 < 6) color = new Color(.8f, .96f, 1); }
                    else if (item.Id.Contains("pop")) color = Color.HSVToRGB(((x / 32 + y / 32) % 6) / 6f, .65f, 1);
                    else if (item.Id.Contains("rainbow")) color = Color.HSVToRGB(y / (float)size, .8f, 1);
                    else if (item.Id.Contains("fire") || item.Id.Contains("sparks")) color = Color.Lerp(new Color(1, .14f, .05f), Color.yellow, y / (float)size);
                    if (item.Category == CosmeticCategory.Spray) color = SprayPixel(item.Id, x / 256f, y / 256f);
                    pixels[y * size + x] = color;
                }
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }

        /// <summary>Рисует улыбку, хитрый смайлик или яркую вспышку на нейтральном фоне.</summary>
        /// <param name="id">Код одного из трёх спреев.</param>
        /// <param name="x">Горизонтальная координата от нуля до единицы.</param>
        /// <param name="y">Вертикальная координата от нуля до единицы.</param>
        /// <returns>Цвет точки рисунка.</returns>
        static Color SprayPixel(string id, float x, float y)
        {
            var p = new Vector2(x - .5f, y - .5f); var ink = new Color(.08f, .1f, .2f);
            var radius = p.magnitude;
            if (id == "spray-boom")
            {
                var edge = .32f + .09f * Mathf.Sin(Mathf.Atan2(p.y, p.x) * 12);
                if (radius > edge) return ink;
                if (Mathf.Abs(p.x) < .035f && p.y > -.04f && p.y < .21f || radius < .04f && p.y < -.1f) return ink;
                return Color.Lerp(new Color(1, .3f, .12f), Color.yellow, 1 - radius / .5f);
            }
            if (radius > .41f) return ink;
            if ((new Vector2(p.x - .14f, p.y - .11f)).magnitude < .035f || (new Vector2(p.x + .14f, p.y - .11f)).magnitude < .035f) return ink;
            if (p.y < -.04f && p.y > -.2f && Mathf.Abs(p.y + .05f + .14f * Mathf.Cos(p.x * 6)) < .025f && Mathf.Abs(p.x) < .24f) return ink;
            if (id == "spray-mischief" && Mathf.Abs(p.x) > .06f && Mathf.Abs(p.x) < .24f && Mathf.Abs(p.y - (.24f - Mathf.Abs(p.x) * .5f)) < .015f) return ink;
            return id == "spray-smile" ? new Color(1, .82f, .12f) : new Color(.65f, .4f, 1);
        }
    }
}
