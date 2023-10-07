using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MyDialogue
{
    public Background[] background;
    public string content;
    public List<Dictionary<string, string>> conversation;
}