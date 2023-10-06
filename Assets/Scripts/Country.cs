using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Country
{
    private int id;
    public int ID { get { return id; } }

    private Vector3 colour; // 3 floats between 0 and 1
    public Vector3 Colour { get { return colour; } }

    private string name;
    public string Name { get { return name; } }

    public Country(int id, Vector3 colour, string name)
    {
        this.id = id;
        this.colour = colour;
        this.name = name;
    }
}
