using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GameCreator.Core;
using System.Xml;
using GameCreator.Characters;
using GameCreator.Localization;
using GameCreator.Messages;
using UnityEditor.VersionControl;
using static GameCreator.Core.ActionTransform;
using UnityEngine.UI;

public class Control : MonoBehaviour
{
    //角色按照这个顺序排列
    //Manna
    //Duke
    //Anna
    //Basil
    //Mary

    //场所 不一定要按照固定顺序排列
    //AjaWineryScene
    //BasilHouseScene
    //MaryLibraryScene
    //MineralClinicScene
    //ChurchScene
    //RoseSquareScene

    //初始位置
    //Manna, in map: AjaWineryScene
    //Duke, in map: AjaWineryScene
    //Anna, in map: BasilHouseScene
    //Basil, in map: BasilHouseScene
    //Mary, in map: BasilHouseScene

    struct TimeInfo
    {
        public string time;
        public Dictionary<string, string> moves;
        public Dictionary<string, string> reasons;
        public List<string> dialogues;
    }

    public List<NavigationMarker> markers = new List<NavigationMarker>();//存放所有路点
    public List<TargetCharacter> npcsMove = new List<TargetCharacter>(); //存放所有人物角色 运动相关
    public List<TargetGameObject> npcsDialogue = new List<TargetGameObject>(); //存放所有人物角色 对话相关
    public GameObject timeUI;
    public GameObject reasonUI;

    private Dictionary<string, TargetCharacter> npcDicsMove = new Dictionary<string, TargetCharacter>();//存放npc的map 运动相关
    private Dictionary<string, TargetGameObject> npcDicsDialogue = new Dictionary<string, TargetGameObject>();//存放npc的map 对话相关
    private Dictionary<string, NavigationMarker> npcMarkers = new Dictionary<string, NavigationMarker>();//存放npc在各个场所的路点
    private Dictionary<string, List<NavigationMarker>> pathMarkers = new Dictionary<string, List<NavigationMarker>>(); //存放场所到场所之间的路径

    private int state = 0; //一共有三种状态 0表示需要取指令 1表示对话阶段 2表示运动阶段  3表示结束状态
    private List<TimeInfo> infos;
    private int nowIndex = 0;
    private List<Actions> moveActions;
    private Actions dialogueActions;
    private bool isPaused = false;

