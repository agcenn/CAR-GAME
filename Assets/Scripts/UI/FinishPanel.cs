using OriginalKartRacer.Race;
using TMPro;
using UnityEngine;

namespace OriginalKartRacer.UI
{
    public sealed class FinishPanel : MonoBehaviour
    {
        [SerializeField] private RaceManager raceManager;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text resultText;

        private void OnEnable() => raceManager.RaceFinished += Show;
        private void OnDisable() => raceManager.RaceFinished -= Show;
        private void Start() => panel.SetActive(false);

        private void Show(float raceTime)
        {
            panel.SetActive(true);
            resultText.text =
                $"FINISH\n<size=65%>TIME  {RaceHUD.FormatTime(raceTime)}\n" +
                $"BEST  {RaceHUD.FormatTime(raceManager.BestLap)}</size>";
        }
    }
}
