using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Slider expSlider;

    [Space]
    [SerializeField] LevelManagerSO levelManager;

    private void Start()
    {
        levelManager.levelUpEvent.AddListener(UpdateExpUI);
    }

    public void UpdateExpUI(int nowExp)
    {
        expSlider.maxValue = levelManager.requireEXP;
        expSlider.value = nowExp;
    }
}
