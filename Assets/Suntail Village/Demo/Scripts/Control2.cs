using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static GameCreator.Inventory.LootTable;
using UnityEngine.Networking;
using GameCreator.Core;
using GameCreator.Dialogue;
using GameCreator.Localization;
using GameCreator.Variables;
using GameCreator.Messages;
using GameCreator.Characters;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

public class Control2 : MonoBehaviour, IControl
{
    public GameObject Anna;
    public TargetCharacter AnnaTarget;
    private int state = 0; //0表示等待输入指令和选择选项阶段 这个阶段会生成一个三选一actions  1表示执行Actions阶段 有两种actions 一种是执行指令 一种是生成对话并且显示
    private int state0 = 0; //0状态下面的子状态 0表示初始状态 1表示收到返回包之后的状态
    private string ip = "127.0.0.1";
    private string postResult = "";
    public GameObject selectDialogue;
    private Actions selectActions;
    private Queue<string> selectDialogueQueue = new Queue<string>();
    private Actions AfterSelectActions;
    bool isSendRequest = false;
    bool talkerOrder = true;//发送请求对话协议的时候 2个talker的顺序
    public float distance = 2;
    private Quaternion oldRotate;
    private String MannaD = "When she's not at home, she usually can be found at Rose Square, chatting with Anna and Sasha. Manna is a town gossip, and loves to chat. Duke and Manna frequently argue over Duke's drinking and Manna's gossiping. Manna acts like a mother to Cliff if he takes the job at the Winery, and he becomes very important to her.";
    private String DukeD = "Duke is the owner of the Aja Winery in Mineral Town. He is proud of his business and willing to give advice.";
    private String AnnaD = "Anna is Mary's mother and Basil's wife. She lives in the house next to the Library with her husband, although she spends most of her time in Rose Square. Anna is strangely sophisticated for a stay at home Mom in a small village, but she doesn't mind living in Mineral Town - especially when she's got friends like Sasha and Manna to gossip with. She is great at making all kinds of desserts.";
    private String BasilD = "Basil works as a world famous botanist and loves the outdoors. He lives next to the Library with his wife Anna and their daughter Mary. Most of the books in the Library were written by Basil himself! If he's not at his home, he will be near Mother's Hill studying the local flora.Basil is very friendly, and may give you a recipe for Fruit Latte.";
    private String MaryD = "She is the only child of Anna and Basil, and the librarian at the local library. She is a shy girl, and admires her father's work with botany. She hopes to one day become a writer, and is currently working on her own novel. She feels rather sad that few people in town take advantage of the library. She has only a few friends in town, including Gray, Saibara, and Carter. She is a participant in the Cooking Festival and plays the organ during the Music Festival.";

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        //从配置中获取ip地址
        string filePathC = "data/config.txt";
        StreamReader readerC = new StreamReader(filePathC);
        string lineC;
        readerC.ReadLine();
        if ((lineC = readerC.ReadLine()) != null)
        {
            ip = lineC;
        }

