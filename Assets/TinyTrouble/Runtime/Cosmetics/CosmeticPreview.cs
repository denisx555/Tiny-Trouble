using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TinyTrouble
{
    /// <summary>Управляет временной примеркой и отдельной камерой объёмных товаров.</summary>
    public sealed class CosmeticPreview : MonoBehaviour
    {
        /// <summary>Выделенный слой предметов, который не виден основной камере.</summary>
        public const int PreviewLayer = 30;
        /// <summary>Ссылки на каталог, героя и его внешний вид.</summary>
        [SerializeField] CosmeticCatalog catalog;
        /// <summary>Единственный владелец материалов одежды.</summary>
        [SerializeField] CharacterAppearance appearance;
        /// <summary>Просмотр танцев через существующее управление жестами.</summary>
        [SerializeField] CharacterDance dance;
        /// <summary>Герой, чьи положение и поворот не меняются при просмотре предмета.</summary>
        [SerializeField] Transform hero;
        /// <summary>Камера, рисующая только отдельный образец.</summary>
        [SerializeField] Camera previewCamera;
        /// <summary>Область картинки предмета в интерфейсе.</summary>
        [SerializeField] RawImage target;
        /// <summary>Отдельная сцена образца и место питомца у главного героя.</summary>
        [SerializeField] Transform previewRoot, petAnchor;
        /// <summary>Принадлежащие этому компоненту временные объекты.</summary>
        GameObject model, pet;
        /// <summary>Последний сохранённый профиль для возврата после примерки.</summary>
        CosmeticProfile saved;
        /// <summary>Переиспользуемое изображение камеры.</summary>
        RenderTexture image;
        /// <summary>Основная камера и прежние видимые слои.</summary>
        Camera mainCamera;
        /// <summary>Слои основной камеры до подключения просмотра.</summary>
        int originalMask;
        /// <summary>Ввод мыши на отдельном изображении предмета.</summary>
        CosmeticPreviewInput input;
        /// <summary>Углы камеры вокруг предмета и доля приближения от общего вида.</summary>
        float yaw, pitch, zoom;
        /// <summary>Центр и радиус образца; не скачут вслед за анимированными частицами.</summary>
        Vector3 modelCenter;
        /// <summary>Размер сферы, в которой помещается весь предмет.</summary>
        float modelRadius;
        /// <summary>Плоский рисунок на стене требует ограничения угла, чтобы рисунок оставался виден.</summary>
        bool isSpray;

        /// <summary>Подключает сохранённые ссылки сцены к просмотру.</summary>
        /// <param name="catalog">Каталог товаров.</param>
        /// <param name="appearance">Одежда главного героя.</param>
        /// <param name="dance">Танцы главного героя.</param>
        /// <param name="hero">Главный герой лобби.</param>
        /// <param name="previewCamera">Отдельная камера предметов.</param>
        /// <param name="target">Изображение камеры в интерфейсе.</param>
        /// <param name="previewRoot">Корень временных предметов.</param>
        /// <param name="petAnchor">Место питомца на платформе.</param>
        public void Configure(CosmeticCatalog catalog, CharacterAppearance appearance, CharacterDance dance, Transform hero, Camera previewCamera, RawImage target, Transform previewRoot, Transform petAnchor)
        {
            this.catalog = catalog; this.appearance = appearance; this.dance = dance; this.hero = hero;
            this.previewCamera = previewCamera; this.target = target; this.previewRoot = previewRoot; this.petAnchor = petAnchor;
            previewRoot.position = new Vector3(1000, 1000, 1000);
            previewCamera.cullingMask = 1 << PreviewLayer; previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.07f, .12f, .26f); previewCamera.fieldOfView = 32; previewCamera.nearClipPlane = .01f; previewCamera.farClipPlane = 100;
            previewCamera.enabled = false; target.enabled = false; target.raycastTarget = true;
            input = target.GetComponent<CosmeticPreviewInput>();
            if (!input) input = target.gameObject.AddComponent<CosmeticPreviewInput>();
            input.Configure(this);
            if (!mainCamera) { mainCamera = Camera.main; if (mainCamera) { originalMask = mainCamera.cullingMask; mainCamera.cullingMask &= ~(1 << PreviewLayer); } }
        }
        /// <summary>Показывает вещь без покупки или сохранения выбора.</summary>
        /// <param name="item">Просматриваемый товар.</param>
        public void Show(CosmeticItem item)
        {
            if (item == null || !catalog) return;
            Restore(saved);
            if (item.Category == CosmeticCategory.Clothes) { appearance.Preview(item); return; }
            if (item.Category == CosmeticCategory.Dance) { dance.Preview(item.Id); return; }
            if (item.Category == CosmeticCategory.Pet) { SetPet(item); return; }
            if (!item.PreviewPrefab) return;
            model = Instantiate(item.PreviewPrefab, previewRoot, false); model.name = item.Id;
            if (item.Category == CosmeticCategory.Trail)
            {
                var miniature = Instantiate(hero.gameObject, model.transform, false); miniature.name = "FallingHero";
                foreach (var behaviour in miniature.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
                foreach (var collider in miniature.GetComponentsInChildren<Collider>()) Destroy(collider);
                miniature.transform.localPosition = new Vector3(0, -.65f, 0);
                model.AddComponent<CosmeticTrailPreview>().Configure(item);
            }
            foreach (var part in model.GetComponentsInChildren<Transform>()) part.gameObject.layer = PreviewLayer;
            isSpray = item.Category == CosmeticCategory.Spray;
            yaw = isSpray ? 0 : -25; pitch = isSpray ? 0 : item.Category == CosmeticCategory.Trail ? -20 : 12; zoom = 0;
            Measure(item.Category == CosmeticCategory.Trail);
            target.enabled = true; ResizeImage(); Frame(); previewCamera.enabled = true;
        }
        /// <summary>Возвращает сохранённую одежду и питомца; временные товары и танец удаляются.</summary>
        /// <param name="profile">Последний сохранённый профиль.</param>
        public void Restore(CosmeticProfile profile)
        {
            if (profile != null) saved = profile.Clone();
            if (input) input.Cancel();
            Remove(ref model); if (dance) dance.Stop();
            if (target) target.enabled = false; if (previewCamera) previewCamera.enabled = false;
            if (saved == null) return;
            if (catalog.TryGet(saved.EquippedIds[(int)CosmeticSlot.Clothes], out var clothes)) appearance.Apply(clothes);
            catalog.TryGet(saved.EquippedIds[(int)CosmeticSlot.Pet], out var companion); SetPet(companion);
        }
        /// <summary>Закрывает просмотр и возвращает сохранённый вид.</summary>
        /// <param name="profile">Последний сохранённый профиль.</param>
        public void Close(CosmeticProfile profile) { Restore(profile); }
        /// <summary>Заменяет только питомца; сохранённый профиль остаётся прежним.</summary>
        /// <param name="item">Питомец или отсутствие выбранного спутника.</param>
        void SetPet(CosmeticItem item)
        {
            if (pet && item != null && pet.name == item.Id) return;
            Remove(ref pet);
            var layout = GetComponent<LobbyLayout>();
            if (layout) layout.SetCosmeticView(layout.IsCosmeticView, item != null && item.PreviewPrefab);
            if (item == null || !item.PreviewPrefab || !petAnchor) return;
            pet = Instantiate(item.PreviewPrefab, petAnchor, false); pet.name = item.Id;
        }
        /// <summary>Создаёт новое изображение только при фактическом изменении его размеров.</summary>
        void ResizeImage()
        {
            var canvas = target.canvas; var scale = canvas ? canvas.scaleFactor : 1;
            var width = Mathf.Clamp(Mathf.RoundToInt(target.rectTransform.rect.width * scale), 64, 1024);
            var height = Mathf.Clamp(Mathf.RoundToInt(target.rectTransform.rect.height * scale), 64, 1024);
            if (image && image.width == width && image.height == height) return;
            ReleaseImage(); image = new RenderTexture(width, height, 24) { name = "TinyTrouble.CosmeticPreview" }; image.Create();
            previewCamera.targetTexture = image; target.texture = image; previewCamera.aspect = width / (float)height;
        }
        /// <summary>Измеряет общий вид один раз, чтобы дыхание и частицы не дёргали камеру.</summary>
        /// <param name="animatedTrail">Нужно ли оставить запас для движения следа.</param>
        void Measure(bool animatedTrail)
        {
            if (!model) return;
            var surfaces = model.GetComponentsInChildren<Renderer>(); if (surfaces.Length == 0) return;
            var bounds = surfaces[0].bounds; foreach (var surface in surfaces.Skip(1)) bounds.Encapsulate(surface.bounds);
            modelCenter = bounds.center;
            modelRadius = Mathf.Max(.05f, bounds.extents.magnitude + (animatedTrail ? .45f : 0));
        }
        /// <summary>Поворачивает общий вид предмета; спрей ограничен углами видимости стены.</summary>
        /// <param name="delta">Смещение в долях ширины и высоты картинки.</param>
        public void Turn(Vector2 delta)
        {
            if (!model || !target.enabled || float.IsNaN(delta.x) || float.IsNaN(delta.y) || float.IsInfinity(delta.x) || float.IsInfinity(delta.y)) return;
            yaw = Mathf.Repeat(yaw + delta.x * 240 + 180, 360) - 180;
            pitch = Mathf.Clamp(pitch + delta.y * 150, -70, 70);
            if (isSpray) { yaw = Mathf.Clamp(yaw, -60, 60); pitch = Mathf.Clamp(pitch, -45, 45); }
        }
        /// <summary>Меняет приближение в безопасных пределах без записи выбора игрока.</summary>
        /// <param name="amount">Положительное значение приближает предмет.</param>
        public void Zoom(float amount)
        {
            if (!model || !target.enabled || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            zoom = Mathf.Clamp01(zoom + amount);
        }
        /// <summary>Размещает камеру вокруг постоянного центра; общий вид помещается в любом формате окна.</summary>
        void Frame()
        {
            if (!model) return;
            var distance = modelRadius / Mathf.Sin(previewCamera.fieldOfView * .5f * Mathf.Deg2Rad) * 1.12f / Mathf.Min(1, previewCamera.aspect);
            distance *= Mathf.Lerp(1, .4f, zoom);
            previewCamera.transform.position = modelCenter + Quaternion.Euler(pitch, yaw, 0) * Vector3.back * distance;
            previewCamera.transform.LookAt(modelCenter);
        }
        /// <summary>Следит за размером области просмотра без создания камеры каждый кадр.</summary>
        void LateUpdate() { if (target && target.enabled) { ResizeImage(); Frame(); } }
        /// <summary>Уход из окна плавно прекращает показ танца.</summary>
        /// <param name="focus">Получает ли окно ввод.</param>
        void OnApplicationFocus(bool focus) { if (!focus && dance) dance.Stop(); }
        /// <summary>Сразу убирает объект из просмотра и отдаёт его удаление Unity.</summary>
        /// <param name="value">Принадлежащий нам временный объект.</param>
        static void Remove(ref GameObject value)
        { if (!value) return; value.SetActive(false); value.transform.SetParent(null); if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); value = null; }
        /// <summary>Отсоединяет и освобождает только созданное нами изображение камеры.</summary>
        void ReleaseImage()
        { if (previewCamera) previewCamera.targetTexture = null; if (target) target.texture = null; if (image) { image.Release(); if (Application.isPlaying) Destroy(image); else DestroyImmediate(image); image = null; } }
        /// <summary>Не оставляет предметы, питомца или изображение после закрытия лобби.</summary>
        void OnDestroy()
        { Remove(ref model); Remove(ref pet); ReleaseImage(); if (dance) dance.Stop(); if (mainCamera) mainCamera.cullingMask = originalMask; }
    }
}
