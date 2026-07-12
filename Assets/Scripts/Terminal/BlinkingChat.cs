using System.Collections;
using UnityEngine;
using TMPro;

public class PassiveBlink : MonoBehaviour
{
    private TextMeshProUGUI textComponent;
    [SerializeField] private float blinkSpeed = 0.5f; // Kecepatan kedip (detik)
    private string baseText;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        baseText = textComponent.text; 
        StartCoroutine(BlinkRoutine());
    }

    IEnumerator BlinkRoutine()
    {
        while (true)
        {
            textComponent.text = baseText; 
            yield return new WaitForSeconds(blinkSpeed);
            
            textComponent.text = baseText.Replace("_", " "); 
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
}