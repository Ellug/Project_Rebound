using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Calendar/Holiday Database")]
public class HolidayDatabase : ScriptableObject
{
    public List<int> dates = new List<int>();
}