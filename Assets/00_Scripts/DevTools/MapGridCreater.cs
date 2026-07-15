using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

public class MapGridCreater : MonoBehaviour
{
    [SerializeField] private Sprite[] Images;
    [SerializeField] private GameObject MapElementBase;
    [SerializeField] private Vector2 size;

    private Queue<GameObject> generatedElements = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        
    }

    /// <summary>
    /// size 크기만큼 그리드를 생성합니다.
    /// </summary>
    [Button]
    void CreateMap()
    {
        if (Images.Length <= 0)
        {
            Debug.LogWarning("이미지가 비어있어요. 작동을 중단합니다.");
            return;
        }
        Vector2 offset = (size / 2f) - (Vector2)MapElementBase.transform.localScale / 2;
        for (int y = 0; y < size.y; ++y)
        {
            for (int x = 0; x < size.x; ++x)
            {
                GameObject element = Instantiate(MapElementBase, transform);
                element.transform.localPosition = new Vector3(x - offset.x, y - offset.y, 0);
                element.GetComponent<SpriteRenderer>().sprite = Images[(x + y) % Images.Length];
                element.SetActive(true);
                generatedElements.Enqueue(element);
            }
        }
    }

    [Button]
    void DeleteMap()
    {
        while(generatedElements.Count > 0)
            DestroyImmediate(generatedElements.Dequeue());
    }
}
