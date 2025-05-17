using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuController : MonoBehaviour
{
    public void LoadHighway() { SceneManager.LoadScene("Highway"); }
    public void LoadCity() { SceneManager.LoadScene("City"); }
    public void LoadDirt() { SceneManager.LoadScene("DirtRoad"); }

    public void LoadMenu() { SceneManager.LoadScene("MainMenu"); }
    public void QuitGame() { Application.Quit(); }
}
