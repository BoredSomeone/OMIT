using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Slider expSlider;

    [Space]
    [SerializeField] LevelManagerSO levelManager;

    private void Start()
    {
        levelManager.expUpEvent.AddListener(UpdateExpUI);
        UpdateExpUI();
    }

    public void UpdateExpUI()
    {
        expSlider.maxValue = levelManager.requireEXP;
        expSlider.value = levelManager.nowEXP;
    }
}
