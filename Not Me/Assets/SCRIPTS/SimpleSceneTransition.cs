using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneTransition : MonoBehaviour
{
    // Public variable to specify the scene name in Unity
    public string sceneName;

    // Function to load the specified scene
    public void LoadScene()
    {
        // Check if sceneName has been set
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is not set in the Inspector!");
        }
    }
}