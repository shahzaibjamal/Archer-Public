using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TMP_Text healthAmount;
    [SerializeField] private PlayerController _playerController;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (_playerController != null)
        {
            _playerController.OnHealthChanged += UpdateHealthBar;
            UpdateHealthBar(_playerController.CurrentHealth, _playerController.MaxHealth);
        }
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            // transform.transform.LookAt(mainCamera.transform);
            transform.forward = mainCamera.transform.forward;
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

}