    // Start is called before the first frame update
    void Start()
    {
        // 设置鼠标为非独占，并且显示模式
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //Manna
        //Duke
        //Anna
        //Basil
        //Mary

        //场所 不一定要按照固定顺序排列 后面是路点使用该场景的简称
        //AjaWineryScene A AO AI 表示从A出去和从A进来 下面的同理
        //BasilHouseScene B BO BI
        //MaryLibraryScene Ma MaO MaI
        //MineralClinicScene Mi MiO MiI
        //ChurchScene C CO CI
        //RoseSquareScene R 广场是开放区域 不用使用这种路径

        //左上红房子为AjaWineryScene House_2C (2)
        //右上红房子为BasilHouseScene House_7C (1)
        //左下蓝房子为MaryLibraryScene House_5C (1)
        //右下蓝房子为MineralClinicScene House_8C

        //初始化角色-房间路点 最初的30个路点存放角色-房间路点
        npcMarkers["Manna AjaWineryScene"] = markers[0];
        npcMarkers["Manna BasilHouseScene"] = markers[1];
        npcMarkers["Manna MaryLibraryScene"] = markers[2];
        npcMarkers["Manna MineralClinicScene"] = markers[3];
        npcMarkers["Manna ChurchScene"] = markers[4];
        npcMarkers["Manna RoseSquareScene"] = markers[5];

        npcMarkers["Duke AjaWineryScene"] = markers[6];
        npcMarkers["Duke BasilHouseScene"] = markers[7];
        npcMarkers["Duke MaryLibraryScene"] = markers[8];
        npcMarkers["Duke MineralClinicScene"] = markers[9];
        npcMarkers["Duke ChurchScene"] = markers[10];
        npcMarkers["Duke RoseSquareScene"] = markers[11];

        npcMarkers["Anna AjaWineryScene"] = markers[12];
        npcMarkers["Anna BasilHouseScene"] = markers[13];
        npcMarkers["Anna MaryLibraryScene"] = markers[14];
        npcMarkers["Anna MineralClinicScene"] = markers[15];
        npcMarkers["Anna ChurchScene"] = markers[16];
        npcMarkers["Anna RoseSquareScene"] = markers[17];

        npcMarkers["Basil AjaWineryScene"] = markers[18];
        npcMarkers["Basil BasilHouseScene"] = markers[19];
        npcMarkers["Basil MaryLibraryScene"] = markers[20];
        npcMarkers["Basil MineralClinicScene"] = markers[21];
        npcMarkers["Basil ChurchScene"] = markers[22];
        npcMarkers["Basil RoseSquareScene"] = markers[23];

        npcMarkers["Mary AjaWineryScene"] = markers[24];
        npcMarkers["Mary BasilHouseScene"] = markers[25];
        npcMarkers["Mary MaryLibraryScene"] = markers[26];
        npcMarkers["Mary MineralClinicScene"] = markers[27];
        npcMarkers["Mary ChurchScene"] = markers[28];
        npcMarkers["Mary RoseSquareScene"] = markers[29];

        //存放每一条路径的Marker list
        //路径相关 先只记录中间的
        //AjaWineryScene to BasilHouseScene MP0  
        //BasilHouseScene to AjaWineryScene NP0
        //AjaWineryScene to MaryLibraryScene 无
        //MaryLibraryScene to AjaWineryScene 无
        //AjaWineryScene to MineralClinicScene MP1
        //MineralClinicScene to AjaWineryScene NP1
        //AjaWineryScene to ChurchScene MP0 MP2
        //ChurchScene to AjaWineryScene NP2 NP0
        //AjaWineryScene to RoseSquareScene MP1
        //RoseSquareScene to AjaWineryScene NP1

        //BasilHouseScene to MaryLibraryScene NP0
        //MaryLibraryScene to BasilHouseScene MP0
        //BasilHouseScene to MineralClinicScene NP0 NP1
        //MineralClinicScene to BasilHouseScene MP1 MP0
        //BasilHouseScene to ChurchScene MP2
        //ChurchScene to BasilHouseScene NP2
        //BasilHouseScene to RoseSquareScene NP0 MP1
        //RoseSquareScene to BasilHouseScene NP1 MP0

        //MaryLibraryScene to MineralClinicScene MP1
        //MineralClinicScene to MaryLibraryScene NP1
        //MaryLibraryScene to ChurchScene MP0 MP2
        //ChurchScene to MaryLibraryScene NP2 NP0
        //MaryLibraryScene to RoseSquareScene 无
        //RoseSquareScene to MaryLibraryScene 无

        //MineralClinicScene to ChurchScene NP1 MP0 MP2
        //ChurchScene to MineralClinicScene NP2 NP0 MP1
        //MineralClinicScene to RoseSquareScene 无
        //RoseSquareScene to MineralClinicScene 无

        //ChurchScene to RoseSquareScene  NP2 NP0 MP1
        //RoseSquareScene to ChurchScene  NP1 MP0 MP2


        //mp0 92  mp1 93  mp2 94  np0 95  np1 96  np2 97
        pathMarkers["AjaWineryScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene BasilHouseScene"].Add(markers[92]);

        pathMarkers["AjaWineryScene MaryLibraryScene"] = new List<NavigationMarker>();

        pathMarkers["AjaWineryScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene MineralClinicScene"].Add(markers[93]);

        pathMarkers["AjaWineryScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene ChurchScene"].Add(markers[94]);

        pathMarkers["AjaWineryScene RoseSquareScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene RoseSquareScene"].Add(markers[93]);

        pathMarkers["BasilHouseScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene AjaWineryScene"].Add(markers[95]);

        pathMarkers["BasilHouseScene MaryLibraryScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene MaryLibraryScene"].Add(markers[95]);

        pathMarkers["BasilHouseScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene MineralClinicScene"].Add(markers[95]);
        pathMarkers["BasilHouseScene MineralClinicScene"].Add(markers[96]);

        pathMarkers["BasilHouseScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene ChurchScene"].Add(markers[94]);

        pathMarkers["BasilHouseScene RoseSquareScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene RoseSquareScene"].Add(markers[95]);
        pathMarkers["BasilHouseScene RoseSquareScene"].Add(markers[93]);

        pathMarkers["MaryLibraryScene AjaWineryScene"] = new List<NavigationMarker>();

        pathMarkers["MaryLibraryScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["MaryLibraryScene BasilHouseScene"].Add(markers[92]);

        pathMarkers["MaryLibraryScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["MaryLibraryScene MineralClinicScene"].Add(markers[93]);

        pathMarkers["MaryLibraryScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["MaryLibraryScene ChurchScene"].Add(markers[92]);
        pathMarkers["MaryLibraryScene ChurchScene"].Add(markers[94]);

        pathMarkers["MaryLibraryScene RoseSquareScene"] = new List<NavigationMarker>();

        pathMarkers["MineralClinicScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene AjaWineryScene"].Add(markers[96]);

        pathMarkers["MineralClinicScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene BasilHouseScene"].Add(markers[93]);
        pathMarkers["MineralClinicScene BasilHouseScene"].Add(markers[92]);

        pathMarkers["MineralClinicScene MaryLibraryScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene MaryLibraryScene"].Add(markers[96]);

        pathMarkers["MineralClinicScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene ChurchScene"].Add(markers[96]);
        pathMarkers["MineralClinicScene ChurchScene"].Add(markers[92]);
        pathMarkers["MineralClinicScene ChurchScene"].Add(markers[94]);

        pathMarkers["MineralClinicScene RoseSquareScene"] = new List<NavigationMarker>();

        pathMarkers["ChurchScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene AjaWineryScene"].Add(markers[97]);
        pathMarkers["ChurchScene AjaWineryScene"].Add(markers[95]);

        pathMarkers["ChurchScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene BasilHouseScene"].Add(markers[97]);

        pathMarkers["ChurchScene MaryLibraryScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene MaryLibraryScene"].Add(markers[97]);
        pathMarkers["ChurchScene MaryLibraryScene"].Add(markers[95]);

        pathMarkers["ChurchScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene MineralClinicScene"].Add(markers[97]);
        pathMarkers["ChurchScene MineralClinicScene"].Add(markers[95]);
        pathMarkers["ChurchScene MineralClinicScene"].Add(markers[93]);

        pathMarkers["ChurchScene RoseSquareScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene RoseSquareScene"].Add(markers[97]);
        pathMarkers["ChurchScene RoseSquareScene"].Add(markers[95]);
        pathMarkers["ChurchScene RoseSquareScene"].Add(markers[93]);

        pathMarkers["RoseSquareScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["RoseSquareScene AjaWineryScene"].Add(markers[96]);

        pathMarkers["RoseSquareScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["RoseSquareScene BasilHouseScene"].Add(markers[96]);
        pathMarkers["RoseSquareScene BasilHouseScene"].Add(markers[92]);

        pathMarkers["RoseSquareScene MaryLibraryScene"] = new List<NavigationMarker>();

        pathMarkers["RoseSquareScene MineralClinicScene"] = new List<NavigationMarker>();

        pathMarkers["RoseSquareScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["RoseSquareScene ChurchScene"].Add(markers[96]);
        pathMarkers["RoseSquareScene ChurchScene"].Add(markers[92]);
        pathMarkers["RoseSquareScene ChurchScene"].Add(markers[94]);

        //初始化角色字典
        npcDicsMove["Manna"] = npcsMove[0];
        npcDicsMove["Duke"] = npcsMove[1];
        npcDicsMove["Anna"] = npcsMove[2];
        npcDicsMove["Basil"] = npcsMove[3];
        npcDicsMove["Mary"] = npcsMove[4];

        npcDicsDialogue["Manna"] = npcsDialogue[0];
        npcDicsDialogue["Duke"] = npcsDialogue[1];
        npcDicsDialogue["Anna"] = npcsDialogue[2];
        npcDicsDialogue["Basil"] = npcsDialogue[3];
        npcDicsDialogue["Mary"] = npcsDialogue[4];

        infos = new List<TimeInfo>();
        string filePath = "data/agent.txt";
        StreamReader reader = new StreamReader(filePath);
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            //初始化每一个时间段的数据结构
            if (line.StartsWith("current timestep:"))
            {
                string[] test  = new string[1];
                test[0] = "time: ";
                string time = line.Split(test, System.StringSplitOptions.None)[1];

                TimeInfo info = new TimeInfo();
                info.time = time;
                info.moves = new Dictionary<string, string>();
                info.reasons = new Dictionary<string, string>();
                info.dialogues = new List<string>();
                infos.Add(info);
            }

            //处理运动信息
            if (line.StartsWith("take action:"))
            {
                if (line.Contains("no action!!!!") || line.Contains("talk to"))
                    continue;

                string[] test1 = new string[1];
                test1[0] = "agent name: ";
                string[] temp1 = line.Split(test1, System.StringSplitOptions.None);

                string[] test2 = new string[1];
                test2[0] = ", ";
                string[] temp2 = temp1[1].Split(test2, System.StringSplitOptions.None);
                string name = temp2[0];

                string from = temp2[1].Split(' ')[2];
                string to = temp2[1].Split(' ')[5];

                //暂时把log里面的SouthSideOfMineralTownScene改成RoseSquareScene
                if (to.Equals("SouthSideOfMineralTownScene") || to.Equals("NorthSideOfMineralTownScene"))
                    to = "RoseSquareScene";

                if (from.Equals("SouthSideOfMineralTownScene") || from.Equals("NorthSideOfMineralTownScene"))
                    from = "RoseSquareScene";

                //如果起点和终点一样 就不动
                if (from.Equals(to))
                    continue;

                infos[infos.Count - 1].moves[name] = from + " " + to;
            }

            //处理reason信息
            if (line.StartsWith("reason:"))
            {
                string[] test1 = new string[1];
                test1[0] = "reason:";
                string reason = line.Split(test1, System.StringSplitOptions.None)[1];
                string name = reason.Split(' ')[1];
                infos[infos.Count - 1].reasons[name] = reason;
            }

            //处理dialogue信息
            if (line.StartsWith("dialogue:"))
            {
                string[] test1 = new string[1];
                test1[0] = "dialogue: ";
                string content = line.Split(test1, System.StringSplitOptions.None)[1];
                infos[infos.Count - 1].dialogues.Add(content);
            }
        }

       /* for(int i = 0 ; i < infos.Count; i++)
        {
            Debug.Log(i + "\t" + infos[i].time);
            foreach (string key in infos[i].reasons.Keys)
            {
                Debug.Log("Key: " + key);
                Debug.Log("Value: " + infos[i].reasons[key]);
            }
        }*/
     }

    // Update is called once per frame
    void Update()
    {
        //取指令阶段
        if(state == 0)
        {
            //当前所有的指令都已经被取完 进入结束状态
            if(nowIndex >= infos.Count)
            {
                state = 3;
                Debug.Log("finish");
            }
            else
            {
                Text tempText = timeUI.GetComponent<Text>();
                tempText.text = infos[nowIndex].time;
                Debug.Log(infos[nowIndex].time);
                //生成对话Actions 
                dialogueActions = GetDiglogueActions();
                state = 1 ;
            }
        }

        //对话阶段
        if (state == 1)
        {
            if (dialogueActions == null)
            {
                //取运动指令 生成运动的actions
                moveActions = new List<Actions>();
                foreach (var pair in infos[nowIndex].moves)
                {
                    string name = pair.Key;
                    string path = pair.Value;
                    string start = path.Split(' ')[0];
                    string end = path.Split(' ')[1];
                    Debug.Log(name + "\tstart\t" + start + "\tend\t" + end);
                    moveActions.Add(GetMoveActions(name, start, end));
                }
                state = 2;
            }
        }

        //运动阶段
        if (state == 2)
        {
            //判断运动的action是否都已经结束 
            bool isFinish = true;
            for(int i = 0; i < moveActions.Count; i++)
            {
                if (moveActions[i] != null)
                {
                    isFinish = false;   
                    break;
                }
            }
            if(isFinish)
            {
                state = 0;
                nowIndex++;
            }
        }

        //暂停功能
        if (Input.GetButtonDown("PauseResume"))
        {
            Debug.Log("paused");
            if (isPaused)
            {
                Time.timeScale = 1f;
                isPaused = false;
            }
            else
            {
                Time.timeScale = 0f;
                isPaused = true;
            }
        }
    }

    Actions GetMoveActions(string name, string start, string end)
    {
        GameObject tempObject= new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        //获得路径的Markers 不算起点和终点 每个人的起点和终点
        List<NavigationMarker> markers = GetMarkers(name, start, end);
        actions.actionsList.actions = new IAction[markers.Count];
        for(int i = 0; i < markers.Count; i++)
        {
            ActionCharacterMoveTo moveTo = actions.gameObject.AddComponent<ActionCharacterMoveTo>();
            moveTo.target = npcDicsMove[name];
            moveTo.moveTo = ActionCharacterMoveTo.MOVE_TO.Marker;
            moveTo.marker = markers[i];
            actions.actionsList.actions[i] = moveTo;
        }
            
        actions.Execute();
        return actions;
    }

    //不返回初始路点
    List<NavigationMarker> GetMarkers(string name, string start, string end)
    {
        List<NavigationMarker> resultMarkers = new List<NavigationMarker>();

        if (start.Equals("AjaWineryScene"))
        {
            resultMarkers.Add(markers[30]);
            resultMarkers.Add(markers[31]);
            resultMarkers.Add(markers[32]);
            resultMarkers.Add(markers[33]);
            resultMarkers.Add(markers[34]);
            resultMarkers.Add(markers[35]);
            resultMarkers.Add(markers[36]);
            resultMarkers.Add(markers[37]);
            resultMarkers.Add(markers[38]);
            resultMarkers.Add(markers[39]);
        }
        if (start.Equals("BasilHouseScene"))
        {
            resultMarkers.Add(markers[50]);
            resultMarkers.Add(markers[51]);
            resultMarkers.Add(markers[52]);
            resultMarkers.Add(markers[53]);
            resultMarkers.Add(markers[54]);
        }
        if (start.Equals("MaryLibraryScene"))
        {
            resultMarkers.Add(markers[60]);
            resultMarkers.Add(markers[61]);
            resultMarkers.Add(markers[62]);
            resultMarkers.Add(markers[63]);
            resultMarkers.Add(markers[64]);
            resultMarkers.Add(markers[65]);
            resultMarkers.Add(markers[66]);
        }
        if (start.Equals("MineralClinicScene"))
        {
            resultMarkers.Add(markers[74]);
            resultMarkers.Add(markers[75]);
            resultMarkers.Add(markers[76]);
            resultMarkers.Add(markers[77]);
            resultMarkers.Add(markers[78]);
            resultMarkers.Add(markers[79]);
        }
        if (start.Equals("ChurchScene"))
        {
            resultMarkers.Add(markers[86]);
            resultMarkers.Add(markers[87]);
            resultMarkers.Add(markers[88]);
        }
        List<NavigationMarker> path = pathMarkers[start + " " + end];
        for(int i = 0; i < path.Count; i++)
            resultMarkers.Add(path[i]);

        if (end.Equals("AjaWineryScene"))
        {
            resultMarkers.Add(markers[49]);
            resultMarkers.Add(markers[48]);
            resultMarkers.Add(markers[47]);
            resultMarkers.Add(markers[46]);
            resultMarkers.Add(markers[45]);
            resultMarkers.Add(markers[44]);
            resultMarkers.Add(markers[43]);
            resultMarkers.Add(markers[42]);
            resultMarkers.Add(markers[41]);
            resultMarkers.Add(markers[40]);
        }
        if (end.Equals("BasilHouseScene"))
        {   
            resultMarkers.Add(markers[59]);
            resultMarkers.Add(markers[58]);
            resultMarkers.Add(markers[57]);
            resultMarkers.Add(markers[56]);
            resultMarkers.Add(markers[55]);
        }
        if (end.Equals("MaryLibraryScene"))
        {   
            resultMarkers.Add(markers[73]);
            resultMarkers.Add(markers[72]);
            resultMarkers.Add(markers[71]);
            resultMarkers.Add(markers[70]);
            resultMarkers.Add(markers[69]);
            resultMarkers.Add(markers[68]);
            resultMarkers.Add(markers[67]);
        }
        if (end.Equals("MineralClinicScene"))
        {
            resultMarkers.Add(markers[85]);
            resultMarkers.Add(markers[84]);
            resultMarkers.Add(markers[83]);
            resultMarkers.Add(markers[82]);
            resultMarkers.Add(markers[81]);
            resultMarkers.Add(markers[80]);      
        }
        if (end.Equals("ChurchScene"))
        {
            resultMarkers.Add(markers[91]);
            resultMarkers.Add(markers[90]);
            resultMarkers.Add(markers[89]);
        }
        resultMarkers.Add(npcMarkers[name + " " + end]);
        return resultMarkers;
    }

    Actions GetDiglogueActions()
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        actions.actionsList.actions = new IAction[infos[nowIndex].dialogues.Count];
        for (int i = 0; i < infos[nowIndex].dialogues.Count; i++)
        {
            string name = infos[nowIndex].dialogues[i].Split(' ')[0];

            string[] test = new string[1];
            test[0] = "said \"";
            string content = infos[nowIndex].dialogues[i].Split(test, System.StringSplitOptions.None)[1].Split('\"')[0];
            ActionFloatingMessage message = actions.gameObject.AddComponent<ActionFloatingMessage>();
            message.message = new LocString(content);
            message.target = npcDicsDialogue[name];
            actions.actionsList.actions[i] = message;
        }

        actions.Execute();
        return actions;
    }
}
