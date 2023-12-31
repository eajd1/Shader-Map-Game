using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Country
{
    private uint id;
    public uint ID { get { return id; } }
    public Vector3 colour; // 3 floats between 0 and 1
    public string name;
    public Vector2 namePoint = new Vector2(-999, -999);

    public Country(uint id, Vector3 colour, string name)
    {
        this.id = id;
        this.colour = colour;
        this.name = name;
    }

    public override string ToString()
    {
        return $"id: {id}, colour: {colour}, name: {name}";
    }
}
