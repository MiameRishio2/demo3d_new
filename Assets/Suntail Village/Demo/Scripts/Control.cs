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
using static GameCreator.Core.ActionTransform;
using UnityEngine.UI;
using GameCreator.Variables;
using UnityEngine.Animations;
using JetBrains.Annotations;
using GameCreator.Dialogue;
using System.Net;
using UnityEditor.PackageManager.Requests;
using UnityEngine.Networking;
using System.ComponentModel.Design;

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
        public Dictionary<string, string> locations;
        public List<string> dialogues;

        public List<string> choices;
        public List<string> choice_dialogues;
    }

    public List<NavigationMarker> locationMarkers = new List<NavigationMarker>();//存放所有场所路点
    public List<NavigationMarker> AjaWinerySceneOutMarkers = new List<NavigationMarker>(); //存放从AjaWineryScene出来的路点
    public List<NavigationMarker> AjaWinerySceneInMarkers = new List<NavigationMarker>(); //存放进入AjaWineryScene的路点
    public List<NavigationMarker> BasilHouseSceneOutMarkers = new List<NavigationMarker>(); //存放从BasilHouseScene出来的路点
    public List<NavigationMarker> BasilHouseSceneInMarkers = new List<NavigationMarker>(); //存放进入BasilHouseScene的路点
    public List<NavigationMarker> MaryLibrarySceneOutMarkers = new List<NavigationMarker>(); //存放从MaryLibraryScene出来的路点
    public List<NavigationMarker> MaryLibrarySceneInMarkers = new List<NavigationMarker>(); //存放进入MaryLibraryScene的路点
    public List<NavigationMarker> MineralClinicSceneOutMarkers = new List<NavigationMarker>(); //存放从MineralClinicScene出来的路点
    public List<NavigationMarker> MineralClinicSceneInMarkers = new List<NavigationMarker>(); //存放进入MineralClinicScene的路点
    public List<NavigationMarker> ChurchSceneOutMarkers = new List<NavigationMarker>(); //存放从ChurchScene出来的路点
    public List<NavigationMarker> ChurchSceneInMarkers = new List<NavigationMarker>(); //存放进入ChurchScene的路点
    public List<NavigationMarker> MiddlePMarkers = new List<NavigationMarker>(); //中间正向路点
    public List<NavigationMarker> MiddleNMarkers = new List<NavigationMarker>(); //中间反向路点

    public List<TargetCharacter> npcsMove = new List<TargetCharacter>(); //存放所有人物角色 运动相关
    public List<TargetGameObject> npcsDialogue = new List<TargetGameObject>(); //存放所有人物角色 对话相关
    public List<GameObject> npcsGameObject = new List<GameObject>(); //存放所有人物object 用于计算角色位置
    public GameObject timeUI; //显示时间的ui
    public GameObject reasonUI; //显示reason的ui

    //角色物体
    public GameObject Manna;
    public GameObject Duke;
    public GameObject Anna;
    public GameObject Basil;
    public GameObject Mary;

    //向下的相机
    public GameObject topViewer;

    //人物的高空Trigger
    public GameObject MannaTriggerH;
    public GameObject DukeTriggerH;
    public GameObject AnnaTriggerH;
    public GameObject BasilTriggerH;
    public GameObject MaryTriggerH;

    public List<GameObject> NPCNames = new List<GameObject>();
    public GameObject selectDialogue;

    //三选一的2个人物的名字
    public string talker1 = "nobody";
    public string talker2 = "nobody";

    //正对人物的相机
    public GameObject selectCamera;
    public Actions switchTop;

    private Dictionary<string, TargetCharacter> npcDicsMove = new Dictionary<string, TargetCharacter>();//存放npc的map 运动相关
    private Dictionary<string, TargetGameObject> npcDicsDialogue = new Dictionary<string, TargetGameObject>();//存放npc的map 对话相关
    private Dictionary<string, NavigationMarker> npcMarkers = new Dictionary<string, NavigationMarker>();//存放npc在各个场所的路点
    private Dictionary<string, List<NavigationMarker>> pathMarkers = new Dictionary<string, List<NavigationMarker>>(); //存放场所到场所之间的路径

    public int state = 0; //一共有4种状态 0表示需要取指令 1表示选择阶段  2表示对话阶段 3表示运动阶段  4表示结束状态
    public int before_choice_state = 0; //在状态state 0下面的子状态 0表示未按下k键的状态 1表示按下k键 开始自动选人 然后发送post请求状态 同步发送 2表示等待返回 3表示请求返回 开始生成新的actions 执行切换视角等操作
    public int choice_state = 0; //在state 1下面的子状态 0表示默认状态  显示三选一和对话框 1表示等待返回结果状态 2
    private List<TimeInfo> infos;
    private int nowIndex = 0;
    private List<Actions> moveActions;
    private Actions dialogueActions;
    private bool isPaused = false;
    private Actions selectActions;
    private float nowSpeed = 1f;
    private bool isSelectInfoReturned = false;

    public int version = 1;
    private String postResult = ""; //post的返回结果
    private String MannaD = "{\"name\":\"Manna\",\"description\":\"When she's not at home, she usually can be found at Rose Square, chatting with Anna and Sasha. Manna is a town gossip, and loves to chat. Duke and Manna frequently argue over Duke's drinking and Manna's gossiping. Manna acts like a mother to Cliff if he takes the job at the Winery, and he becomes very important to her. Manna and Duke will have a wedding ceremony at church.\"}";
    private String DukeD = "{\"name\":\"Duke\",\"description\":\"Duke is the owner of the Aja Winery in Mineral Town. He is proud of his business and willing to give advice. Manna and Duke will have a wedding ceremony at church.\"}";
    private String AnnaD = "{\"name\":\"Anna\",\"description\":\"Anna is Mary's mother and Basil's wife. She lives in the house next to the Library with her husband, although she spends most of her time in Rose Square. Anna is strangely sophisticated for a stay at home Mom in a small village, but she doesn't mind living in Mineral Town - especially when she's got friends like Sasha and Manna to gossip with. She is great at making all kinds of desserts. \"}";
    private String BasilD = "{\"name\":\"Basil\",\"description\":\"Basil works as a world famous botanist and loves the outdoors. He lives next to the Library with his wife Anna and their daughter Mary. Most of the books in the Library were written by Basil himself! If he's not at his home, he will be near Mother's Hill studying the local flora.Basil is very friendly, and may give you a recipe for Fruit Latte.\"}";
    private String MaryD = "{\"name\":\"Mary\",\"description\":\"She is the only child of Anna and Basil, and the librarian at the local library. She is a shy girl, and admires her father's work with botany. She hopes to one day become a writer, and is currently working on her own novel. She feels rather sad that few people in town take advantage of the library. She has only a few friends in town, including Gray, Saibara, and Carter. She is a participant in the Cooking Festival and plays the organ during the Music Festival.\"}";

    public Actions switchManna;
    public Actions switchDuke;
    public Actions switchAnna;
    public Actions switchBasil;
    public Actions switchMary;

    //专门用于调试相关的信息
    public string defaultSelectItem = "1";

    // Start is called before the first frame update
    void Start()
    {
        //version 从配置文件中获取
        string filePathC = "data/config.txt";
        StreamReader readerC = new StreamReader(filePathC);
        string lineC;

        if ((lineC = readerC.ReadLine()) != null)
        {
            version = int.Parse(lineC);
        }

            //测试代码创建三选一对话过程
            /*  GameObject tempObject = new GameObject();
              dialogueActions = tempObject.AddComponent<Actions>();
              dialogueActions.destroyAfterFinishing = true;

              dialogueActions.actionsList.actions = new IAction[1];
              ActionDialogue select = dialogueActions.gameObject.AddComponent<ActionDialogue>();
              select.dialogue = selectDialogue.GetComponent<Dialogue>();
              select.dialogue.itemInstances[2].content = new LocString("111");
              select.dialogue.itemInstances[3].content = new LocString("222");
              select.dialogue.itemInstances[4].content = new LocString("333");
              dialogueActions.actionsList.actions[0] = select;
              dialogueActions.Execute();*/

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
        npcMarkers["Manna AjaWineryScene"] = locationMarkers[0];
        npcMarkers["Manna BasilHouseScene"] = locationMarkers[1];
        npcMarkers["Manna MaryLibraryScene"] = locationMarkers[2];
        npcMarkers["Manna MineralClinicScene"] = locationMarkers[3];
        npcMarkers["Manna ChurchScene"] = locationMarkers[4];
        npcMarkers["Manna RoseSquareScene"] = locationMarkers[5];

        npcMarkers["Duke AjaWineryScene"] = locationMarkers[6];
        npcMarkers["Duke BasilHouseScene"] = locationMarkers[7];
        npcMarkers["Duke MaryLibraryScene"] = locationMarkers[8];
        npcMarkers["Duke MineralClinicScene"] = locationMarkers[9];
        npcMarkers["Duke ChurchScene"] = locationMarkers[10];
        npcMarkers["Duke RoseSquareScene"] = locationMarkers[11];

        npcMarkers["Anna AjaWineryScene"] = locationMarkers[12];
        npcMarkers["Anna BasilHouseScene"] = locationMarkers[13];
        npcMarkers["Anna MaryLibraryScene"] = locationMarkers[14];
        npcMarkers["Anna MineralClinicScene"] = locationMarkers[15];
        npcMarkers["Anna ChurchScene"] = locationMarkers[16];
        npcMarkers["Anna RoseSquareScene"] = locationMarkers[17];

        npcMarkers["Basil AjaWineryScene"] = locationMarkers[18];
        npcMarkers["Basil BasilHouseScene"] = locationMarkers[19];
        npcMarkers["Basil MaryLibraryScene"] = locationMarkers[20];
        npcMarkers["Basil MineralClinicScene"] = locationMarkers[21];
        npcMarkers["Basil ChurchScene"] = locationMarkers[22];
        npcMarkers["Basil RoseSquareScene"] = locationMarkers[23];

        npcMarkers["Mary AjaWineryScene"] = locationMarkers[24];
        npcMarkers["Mary BasilHouseScene"] = locationMarkers[25];
        npcMarkers["Mary MaryLibraryScene"] = locationMarkers[26];
        npcMarkers["Mary MineralClinicScene"] = locationMarkers[27];
        npcMarkers["Mary ChurchScene"] = locationMarkers[28];
        npcMarkers["Mary RoseSquareScene"] = locationMarkers[29];

        //存放每一条路径的Marker list
        //路径相关 先只记录中间的
        pathMarkers["AjaWineryScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene BasilHouseScene"].Add(MiddleNMarkers[1]);

        pathMarkers["AjaWineryScene MaryLibraryScene"] = new List<NavigationMarker>();

        pathMarkers["AjaWineryScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene MineralClinicScene"].Add(MiddlePMarkers[2]);
        pathMarkers["AjaWineryScene MineralClinicScene"].Add(MiddlePMarkers[3]);

        pathMarkers["AjaWineryScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene ChurchScene"].Add(MiddleNMarkers[1]);
        pathMarkers["AjaWineryScene ChurchScene"].Add(MiddlePMarkers[0]);

        pathMarkers["AjaWineryScene RoseSquareScene"] = new List<NavigationMarker>();
        pathMarkers["AjaWineryScene RoseSquareScene"].Add(MiddlePMarkers[2]);

        pathMarkers["BasilHouseScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene AjaWineryScene"].Add(MiddlePMarkers[1]);

        pathMarkers["BasilHouseScene MaryLibraryScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene MaryLibraryScene"].Add(MiddlePMarkers[1]);
        pathMarkers["BasilHouseScene MaryLibraryScene"].Add(MiddlePMarkers[2]);

        pathMarkers["BasilHouseScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene MineralClinicScene"].Add(MiddlePMarkers[1]);
        pathMarkers["BasilHouseScene MineralClinicScene"].Add(MiddlePMarkers[2]);
        pathMarkers["BasilHouseScene MineralClinicScene"].Add(MiddlePMarkers[3]);

        pathMarkers["BasilHouseScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene ChurchScene"].Add(MiddlePMarkers[1]);
        pathMarkers["BasilHouseScene ChurchScene"].Add(MiddlePMarkers[0]);

        pathMarkers["BasilHouseScene RoseSquareScene"] = new List<NavigationMarker>();
        pathMarkers["BasilHouseScene RoseSquareScene"].Add(MiddlePMarkers[1]);
        pathMarkers["BasilHouseScene RoseSquareScene"].Add(MiddlePMarkers[2]);

        pathMarkers["MaryLibraryScene AjaWineryScene"] = new List<NavigationMarker>();

        pathMarkers["MaryLibraryScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["MaryLibraryScene BasilHouseScene"].Add(MiddleNMarkers[2]);
        pathMarkers["MaryLibraryScene BasilHouseScene"].Add(MiddleNMarkers[1]);

        pathMarkers["MaryLibraryScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["MaryLibraryScene MineralClinicScene"].Add(MiddlePMarkers[3]);

        pathMarkers["MaryLibraryScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["MaryLibraryScene ChurchScene"].Add(MiddleNMarkers[2]);
        pathMarkers["MaryLibraryScene ChurchScene"].Add(MiddleNMarkers[1]);
        pathMarkers["MaryLibraryScene ChurchScene"].Add(MiddlePMarkers[0]);

        pathMarkers["MaryLibraryScene RoseSquareScene"] = new List<NavigationMarker>();

        pathMarkers["MineralClinicScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene AjaWineryScene"].Add(MiddleNMarkers[3]);
        pathMarkers["MineralClinicScene AjaWineryScene"].Add(MiddleNMarkers[2]);

        pathMarkers["MineralClinicScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene BasilHouseScene"].Add(MiddleNMarkers[3]);
        pathMarkers["MineralClinicScene BasilHouseScene"].Add(MiddleNMarkers[2]);
        pathMarkers["MineralClinicScene BasilHouseScene"].Add(MiddleNMarkers[1]);

        pathMarkers["MineralClinicScene MaryLibraryScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene MaryLibraryScene"].Add(MiddleNMarkers[3]);

        pathMarkers["MineralClinicScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["MineralClinicScene ChurchScene"].Add(MiddleNMarkers[3]);
        pathMarkers["MineralClinicScene ChurchScene"].Add(MiddleNMarkers[2]);
        pathMarkers["MineralClinicScene ChurchScene"].Add(MiddleNMarkers[1]);
        pathMarkers["MineralClinicScene ChurchScene"].Add(MiddlePMarkers[0]);

        pathMarkers["MineralClinicScene RoseSquareScene"] = new List<NavigationMarker>();

        pathMarkers["ChurchScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene AjaWineryScene"].Add(MiddleNMarkers[0]);
        pathMarkers["ChurchScene AjaWineryScene"].Add(MiddlePMarkers[1]);

        pathMarkers["ChurchScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene BasilHouseScene"].Add(MiddleNMarkers[0]);

        pathMarkers["ChurchScene MaryLibraryScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene MaryLibraryScene"].Add(MiddleNMarkers[0]);
        pathMarkers["ChurchScene MaryLibraryScene"].Add(MiddlePMarkers[1]);
        pathMarkers["ChurchScene MaryLibraryScene"].Add(MiddlePMarkers[2]);

        pathMarkers["ChurchScene MineralClinicScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene MineralClinicScene"].Add(MiddleNMarkers[0]);
        pathMarkers["ChurchScene MineralClinicScene"].Add(MiddlePMarkers[1]);
        pathMarkers["ChurchScene MineralClinicScene"].Add(MiddlePMarkers[2]);
        pathMarkers["ChurchScene MineralClinicScene"].Add(MiddlePMarkers[3]);

        pathMarkers["ChurchScene RoseSquareScene"] = new List<NavigationMarker>();
        pathMarkers["ChurchScene RoseSquareScene"].Add(MiddleNMarkers[0]);
        pathMarkers["ChurchScene RoseSquareScene"].Add(MiddlePMarkers[1]);
        pathMarkers["ChurchScene RoseSquareScene"].Add(MiddlePMarkers[2]);

        pathMarkers["RoseSquareScene AjaWineryScene"] = new List<NavigationMarker>();
        pathMarkers["RoseSquareScene AjaWineryScene"].Add(MiddleNMarkers[2]);

        pathMarkers["RoseSquareScene BasilHouseScene"] = new List<NavigationMarker>();
        pathMarkers["RoseSquareScene BasilHouseScene"].Add(MiddleNMarkers[2]);
        pathMarkers["RoseSquareScene BasilHouseScene"].Add(MiddleNMarkers[1]);

        pathMarkers["RoseSquareScene MaryLibraryScene"] = new List<NavigationMarker>();

        pathMarkers["RoseSquareScene MineralClinicScene"] = new List<NavigationMarker>();

        pathMarkers["RoseSquareScene ChurchScene"] = new List<NavigationMarker>();
        pathMarkers["RoseSquareScene ChurchScene"].Add(MiddleNMarkers[2]);
        pathMarkers["RoseSquareScene ChurchScene"].Add(MiddleNMarkers[1]);
        pathMarkers["RoseSquareScene ChurchScene"].Add(MiddlePMarkers[0]);

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
        string filePath = "";
        if(version == 1)
            filePath = "data/agent1.txt";
        if (version == 2)
            filePath = "data/agent2.txt";

        StreamReader reader = new StreamReader(filePath);
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            //初始化每一个时间段的数据结构
            if (line.StartsWith("current timestep:"))
            {
                string[] test = new string[1];
                test[0] = "time: ";
                string time = line.Split(test, System.StringSplitOptions.None)[1];

                TimeInfo info = new TimeInfo();
                info.time = time;
                info.moves = new Dictionary<string, string>();
                info.reasons = new Dictionary<string, string>();
                info.dialogues = new List<string>();
                info.choices = new List<string>();
                info.choice_dialogues = new List<string>();
                info.locations = new Dictionary<string, string>();
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

            //处理选项信息
            if (line.StartsWith("choice "))
            {
                string[] test1 = new string[1];
                test1[0] = "]: ";
                string content = line.Split(test1, System.StringSplitOptions.None)[1];
                infos[infos.Count - 1].choices.Add(content);
                // Debug.Log("choice\t" + content);
            }

            //处理选择到的选项
            if (line.StartsWith("debug: choice "))
            {
                string content = line.Split('[')[1].Split(']')[0];
                infos[infos.Count - 1].choices.Add(content);
                //  Debug.Log("number\t" + content);
            }

            //处理选项的结果
            if (line.StartsWith("choice_dialogue:"))
            {
                string[] test1 = new string[1];
                test1[0] = "choice_dialogue: ";
                string content = line.Split(test1, System.StringSplitOptions.None)[1];
                infos[infos.Count - 1].choice_dialogues.Add(content);
                // Debug.Log("content\t" + content);
            }

            if(line.StartsWith("current agent state"))
            {
                string[] test1 = new string[1];
                test1[0] = "name: ";
                string temp1 = line.Split(test1, System.StringSplitOptions.None)[1];
                string name = temp1.Split(',')[0];
                test1[0] = "in map: ";
                string loc = temp1.Split(test1, System.StringSplitOptions.None)[1];
                if (loc.Equals("SouthSideOfMineralTownScene") || loc.Equals("NorthSideOfMineralTownScene"))
                    loc = "RoseSquareScene";

                infos[infos.Count - 1].locations[name] = loc;
             //   Debug.Log("name\t" + name + "\t" + loc);
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
        object isTopV = VariablesManager.GetGlobal("isTop");
        bool isTop = (bool)isTopV;
        if (isTop)
        {
            //俯视角下名字可见
            NPCNames[0].SetActive(true);
            NPCNames[1].SetActive(true);
            NPCNames[2].SetActive(true);
            NPCNames[3].SetActive(true);
            NPCNames[4].SetActive(true);

            //俯视角下对话框大小变大
            float scale = 0.08f;
            Transform childTransform = Manna.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(scale, scale, scale);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = false;
                childTransform.rotation = new Quaternion(-0.6f, 0f, 0f, -0.8f);
                //调节对话框位置
                childTransform.position = Manna.transform.position + new Vector3(0, 0, 5);
            }

            childTransform = Duke.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(scale, scale, scale);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = false;
                childTransform.rotation = new Quaternion(-0.6f, 0f, 0f, -0.8f);
                //调节对话框位置
                childTransform.position = Duke.transform.position + new Vector3(0, 0, 5);
            }

            childTransform = Anna.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(scale, scale, scale);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = false;
                childTransform.rotation = new Quaternion(-0.6f, 0f, 0f, -0.8f);
                //调节对话框位置
                childTransform.position = Anna.transform.position + new Vector3(0, 0, 5);
            }

            childTransform = Basil.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(scale, scale, scale);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = false;
                childTransform.rotation = new Quaternion(-0.6f, 0f, 0f, -0.8f);
                //调节对话框位置
                childTransform.position = Basil.transform.position + new Vector3(0, 0, 5);
            }

            childTransform = Mary.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(scale, scale, scale);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = false;
                childTransform.rotation = new Quaternion(-0.6f, 0f, 0f, -0.8f);
                //调节对话框位置
                childTransform.position = Mary.transform.position + new Vector3(0, 0, 5);
            }

            /* GameObject[] objectsOfType = FindObjectsOfType<GameObject>();
             foreach (GameObject obj in objectsOfType)
             {
                 if (obj.name.Contains("FloatingMessage"))
                 {
                     obj.transform.localScale = new Vector3(scale, scale, scale);
                     //调整俯视角下面对话框的朝向
                     LookAtConstraint constraint = obj.GetComponent<LookAtConstraint>();
                     constraint.constraintActive = false;
                     // Debug.Log(obj.transform.rotation);
                     obj.transform.rotation = new Quaternion(-0.6f, 0f, 0f, -0.8f);
                     string parentName = obj.transform.parent.name;

                     //调节对话框位置
                     if (parentName.Equals("Manna"))
                         obj.transform.position = Manna.transform.position + new Vector3(0, 0, 5);
                     if (parentName.Equals("Duke"))
                         obj.transform.position = Duke.transform.position + new Vector3(0, 0, 5);
                     if (parentName.Equals("Anna"))
                         obj.transform.position = Anna.transform.position + new Vector3(0, 0, 5);
                     if (parentName.Equals("Basil"))
                         obj.transform.position = Basil.transform.position + new Vector3(0, 0, 5);
                     if (parentName.Equals("Mary"))
                         obj.transform.position = Mary.transform.position + new Vector3(0, 0, 5);
                 }
             }*/

            //俯视角下可以按 wasd操作相机位置
            Vector3 currentPosition = topViewer.transform.position;
            float moveSpeed = 10f;
            // 计算新的位置
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 moveDirection = new Vector3(horizontalInput, 0, verticalInput);
            Vector3 newPosition = currentPosition + moveDirection * moveSpeed * Time.deltaTime;

            // 设置新位置
            topViewer.transform.position = newPosition;
        }
        else
        {
            //非俯视角下名字不可见
            NPCNames[0].SetActive(false);
            NPCNames[1].SetActive(false);
            NPCNames[2].SetActive(false);
            NPCNames[3].SetActive(false);
            NPCNames[4].SetActive(false);

            //非俯视角下对话框大小正常
            Transform childTransform = Manna.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = true;
                //调节对话框位置
                childTransform.position = Manna.transform.position + new Vector3(0, 2f, 0);
            }

            childTransform = Duke.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = true;
                //调节对话框位置
                childTransform.position = Duke.transform.position + new Vector3(0, 2f, 0);
            }

            childTransform = Anna.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = true;
                //调节对话框位置
                childTransform.position = Anna.transform.position + new Vector3(0, 2f, 0);
            }

            childTransform = Basil.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = true;
                //调节对话框位置
                childTransform.position = Basil.transform.position + new Vector3(0, 2f, 0);
            }

            childTransform = Mary.transform.Find("FloatingMessage(Clone)");
            if (childTransform != null)
            {
                childTransform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                //调整俯视角下面对话框的朝向
                LookAtConstraint constraint = childTransform.GetComponent<LookAtConstraint>();
                constraint.constraintActive = true;
                //调节对话框位置
                childTransform.position = Mary.transform.position + new Vector3(0, 2f, 0);
            }

            /*  GameObject[] objectsOfType = FindObjectsOfType<GameObject>();
              foreach (GameObject obj in objectsOfType)
              {
                  if (obj.name.Contains("FloatingMessage"))
                  {
                      obj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                      LookAtConstraint constraint = obj.GetComponent<LookAtConstraint>();
                      constraint.constraintActive = true;

                      string parentName = obj.transform.parent.name;
                      if (parentName.Equals("Manna"))
                          obj.transform.position = Manna.transform.position + new Vector3(0, 2f, 0);
                      if (parentName.Equals("Duke"))
                          obj.transform.position = Duke.transform.position + new Vector3(0, 2f, 0);
                      if (parentName.Equals("Anna"))
                          obj.transform.position = Anna.transform.position + new Vector3(0, 2f, 0);
                      if (parentName.Equals("Basil"))
                          obj.transform.position = Basil.transform.position + new Vector3(0, 2f, 0);
                      if (parentName.Equals("Mary"))
                          obj.transform.position = Mary.transform.position + new Vector3(0, 2f, 0);
                  }
              }*/
        }

        //处理高空trigger
        MannaTriggerH.transform.position = getTriggerHPosition(Manna, 20);
        DukeTriggerH.transform.position = getTriggerHPosition(Duke, 20);
        AnnaTriggerH.transform.position = getTriggerHPosition(Anna, 20);
        BasilTriggerH.transform.position = getTriggerHPosition(Basil, 20);
        MaryTriggerH.transform.position = getTriggerHPosition(Mary, 20);

        //俯视角下显示名字
        float offset_x = 130f;
        Vector3 targetPosition = getTriggerHPosition(Manna, 5);
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[0].transform.position = screenPosition + new Vector3(offset_x, 0);
     /*   if(talker1.Equals("Manna"))
            NPCNames[0].GetComponent<Text>().color = Color.red;
        else
            NPCNames[0].GetComponent<Text>().color = Color.black;*/

        targetPosition = getTriggerHPosition(Duke, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[1].transform.position = screenPosition + new Vector3(offset_x, 0);
      /*  if (talker1.Equals("Duke"))
            NPCNames[1].GetComponent<Text>().color = Color.red;
        else
            NPCNames[1].GetComponent<Text>().color = Color.black;*/

        targetPosition = getTriggerHPosition(Anna, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[2].transform.position = screenPosition + new Vector3(offset_x, 0);
       /* if (talker1.Equals("Anna"))
            NPCNames[2].GetComponent<Text>().color = Color.red;
        else
            NPCNames[2].GetComponent<Text>().color = Color.black;*/

        targetPosition = getTriggerHPosition(Basil, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[3].transform.position = screenPosition + new Vector3(offset_x, 0);
       /* if (talker1.Equals("Basil"))
            NPCNames[3].GetComponent<Text>().color = Color.red;
        else
            NPCNames[3].GetComponent<Text>().color = Color.black;*/

        targetPosition = getTriggerHPosition(Mary, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[4].transform.position = screenPosition + new Vector3(offset_x, 0);
       /* if (talker1.Equals("Mary"))
            NPCNames[4].GetComponent<Text>().color = Color.red;
        else
            NPCNames[4].GetComponent<Text>().color = Color.black;*/

        //取指令阶段
        if (state == 0)
        {
            //当前所有的指令都已经被取完 进入结束状态
            if (nowIndex >= infos.Count)
            {
                state = 4;
                Debug.Log("finish");
            }
            else
            {
                Text tempText = timeUI.GetComponent<Text>();
                tempText.text = infos[nowIndex].time + "\t" + nowSpeed.ToString() + "+";

                if(version == 1 || infos[nowIndex].choices.Count == 0)
                {
                    selectActions = null;
                    state = 1;
                }
                else 
                {
                    //等待按k键 
                    if (before_choice_state == 0)
                    {
                        if (Input.GetKeyDown(KeyCode.K))
                            before_choice_state = 1;
                    }

                    //按下k键之后 开始自动选择人物 然后发包 
                    //人物优先级 Mary Manna Anna Duke Basil 交谈的双方必须在同一场景 如果没有人在同一场景 就跳过这一阶段
                    if(before_choice_state == 1)
                    {
                        bool result = false;
                        result = getTalkers("Mary");
                        if(!result)
                            result = getTalkers("Manna");
                        if (!result)
                            result = getTalkers("Anna");
                        if (!result)
                            result = getTalkers("Duke");
                        if (!result)
                            result = getTalkers("Basil");

                        if (result)
                        {
                               //构造发送的json
                            String talker1D = "";
                            String talker2D = "";
                            if (talker1.Equals("Manna"))
                                talker1D = MannaD;
                            if (talker1.Equals("Duke"))
                                talker1D = DukeD;
                            if (talker1.Equals("Anna"))
                                talker1D = AnnaD;
                            if (talker1.Equals("Basil"))
                                talker1D = BasilD;
                            if (talker1.Equals("Mary"))
                                talker1D = MaryD;
                            if (talker2.Equals("Manna"))
                                talker2D = MannaD;
                            if (talker2.Equals("Duke"))
                                talker2D = DukeD;
                            if (talker2.Equals("Anna"))
                                talker2D = AnnaD;
                            if (talker2.Equals("Basil"))
                                talker2D = BasilD;
                            if (talker2.Equals("Mary"))
                                talker2D = MaryD;

                            String postData = "{\"background\":[" + talker1D + "," + talker2D + "],\"content\":\"" + talker1 + " is at " + infos[nowIndex].locations[talker1] + " now. "
                                + talker2 + " is at " + infos[nowIndex].locations[talker1] + " now. Current event is wedding.\"}";
                            String url = "http://127.0.0.1:18981/choice";
                            before_choice_state = 2;
                            //发送json
                            SendPostRequest(url, postData);
                        }
                        else
                        {
                            selectActions = null;
                            before_choice_state = 0;
                            state = 1;
                        }
                    }

                    //处理返回结果 切换相机 生成actions
                    if(before_choice_state == 3)
                    {
                        selectActions = GetSelectActions1();
                        if (talker2 == "Manna")
                        {  
                            Transform tempT = Manna.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                            switchManna.Execute();
                        }
                        if (talker2 == "Duke")
                        {
                            Transform tempT = Duke.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                            switchDuke.Execute();
                        }
                        if (talker2 == "Anna")
                        {
                            Transform tempT = Anna.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                            switchAnna.Execute();
                        }
                        if (talker2 == "Basil")
                        {
                            Transform tempT = Basil.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                            switchBasil.Execute();
                        }
                        if (talker2 == "Mary")
                        {
                            Transform tempT = Mary.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                            switchMary.Execute();
                        }

                        selectActions.Execute();
                        state = 1;
                    }
                }
            }
        }

        //选择阶段
        if (state == 1)
        {
            //当前所有的指令都已经被取完 进入结束状态
            if (selectActions == null)
            {
                talker1 = "nobody";
                talker2 = "nobody";
                VariablesManager.SetGlobal("isMannaSelected", false);
                VariablesManager.SetGlobal("isDukeSelected", false);
                VariablesManager.SetGlobal("isAnnaSelected", false);
                VariablesManager.SetGlobal("isBasilSelected", false);
                VariablesManager.SetGlobal("isAnnaSelected", false);
                VariablesManager.SetGlobal("isSelect", false);

                //三选一结束 切回顶视角
                switchTop.Execute();
                //生成对话Actions 
                dialogueActions = GetDiglogueActions();
                state = 2;
            }
        }

        //对话阶段
        if (state == 2)
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
                state = 3;
            }
        }

        //运动阶段
        if (state == 3)
        {
            //判断运动的action是否都已经结束 
            bool isFinish = true;
            for (int i = 0; i < moveActions.Count; i++)
            {
                if (moveActions[i] != null)
                {
                    isFinish = false;
                    break;
                }
            }
            if (isFinish)
            {
                state = 0;
                nowIndex++;
            }
        }

        //游戏加速功能
        if (Input.GetButtonDown("addSpeed"))
        {
            nowSpeed += 1f;
            Time.timeScale = nowSpeed;
            Text tempText = timeUI.GetComponent<Text>();
            tempText.text = infos[nowIndex].time + "\t" + nowSpeed.ToString() + "+";
        }

        if (Input.GetButtonDown("deSpeed"))
        {
            nowSpeed -= 1f;
            if (nowSpeed <= 1)
                nowSpeed = 1;
            Time.timeScale = nowSpeed;
            Text tempText = timeUI.GetComponent<Text>();
            tempText.text = infos[nowIndex].time + "\t" + nowSpeed.ToString() + "+";
        }

            //暂停功能
        if (Input.GetButtonDown("PauseResume"))
        {
            Debug.Log("paused");
            if (isPaused)
            {
                Time.timeScale = nowSpeed;
                isPaused = false;
            }
            else
            {
                Time.timeScale = 0f;
                isPaused = true;
            }
        }

        reasonUI.SetActive(false);
        //悬停显示reason
        if (isPaused)
        {
            Vector3 mousePosition = Input.mousePosition;

            // 将屏幕坐标转换为世界坐标
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);
            RaycastHit hit;

            // 进行射线投射检测
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject != null)
                {
                    if (hit.collider.gameObject.name.Equals("MannaTrigger") || hit.collider.gameObject.name.Equals("MannaTriggerH"))
                    {
                        reasonUI.SetActive(true);
                        Text reasonText = reasonUI.GetComponent<Text>();
                        reasonText.text = infos[nowIndex].reasons["Manna"];
                    }

                    if (hit.collider.gameObject.name.Equals("DukeTrigger") || hit.collider.gameObject.name.Equals("DukeTriggerH"))
                    {
                        reasonUI.SetActive(true);
                        Text reasonText = reasonUI.GetComponent<Text>();
                        reasonText.text = infos[nowIndex].reasons["Duke"];
                    }

                    if (hit.collider.gameObject.name.Equals("AnnaTrigger") || hit.collider.gameObject.name.Equals("AnnaTriggerH"))
                    {
                        reasonUI.SetActive(true);
                        Text reasonText = reasonUI.GetComponent<Text>();
                        reasonText.text = infos[nowIndex].reasons["Anna"];
                    }

                    if (hit.collider.gameObject.name.Equals("BasilTrigger") || hit.collider.gameObject.name.Equals("BasilTriggerH"))
                    {
                        reasonUI.SetActive(true);
                        Text reasonText = reasonUI.GetComponent<Text>();
                        reasonText.text = infos[nowIndex].reasons["Basil"];
                    }

                    if (hit.collider.gameObject.name.Equals("MaryTrigger") || hit.collider.gameObject.name.Equals("MaryTriggerH"))
                    {
                        reasonUI.SetActive(true);
                        Text reasonText = reasonUI.GetComponent<Text>();
                        reasonText.text = infos[nowIndex].reasons["Mary"];
                    }
                }
            }
        }
    }

    Actions GetMoveActions(string name, string start, string end)
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        //获得路径的Markers 不算起点和终点 每个人的起点和终点
        List<NavigationMarker> markers = GetMarkers(name, start, end);
        actions.actionsList.actions = new IAction[markers.Count];
        for (int i = 0; i < markers.Count; i++)
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
            for (int i = 0; i < AjaWinerySceneOutMarkers.Count; i++)
                resultMarkers.Add(AjaWinerySceneOutMarkers[i]);
        }
        if (start.Equals("BasilHouseScene"))
        {
            for (int i = 0; i < BasilHouseSceneOutMarkers.Count; i++)
                resultMarkers.Add(BasilHouseSceneOutMarkers[i]);
        }
        if (start.Equals("MaryLibraryScene"))
        {
            for (int i = 0; i < MaryLibrarySceneOutMarkers.Count; i++)
                resultMarkers.Add(MaryLibrarySceneOutMarkers[i]);
        }
        if (start.Equals("MineralClinicScene"))
        {
            for (int i = 0; i < MineralClinicSceneOutMarkers.Count; i++)
                resultMarkers.Add(MineralClinicSceneOutMarkers[i]);
        }
        if (start.Equals("ChurchScene"))
        {
            for (int i = 0; i < ChurchSceneOutMarkers.Count; i++)
                resultMarkers.Add(ChurchSceneOutMarkers[i]);
        }
        List<NavigationMarker> path = pathMarkers[start + " " + end];
        for (int i = 0; i < path.Count; i++)
            resultMarkers.Add(path[i]);

        if (end.Equals("AjaWineryScene"))
        {
            for (int i = 0; i < AjaWinerySceneInMarkers.Count; i++)
                resultMarkers.Add(AjaWinerySceneInMarkers[AjaWinerySceneInMarkers.Count - i - 1]);
        }
        if (end.Equals("BasilHouseScene"))
        {
            for (int i = 0; i < BasilHouseSceneInMarkers.Count; i++)
                resultMarkers.Add(BasilHouseSceneInMarkers[BasilHouseSceneInMarkers.Count - i - 1]);
        }
        if (end.Equals("MaryLibraryScene"))
        {
            for (int i = 0; i < MaryLibrarySceneInMarkers.Count; i++)
                resultMarkers.Add(MaryLibrarySceneInMarkers[MaryLibrarySceneInMarkers.Count - i - 1]);
        }
        if (end.Equals("MineralClinicScene"))
        {
            for (int i = 0; i < MineralClinicSceneInMarkers.Count; i++)
                resultMarkers.Add(MineralClinicSceneInMarkers[MineralClinicSceneInMarkers.Count - i - 1]);
        }
        if (end.Equals("ChurchScene"))
        {
            for (int i = 0; i < ChurchSceneInMarkers.Count; i++)
                resultMarkers.Add(ChurchSceneInMarkers[ChurchSceneInMarkers.Count - i - 1]);
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

    Actions GetSelectActions()
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        actions.actionsList.actions = new IAction[infos[nowIndex].choice_dialogues.Count + 1];
        defaultSelectItem = infos[nowIndex].choices[3];

        VariablesManager.SetGlobal("defaultSelectNumber", int.Parse(defaultSelectItem) - 1);
        //defaultSelectNumber

        //添加选项事件
        ActionDialogue select = actions.gameObject.AddComponent<ActionDialogue>();
        select.waitToComplete = true;
        select.dialogue = selectDialogue.GetComponent<Dialogue>();
        select.dialogue.itemInstances[2].content = new LocString(infos[nowIndex].choices[0]);
        select.dialogue.itemInstances[3].content = new LocString(infos[nowIndex].choices[1]);
        select.dialogue.itemInstances[4].content = new LocString(infos[nowIndex].choices[2]);
        actions.actionsList.actions[0] = select;

        string[] temp1 = infos[nowIndex].choices[0].Split(' ');
        talker1 = temp1[0];
        for (int i = 1; i < temp1.Length; i++)
        {
            if (temp1[i].Contains("Manna") && !temp1[i].Contains(talker1))
            {
                talker2 = "Manna";
            //    Transform tempT = Manna.transform.Find("CameraLocation");
             //   selectCamera.transform.position = tempT.position;
             //   selectCamera.transform.rotation = tempT.rotation;
                break;
            }
            if (temp1[i].Contains("Duke") && !temp1[i].Contains(talker1))
            {
                talker2 = "Duke";
              //  Transform tempT = Duke.transform.Find("CameraLocation");
              //  selectCamera.transform.position = tempT.position;
              //  selectCamera.transform.rotation = tempT.rotation;
                break;
            }
            if (temp1[i].Contains("Anna") && !temp1[i].Contains(talker1))
            {
                talker2 = "Anna";
              //  Transform tempT = Anna.transform.Find("CameraLocation");
              //  selectCamera.transform.position = tempT.position;
              //  selectCamera.transform.rotation = tempT.rotation;
                break;
            }
            if (temp1[i].Contains("Basil") && !temp1[i].Contains(talker1))
            {
                talker2 = "Basil";
              //  Transform tempT = Basil.transform.Find("CameraLocation");
              //  selectCamera.transform.position = tempT.position;
              //  selectCamera.transform.rotation = tempT.rotation;
                break;
            }
            if (temp1[i].Contains("Mary") && !temp1[i].Contains(talker1))
            {
                talker2 = "Mary";
              //  Transform tempT = Mary.transform.Find("CameraLocation");
              //  selectCamera.transform.position = tempT.position;
              //  selectCamera.transform.rotation = tempT.rotation;
                break;
            }
        }

        if(talker1.Equals("Manna"))
        {
            VariablesManager.SetGlobal("isMannaSelected", true);
        }
        if (talker1.Equals("Duke"))
        {
            VariablesManager.SetGlobal("isDukeSelected", true);
        }
        if (talker1.Equals("Anna"))
        {
            VariablesManager.SetGlobal("isAnnaSelected", true);
        }
        if (talker1.Equals("Basil"))
        {
            VariablesManager.SetGlobal("isBasilSelected", true);
        }
        if (talker1.Equals("Mary"))
        {
            VariablesManager.SetGlobal("isMarySelected", true);
        }

        //添加选项后续对话事件
        for (int i = 0; i < infos[nowIndex].choice_dialogues.Count; i++)
        {
            string[] temp2 = infos[nowIndex].choice_dialogues[i].Split(' ');
            string name = temp2[0];
            string[] test = new string[1];
            test[0] = "said \"";
            string content = infos[nowIndex].choice_dialogues[i].Split(test, System.StringSplitOptions.None)[1].Split('\"')[0];
            ActionSimpleMessageShow message = actions.gameObject.AddComponent<ActionSimpleMessageShow>();
            message.message = new LocString(name + ":\t\t"  + content);
            actions.actionsList.actions[i+1] = message;
        }

        //actions.Execute();
        return actions;
    }

    //通过网络的返回结果生成selections
    Actions GetSelectActions1()
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        actions.actionsList.actions = new IAction[1];
        ActionDialogue select = actions.gameObject.AddComponent<ActionDialogue>();
        select.waitToComplete = true;
        select.dialogue = selectDialogue.GetComponent<Dialogue>();
        
        string[] test = new string[1];
        test[0] = "\"choice1\":\"";
        //Debug.Log(postResult);
        string choice1 = postResult.Split(test, System.StringSplitOptions.None)[1].Split('\"')[0];

        test[0] = "\"choice2\":\"";
        string choice2 = postResult.Split(test, System.StringSplitOptions.None)[1].Split('\"')[0];

        test[0] = "\"choice3\":\"";
        string choice3 = postResult.Split(test, System.StringSplitOptions.None)[1].Split('\"')[0];

        select.dialogue.itemInstances[2].content = new LocString(choice1);
        select.dialogue.itemInstances[3].content = new LocString(choice2);
        select.dialogue.itemInstances[4].content = new LocString(choice3);
        actions.actionsList.actions[0] = select;
        return actions;
    }

    //生成网络的返回结果生成后续的对话事件
    Actions GetSelectActions2()
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        actions.actionsList.actions = new IAction[infos[nowIndex].choice_dialogues.Count + 1];
        return actions;
    }

    private Vector3 getTriggerHPosition(GameObject people, float height)
    {
        Vector3 cameraPosition = topViewer.transform.position;
        Vector3 targetPosition = people.transform.position;

        Vector3 directionToCamera = cameraPosition - targetPosition;
      //  float distanceToCamera = directionToCamera.magnitude;

        // 将要挡住的物体放置在相机和要被挡住的物体之间，可以通过以下方式：
        // 调整挡住物体的距离
        Vector3 newPosition = targetPosition + directionToCamera.normalized * height;

        // 返回要挡住的物体的新位置
        return newPosition;
    }

    private bool getTalkers(string target)
    {
        String targetLoc = infos[nowIndex].locations[target];
        foreach (var pair in infos[nowIndex].locations)
        {
            string name = pair.Key;
            string loc = pair.Value;
            if (loc.Equals(targetLoc))
            {
                talker1 = name;
                talker2 = target;
                return true;
            }
        }
        return false;
    }

    //其中condition表示是在什么情况下发的请求 condition=1 表示在请求三选一选项
    public void SendPostRequest(String url, String postData)
    {
        StartCoroutine(PostData(url, postData));
    }

    IEnumerator PostData(string url, string postData)
    {
        UnityWebRequest webRequest = UnityWebRequest.Post(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData); // 替换为你的JSON字符串
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
        yield return webRequest.SendWebRequest();
        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            postResult = "";
            Debug.LogError("POST请求失败: " + webRequest.error);
        }
        else
        {
            postResult = webRequest.downloadHandler.text;
            before_choice_state = 3;
            Debug.Log("POST请求成功: " + webRequest.downloadHandler.text);
        }
    }
}
