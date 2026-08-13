using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup)), ExecuteAlways]
public class GridControlChildSize : MonoBehaviour
{

    private GridLayoutGroup _glg;
    private RectTransform _rectTransform;

    private GridLayoutGroup glg
    {
        get
        {
            _glg ??= GetComponent<GridLayoutGroup>();
            return _glg;
        }
    }

    private RectTransform rectTransform
    {
        get
        {
            _rectTransform ??= GetComponent<RectTransform>();
            return _rectTransform;
        }
        
    }

    private void OnRectTransformDimensionsChange()
    {
        SizeSet();
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (UnityEditor.EditorApplication.isPlaying)
            return;

        SizeSet();
    }
#endif

    public void SizeSet()
    {
        if (glg.constraint == GridLayoutGroup.Constraint.Flexible)
            return;
        float totalSize = 0;
        float padding = 0;

        if(glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
        {
            totalSize = rectTransform.rect.width;
            padding = glg.padding.left + glg.padding.right + (glg.spacing.x * (glg.constraintCount - 1));
        }
        else if(glg.constraint == GridLayoutGroup.Constraint.FixedRowCount)
        {
            totalSize = rectTransform.rect.height;
            padding = glg.padding.top + glg.padding.bottom + (glg.spacing.y * (glg.constraintCount - 1));
        }

        float cellsize = (totalSize - padding) / glg.constraintCount;
        glg.cellSize = Vector2.one * cellsize;
    }
}
