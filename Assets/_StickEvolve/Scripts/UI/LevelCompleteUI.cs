using StickEvolve.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StickEvolve.UI
{
    /// <summary>
    /// Экран "Уровень пройден" со звёздами, наградой и кнопками
    /// "Следующий уровень" и "К карте".
    /// </summary>
    public class LevelCompleteUI : MonoBehaviour
    {
        private GameObject _root;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _rewardText;
        private TextMeshProUGUI _starsText;
        private TextMeshProUGUI _nextBtnText;
        private System.Action _onNextLevel;
        private System.Action _onShowMap;

        public static LevelCompleteUI Create(Canvas canvas, System.Action onNextLevel, System.Action onShowMap)
        {
            var go = new GameObject("LevelCompleteUI");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var ui = go.AddComponent<LevelCompleteUI>();
            ui._onNextLevel = onNextLevel;
            ui._onShowMap = onShowMap;
            ui.Build();
            ui._root.SetActive(false);
            return ui;
        }

        private void Build()
        {
            _root = new GameObject("Root");
            _root.transform.SetParent(transform, false);
            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = new GameObject("BG");
            bg.transform.SetParent(_root.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.75f);
            bgImg.raycastTarget = true;

            // Заголовок
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(_root.transform, false);
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.75f);
            titleRT.anchorMax = new Vector2(0.5f, 0.75f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.sizeDelta = new Vector2(900f, 140f);
            _titleText = titleGO.AddComponent<TextMeshProUGUI>();
            _titleText.text = "УРОВЕНЬ ПРОЙДЕН";
            _titleText.fontSize = 84;
            _titleText.color = new Color(1f, 0.85f, 0.2f);
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontStyle = FontStyles.Bold;

            // Звёзды
            var starsGO = new GameObject("Stars");
            starsGO.transform.SetParent(_root.transform, false);
            var starsRT = starsGO.AddComponent<RectTransform>();
            starsRT.anchorMin = new Vector2(0.5f, 0.6f);
            starsRT.anchorMax = new Vector2(0.5f, 0.6f);
            starsRT.pivot = new Vector2(0.5f, 0.5f);
            starsRT.sizeDelta = new Vector2(600f, 100f);
            _starsText = starsGO.AddComponent<TextMeshProUGUI>();
            _starsText.text = "";
            _starsText.fontSize = 90;
            _starsText.color = new Color(1f, 0.95f, 0.4f);
            _starsText.alignment = TextAlignmentOptions.Center;

            // Награда
            var rewardGO = new GameObject("Reward");
            rewardGO.transform.SetParent(_root.transform, false);
            var rewardRT = rewardGO.AddComponent<RectTransform>();
            rewardRT.anchorMin = new Vector2(0.5f, 0.45f);
            rewardRT.anchorMax = new Vector2(0.5f, 0.45f);
            rewardRT.pivot = new Vector2(0.5f, 0.5f);
            rewardRT.sizeDelta = new Vector2(900f, 200f);
            _rewardText = rewardGO.AddComponent<TextMeshProUGUI>();
            _rewardText.text = "";
            _rewardText.fontSize = 42;
            _rewardText.color = Color.white;
            _rewardText.alignment = TextAlignmentOptions.Center;

            // Кнопка Next
            var btnGO = new GameObject("NextBtn");
            btnGO.transform.SetParent(_root.transform, false);
            var btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.5f, 0.25f);
            btnRT.anchorMax = new Vector2(0.5f, 0.25f);
            btnRT.pivot = new Vector2(0.5f, 0.5f);
            btnRT.anchoredPosition = new Vector2(-240f, 0f);
            btnRT.sizeDelta = new Vector2(420f, 110f);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.25f, 0.7f, 0.35f);
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() =>
            {
                Hide();
                _onNextLevel?.Invoke();
            });

            var btnTGO = new GameObject("BtnText");
            btnTGO.transform.SetParent(btnGO.transform, false);
            var btnTRT = btnTGO.AddComponent<RectTransform>();
            btnTRT.anchorMin = Vector2.zero;
            btnTRT.anchorMax = Vector2.one;
            btnTRT.offsetMin = Vector2.zero;
            btnTRT.offsetMax = Vector2.zero;
            _nextBtnText = btnTGO.AddComponent<TextMeshProUGUI>();
            _nextBtnText.text = "СЛЕД. УРОВЕНЬ";
            _nextBtnText.fontSize = 36;
            _nextBtnText.color = Color.white;
            _nextBtnText.alignment = TextAlignmentOptions.Center;
            _nextBtnText.fontStyle = FontStyles.Bold;

            // Кнопка Map
            var mapGO = new GameObject("MapBtn");
            mapGO.transform.SetParent(_root.transform, false);
            var mapRT = mapGO.AddComponent<RectTransform>();
            mapRT.anchorMin = new Vector2(0.5f, 0.25f);
            mapRT.anchorMax = new Vector2(0.5f, 0.25f);
            mapRT.pivot = new Vector2(0.5f, 0.5f);
            mapRT.anchoredPosition = new Vector2(240f, 0f);
            mapRT.sizeDelta = new Vector2(420f, 110f);
            var mapImg = mapGO.AddComponent<Image>();
            mapImg.color = new Color(0.30f, 0.45f, 0.85f);
            var mapBtn = mapGO.AddComponent<Button>();
            mapBtn.targetGraphic = mapImg;
            mapBtn.onClick.AddListener(() =>
            {
                Hide();
                _onShowMap?.Invoke();
            });

            var mapTGO = new GameObject("MapText");
            mapTGO.transform.SetParent(mapGO.transform, false);
            var mapTRT = mapTGO.AddComponent<RectTransform>();
            mapTRT.anchorMin = Vector2.zero;
            mapTRT.anchorMax = Vector2.one;
            mapTRT.offsetMin = Vector2.zero;
            mapTRT.offsetMax = Vector2.zero;
            var mapText = mapTGO.AddComponent<TextMeshProUGUI>();
            mapText.text = "К КАРТЕ";
            mapText.fontSize = 36;
            mapText.color = Color.white;
            mapText.alignment = TextAlignmentOptions.Center;
            mapText.fontStyle = FontStyles.Bold;
        }

        public void Show(int justCompletedLevel, long goldEarned, int stars)
        {
            _root.SetActive(true);

            int nextLevel = justCompletedLevel + 1;
            bool isCampaignDone = nextLevel > CampaignBuilder.TotalLevels;
            bool isBiomeEnd = (justCompletedLevel % CampaignBuilder.LevelsPerBiome) == 0;

            // Звёзды
            string starsLine = "";
            for (int i = 0; i < stars; i++) starsLine += "\u2605";
            for (int i = stars; i < 3; i++) starsLine += "\u2606";
            _starsText.text = starsLine;

            if (isCampaignDone)
            {
                _titleText.text = "ИГРА ПРОЙДЕНА!";
                _titleText.color = new Color(1f, 0.5f, 0.2f);
                _rewardText.text = $"Все 40 уровней пройдены.\nЗолота за уровень: {goldEarned}";
                _nextBtnText.text = "ЗАНОВО";
            }
            else if (isBiomeEnd)
            {
                var nextDef = CampaignBuilder.Build(nextLevel);
                _titleText.text = "БИОМ ПРОЙДЕН!";
                _titleText.color = new Color(1f, 0.85f, 0.2f);
                _rewardText.text = $"Босс повержен! Далее: {nextDef.biomeName}\nЗолота: {goldEarned}";
                _nextBtnText.text = "В " + nextDef.biomeName.ToUpper();
            }
            else
            {
                _titleText.text = "УРОВЕНЬ ПРОЙДЕН";
                _titleText.color = new Color(1f, 0.85f, 0.2f);
                _rewardText.text = $"Уровень {justCompletedLevel} завершён\nЗолота: {goldEarned}";
                _nextBtnText.text = "СЛЕД. УРОВЕНЬ";
            }
        }

        public void Hide()
        {
            _root.SetActive(false);
        }
    }
}
