using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

[RequireComponent(typeof(PlayerController))]
public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private LayerMask ignoreLayer;
    [SerializeField] private Transform canvas;
    [SerializeField] private Transform countryNamesParent;
    [SerializeField] private GameObject textPrefab;

    private PlayerController player;
    private RectTransform[] countryNames;

    public void SelectCountry(string unused)
    {
        player.SelectCountry(dropdown.options.ToArray()[dropdown.value].text);
    }

    public bool CursorInCollider()
    {
        List<RectTransform> rects = new List<RectTransform>();
        rects = GetRects(canvas, rects);
        foreach (RectTransform rectTransform in rects)
        {
            if (rectTransform.rect.Contains(rectTransform.InverseTransformPoint(Input.mousePosition)) && ignoreLayer.value != rectTransform.gameObject.layer)
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

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<PlayerController>();

        dropdown.ClearOptions();
        dropdown.AddOptions(World.Instance.Countries.Select(country => new TMP_Dropdown.OptionData(country.name)).ToList());

        countryNames = new RectTransform[World.Instance.Countries.Length - 1];
        for (int i = 0; i < countryNames.Length; i++)
        {
            countryNames[i] = Instantiate(textPrefab).GetComponent<RectTransform>();
            countryNames[i].gameObject.layer = ignoreLayer.value;
            countryNames[i].SetParent(countryNamesParent);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int pos = player.CursorPosition;
        text.text = $"X: {pos.x}\nY: {pos.y}\nHeight: {World.Instance.GetHeight(pos)}";

        for (int i = 0; i < countryNames.Length; i++)
        {
            Country country = World.Instance.Countries[i + 1];

            Vector2 midpoint = country.namePoint;
            midpoint.x /= World.Instance.WorldResolution * 2;
            midpoint.y /= World.Instance.WorldResolution;
            midpoint.x = midpoint.x - 0.5f;
            midpoint.y = midpoint.y - 0.5f;
            midpoint -= player.GetControls().GetUV() / 2f;
            midpoint /= player.GetControls().GetZoom();
            midpoint.x *= Screen.currentResolution.width;
            midpoint.y *= Screen.currentResolution.height;

            RectTransform rectTransform = countryNames[i];
            rectTransform.anchoredPosition = midpoint;
            rectTransform.GetComponent<TextMeshProUGUI>().text = country.name;
        }
    }
}
