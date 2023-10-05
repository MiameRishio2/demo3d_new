using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class testPost : MonoBehaviour
{
    private string url = "http://127.0.0.1:18981/choice"; // 替换为您的目标API端点

    private string postData = "{\"background\":[{\"name\":\"\",\"description\":\"\"},{\"name\":\"\",\"description\":\"\"}],\"content\":\"\"}";
    
    public void SendPostRequest()
    {
        StartCoroutine(PostData());
    }

    IEnumerator PostData()
    {
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
           Debug.Log("POST请求成功: " + webRequest.downloadHandler.text);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        string filePathC = "data/test.txt";
        StreamReader readerC = new StreamReader(filePathC);
        string lineC;

        if ((lineC = readerC.ReadLine()) != null)
        {
            postData = lineC;
        }

        SendPostRequest();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
