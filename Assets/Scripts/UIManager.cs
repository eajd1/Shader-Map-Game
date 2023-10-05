using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(PlayerController))]
public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;

    private PlayerController player;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2Int pos = player.CursorPosition;
        text.text = $"X: {pos.x}\nY: {pos.y}\nHeight: {World.Instance.GetHeight(pos)}";
    }
}
