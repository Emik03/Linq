﻿using System;
using UnityEngine;
using Rnd = UnityEngine.Random;

internal static class Function
{
    /// <summary>
    /// Returns the character as lowercase.
    /// </summary>
    /// <typeparam name="T">The datatype of the variable.</typeparam>
    /// <param name="source">The variable to apply lowercase to.</param>
    /// <returns>The lowercase version of the character.</returns>
    internal static char ToLower<Tchar>(this Tchar source)
    {
        return source.ToString().ToLowerInvariant()[0];
    }


    public static bool HasComponent<T>(this GameObject obj)
    {
        return (obj.GetComponent<T>() as Component) != null;
    }

    /// <summary>
    /// Inverts a boolean.
    /// </summary>
    /// <param name="boolean">The boolean to invert.</param>
    internal static void InvertBoolean(ref bool boolean)
    {
        boolean = !boolean;
    }

    /// <summary>
    /// Inverts all booleans in an array.
    /// </summary>
    /// <param name="booleans">The boolean array to invert.</param>
    internal static void InvertBooleanArray(bool[] booleans)
    {
        for (int i = 0; i < booleans.Length; i++)
            booleans[i] = !booleans[i];
    }

    /// <summary>
    /// Generates and returns a boolean array that is random.
    /// </summary>
    /// <param name="length">The length of the array.</param>
    /// <returns>A boolean array of random values.</returns>
    internal static bool[] RandomBools(int length, float weighting = 0.5f)
    {
        bool[] array = new bool[length];
        for (int i = 0; i < array.Length; i++)
            array[i] = Rnd.Range(0, 1f) > weighting;
        return array;
    }
}
