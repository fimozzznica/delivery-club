using UnityEngine;
using UnityEngine.UI;

public class Next : MonoBehaviour
{
    [SerializeField] Button nextButton;
    [SerializeField] GameObject nextMenu;
    [SerializeField] GameObject thisCanvas;
    public void DoButtonThing()
    {
        thisCanvas.SetActive(nextMenu.activeInHierarchy);
        nextMenu.SetActive(!nextMenu.activeInHierarchy);
    }

    private void Awake()
    {
        nextButton.onClick.AddListener(DoButtonThing);
    }
}

