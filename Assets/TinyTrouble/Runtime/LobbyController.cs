using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TinyTrouble
{
    public sealed class LobbyController : MonoBehaviour
    {
        [SerializeField] GameObject[] panels;
        [SerializeField] Button[] tabs;
        [SerializeField] Button playButton;
        [SerializeField] Slider volumeSlider;
        [SerializeField] Text volumeText;
        [SerializeField] Button[] colorButtons;
        [SerializeField] Renderer[] hoodieRenderers;
        [SerializeField] Text statusText;
        [SerializeField] string gameSceneName = "SampleScene";

        static readonly string[] HoodieHex = { "#FFD84A", "#46C8F5", "#FF646B", "#AA79FF" };
        Material[] ownedMaterials, originalMaterials;
        public bool IsLoading { get; private set; }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
                AdvanceFocus(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
        }
        public void AdvanceFocus(bool reverse)
        {
            if (IsLoading || !EventSystem.current) return;
            var choices = new List<Selectable>();
            foreach (var tab in tabs) if (tab.isActiveAndEnabled && tab.IsInteractable()) choices.Add(tab);
            foreach (var panel in panels)
                if (panel.activeInHierarchy)
                    foreach (var control in panel.GetComponentsInChildren<Selectable>())
                        if (control.isActiveAndEnabled && control.IsInteractable()) choices.Add(control);
            if (choices.Count == 0) return;
            var current = choices.FindIndex(s => s.gameObject == EventSystem.current.currentSelectedGameObject);
            var next = current < 0 ? 0 : (current + (reverse ? choices.Count - 1 : 1)) % choices.Count;
            EventSystem.current.SetSelectedGameObject(choices[next].gameObject);
        }

        void Awake()
        {
            ownedMaterials = new Material[hoodieRenderers.Length];
            originalMaterials = new Material[hoodieRenderers.Length];
            for (var i = 0; i < hoodieRenderers.Length; i++)
            {
                originalMaterials[i] = hoodieRenderers[i].sharedMaterial;
                ownedMaterials[i] = new Material(originalMaterials[i]);
                hoodieRenderers[i].sharedMaterial = ownedMaterials[i];
            }
            ApplyColor(LobbyPreferences.LoadHoodieColor());
            ApplyVolume(LobbyPreferences.LoadVolume());
            SelectTab(0);
        }
        public void SelectTab(int index)
        {
            if (IsLoading || index < 0 || index >= panels.Length) return;
            for (var i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == index);
                tabs[i].image.color = i == index ? new Color(1f, .85f, .29f) : new Color(.13f, .20f, .36f);
                tabs[i].GetComponentInChildren<Text>().color = i == index ? new Color(.04f, .09f, .18f) : Color.white;
            }
            Selectable first = index == 0 ? (Selectable)playButton : index == 1 ? colorButtons[LobbyPreferences.LoadHoodieColor()] : volumeSlider;
            for (var i = 0; i < tabs.Length; i++)
            {
                var nav = tabs[i].navigation;
                nav.selectOnDown = first;
                tabs[i].navigation = nav;
            }
            foreach (var selectable in panels[index].GetComponentsInChildren<Selectable>())
            {
                var nav = selectable.navigation;
                nav.selectOnUp = tabs[index];
                selectable.navigation = nav;
            }
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        public void SetHoodieColor(int index) => ApplyColor(LobbyPreferences.SaveHoodieColor(index));
        void ApplyColor(int index)
        {
            ColorUtility.TryParseHtmlString(HoodieHex[index], out var color);
            foreach (var material in ownedMaterials) material.color = color;
            for (var i = 0; i < colorButtons.Length; i++)
                colorButtons[i].transform.Find("SelectionFrame").gameObject.SetActive(i == index);
        }
        public void SetVolume(float value) => ApplyVolume(LobbyPreferences.SaveVolume(value));
        void ApplyVolume(float value)
        {
            AudioListener.volume = value;
            volumeSlider.SetValueWithoutNotify(value);
            volumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
        public void Play()
        {
            if (IsLoading) return;
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                statusText.text = "Не удалось найти игровой уровень";
                return;
            }
            IsLoading = true;
            playButton.interactable = false;
            foreach (var tab in tabs) tab.interactable = false;
            statusText.text = "ЗАГРУЗКА…";
            try { SceneManager.LoadSceneAsync(gameSceneName); }
            catch (Exception)
            {
                IsLoading = false;
                playButton.interactable = true;
                foreach (var tab in tabs) tab.interactable = true;
                statusText.text = "Не удалось открыть уровень. Попробуйте ещё раз";
            }
        }
        void OnDestroy()
        {
            if (ownedMaterials == null) return;
            for (var i = 0; i < ownedMaterials.Length; i++)
            {
                if (hoodieRenderers[i]) hoodieRenderers[i].sharedMaterial = originalMaterials[i];
                if (ownedMaterials[i]) Destroy(ownedMaterials[i]);
            }
        }
    }
}
