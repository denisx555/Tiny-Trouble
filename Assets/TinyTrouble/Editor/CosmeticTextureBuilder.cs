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
            if (item.Id.EndsWith("-gray")) return new Color(.46f, .5f, .56f);
            if (item.BasicColorIndex >= 0)
            { ColorUtility.TryParseHtmlString(new[] { "#FFD84A", "#46C8F5", "#FF646B", "#AA79FF" }[item.BasicColorIndex], out var color); return color; }
            if (item.Id.Contains("space") || item.Id.Contains("stars")) return new Color(.16f, .13f, .42f);
            if (item.Id.Contains("neon")) return new Color(.09f, .12f, .2f);
            if (item.Id.Contains("ice") || item.Id.Contains("clouds") || item.Id.Contains("wave")) return new Color(.16f, .7f, .94f);
            if (item.Id.Contains("lime")) return new Color(.4f, .85f, .16f);
            if (item.Id.Contains("kitten")) return new Color(.94f, .55f, .27f);
            if (item.Id.Contains("puppy")) return new Color(.72f, .45f, .26f);
            if (item.Id.Contains("robot")) return new Color(.24f, .75f, .82f);
            return new Color(1, .32f, .45f);
        }

        /// <summary>Собирает узоры одежды и читает готовые собственные рисунки спреев с прозрачным фоном.</summary>
        /// <param name="item">Товар с названием рисунка.</param>
        /// <returns>Временная текстура; вызывающий код сохраняет её и удаляет из памяти.</returns>
        public static Texture2D Draw(CosmeticItem item)
        {
            if (item.Category == CosmeticCategory.Spray)
            {
                var art = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                art.LoadImage(System.IO.File.ReadAllBytes(CosmeticAssetBuilder.Root + "/Artwork/" + item.Id + ".png"));
                return art;
            }
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
                    pixels[y * size + x] = color;
                }
            texture.SetPixels(pixels); texture.Apply(); return texture;
        }

    }
}
