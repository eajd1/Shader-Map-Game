using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

[RequireComponent(typeof(Canvas))]
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

    // Checks if the Cursor is in a UI Element that doesn't have the Ignore Cursor layer
    public bool CursorInCollider()
    {
        List<RectTransform> rects = new List<RectTransform>();
        rects = GetRects(transform, rects);
        foreach (RectTransform rectTransform in rects)
        {
            // Ignore Cursor layer means that the UI doesn't trigger this check
            if (rectTransform.rect.Contains(rectTransform.InverseTransformPoint(Input.mousePosition)) && LayerMask.NameToLayer("Ignore Cursor") != rectTransform.gameObject.layer)
            {
                return true;
            }
        }
        return false;
    }

    private List<RectTransform> GetRects(Transform parent, List<RectTransform> rects)
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

    protected uint GetCountryID(string name)
    {
        foreach (Country country in World.Instance.Countries)
        {
            if (country.name.Equals(name))
            {
                return country.ID;
            }
        }
        return 0;
    }
}
