using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace TinyTrouble.Tests
{
    /// <summary>Нажимает настоящие карточки, покупку и применение в сохранённом лобби.</summary>
    public class CosmeticFlowTests : CosmeticLobbyTestScope
    {
        /// <summary>Открывает готовую сцену с изолированными настройками.</summary>
        /// <returns>Ожидание загрузки.</returns>
        [UnitySetUp] public IEnumerator OpenLobby() { yield return SceneManager.LoadSceneAsync("Lobby"); yield return null; }
        /// <summary>Находит один из двух режимов общего интерфейса.</summary>
        /// <param name="name">Имя сохранённой панели.</param>
        /// <returns>Панель магазина или инвентаря.</returns>
        static CosmeticPanel Panel(string name)
        {
            var result = Object.FindObjectsOfType<CosmeticPanel>(true).SingleOrDefault(p => p.name == name);
            Assert.That(result, Is.Not.Null, name + " ещё не подключена"); return result;
        }
        /// <summary>Нажимает кнопку в указанной панели.</summary>
        /// <param name="panel">Родитель кнопки.</param>
        /// <param name="name">Имя кнопки.</param>
        static void Click(CosmeticPanel panel, string name) => panel.GetComponentsInChildren<Button>(true).Single(b => b.name == name).onClick.Invoke();
        /// <summary>Покупка сохраняет владение, а отдельное применение — внешний вид после перезагрузки.</summary>
        /// <returns>Ожидание сцен и кадров.</returns>
        [UnityTest] public IEnumerator PurchaseThenEquipThroughButtonsAndReload()
        {
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2);
            var shop = Panel("ShopPanel"); var card = shop.GetComponentsInChildren<CosmeticCard>().Single(c => c.ItemId == "stripes");
            card.GetComponent<Button>().onClick.Invoke(); Click(shop, "ActionButton"); yield return null;
            Assert.That(controller.Cosmetics.Snapshot.Balance, Is.EqualTo(600));
            Assert.That(controller.Cosmetics.Snapshot.EquippedIds[0], Is.EqualTo("base-yellow"));
            controller.SelectTab(1); var inventory = Panel("CharacterPanel");
            inventory.GetComponentsInChildren<CosmeticCard>().Single(c => c.ItemId == "stripes").GetComponent<Button>().onClick.Invoke();
            Click(inventory, "ActionButton"); yield return null;
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null;
            controller = Object.FindObjectOfType<LobbyController>();
            Assert.That(controller.Cosmetics.Snapshot.Balance, Is.EqualTo(600)); Assert.That(controller.Cosmetics.Snapshot.OwnedIds, Does.Contain("stripes"));
            Assert.That(controller.Cosmetics.Snapshot.EquippedIds[0], Is.EqualTo("stripes"));
            Assert.That(GameObject.Find("LobbyCharacter").GetComponent<CharacterAppearance>().AppliedId, Is.EqualTo("stripes"));
            Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
        }
        /// <summary>Открытие магазина и покупка сохраняют примерку выделенной одежды без автоматического надевания.</summary>
        [Test] public void OpeningShopPreviewsHighlightedClothesWithoutEquipping()
        {
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2);
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/TinyTrouble/Cosmetics/Catalog.asset");
            catalog.TryGet("stripes", out var stripes);
            var appearance = GameObject.Find("LobbyCharacter").GetComponent<CharacterAppearance>();
            var hoodie = GameObject.Find("LobbyCharacter").GetComponentsInChildren<Renderer>().Single(r => r.name == "HoodieBody");
            Assert.That(hoodie.sharedMaterial.mainTexture, Is.SameAs(stripes.Material.mainTexture));
            Assert.That(controller.Cosmetics.Snapshot.Balance, Is.EqualTo(1000));
            Assert.That(appearance.AppliedId, Is.EqualTo("base-yellow"));
            Click(Panel("ShopPanel"), "ActionButton");
            Assert.That(hoodie.sharedMaterial.mainTexture, Is.SameAs(stripes.Material.mainTexture));
            Assert.That(controller.Cosmetics.Snapshot.Balance, Is.EqualTo(600));
            Assert.That(controller.Cosmetics.Snapshot.EquippedIds[0], Is.EqualTo("base-yellow"));
            controller.SelectTab(0);
            Assert.That(hoodie.sharedMaterial.mainTexture, Is.Null);
        }
        /// <summary>Применение каждого объёмного предмета сохраняет его крупный просмотр до выхода из инвентаря.</summary>
        /// <returns>Ожидание загрузки и удаления прежних моделей.</returns>
        [UnityTest] public IEnumerator ApplyingInventoryObjectKeepsSelectedPreviewUntilExit()
        {
            var ids = new[] { "pistol-ice", "spray-boom", "parachute-clouds", "trail-stars", "pickaxe-ice" };
            var profile = new CosmeticProfile { Balance = 300, OwnedIds = ids }; profile.EquippedIds[0] = "base-yellow";
            PlayerPrefs.SetString(CosmeticProfileStore.ProfileKey, JsonUtility.ToJson(profile));
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null;
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/TinyTrouble/Cosmetics/Catalog.asset");
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(1); var inventory = Panel("CharacterPanel");
            var image = controller.GetComponentInChildren<RawImage>(true);
            var camera = controller.GetComponentInChildren<Camera>(true); var stage = GameObject.Find("CosmeticPreviewRoot").transform;
            foreach (var id in ids)
            {
                catalog.TryGet(id, out var item); inventory.SelectCategory(item.Category); inventory.SelectItem(id); yield return null;
                Assert.That(image.enabled, Is.True); Click(inventory, "ActionButton"); yield return null;
                Assert.That(controller.Cosmetics.Snapshot.EquippedIds[(int)item.Slot], Is.EqualTo(id));
                Assert.That(controller.Cosmetics.Snapshot.Balance, Is.EqualTo(300));
                Assert.That(image.enabled, Is.True, id + " исчез из просмотра после применения");
                Assert.That(camera.enabled, Is.True);
                var models = stage.Cast<Transform>().Where(t => !t.GetComponent<Light>()).ToArray();
                Assert.That(models.Length, Is.EqualTo(1)); Assert.That(models[0].name, Is.EqualTo(id));
            }
            controller.SelectTab(0); yield return null;
            Assert.That(image.enabled, Is.False); Assert.That(camera.enabled, Is.False); Assert.That(stage.Cast<Transform>().Count(t => !t.GetComponent<Light>()), Is.Zero);
            Assert.That(GameObject.Find("LobbyCharacter").GetComponent<CharacterAppearance>().AppliedId, Is.EqualTo("base-yellow"));
        }
        /// <summary>Недостаток средств, повторная покупка и ошибка записи не показываются успехом.</summary>
        [Test] public void InsufficientDuplicateAndFailedSaveShowStatus()
        {
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2); var shop = Panel("ShopPanel");
            shop.SelectItem("stripes"); Click(shop, "ActionButton");
            Assert.That(shop.GetComponentsInChildren<Button>().Single(b => b.name == "ActionButton").interactable, Is.False);
            shop.SelectCategory(CosmeticCategory.Pet); shop.SelectItem("pet-puppy"); Click(shop, "ActionButton");
            shop.SelectCategory(CosmeticCategory.Weapon); shop.SelectItem("pistol-ice");
            Assert.That(shop.GetComponentsInChildren<Button>().Single(b => b.name == "ActionButton").interactable, Is.False);
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CosmeticCatalog>("Assets/TinyTrouble/Cosmetics/Catalog.asset");
            var failed = new CosmeticService(catalog, new FailingCosmeticStore(), 0);
            shop.Configure(catalog, failed, Object.FindObjectOfType<CosmeticPreview>(), CosmeticPanelMode.Shop); shop.SelectCategory(CosmeticCategory.Clothes); shop.SelectItem("stripes"); Click(shop, "ActionButton");
            Assert.That(failed.Snapshot.OwnedIds, Is.Empty);
            Assert.That(shop.GetComponentsInChildren<Text>().Single(t => t.name == "OperationStatus").text, Does.Contain("сохранить"));
        }
        /// <summary>Стартовые скины доступны сразу, пустая категория поясняется, а оружие выбирается независимо.</summary>
        [Test] public void InventoryFiltersAndWeaponChoicesAreIndependent()
        {
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(1); var inventory = Panel("CharacterPanel");
            inventory.SelectCategory(CosmeticCategory.Weapon);
            Assert.That(inventory.GetComponentsInChildren<CosmeticCard>().Length, Is.EqualTo(3));
            Assert.That(inventory.GetComponentsInChildren<Text>(true).Single(t => t.name == "EmptyMessage").gameObject.activeSelf, Is.False);
            inventory.SelectCategory(CosmeticCategory.Dance);
            Assert.That(inventory.GetComponentsInChildren<CosmeticCard>().Length, Is.Zero);
            Assert.That(inventory.GetComponentsInChildren<Text>(true).Single(t => t.name == "EmptyMessage").gameObject.activeSelf, Is.True);
            inventory.SelectCategory(CosmeticCategory.Weapon);
            controller.Cosmetics.Buy("pistol-ice"); controller.Cosmetics.Buy("rifle-lime"); inventory.Refresh();
            inventory.SelectItem("pistol-ice"); Click(inventory, "ActionButton"); inventory.SelectItem("rifle-lime"); Click(inventory, "ActionButton");
            Assert.That(controller.Cosmetics.Snapshot.EquippedIds[1], Is.EqualTo("pistol-ice")); Assert.That(controller.Cosmetics.Snapshot.EquippedIds[2], Is.EqualTo("rifle-lime"));
        }
        /// <summary>Повреждённый профиль запрещает покупки, сохраняя доступ к обычным вкладкам.</summary>
        /// <returns>Ожидание повторной загрузки.</returns>
        [UnityTest] public IEnumerator CorruptProfileAllowsOtherTabsButBlocksWrites()
        {
            PlayerPrefs.SetString(CosmeticProfileStore.ProfileKey, "broken");
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null;
            var controller = Object.FindObjectOfType<LobbyController>(); controller.SelectTab(2); var shop = Panel("ShopPanel"); shop.SelectItem("stripes");
            Assert.That(controller.Cosmetics.CanWrite, Is.False); Assert.That(shop.GetComponentsInChildren<Button>().Single(b => b.name == "ActionButton").interactable, Is.False);
            controller.SelectTab(3); Assert.That(GameObject.Find("VolumeSlider"), Is.Not.Null);
            controller.SelectTab(0); Assert.That(GameObject.Find("PlayButton").GetComponent<Button>().interactable, Is.True);
        }
        /// <summary>Временная очистка возвращает заранее записанные профиль, резерв, цвет и громкость.</summary>
        [Test] public void TestsPreserveExistingProfile()
        {
            PlayerPrefs.SetString(CosmeticProfileStore.ProfileKey, "test-existing-profile"); PlayerPrefs.SetString(CosmeticProfileStore.RecoveryKey, "test-recovery");
            PlayerPrefs.SetInt(CosmeticProfileStore.ColorKey, 3); PlayerPrefs.SetFloat("TinyTrouble.Volume", .37f);
            using (new CosmeticPrefsScope()) { PlayerPrefs.SetString(CosmeticProfileStore.ProfileKey, "temporary"); }
            Assert.That(PlayerPrefs.GetString(CosmeticProfileStore.ProfileKey), Is.EqualTo("test-existing-profile")); Assert.That(PlayerPrefs.GetString(CosmeticProfileStore.RecoveryKey), Is.EqualTo("test-recovery"));
            Assert.That(PlayerPrefs.GetInt(CosmeticProfileStore.ColorKey), Is.EqualTo(3)); Assert.That(PlayerPrefs.GetFloat("TinyTrouble.Volume"), Is.EqualTo(.37f));
        }
    }
    /// <summary>Имитирует недоступный диск только в тесте, не меняя настоящие настройки.</summary>
    sealed class FailingCosmeticStore : ICosmeticProfileStore
    {
        /// <inheritdoc/>
        public CosmeticLoadResult Load(CosmeticCatalog catalog, int legacyColor) { var profile = new CosmeticProfile { Balance = 1000 }; profile.EquippedIds[0] = "base-yellow"; return new CosmeticLoadResult { Profile = profile, CanWrite = true }; }
        /// <inheritdoc/>
        public void Save(CosmeticProfile profile, int? basicColorIndex = null) { throw new System.IO.IOException("Тестовая ошибка диска"); }
    }
}
