using UnityEngine;
using System.Collections.Generic;
using TMPro;

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

    private List<CSVData> csvDataList = new List<CSVData>();
    private CSVData currentCSVData;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        answerText.gameObject.SetActive(false);
        LoadCSVData();
        SpawnNextCSVData();
    }

    // Update is called once per frame
    void Update()
    {
        
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
            answerText.color = Color.red;
            answerText.text = "正解";
        }
        else
        {
            answerText.color = Color.blue;
            answerText.text = "不正解";
        }
    }
}
