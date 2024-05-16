using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.Threading;

public class CountryPainterUI : UIManager
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private TMP_Dropdown dropdown;

    private PlayerController player;

    private uint selectedCountry = 0;

    public void SelectCountry(string unused)
    {
        selectedCountry = GetCountryID(dropdown.options.ToArray()[dropdown.value].text);
    }

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

        Vector2Int pos = player.CursorPosition;
        text.text = $"X: {pos.x}\nY: {pos.y}\nHeight: {World.Instance.GetHeight(pos)}\nOwner:\n{World.Instance.GetOwner(pos).name}";

        if (!CursorInCollider())
        {
            if (Input.GetButtonDown(Inputs.MMB))
            {
                SetOwnerFill(pos);
            }

            if (Input.GetButton(Inputs.LMB))
            {
                World.Instance.SetOwnerInstant(pos, selectedCountry);
            }

            if (Input.GetButton(Inputs.RMB))
            {
                World.Instance.SetOwnerInstant(pos, 0);
            }
        }
    }

    private void SetOwnerFill(Vector2Int start)
    {
        Thread thread = new Thread(new ThreadStart(() =>
        {
            int resolution = World.Instance.WorldResolution;
            if (World.Instance.Tiles[start.x * resolution + start.y].IsOcean())
                return;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            List<int> visited = new List<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                Vector2Int pos = queue.Dequeue();
                int index = pos.x * resolution + pos.y;
                if (World.Instance.Tiles[index].owner != selectedCountry)
                {
                    World.Instance.SetOwner(pos, selectedCountry);
                }

                Vector2Int newPos = World.Instance.ValidatePosition(new Vector2Int(pos.x + 1, pos.y));
                index = newPos.x * resolution + newPos.y;
                if (!World.Instance.Tiles[index].IsOcean() && World.Instance.Tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = World.Instance.ValidatePosition(new Vector2Int(pos.x - 1, pos.y));
                index = newPos.x * resolution + newPos.y;
                if (!World.Instance.Tiles[index].IsOcean() && World.Instance.Tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = World.Instance.ValidatePosition(new Vector2Int(pos.x, pos.y + 1));
                index = newPos.x * resolution + newPos.y;
                if (!World.Instance.Tiles[index].IsOcean() && World.Instance.Tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
                newPos = World.Instance.ValidatePosition(new Vector2Int(pos.x, pos.y - 1));
                index = newPos.x * resolution + newPos.y;
                if (!World.Instance.Tiles[index].IsOcean() && World.Instance.Tiles[index].owner == 0 && !visited.Contains(index))
                {
                    queue.Enqueue(new Vector2Int(newPos.x, newPos.y));
                    visited.Add(index);
                }
            }
        }));
        thread.Start();
    }
}
