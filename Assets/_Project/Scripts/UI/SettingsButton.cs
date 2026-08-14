using UnityEngine;
using UnityEngine.UI;

public class SettingsButton : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenSettings);
    }

    void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}