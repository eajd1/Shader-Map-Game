using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CountryManager
{
    private Country[] countries;

    public CountryManager()
    {
        countries = new Country[2];
        countries[0] = new Country(0, new Vector3(0, 0, 0));
        countries[1] = new Country(1, new Vector3(1, 0, 0));
    }

    public Vector3[] GetColours()
    {
        Vector3[] array = new Vector3[countries.Length];
        for (int i = 0; i < countries.Length; i++)
        {
            array[i] = countries[i].Colour;
        }
        return array;
    }
}
