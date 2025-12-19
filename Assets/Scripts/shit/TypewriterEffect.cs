using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    public TextMeshProUGUI dialogueText;
    [TextArea] public string fullText;
    public float typingSpeed = 0.05f;

    [Header("Typing Sound")]
    public AudioClip typingSound;  // ← сюда добавим звук
    public AudioSource audioSource; // ← источник звука (можно общий на сцене)

    void Start()
    {
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        dialogueText.text = "";

        foreach (char letter in fullText.ToCharArray())
        {
            dialogueText.text += letter;

            if (typingSound != null && audioSource != null)
            {
                // Проигрываем короткий звук каждой буквы
                audioSource.PlayOneShot(typingSound);
            }

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}
