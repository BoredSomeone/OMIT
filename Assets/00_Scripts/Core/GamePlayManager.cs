using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

public class GamePlayManager : MonoBehaviour
{
    [SerializeField] private LevelManagerSO levelManager;
    [SerializeField] private PauseManagerSO pauseManager;


    private void Awake()
    {
        InitScene();
    }

    private void InitScene()
    {
        //저장 기능 완성되면 불러오기 기능 만들어야되요.
        levelManager.LevelSet(1);
        levelManager.ExpSet(0);

        pauseManager.ResetPauseCount();
    }
}
