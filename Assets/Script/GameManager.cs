using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;

//CSV
[System.Serializable]
public class CSVData
{
    public int no;
    public string name;
}


public class GameManager : MonoBehaviour
{
    public static GameManager instance{get; private set;}
    [SerializeField] 
    private TextMeshProUGUI odaiText;
    [SerializeField]
    private TextMeshProUGUI answerText;
    [SerializeField]
    private Button okButton;
    [SerializeField]
    private GameObject endObject;
    [SerializeField]
    private float timeToShowEndObject = 2f;

    private List<CSVData> csvDataList = new List<CSVData>();
    private CSVData currentCSVData;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        endObject.gameObject.SetActive(false);
        answerText.gameObject.SetActive(false);
        LoadCSVData();
        SpawnNextCSVData();
    }

    private void LoadCSVData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("odai");
        string[] lines = csvFile.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            CSVData data = new CSVData
            {
                no = int.Parse(values[0]),
                name = values[1]
            };
            csvDataList.Add(data);
        }
    }

    public void SpawnNextCSVData()
    {
        if (csvDataList.Count == 0)return;

        if(answerText.text != null)
        {
            answerText.text = "";
        }


        int randomIndex = Random.Range(0, csvDataList.Count);
        currentCSVData = csvDataList[randomIndex];

        odaiText.text = $"お題 {currentCSVData.name}";
        answerText.text = "";
    }

    public void CheckAnswer(string inputText)
    {
        if(answerText == null || odaiText == null)return;

        answerText.gameObject.SetActive(true);
        string clarifiedInputText = inputText.Trim();
        string clearedCorrectAnswer = currentCSVData.name.Trim();
        if (currentCSVData != null && clarifiedInputText.Equals(clearedCorrectAnswer))
        {
            odaiText.gameObject.SetActive(true);
            okButton.gameObject.SetActive(false);
            StartCoroutine(ShowEndObject(timeToShowEndObject));
            answerText.color = Color.red;
            answerText.text = "正解";
        }
        else
        {
            answerText.color = Color.blue;
            answerText.text = "不正解";
        }
    }

    private IEnumerator ShowEndObject(float time)
    {
        yield return new WaitForSeconds(time);
        endObject.gameObject.SetActive(true);
    }

    
}
