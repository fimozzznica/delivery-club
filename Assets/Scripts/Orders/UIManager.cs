using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject orderScreenPanel;
    public GameObject profileScreenPanel;

    void Start(){ShowOrderScreen();}

    public void ShowOrderScreen()
    {
        if (orderScreenPanel) orderScreenPanel.SetActive(true);
        if (profileScreenPanel) profileScreenPanel.SetActive(false);
    }
}
