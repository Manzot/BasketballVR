using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [SerializeField] private Button m_quitBtn;
    [SerializeField] private Button m_restartBtn;

    private void OnEnable()
    {
        m_quitBtn.onClick.AddListener(OnQuit);
        m_restartBtn.onClick.AddListener(OnRestart);
    }

    private void OnDisable()
    {
        m_quitBtn.onClick.RemoveListener(OnQuit);
        m_restartBtn.onClick.RemoveListener(OnRestart);
    }

    private void OnRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnQuit()
    {
        Application.Quit();
    }
}
