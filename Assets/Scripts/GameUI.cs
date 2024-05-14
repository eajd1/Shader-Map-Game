using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class GameUI : UIManager
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Transform canvas;
    [SerializeField] private Transform countryNamesParent;
    [SerializeField] private GameObject textPrefab;

    private PlayerController player;
    private RectTransform[] countryNames;

    public void SelectCountry(string unused)
    {
        player.SelectCountry(dropdown.options.ToArray()[dropdown.value].text);
    }

    override public bool CursorInCollider()
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

    // Start is called before the first frame update
    void Start()
    {
        countryNames = new RectTransform[World.Instance.Countries.Length - 1];
        for (int i = 0; i < countryNames.Length; i++)
        {
            countryNames[i] = Instantiate(textPrefab).GetComponent<RectTransform>();
            countryNames[i].gameObject.layer = LayerMask.NameToLayer("Ignore Cursor");
            countryNames[i].SetParent(countryNamesParent);
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(World.Instance.Countries.Select(country => new TMP_Dropdown.OptionData(country.name)).ToList());
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null)
        {
            player = World.Instance.GetPlayer(0);
        }

        try
        {
            Vector2Int pos = player.CursorPosition;
            text.text = $"X: {pos.x}\nY: {pos.y}\nHeight: {World.Instance.GetHeight(pos)}\nIndex: {pos.x * World.Instance.WorldResolution + pos.y}";
        }
        catch
        {
            return;
        }

        for (int i = 0; i < countryNames.Length; i++)
        {
            Country country = World.Instance.Countries[i + 1];

            Vector2 midpoint = country.namePoint;
            midpoint.x /= World.Instance.WorldResolution * 2;
            midpoint.y /= World.Instance.WorldResolution;
            midpoint.x = midpoint.x - 0.5f;
            midpoint.y = midpoint.y - 0.5f;
            midpoint -= player.GetControls().GetCameraUV() / 2f;
            midpoint /= player.GetControls().GetZoom();
            midpoint.x *= Screen.currentResolution.width;
            midpoint.y *= Screen.currentResolution.height;

            RectTransform rectTransform = countryNames[i];
            rectTransform.anchoredPosition = midpoint;
            rectTransform.GetComponent<TextMeshProUGUI>().text = country.name;
        }
    }
}
