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
    public class LobbyFlowTests
    {
        bool hadVolume, hadColor;
        float oldVolume, oldListener;
        int oldColor;
        GameObject extraCharacter;
        int loadedCount;
        [UnitySetUp] public IEnumerator Setup()
        {
            hadVolume = PlayerPrefs.HasKey("TinyTrouble.Volume"); oldVolume = PlayerPrefs.GetFloat("TinyTrouble.Volume");
            hadColor = PlayerPrefs.HasKey("TinyTrouble.HoodieColor"); oldColor = PlayerPrefs.GetInt("TinyTrouble.HoodieColor");
            oldListener = AudioListener.volume;
            PlayerPrefs.DeleteKey("TinyTrouble.Volume"); PlayerPrefs.DeleteKey("TinyTrouble.HoodieColor");
            Assert.That(Application.CanStreamedLevelBeLoaded("Lobby"), Is.True, "Lobby scene отсутствует");
            yield return SceneManager.LoadSceneAsync("Lobby");
            yield return null;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            SceneManager.sceneLoaded -= CountLoaded;
            if (extraCharacter) Object.Destroy(extraCharacter);
            if (hadVolume) PlayerPrefs.SetFloat("TinyTrouble.Volume", oldVolume); else PlayerPrefs.DeleteKey("TinyTrouble.Volume");
            if (hadColor) PlayerPrefs.SetInt("TinyTrouble.HoodieColor", oldColor); else PlayerPrefs.DeleteKey("TinyTrouble.HoodieColor");
            PlayerPrefs.Save(); AudioListener.volume = oldListener;
            yield return null;
        }
        void CountLoaded(Scene scene, LoadSceneMode mode) { if (scene.name == "SampleScene") loadedCount++; }
        static T Find<T>(string name) where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<T>(true)).Single(c => c.name == name);
        static GameObject Panel(string name) => Find<RectTransform>(name).gameObject;

        [UnityTest] public IEnumerator TabsSwitchPanelsAndKeepFocusOnVisibleControls()
        {
            Assert.That(Panel("PlayPanel").activeSelf, Is.True);
            Find<Button>("TabCharacter").onClick.Invoke(); yield return null;
            Assert.That(Panel("CharacterPanel").activeSelf, Is.True);
            Assert.That(Panel("PlayPanel").activeSelf, Is.False);
            Assert.That(Panel("SettingsPanel").activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
            ExecuteEvents.Execute(Find<Button>("TabSettings").gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            yield return null;
            Assert.That(Panel("SettingsPanel").activeSelf, Is.True);
            Assert.That(Panel("CharacterPanel").activeSelf, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(Find<Slider>("VolumeSlider").gameObject));
        }
        [UnityTest] public IEnumerator AllHoodieColorsChangeOnlyClothingAndPersist()
        {
            var root = GameObject.Find("LobbyCharacter");
            var skin = root.GetComponentsInChildren<Renderer>().Single(r => r.name == "Head");
            var pants = root.GetComponentsInChildren<Renderer>().Single(r => r.name == "LeftTrouser");
            var skinBefore = skin.sharedMaterial.color; var pantsBefore = pants.sharedMaterial.color;
#if UNITY_EDITOR
            extraCharacter = Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TinyTrouble/Prefabs/LobbyCharacter.prefab"));
            var otherHoodie = extraCharacter.GetComponentsInChildren<Renderer>().First(r => r.name.StartsWith("Hoodie"));
            var otherColor = otherHoodie.sharedMaterial.color;
#endif
            Find<Button>("TabCharacter").onClick.Invoke();
            string[] hex = { "#FFD84A", "#46C8F5", "#FF646B", "#AA79FF" };
            for (var i = 0; i < 4; i++)
            {
                Find<Button>("Color" + i).onClick.Invoke(); yield return null;
                ColorUtility.TryParseHtmlString(hex[i], out var expected);
                foreach (var hoodie in root.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Hoodie")))
                    Assert.That(Vector4.Distance(hoodie.sharedMaterial.color, expected), Is.LessThan(.005f));
                Assert.That(PlayerPrefs.GetInt("TinyTrouble.HoodieColor"), Is.EqualTo(i));
                Assert.That(skin.sharedMaterial.color, Is.EqualTo(skinBefore));
                Assert.That(pants.sharedMaterial.color, Is.EqualTo(pantsBefore));
#if UNITY_EDITOR
                Assert.That(otherHoodie.sharedMaterial.color, Is.EqualTo(otherColor));
#endif
            }
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null;
            ColorUtility.TryParseHtmlString("#AA79FF", out var purple);
            Assert.That(Vector4.Distance(GameObject.Find("LobbyCharacter").GetComponentsInChildren<Renderer>().First(r => r.name.StartsWith("Hoodie")).sharedMaterial.color, purple), Is.LessThan(.005f));
        }
        [UnityTest] public IEnumerator VolumeSliderUpdatesPercentageAndRestoresOnReload()
        {
            Find<Button>("TabSettings").onClick.Invoke();
            var slider = Find<Slider>("VolumeSlider");
            foreach (var value in new[] { 0f, 1f, .37f })
            {
                slider.value = value; yield return null;
                Assert.That(AudioListener.volume, Is.EqualTo(value).Within(.001f));
                Assert.That(Find<Text>("VolumePercent").text, Is.EqualTo(Mathf.RoundToInt(value * 100) + "%"));
            }
            yield return SceneManager.LoadSceneAsync("Lobby"); yield return null;
            Assert.That(AudioListener.volume, Is.EqualTo(.37f).Within(.001f));
            Assert.That(Find<Slider>("VolumeSlider").value, Is.EqualTo(.37f).Within(.001f));
        }
        [UnityTest] public IEnumerator KeyboardFocusCyclesOnlyVisibleControls()
        {
            Find<Button>("TabSettings").onClick.Invoke();
            var controller = Object.FindObjectsOfType<MonoBehaviour>().Single(m => m.GetType().Name == "LobbyController");
            var advance = controller.GetType().GetMethod("AdvanceFocus");
            Assert.That(advance, Is.Not.Null, "Нет переключения фокуса клавишей Tab");
            foreach (var name in new[] { "TabPlay", "TabCharacter", "TabSettings", "VolumeSlider" })
            {
                advance.Invoke(controller, new object[] { false }); yield return null;
                Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo(name));
                Assert.That(EventSystem.current.currentSelectedGameObject.activeInHierarchy, Is.True);
            }
            advance.Invoke(controller, new object[] { true }); yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("TabSettings"));
        }
        [UnityTest] public IEnumerator SliderTrackAcceptsPointerAndChangesVolume()
        {
            Find<Button>("TabSettings").onClick.Invoke(); yield return null;
            var slider = Find<Slider>("VolumeSlider"); var rect = (RectTransform)slider.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * .25f, 0, 0)));
            var data = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            var hit = hits.FirstOrDefault(h => h.gameObject.transform.IsChildOf(slider.transform));
            Assert.That(hit.gameObject, Is.Not.Null, "Шкала громкости не принимает нажатия");
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, data, ExecuteEvents.pointerDownHandler);
            yield return null;
            Assert.That(slider.value, Is.InRange(.2f, .3f));
            Assert.That(AudioListener.volume, Is.EqualTo(slider.value).Within(.001f));
        }
        [UnityTest] public IEnumerator RepeatedPlayLoadsSampleSceneOnceAndKeepsVolume()
        {
            Find<Button>("TabSettings").onClick.Invoke(); Find<Slider>("VolumeSlider").value = .37f;
            Find<Button>("TabPlay").onClick.Invoke();
            loadedCount = 0; SceneManager.sceneLoaded += CountLoaded;
            var button = Find<Button>("PlayButton");
            button.onClick.Invoke(); button.onClick.Invoke();
            Assert.That(button.interactable, Is.False);
            var deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().name != "SampleScene" && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("SampleScene"));
            Assert.That(loadedCount, Is.EqualTo(1));
            Assert.That(AudioListener.volume, Is.EqualTo(.37f).Within(.001f));
        }
    }
}
