using UnityEngine;

/// <summary>
/// Управляет переключением между экранами заказов и профиля
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject orderScreenPanel;
    public GameObject profileScreenPanel;

    void Start()
    {
        ShowOrderScreen();
    }

    public void ShowOrderScreen()
    {
        if (orderScreenPanel) orderScreenPanel.SetActive(true);
        if (profileScreenPanel) profileScreenPanel.SetActive(false);
    }

    public void ShowProfileScreen()
    {
        if (orderScreenPanel) orderScreenPanel.SetActive(false);
        if (profileScreenPanel) profileScreenPanel.SetActive(true);
    }
}
