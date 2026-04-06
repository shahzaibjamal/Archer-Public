using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameplayUI : MonoBehaviour
{
    [Header("Upgrade Buttons")]
    public Button upgradeDamageBtn;
    public Button upgradeSpeedBtn;
    public Button upgradeCooldownBtn;
    public Button upgradeMoveSpeedBtn;
    public Button upgradeRangeBtn;

    [Header("Power Up Buttons")]
    public Button healBtn;
    public Button invincibilityBtn;
    public Button attackUpBtn;
    public Button defenseUpBtn;
    public Button defenseDownBtn;

    [Header("Special Arsenal")]
    public Button firePiercingBtn;


    public Button MenuBtn;
    public GameObject MenuPanel;
    private bool _isMenuOpen = false;

    private void Start()
    {
        // Bind Upgrades
        upgradeDamageBtn?.onClick.AddListener(() => DataManager.Instance.UpgradeStat(StatType.Damage));
        upgradeSpeedBtn?.onClick.AddListener(() => DataManager.Instance.UpgradeStat(StatType.AttackInterval));
        upgradeCooldownBtn?.onClick.AddListener(() => DataManager.Instance.UpgradeStat(StatType.Cooldown));
        upgradeMoveSpeedBtn?.onClick.AddListener(() => DataManager.Instance.UpgradeStat(StatType.MoveSpeed));
        upgradeRangeBtn?.onClick.AddListener(() => DataManager.Instance.UpgradeStat(StatType.Range));

        // Bind PowerUps
        healBtn?.onClick.AddListener(() => GameEvents.TriggerPowerUp(PowerUpType.Heal));
        invincibilityBtn?.onClick.AddListener(() => GameEvents.TriggerPowerUp(PowerUpType.Invincibility));
        attackUpBtn?.onClick.AddListener(() => GameEvents.TriggerPowerUp(PowerUpType.AttackUp));
        defenseUpBtn?.onClick.AddListener(() => GameEvents.TriggerPowerUp(PowerUpType.DefenseUp));
        defenseDownBtn?.onClick.AddListener(() => GameEvents.TriggerPowerUp(PowerUpType.DefenseDown));

        // Bind Special
        firePiercingBtn?.onClick.AddListener(() => GameEvents.TriggerSpecialArrow(ArrowType.Piercing));
        MenuBtn?.onClick.AddListener(ShowMenu);

    }

    private void ShowMenu()
    {
        _isMenuOpen = !_isMenuOpen;
        MenuPanel.SetActive(_isMenuOpen);
    }
}
