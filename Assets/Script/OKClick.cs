using UnityEngine;
using TMPro;

public class OKClick : MonoBehaviour
{
    TMP_InputField inputField;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    public void OnOKButtonClick()
    {
        string inputText = inputField.text;
        Debug.Log("Input Text: " + inputText);
    }
}
