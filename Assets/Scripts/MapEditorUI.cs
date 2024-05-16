using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class MapEditorUI : UIManager
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_InputField height;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private Toggle oceanToggle;
    [SerializeField] private Toggle lakeToggle;

    private PlayerController player;
    private bool tileSelected = false;
    private Tile selectedTile;
    private Vector2Int tilePos;

    // Start is called before the first frame update
    void Start()
    {
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

        if (Input.GetButton(Inputs.Shift))
        {
            if (Input.GetButton(Inputs.LMB) && !CursorInCollider())
            {
                selectedTile = World.Instance.GetTile(player.CursorPosition);
                tilePos = player.CursorPosition;
                tileSelected = true;
                ApplyChanges("");
            }
        }
        else
        {
            if (Input.GetButtonDown(Inputs.LMB) && !CursorInCollider())
            {
                selectedTile = World.Instance.GetTile(player.CursorPosition);
                tilePos = player.CursorPosition;
                tileSelected = true;
            }
        }

        if (tileSelected)
        {
            text.text = $"Height: {selectedTile.height}\nOwner: {selectedTile.owner}\nDetails: {System.Convert.ToString(selectedTile.details, 2).PadLeft(32, '0')}";
        }
    }

    public void ApplyChanges(string unused)
    {
        if (float.TryParse(height.text, out float heightValue))
        {
            selectedTile.height = heightValue;
        }
        string countryName = dropdown.options.ToArray()[dropdown.value].text;
        selectedTile.owner = GetCountryID(countryName);
        uint details = 0;
        if (oceanToggle.isOn)
        {
            details |= 1u << 31;
        }
        if (lakeToggle.isOn)
        {
            details |= 1u << 30;
        }
        selectedTile.details = details;

        World.Instance.ChangeTile(selectedTile, tilePos);
    }
}
