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

    private PlayerController player;

    public void SelectCountry(string unused)
    {
        player.SelectCountry(dropdown.options.ToArray()[dropdown.value].text);
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<PlayerController>();

        dropdown.ClearOptions();
        dropdown.AddOptions(World.Instance.Countries.Select(country => new TMP_Dropdown.OptionData(country.Name)).ToList());
    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int pos = player.CursorPosition;
        text.text = $"X: {pos.x}\nY: {pos.y}\nHeight: {World.Instance.GetHeight(pos)}";
    }
}
