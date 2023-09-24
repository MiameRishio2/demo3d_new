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
using GameCreator.Variables;
using UnityEngine.Animations;
using JetBrains.Annotations;
using GameCreator.Dialogue;
using System.Linq;

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
    private List<TimeInfo> infos;
    private int nowIndex = 0;
    private List<Actions> moveActions;
    private Actions dialogueActions;
    private Actions selectActions;
    private bool isPaused = false;
    private float nowSpeed = 1f;

    public int version = 1;



    //专门用于调试相关的信息
    public string defaultSelectItem = "1";

    // Start is called before the first frame update
    void Start()
    {
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
        string filePath = "data/agent.txt";
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
        float offset_x = 100f;
        Vector3 targetPosition = getTriggerHPosition(Manna, 5);
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[0].transform.position = screenPosition + new Vector3(offset_x, 0);
        if(talker1.Equals("Manna"))
            NPCNames[0].GetComponent<Text>().color = Color.red;
        else
            NPCNames[0].GetComponent<Text>().color = Color.black;

        targetPosition = getTriggerHPosition(Duke, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[1].transform.position = screenPosition + new Vector3(offset_x, 0);
        if (talker1.Equals("Duke"))
            NPCNames[1].GetComponent<Text>().color = Color.red;
        else
            NPCNames[1].GetComponent<Text>().color = Color.black;

        targetPosition = getTriggerHPosition(Anna, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[2].transform.position = screenPosition + new Vector3(offset_x, 0);
        if (talker1.Equals("Anna"))
            NPCNames[2].GetComponent<Text>().color = Color.red;
        else
            NPCNames[2].GetComponent<Text>().color = Color.black;

        targetPosition = getTriggerHPosition(Basil, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[3].transform.position = screenPosition + new Vector3(offset_x, 0);
        if (talker1.Equals("Basil"))
            NPCNames[3].GetComponent<Text>().color = Color.red;
        else
            NPCNames[3].GetComponent<Text>().color = Color.black;

        targetPosition = getTriggerHPosition(Mary, 5);
        screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        NPCNames[4].transform.position = screenPosition + new Vector3(offset_x, 0);
        if (talker1.Equals("Mary"))
            NPCNames[4].GetComponent<Text>().color = Color.red;
        else
            NPCNames[4].GetComponent<Text>().color = Color.black;

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


                if(version == 1)
                {
                    selectActions = null;
                    state = 1;
                }
                else 
                {
                    //生成选择Actions 
                    selectActions = GetSelectActions();
                    object isSelectV = VariablesManager.GetGlobal("isSelect");
                    bool isSelect = (bool)isSelectV;
                    if (isSelect)
                    {
                        //正对人物修改相机位置
                        if (talker2 == "Manna")
                        {
                            Transform tempT = Manna.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                        }
                        if (talker2 == "Duke")
                        {
                            Transform tempT = Duke.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                        }
                        if (talker2 == "Anna")
                        {
                            Transform tempT = Anna.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                        }
                        if (talker2 == "Basil")
                        {
                            Transform tempT = Basil.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
                        }
                        if (talker2 == "Mary")
                        {
                            Transform tempT = Mary.transform.Find("CameraLocation");
                            selectCamera.transform.position = tempT.position;
                            selectCamera.transform.rotation = tempT.rotation;
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
            //选择阶段把默认选项变成不一样的颜色
          /*  GameObject contentSelect = GameObject.Find("DefaultDialogueSkin(Clone)/Choices/Content");
            if(contentSelect != null)
            {
                int index = int.Parse(defaultSelectItem);
                Debug.Log(index);
                contentSelect.transform.GetChild(0).GetComponent<Button>().transition = Selectable.Transition.ColorTint;
              //  contentSelect.transform.GetChild(1).GetComponent<Text>().color = Color.red;
                contentSelect.transform.GetChild(index - 1).GetComponent<Text>().color = Color.yellow;
                // Text text = target.GetComponent<Text>();
                //  text.color = Color.red;
            }*/

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
                Time.timeScale = 1f;
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
}
