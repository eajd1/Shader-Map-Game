using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public abstract class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("No UIManager");
            }
            return instance;
        }
    }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("More than one UIManager in scene");
        }
        else
        {
            instance = this;
        }
    }

    [SerializeField] protected Transform canvas;

    public bool CursorInCollider()
    {
        List<RectTransform> rects = new List<RectTransform>();
        rects = GetRects(canvas, rects);
        foreach (RectTransform rectTransform in rects)
        {
            if (rectTransform.rect.Contains(rectTransform.InverseTransformPoint(Input.mousePosition)) && LayerMask.NameToLayer("Ignore Cursor") != rectTransform.gameObject.layer)
            {
                return true;
            }
        }
        return false;
    }

    protected List<RectTransform> GetRects(Transform parent, List<RectTransform> rects)
    {
        foreach (Transform transform in parent)
        {
            foreach (RectTransform rectTransform in transform.GetComponentsInChildren<RectTransform>())
            {
                if (rectTransform != null)
                    rects.Add(rectTransform);
            }
            GetRects(transform, rects);
        }
        return rects;
    }
}
