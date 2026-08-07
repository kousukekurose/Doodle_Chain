using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class OKClick : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button okButton;
    [SerializeField]
    private Button doodleOKButton;
    [SerializeField]
    private TextMeshProUGUI odaiText;
    GameManager gameManager;
    DrawLine drawLine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        drawLine = DrawLine.instance;
        gameManager = GameManager.instance;
        inputField.gameObject.SetActive(false);
        okButton.gameObject.SetActive(false);
        doodleOKButton.gameObject.SetActive(true);
        odaiText.gameObject.SetActive(true);

    }

    public void OnOKButtonClick()
    {
        string inputText = inputField.text;
        if(gameManager != null)
        {
            gameManager.CheckAnswer(inputText);
        }
        else
        {
            Debug.LogError("GameManager instance is null.");
        }
        Debug.Log("Input Text: " + inputText);
    }

    public void OnDoodleOK()
    {
        inputField.gameObject.SetActive(true);
        okButton.gameObject.SetActive(true);
        odaiText.gameObject.SetActive(false);
        doodleOKButton.gameObject.SetActive(false);
        if(drawLine != null)
        {
            drawLine.canDraw = false;
            drawLine.StopDrawingForce(); 
        }
        else
        {
            Debug.LogError("DrawLine instance is null.");
        }
    }

    public void OnClearButtonClick()
    {
        inputField.text = "";
    }
}
