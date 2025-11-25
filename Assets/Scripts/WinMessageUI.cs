using UnityEngine;

public class WinMessageUI : MonoBehaviour
{
    public static WinMessageUI Instance;

    [SerializeField] private GameObject winPanel;

    private void Awake()
    {
        Instance = this;
        winPanel.SetActive(false);
    }

    public static void ShowWin()
    {
        if (Instance != null)
            Instance.winPanel.SetActive(true);
    }
}
