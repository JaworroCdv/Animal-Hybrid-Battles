namespace AnimalHybridBattles.UI
{
    using System;
    using TMPro;
    using UnityEngine;

    public class MoneyUIView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerOneMoneyText;
        [SerializeField] private TextMeshProUGUI playerTwoMoneyText;

        private void Start()
        {
            OnMoneyChanged();
            GameRunner.Instance.OnPlayerMoneyChanged += OnMoneyChanged;
        }

        private void OnMoneyChanged()
        {
            playerOneMoneyText.text = $"Energy: {GameRunner.Instance.PlayersEnergy[0]}";
            playerTwoMoneyText.text = $"Energy: {GameRunner.Instance.PlayersEnergy[1]}";
        }
    }
}