using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneChange : MonoBehaviour
{

    public void SceneChanges(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
