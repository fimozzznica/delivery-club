using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextEditor : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI canvasText;

    public void UpdateText(string text)
    {
        canvasText.text = text;
    }
}