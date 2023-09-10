using GameCreator.Characters;
using GameCreator.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMoveActions : MonoBehaviour
{
    public TargetCharacter npc1_ch;
    public NavigationMarker marker1;
    // Start is called before the first frame update
    void Start()
    {
        GameObject gameObject = new GameObject();
        Actions actions = gameObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;
        actions.actionsList.actions = new IAction[1];
        ActionCharacterMoveTo moveTo1 = actions.gameObject.AddComponent<ActionCharacterMoveTo>();
        moveTo1.target = npc1_ch;
        moveTo1.moveTo = ActionCharacterMoveTo.MOVE_TO.Marker;
        moveTo1.marker = marker1;
        actions.actionsList.actions[0] = moveTo1;
        actions.Execute();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
