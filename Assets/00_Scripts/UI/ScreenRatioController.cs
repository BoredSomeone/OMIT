using System.Collections;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ScreenRatioController : MonoBehaviour
{
    public enum Status
    {
        None,
        Pillar,
        Letter
    }

    public RectTransform mainUIArea;
    public RectTransform boxTopLeft;
    public RectTransform boxBotRight;

    public CanvasScaler canvasScaler;

    public Vector2 targetScreenRatio;
    public float mainCameraSize = 10;

    [SerializeField, Range(-1f, 1f)]
    private float _layoutOffset;

    public float layoutOffset
    {
        get
        {
            return (_layoutOffset + 1f) * 0.5f;
        }
    }

    [SerializeField, ReadOnly]
    float screenRatioX;
    [SerializeField, ReadOnly]
    Status status;

    public UnityEvent resolutionChangeEvent;

    Camera _mainCam;
    Camera mainCam
    {
        get
        {
            if (_mainCam == null)
                _mainCam = Camera.main;

            return _mainCam;
        }
    }

    [ShowInInspector, ReadOnly]
    float targetRatio => targetScreenRatio.y != 0 ? targetScreenRatio.x / targetScreenRatio.y : 0;
    private void Awake()
    {
        resolutionChangeEvent.RemoveListener(UpdateScreenLayout);
        resolutionChangeEvent.AddListener(UpdateScreenLayout);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(ResolutionWatcher());

        screenRatioX = (float)Screen.width / Screen.height;
        UpdateScreenLayout();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    IEnumerator ResolutionWatcher()
    {
        Vector2Int res = new();
        Vector2Int nowRes = new();

        res.x = Screen.width;
        res.y = Screen.height;
        while (true)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            nowRes.x = Screen.width;
            nowRes.y = Screen.height;

            if (res != nowRes)
            {
                screenRatioX = (float)Screen.width / Screen.height;
                res = nowRes;
                if (resolutionChangeEvent != null)
                    resolutionChangeEvent.Invoke();
            }
        }
    }
    void UpdateScreenLayout()
    {
        void resetRect(params RectTransform[] rt)
        {
            for (int i = 0; i < rt.Length; ++i)
            {
                if (rt[i] != null)
                {
                    rt[i].offsetMin = Vector2.zero;
                    rt[i].offsetMax = Vector2.zero;
                }
            }
        }

        //가로로 더 길 때
        if (screenRatioX > targetRatio)
        {
            float diff = (screenRatioX - targetRatio) / screenRatioX;

            float left = diff * layoutOffset;
            float right = 1 - diff * (1 - layoutOffset);

            if (boxTopLeft)
            {
                boxTopLeft.anchorMin = Vector2.zero;
                boxTopLeft.anchorMax = new Vector2(left, 1);
            }
            if (boxBotRight)
            {
                boxBotRight.anchorMin = new Vector2(right, 0);
                boxBotRight.anchorMax = Vector2.one;
            }

            if (mainUIArea)
            {
                mainUIArea.anchorMin = new(left, 0);
                mainUIArea.anchorMax = new(right, 1);
            }

            resetRect(boxTopLeft, boxBotRight, mainUIArea);
            mainCam.rect = new Rect(left, 0, right - left, 1);
            ScreenRatioStatusChange(Status.Pillar);
        }
        //세로가 더 길 때
        else if (screenRatioX < targetRatio)
        {
            float diff = (targetRatio - screenRatioX) / targetRatio;

            float top = 1 - diff * (1 - layoutOffset);
            float bot = diff * layoutOffset;

            if (boxTopLeft)
            {
                boxTopLeft.anchorMin = new Vector2(0, top);
                boxTopLeft.anchorMax = Vector2.one;
            }
            if (boxBotRight)
            {
                boxBotRight.anchorMin = Vector2.zero;
                boxBotRight.anchorMax = new Vector2(1, bot);
            }

            if (mainUIArea)
            {
                mainUIArea.anchorMin = new(0, bot);
                mainUIArea.anchorMax = new(1, top);
            }

            resetRect(boxTopLeft, boxBotRight, mainUIArea);
            mainCam.rect = new Rect(0, bot, 1, top - bot);
            ScreenRatioStatusChange(Status.Letter);
        }

        else
        {
            if (boxTopLeft)
            {
                boxTopLeft.anchorMin = Vector2.zero;
                boxTopLeft.anchorMax = Vector2.zero;
            }
            if (boxBotRight)
            {
                boxBotRight.anchorMin = Vector2.one;
                boxBotRight.anchorMax = Vector2.one;
            }
            if (mainUIArea)
            {
                mainUIArea.anchorMin = Vector2.zero;
                mainUIArea.anchorMax = Vector2.one;
            }

            resetRect(boxTopLeft, boxBotRight, mainUIArea);
            mainCam.rect = new Rect(0, 0, 1, 1);
            ScreenRatioStatusChange(Status.None);
        }
    }

    void ScreenRatioStatusChange(Status s)
    {
        if (status == s || canvasScaler == null)
            return;

        status = s;

        if (s == Status.Pillar)
            canvasScaler.matchWidthOrHeight = 1;
        else
            canvasScaler.matchWidthOrHeight = 0;
    }

    public bool STOP_IN_EDITOR;
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (STOP_IN_EDITOR)
            return;
        if (UnityEditor.EditorApplication.isPlaying)
            return;

        Vector2 screen = EditorGameViewResolution();

        screenRatioX = screen.x / screen.y;

        UpdateScreenLayout();
    }

    Vector2 EditorGameViewResolution()
    {
        System.Type T = System.Type.GetType("UnityEditor.GameView, UnityEditor");
        System.Reflection.MethodInfo getSizeOfMainGameView = T.GetMethod("GetSizeOfMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        object res = getSizeOfMainGameView.Invoke(null, null);

        return (Vector2)res;
    }
#endif
}
