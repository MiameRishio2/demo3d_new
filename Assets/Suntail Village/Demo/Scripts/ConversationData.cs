using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConversationData
{
    public List<Dictionary<string, string>> conversation;
    public string content;
    public bool isEnd;
}