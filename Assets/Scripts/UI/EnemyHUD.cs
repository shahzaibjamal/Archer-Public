using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHUD : MonoBehaviour
{
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthAmount;
    [SerializeField] private Enemy targetEnemy;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (targetEnemy != null)
        {
            transform.forward = mainCamera.transform.forward;
            targetEnemy.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(targetEnemy.CurrentHealth, targetEnemy.MaxHealth);
        }
    }

    public void SetTarget(Enemy enemy)
    {
        if (targetEnemy != null)
        {
            targetEnemy.OnHealthChanged -= UpdateHealthBar;
        }

        targetEnemy = enemy;
        if (targetEnemy != null)
        {
            targetEnemy.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(targetEnemy.CurrentHealth, targetEnemy.MaxHealth);
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = current / max;
        }
        if (healthAmount != null)
        {
            healthAmount.text = $"{current} / {max}";
        }
    }

    private void OnDestroy()
    {
        if (targetEnemy != null)
        {
            targetEnemy.OnHealthChanged -= UpdateHealthBar;
        }
    }
}