        oldRotate = Anna.transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == 0)
        {
            if (state0 == 0)
            {
                String postData = "{\"background\":[{\"name\":\"Duke\",\"description\":\"Duke is the owner of the Aja Winery in Mineral Town. He is proud of his business and willing to give advice. \"},{\"name\":\"Manna\",\"description\":\"When she's not at home, she usually can be found at Rose Square, chatting with Anna and Sasha. Manna is a town gossip, and loves to chat. Duke and Manna frequently argue over Duke's drinking and Manna's gossiping. Manna acts like a mother to Cliff if he takes the job at the Winery, and he becomes very important to her. \"}],\"content\":\"Duke is at RoseSquareScene now. Manna is at RoseSquareScene now. Duke and Manna are in everyday conversation.\"}";
                String url = "http://" + ip + ":18981/choice";
                SendPostRequest(url, postData, 1);
                state0 = 1;
            }

            if (state0 == 2)
            {
                selectActions = GetSelectActions1();
                isSendRequest = false;
                selectActions.Execute();
                state0 = 0;
                state = 1;
            }
        }

        if (state == 1)
        {
            if (selectDialogueQueue.Count != 0 && AfterSelectActions == null)
            {
                string info = selectDialogueQueue.Dequeue();
                //生成新的actions 并且执行    
                AfterSelectActions = GetSelectActions2(info);
                AfterSelectActions.Execute();
            }

            //当前所有的指令都已经被取完 进入结束状态
            if (selectActions == null && AfterSelectActions == null && isSendRequest == true)
            {
                Anna.transform.rotation = oldRotate;
                state = 0;
            }
        }
    }

    public void SendPostRequest(String url, String postData, int con, string extra = "")
    {
        StartCoroutine(PostData(url, postData, con, extra));
    }

    IEnumerator PostData(string url, string postData, int con, String extra = "")
    {
      /*  Debug.Log("url\t" + url);
        Debug.Log("postData\t" + postData);*/

        UnityWebRequest webRequest = UnityWebRequest.Post(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData); // 替换为你的JSON字符串
        webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
        webRequest.SetRequestHeader("Content-Type", "application/json;charset=utf-8");
        yield return webRequest.SendWebRequest();
        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.LogError("POST请求失败: " + webRequest.error);
        }
        else
        {
            postResult = webRequest.downloadHandler.text;
            if (con == 1)
                state0 = 2;
            if (con == 2)
            {
                //把返回的消息放入队列
                selectDialogueQueue.Enqueue(postResult);
                //如果isEnd为空 结束这轮对话
                ConversationData myData = JsonConvert.DeserializeObject<ConversationData>(postResult);
                if (myData.isEnd)
                    isSendRequest = true;
                else
                    SendInfo1(myData.content, myData.conversation);
            }
            if (con == 3)
            {
                string[] temp = postResult.Split('\"');
                string isWalking = temp[5];
                string direction = temp[9];
                Debug.Log("isWalking\t" + isWalking);
                Debug.Log("direction\t" + direction);

                if (isWalking.Equals("True"))
                {
                    AfterSelectActions = GetSelectActions3(direction);
                    AfterSelectActions.Execute();
                    isSendRequest = true;
                }
                else
                    SendInfo(extra);
            }

            Debug.Log("POST请求成功: " + webRequest.downloadHandler.text);
        }
    }

    Actions GetSelectActions1()
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;

        actions.actionsList.actions = new IAction[1];
        ActionDialogue select = actions.gameObject.AddComponent<ActionDialogue>();
        select.waitToComplete = true;
        select.dialogue = selectDialogue.GetComponent<Dialogue>();

        string Result = Regex.Replace(postResult, @"\w+ will talk to \w+", "");
        Choices choices = JsonConvert.DeserializeObject<Choices>(Result);

        select.dialogue.itemInstances[2].content = new LocString(choices.choice1);
        select.dialogue.itemInstances[3].content = new LocString(choices.choice2);
        select.dialogue.itemInstances[4].content = new LocString(choices.choice3);
        actions.actionsList.actions[0] = select;
        return actions;
    }

    Actions GetSelectActions2(string postResult)
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;
        actions.actionsList.actions = new IAction[1];
        string[] temp = postResult.Split('{');
        string name = temp[temp.Length - 1].Split('\"')[1];
        //  Debug.Log("name\t" + name);

        string[] test1 = new string[1];
        test1[0] = ":\"\\\"";
        //Debug.Log(postResult);
        temp = postResult.Split(test1, System.StringSplitOptions.None);
        string content = temp[temp.Length - 1].Split('\\')[0];

        ActionSimpleMessageShow message = actions.gameObject.AddComponent<ActionSimpleMessageShow>();
        message.message = new LocString(name + ":\t\t" + content);
        message.time = 6;
        actions.actionsList.actions[0] = message;

        return actions;
    }

    Actions GetSelectActions3(string direction)
    {
        GameObject tempObject = new GameObject();
        Actions actions = tempObject.AddComponent<Actions>();
        actions.destroyAfterFinishing = true;
        actions.actionsList.actions = new IAction[2];

        ActionCharacterMoveTo moveTo1 = actions.gameObject.AddComponent<ActionCharacterMoveTo>();
        moveTo1.target = AnnaTarget;
        moveTo1.moveTo = ActionCharacterMoveTo.MOVE_TO.Position;

        ActionCharacterMoveTo moveTo2 = actions.gameObject.AddComponent<ActionCharacterMoveTo>();
        moveTo2.target = AnnaTarget;
        moveTo2.moveTo = ActionCharacterMoveTo.MOVE_TO.Position;

        GameObject targetNPC = Anna;

        if (direction.Equals("forward"))
            moveTo1.position = targetNPC.transform.position + targetNPC.transform.forward * distance;
        if (direction.Equals("backward"))
            moveTo1.position = targetNPC.transform.position - targetNPC.transform.forward * distance;
        if (direction.Equals("right"))
            moveTo1.position = targetNPC.transform.position + targetNPC.transform.right * distance;
        if (direction.Equals("left"))
            moveTo1.position = targetNPC.transform.position - targetNPC.transform.right * distance;

        moveTo2.position = targetNPC.transform.position;

        actions.actionsList.actions[0] = moveTo1;
        actions.actionsList.actions[1] = moveTo2;

        return actions;
    }

    public void SendInfo(string _content)
    {

        MyDialogue myData = GetMyDialogue("Duke", DukeD, "Manna", MannaD, _content, new List<Dictionary<string, string>>{ });
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };
       
        String result = JsonConvert.SerializeObject(myData, settings);
        String url = "http://" + ip + ":18981/choice_dialogue_step_by_step";
        talkerOrder = false;
        SendPostRequest(url, result, 2);
    }

    //发送后续包的协议
    public void SendInfo1(string _content, List<Dictionary<string, string>> _conversation)
    {
        String result = "";
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };

        if (talkerOrder)
        {
            MyDialogue myData = GetMyDialogue("Duke", DukeD, "Manna", MannaD, _content, _conversation);
            result = JsonConvert.SerializeObject(myData, settings);
            Debug.Log("result2\t" + result);
        }
        else
        {
            MyDialogue myData = GetMyDialogue("Manna", MannaD, "Duke", DukeD, _content, _conversation);
            result = JsonConvert.SerializeObject(myData, settings);
            Debug.Log("result2\t" + result);
        }

        talkerOrder = !talkerOrder;
        String url = "http://" + ip + ":18981/choice_dialogue_step_by_step";
        SendPostRequest(url, result, 2, _content);
    }

    public void SendInfo2(string content)
    {
        //先构造移动指令的协议 失败之后再发送对话协议
        string postData = "{\"instruction\": \"" + content + "\"}";
        string url = "http://" + ip + ":18981/test_choice_is_walking_instruction";
        SendPostRequest(url, postData, 3, content);
    }

    public static MyDialogue GetMyDialogue(string firstName, string firstDes, string secondName, string secondDes, string _content, List<Dictionary<string, string>> _conversation)
    {
        MyDialogue myData = new MyDialogue()
        {
            background = new Background[]
            {
                new Background
                {
                    name = firstName,
                    description = firstDes
                },
                new Background
                {
                    name = secondName,
                    description = secondDes
                }
            },
            content = "Duke is at ChurchScene now. Manna is at ChurchScene now. Duke and Manna are in everyday conversation. " + _content,
            conversation = _conversation
        };

        return myData;
    }


}
