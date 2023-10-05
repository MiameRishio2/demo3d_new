using System;
using System.IO;
using System.Text;
using UnityEngine;
using NAudio.Wave;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;


    /// <summary>
    /// 语音工具
    /// </summary>
    public static class Speech
    {
        /* APPID 于讯飞开发者控制台创建应用申请所得 */
        const string mAppID = "appid=5a0f0ca4";

        /// <summary>
        /// 科大讯飞语音识别
        /// </summary>
        /// <param name="clipBuffer">音频数据</param>
        /// <returns>识别后的字符串结果</returns>
        public static string Asr(byte[] clipBuffer)
        {
            /* 首先调用登录接口
             * 登录成功返回0,否则为错误代码 */
            int res = MSCDLL.MSPLogin(null, null, mAppID);
            if (res != 0)
            {
                Debug.Log($"login failed. error code: {res}");
                return null;
            }

            /* 调用开启一次语音识别的接口
             * 接收返回的句柄,后续调用写入音频、获取结果等接口需要使用
             * 调用成功error code为0,否则为错误代码 
             * 备注:
             *  第二个参数为 开始一次语音识别的参数列表 可以再进行具体的封装
             *  例如 language参数 封装枚举 switch中文 为zh_cn    switch英文 为en_us
             *  具体参照科大讯飞官网SDK文档 */
            IntPtr sessionID = MSCDLL.QISRSessionBegin(null,
                "sub=iat,domain=iat,language=zh_cn,accent=mandarin,sample_rate=16000,result_type=plain,result_encoding= utf-8", ref res);
            if (res != 0)
            {
                Debug.Log($"begin failed. error code: {res}");
                OnErrorEvent();
                return null;
            }

            /* 用于记录端点状态 */
            EpStatus epStatus = EpStatus.MSP_EP_LOOKING_FOR_SPEECH;
            /* 用于记录识别状态 */
            RecogStatus recognizeStatus = RecogStatus.MSP_REC_STATUS_SUCCESS;

            /* 调用音频写入接口 将需要识别的音频数据传入
             * 写入成功返回0,否则为错误代码 */
            res = MSCDLL.QISRAudioWrite(sessionID, clipBuffer, (uint)clipBuffer.Length, AudioStatus.MSP_AUDIO_SAMPLE_CONTINUE, ref epStatus, ref recognizeStatus);
            if (res != 0)
            {
                Debug.Log($"write failed. error code: {res}");
                MSCDLL.QISRSessionEnd(sessionID, "error");
                OnErrorEvent();
                return null;
            }
            res = MSCDLL.QISRAudioWrite(sessionID, null, 0, AudioStatus.MSP_AUDIO_SAMPLE_LAST, ref epStatus, ref recognizeStatus);
            if (res != 0)
            {
                Debug.Log($"write failed. error code: {res}");
                MSCDLL.QISRSessionEnd(sessionID, "error");
                OnErrorEvent();
                return null;
            }

            /* 用于存储识别结果 */
            StringBuilder sb = new StringBuilder();
            /* 用于累加识别结果的长度 */
            int length = 0;

            /* 音频写入后 反复调用获取识别结果的接口直到获取完毕 */
            while (recognizeStatus != RecogStatus.MSP_REC_STATUS_COMPLETE)
            {
                IntPtr curtRslt = MSCDLL.QISRGetResult(sessionID, ref recognizeStatus, 0, ref res);
                if (res != 0)
                {
                    Debug.Log($"get result failed. error code: {res}");
                    MSCDLL.QISRSessionEnd(sessionID, "error");
                    OnErrorEvent();
                    return null;
                }
                /* 当前部分识别结果不为空 将其存入sb*/
                if (null != curtRslt)
                {
                    length += curtRslt.ToString().Length;
                    if (length > 4096)
                    {
                        Debug.Log($"size not enough: {length} > 4096");
                        MSCDLL.QISRSessionEnd(sessionID, "error");
                        OnErrorEvent();
                        return sb.ToString();
                    }
                    sb.Append(Marshal.PtrToStringAnsi(curtRslt));
                }
                Thread.Sleep(150);
            }

            /* 获取完全部识别结果后 结束本次语音识别 */
            res = MSCDLL.QISRSessionEnd(sessionID, "ao li gei !");
            if (res != 0) Debug.Log($"end failed. error code: {res}");

            /* 最终退出登录 返回识别结果*/
            res = MSCDLL.MSPLogout();
            if (res != 0) Debug.Log($"logout failed. error code {res}");
            return sb.ToString();
        }
        /// <summary>
        /// 科大讯飞语音识别
        /// </summary>
        /// <param name="path">音频文件所在路径</param>
        /// <returns>识别后的字符串结果</returns>
        public static string Asr(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("path can not be null.");
                return null;
            }
            byte[] clipBuffer;
            try
            {
                clipBuffer = File.ReadAllBytes(path);
            }
            catch (Exception e)
            {
                Debug.Log($"exception: {e.Message}");
                return null;
            }
            return Asr(clipBuffer);
        }
        /// <summary>
        /// 科大讯飞语音识别
        /// </summary>
        /// <param name="clip">需要识别的AudioClip</param>
        /// <returns>识别后的字符串结果</returns>
        public static string Asr(AudioClip clip)
        {
            byte[] clipBuffer = clip.ToPCM16();
            return Asr(clipBuffer);
        }

        /// <summary>
        /// 科大讯飞语音合成
        /// </summary>
        /// <param name="content">需要合成音频的文本内容</param>
        /// <returns>合成后的音频</returns>
        public static AudioClip Tts(string content, TtsVoice voice = TtsVoice.XuJiu)
        {
            /* 首先调用登录接口
             * 登录成功返回0,否则为错误代码 */
            int res = MSCDLL.MSPLogin(null, null, mAppID);
            if (res != 0)
            {
                Debug.Log($"login failed. error code: {res}");
                return null;
            }

            /* 调用开启一次语音合成的接口
             * 接收返回后的句柄,后续调用写入文本等接口需要使用
             * 调用成功error code为0,否则为错误代码
             * 备注:
             *  第一个参数为 开启一次语音合成的参数列表
             *  具体参照科大讯飞官网SDK文档 */
            string voicer = "";
            switch (voice)
            {
                case TtsVoice.XiaoYan:
                    voicer = "xiaoyan";
                    break;
                case TtsVoice.XuJiu:
                    voicer = "aisjiuxu";
                    break;
                case TtsVoice.XiaoPing:
                    voicer = "aisxping";
                    break;
                case TtsVoice.XiaoJing:
                    voicer = "aisjinger";
                    break;
                case TtsVoice.XuXiaoBao:
                    voicer = "aisbabyxu";
                    break;
                default:
                    break;
            }
            IntPtr sessionID = MSCDLL.QTTSSessionBegin($"engine_type = cloud, voice_name = {voicer}, text_encoding = utf8, sample_rate = 16000", ref res);
            if (res != 0)
            {
                Debug.Log($"begin failed. error code: {res}");
                OnErrorEvent();
                return null;
            }

            /* 调用写入文本的接口 将需要合成内容传入
             * 调用成功返回0,否则为错误代码 */
            res = MSCDLL.QTTSTextPut(sessionID, content, (uint)Encoding.UTF8.GetByteCount(content), string.Empty);
            if (res != 0)
            {
                Debug.Log($"put text failed. error code: {res}");
                OnErrorEvent();
                return null;
            }

            /* 用于记录长度 */
            uint audioLength = 0;
            /* 用于记录合成状态 */
            SynthStatus synthStatus = SynthStatus.MSP_TTS_FLAG_STILL_HAVE_DATA;

            List<byte[]> bytesList = new List<byte[]>();

            /* 文本写入后 调用获取合成音频的接口
             * 获取成功error code为0,否则为错误代码
             * 需反复调用 直到合成状态为结束 或出现错误代码 */
            try
            {
                while (true)
                {
                    IntPtr intPtr = MSCDLL.QTTSAudioGet(sessionID, ref audioLength, ref synthStatus, ref res);
                    byte[] byteArray = new byte[(int)audioLength];
                    if (audioLength > 0) Marshal.Copy(intPtr, byteArray, 0, (int)audioLength);

                    bytesList.Add(byteArray);

                    Thread.Sleep(150);
                    if (synthStatus == SynthStatus.MSP_TTS_FLAG_DATA_END || res != 0)
                        break;
                }
            }
            catch (Exception e)
            {
                OnErrorEvent();
                Debug.Log($"error: {e.Message}");
                return null;
            }

            int size = 0;
            for (int i = 0; i < bytesList.Count; i++)
            {
                size += bytesList[i].Length;
            }

            var header = GetWaveHeader(size);
            byte[] array = header.ToBytes();
            bytesList.Insert(0, array);
            size += array.Length;

            byte[] bytes = new byte[size];

            size = 0;
            for (int i = 0; i < bytesList.Count; i++)
            {
                bytesList[i].CopyTo(bytes, size);
                size += bytesList[i].Length;
            }
            AudioClip clip = bytes.ToWAV();


            res = MSCDLL.QTTSSessionEnd(sessionID, "ao li gei !");
            if (res != 0)
            {
                Debug.Log($"end failed. error code: {res}");
                OnErrorEvent();
                return clip;
            }

            res = MSCDLL.MSPLogout();
            if (res != 0)
            {
                Debug.Log($"logout failed. error code: {res}");
                return clip;
            }
            return clip;
        }
        /// <summary>
        /// 科大讯飞语音合成
        /// </summary>
        /// <param name="content">需要合成的内容</param>
        /// <param name="path">将合成后的音频写入指定的路径</param>
        /// <returns>调用成功返回true 发生异常返回false</returns>
        public static bool Tts(string content, string path)
        {
            /* 首先调用登录接口
             * 登录成功返回0,否则为错误代码 */
            int res = MSCDLL.MSPLogin(null, null, mAppID);
            if (res != 0)
            {
                Debug.Log($"login failed. error code: {res}");
                return false;
            }

            /* 调用开启一次语音合成的接口
             * 接收返回后的句柄,后续调用写入文本等接口需要使用
             * 调用成功error code为0,否则为错误代码
             * 备注:
             *  第一个参数为 开启一次语音合成的参数列表
             *  具体参照科大讯飞官网SDK文档 */
            IntPtr sessionID = MSCDLL.QTTSSessionBegin("engine_type = cloud, voice = xiaoyan, text_encoding = utf8, sample_rate = 16000", ref res);
            if (res != 0)
            {
                Debug.Log($"begin failed. error code: {res}");
                OnErrorEvent();
                return false;
            }

            /* 调用写入文本的接口 将需要合成内容传入
             * 调用成功返回0,否则为错误代码 */
            res = MSCDLL.QTTSTextPut(sessionID, content, (uint)Encoding.UTF8.GetByteCount(content), string.Empty);
            if (res != 0)
            {
                Debug.Log($"put text failed. error code: {res}");
                OnErrorEvent();
                return false;
            }

            /* 用于记录长度 */
            uint audioLength = 0;
            /* 用于记录合成状态 */
            SynthStatus synthStatus = SynthStatus.MSP_TTS_FLAG_STILL_HAVE_DATA;
            /* 开启一个流 */
            MemoryStream ms = new MemoryStream();
            ms.Write(new byte[44], 0, 44);


            /* 文本写入后 调用获取合成音频的接口
             * 获取成功error code为0,否则为错误代码
             * 需反复调用 直到合成状态为结束 或出现错误代码 */
            try
            {
                while (true)
                {
                    IntPtr intPtr = MSCDLL.QTTSAudioGet(sessionID, ref audioLength, ref synthStatus, ref res);
                    byte[] byteArray = new byte[(int)audioLength];
                    if (audioLength > 0) Marshal.Copy(intPtr, byteArray, 0, (int)audioLength);
                    ms.Write(byteArray, 0, (int)audioLength);
                    Thread.Sleep(150);
                    if (synthStatus == SynthStatus.MSP_TTS_FLAG_DATA_END || res != 0)
                        break;
                }
            }
            catch (Exception e)
            {
                OnErrorEvent();
                Debug.Log($"error: {e.Message}");
                return false;
            }


            var header = GetWaveHeader((int)ms.Length);
            byte[] array = header.ToBytes();
            ms.Position = 0L;
            ms.Write(array, 0, array.Length);
            ms.Position = 0L;

            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            ms.WriteTo(fs);
            ms.Close();
            fs.Close();

            res = MSCDLL.QTTSSessionEnd(sessionID, "ao li gei !");
            if (res != 0)
            {
                Debug.Log($"end failed. error code: {res}");
                OnErrorEvent();
                return false;
            }

            res = MSCDLL.MSPLogout();
            if (res != 0)
            {
                Debug.Log($"logout failed. error code: {res}");
                return false;
            }
            return true;
        }

        /* 发生异常后调用退出登录接口 */
        static void OnErrorEvent()
        {
            int res = MSCDLL.MSPLogout();
            if (res != 0)
            {
                Debug.Log($"logout failed. error code: {res}");
            }
        }
        /* 语音音频头 初始化赋值 */
        static WaveHeader GetWaveHeader(int dataLen)
        {
            return new WaveHeader
            {
                RIFFID = 1179011410,
                FileSize = dataLen - 8,
                RIFFType = 1163280727,
                FMTID = 544501094,
                FMTSize = 16,
                FMTTag = 1,
                FMTChannel = 1,
                FMTSamplesPerSec = 16000,
                AvgBytesPerSec = 32000,
                BlockAlign = 2,
                BitsPerSample = 16,
                DataID = 1635017060,
                DataSize = dataLen - 44
            };
        }
    }
    public class MSCDLL
    {
        #region msp_cmn.h 通用接口
        /// <summary>
        /// 初始化msc 用户登录  user login. 
        /// 使用其他接口前必须先调用MSPLogin,可以在应用程序启动时调用
        /// </summary>
        /// <param name="usr">user name. 此参数保留 传入NULL即可</param>
        /// <param name="pwd">password. 此参数保留 传入NULL即可</param>
        /// <param name="parameters">parameters when user login. 每个参数和参数值通过key=value的形式组成参数对,如果有多个参数对,再用逗号进行拼接</param>
        ///     通用 appid 应用ID: 于讯飞开放平台申请SDK成功后获取到的appid
        ///     离线 engine_start 离线引擎启动: 启用离线引擎 支持参数: ivw:唤醒 asr:识别
        ///     离线 [xxx]_res_path 离线引擎资源路径: 设置ivw asr引擎离线资源路径 
        ///             详细格式: fo|[path]|[offset]|[length]|xx|xx 
        ///             单个资源路径示例: ivw_res_path=fo|res/ivw/wakeupresource.jet
        ///             多个资源路径示例: asr_res_path=fo|res/asr/common.jet;fo|res/asr/sms.jet
        /// <returns>return 0 if sucess, otherwise return error code. 成功返回MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int MSPLogin(string usr, string pwd, string parameters);
        /// <summary>
        /// 退出登录  user logout.
        /// 本接口和MSPLogin配合使用 确保其他接口调用结束之后调用MSPLogout,否则结果不可预期
        /// </summary>
        /// <returns>如果函数调用成功返回MSP_SUCCESS,否则返回错误代码 return 0 if sucess, otherwise return error code.</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int MSPLogout();
        /// <summary>
        /// 用户数据上传  upload data such as user config, custom grammar, etc. 
        /// </summary>
        /// <param name="dataName">数据名称字符串  should be unique to diff other data.</param>
        /// <param name="data">待上传数据缓冲区的起始地址  the data buffer pointer, data could be binary.</param>
        /// <param name="dataLen">数据长度(如果是字符串,则不包含'\0')  length of data.</param>
        /// <param name="_params">parameters about uploading data.</param>
        ///     在线 sub = uup,dtt = userword 上传用户词表
        ///     在线 sub = uup,dtt = contact 上传联系人
        /// <param name="errorCode">return 0 if success, otherwise return error code.</param>
        /// <returns>data id returned by server, used for special command.</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MSPUploadData(string dataName, IntPtr data, uint dataLen, string _params, ref int errorCode);
        /// <summary>
        /// write data to msc, such as data to be uploaded, searching text, etc.
        /// </summary>
        /// <param name="data">the data buffer pointer, data could be binary.</param>
        /// <param name="dataLen">length of data.</param>
        /// <param name="dataStatus">data status, 2: first or continuous, 4: last.</param>
        /// <returns>return 0 if success, otherwise return error code.</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int MSPAppendData(IntPtr data, uint dataLen, uint dataStatus);
        /// <summary>
        /// download data such as user config, etc.
        /// </summary>
        /// <param name="_params">parameters about data to be downloaded.</param>
        /// <param name="dataLen">length of received data.</param>
        /// <param name="errorCode">return 0 if success, otherwise return error code.</param>
        /// <returns>received data buffer pointer, data could be binary, null if failed or data does not exsit.</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MSPDownloadData(string _params, ref uint dataLen, ref int errorCode);
        /// <summary>
        /// set param of msc.  参数设置接口、离线引擎初始化接口 
        /// </summary>
        /// <param name="paramName">param name.</param>
        ///     离线 engine_start   启动离线引擎
        ///     离线 engine_destroy 销毁离线引擎
        /// <param name="paramValue">param value. 参数值</param>
        /// <returns>return 0 if success, otherwise return errcode. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int MSPSetParam(string paramName, string paramValue);
        /// <summary>
        /// get param of msc.  获取msc的设置信息
        /// </summary>
        /// <param name="paramName">param name. 参数名,一次调用只支持查询一个参数</param>
        ///     在线 upflow   上行数据量
        ///     在线 downflow 下行数据量
        /// <param name="paramValue">param value.</param>
        ///     输入: buffer首地址
        ///     输出: 向该buffer写入获取到的信息
        /// <param name="valueLen">param value (buffer) length.</param>
        ///     输入: buffer的大小
        ///     输出: 信息实际长度(不含'\0')
        /// <returns>return 0 if success, otherwise return errcode. 函数调用成功返回MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int MSPGetParam(string paramName, ref byte[] paramValue, ref uint valueLen);
        /// <summary>
        /// get version of msc or local engine. 获取msc或本地引擎版本信息
        /// </summary>
        /// <param name="verName">version name, could be "msc", "aitalk", "aisound", "ivw". 参数名,一次调用只支持查询一个参数</param>
        ///     离线 ver_msc msc版本号
        ///     离线 ver_asr 离线识别版本号,目前不支持
        ///     离线 ver_tts 离线合成版本号
        ///     离线 ver_ivw 离线唤醒版本号
        /// <param name="errorCode">return 0 if success, otherwise return error code. 如果函数调用成功返回MSP_SUCCESS,否则返回错误代码</param>
        /// <returns>return version value if success, null if fail.  成功返回缓冲区指针,失败或数据不存在返回NULL</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MSPGetVersion(string verName, ref int errorCode);
        #endregion

        #region qisr.h 语音识别
        /// <summary>
        /// create a recognizer session to recognize audio data. 开始一次语音识别
        /// </summary>
        /// <param name="grammarList">garmmars list, inline grammar support only one. 此参数保留,传入NULL即可</param>
        /// <param name="_params">parameters when the session created.</param>
        ///     通用  engine_type     引擎类型                      cloud在线引擎 local离线引擎
        ///     在线  sub             本次识别请求的类型            iat语音听写 asr命令词识别
        ///     在线  language        语言                          zh_cn简体中文 en_us英文
        ///     在线  domain          领域                          iat语音听写
        ///     在线  accent          语言区域                      mandarin普通话
        ///     通用  sample_rate     音频采样率                    16000 8000
        ///     通用  asr_threshold   识别门限                      离线语法识别结果门限值,设置只返回置信度得分大于此门限值的结果 0-100
        ///     离线  asr_denoise     是否开启降噪功能              0不开启 1开启
        ///     离线  asr_res_path    离线识别资源路径              离线识别资源所在路径
        ///     离线  grm_build_path  离线语法生成路径              构建离线语法所生成数据的保存路径(文件夹)
        ///     通用  result_type     结果格式                      plain json
        ///     通用  text_encoding   文本编码格式                  表示参数中携带的文本编码格式
        ///     离线  local_grammar   离线语法id                    构建离线语法后获得的语法ID
        ///     通用  ptt             添加标点符号(sub=iat时有效)   0:无标点符号;1:有标点符号
        ///     在线  aue             音频编码格式和压缩等级        编码算法:raw;speex;speex-wb;ico 编码等级: raw不进行压缩 speex系列0-10
        ///     通用  result_encoding 识别结果字符串所用编码格式    plain:UTF-8,GB2312 json:UTF-8
        ///     通用  vad_enable      VAD功能开关                   是否启用VAD 默认为开启VAD 0(或false)为关闭
        ///     通用  vad_bos         允许头部静音的最长时间        目前未开启该功能
        ///     通用  vad_eos         允许尾部静音的最长时间        0-10000毫秒 默认为2000
        /// <param name="errorCode">return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</param>
        /// <returns>return session id of current session, null is failed. 函数调用成功返回字符串格式的sessionID,失败返回NULL sessionID是本次识别的句柄</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr QISRSessionBegin(string grammarList, string _params, ref int errorCode);
        /// <summary>
        /// writing binary audio data to recognizer. 写入本次识别的音频
        /// </summary>
        /// <param name="sessionID">the session id returned by recog_begin. 由QISRSessionBegin返回的句柄</param>
        /// <param name="waveData">binary data of waveform. 音频数据缓冲区起始地址</param>
        /// <param name="waveLen">waveform data size in bytes. 音频数据长度,单位字节</param>
        /// <param name="audioStatus">audio status. 用来告知msc音频发送是否完成</param>
        /// <param name="epStatus">ep status. 端点检测(End-point detected)器所处的状态</param>
        /// <param name="recogStatus">recognition status. 识别器返回的状态,提醒用户及时开始\停止获取识别结果</param>
        ///     本接口需不断调用,直到音频全部写入为止 上传音频时,需更新audioStatus的值 具体来说:
        ///         当写入首块音频时,将audioStatus置为MSP_AUDIO_SAMPLE_FIRST
        ///         当写入最后一块音频时,将audioStatus置为MSP_AUDIO_SAMPLE_LAST
        ///         其余情况下,将audioStatus置为MSP_AUDIO_SAMPLE_CONTINUE
        ///     同时,需定时检查两个变量: epStatus和recogStatus 具体来说:
        ///         当epStatus显示已检测到后端点时,MSC已不再接收音频,应及时停止音频写入
        ///         当rsltStatus显示有识别结果返回时,即可从MSC缓存中获取结果
        /// <returns>return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QISRAudioWrite(IntPtr sessionID, byte[] waveData, uint waveLen, AudioStatus audioStatus, ref EpStatus epStatus, ref RecogStatus recogStatus);
        /// <summary>
        /// get recognize result in specified format. 获取识别结果
        /// </summary>
        /// <param name="sessionID">session id returned by session begin. 由QISRSessionBegin返回的句柄</param>
        /// <param name="rsltStatus">status of recognition result, 识别结果的状态,其取值范围和含义请参考QISRAudioWrite 的参数recogStatus</param>
        /// <param name="waitTime">此参数做保留用</param>
        /// <param name="errorCode">return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</param>
        /// 当写入音频过程中已经有部分识别结果返回时,可以获取结果
        /// 在音频写入完毕后,用户需反复调用此接口,直到识别结果获取完毕(rlstStatus值为5)或返回错误码 
        /// 注意:如果某次成功调用后暂未获得识别结果,请将当前线程sleep一段时间,以防频繁调用浪费CPU资源
        /// <returns>return 0 if success, otherwise return error code. 函数执行成功且有识别结果时,返回结果字符串指针 其他情况(失败或无结果)返回NULL</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr QISRGetResult(IntPtr sessionID, ref RecogStatus rsltStatus, int waitTime, ref int errorCode);
        /// <summary>
        /// end the recognizer session, release all resource. 结束本次语音识别
        /// 本接口和QISRSessionBegin对应,调用此接口后,该句柄对应的相关资源(参数、语法、音频、实例等)都会被释放,用户不应再使用该句柄
        /// </summary>
        /// <param name="sessionID">session id string to end. 由QISRSessionBegin返回的句柄</param>
        /// <param name="hints">user hints to end session, hints will be logged to CallLog. 结束本次语音识别的原因描述,为用户自定义内容</param>
        /// <returns>return 0 if sucess, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QISRSessionEnd(IntPtr sessionID, string hints);
        /// <summary>
        /// get params related with msc. 获取当次语音识别信息,如上行流量、下行流量等
        /// </summary>
        /// <param name="sessionID">session id of related param, set null to got global param. 由QISRSessionbegin返回的句柄,如果为NULL,获取msc的设置信息</param>
        /// <param name="paramName">param name,could pass more than one param split by ','';'or'\n'. 参数名,一次调用只支持查询一个参数</param>
        ///     在线  sid         服务端会话ID 长度为32字节
        ///     在线  upflow      上行数据量
        ///     在线  downflow    下行数据量
        ///     通用  volume      最后一次写入的音频的音量
        /// <param name="paramValue">param value buffer, malloced by user.</param>
        ///     输入: buffer首地址
        ///     输出: 向该buffer写入获取到的信息
        /// <param name="valueLen">pass in length of value buffer, and return length of value string.</param>
        ///     输入: buffer的大小
        ///     输出: 信息实际长度(不含’\0’)
        /// <returns>return 0 if success, otherwise return errcode. 函数调用成功返回MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QISRGetParam(string sessionID, string paramName, ref byte[] paramValue, ref uint valueLen);
        #endregion

        #region qtts.h 语音合成
        /// <summary>
        /// create a tts session to synthesize data. 开始一次语音合成,分配语音合成资源
        /// </summary>
        /// <param name="_params">parameters when the session created. 传入的参数列表</param>
        ///     通用  engine_type     引擎类型                      cloud在线引擎 local离线引擎
        ///     通用  voice_name      发音人                        不同的发音人代表了不同的音色 如男声、女声、童声等
        ///     通用  speed           语速                          0-100 default50
        ///     通用  volume          音量                          0-100 dafault50
        ///     通用  pitch           语调                          0-100 default50
        ///     离线  tts_res_path    合成资源路径                  合成资源所在路径,支持fo 方式参数设置
        ///     通用  rdn             数字发音                      0数值优先 1完全数值 2完全字符串 3字符串优先
        ///     离线  rcn             1 的中文发音                  0表示发音为yao 1表示发音为yi
        ///     通用  text_encoding   文本编码格式(必传)            合成文本编码格式,支持参数,GB2312,GBK,BIG5,UNICODE,GB18030,UTF8
        ///     通用  sample_rate     合成音频采样率                合成音频采样率,支持参数,16000,8000,默认为16000
        ///     在线  background_sound背景音                        0无背景音乐   1有背景音乐
        ///     在线  aue             音频编码格式和压缩等级        编码算法:raw;speex;speex-wb;ico 编码等级: raw不进行压缩 speex系列0-10
        ///     在线  ttp             文本类型                      text普通格式文本 cssml格式文本
        ///     离线  speed_increase  语速增强                      1正常 2二倍语速 4四倍语速
        ///     离线  effect          合成音效                      0无音效 1忽远忽近 2回声 3机器人 4合唱 5水下 6混响 7阴阳怪气
        /// <param name="errorCode">return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</param>
        /// <returns>return the new session id if success, otherwise return null. 函数调用成功返回字符串格式的sessionID,失败返回NULL sessionID是本次合成的句柄</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr QTTSSessionBegin(string _params, ref int errorCode);
        /// <summary>
        /// writing text string to synthesizer. 写入要合成的文本
        /// </summary>
        /// <param name="sessionID">the session id returned by sesson begin. 由QTTSSessionBegin返回的句柄</param>
        /// <param name="textString">text buffer. 字符串指针 指向待合成的文本字符串</param>
        /// <param name="textLen">text size in bytes. 合成文本长度,最大支持8192个字节</param>
        /// <param name="_params">parameters when the session created. 本次合成所用的参数,只对本次合成的文本有效 目前为空</param>
        /// <returns>return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QTTSTextPut(IntPtr sessionID, string textString, uint textLen, string _params);
        /// <summary>
        /// synthesize text to audio, and return audio information. 获取合成音频
        /// </summary>
        /// <param name="sessionID">session id returned by session begin. 由QTTSSessionBegin返回的句柄</param>
        /// <param name="audioLen">synthesized audio size in bytes. 合成音频长度,单位字节</param>
        /// <param name="synthStatus">synthesizing status. 合成音频状态</param>
        /// <param name="errorCode">return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</param>
        /// 用户需要反复获取音频,直到音频获取完毕或函数调用失败
        /// 在重复获取音频时,如果暂未获得音频数据,需要将当前线程sleep一段时间,以防频繁调用浪费CPU资源
        /// <returns>return current synthesized audio data buffer, size returned by QTTSTextSynth. 函数调用成功且有音频数据时返回非空指针 调用失败或无音频数据时,返回NULL</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr QTTSAudioGet(IntPtr sessionID, ref uint audioLen, ref SynthStatus synthStatus, ref int errorCode);
        /// <summary>
        /// get synthesized audio data information.
        /// </summary>
        /// <param name="sessionID">session id returned by session begin.</param>
        /// <returns>return audio info string.</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr QTTSAudioInfo(IntPtr sessionID);
        /// <summary>
        /// end the recognizer session, release all resource. 结束本次语音合成
        /// 本接口和QTTSSessionBegin对应,调用此接口后,该句柄对应的相关资源(参数 合成文本 实例等)都会被释放,用户不应再使用该句柄
        /// </summary>
        /// <param name="sessionID">session id string to end. 由QTTSSessionBegin返回的句柄</param>
        /// <param name="hints">user hints to end session, hints will be logged to CallLog. 结束本次语音合成的原因描述,为用户自定义内容</param>
        /// <returns>return 0 if success, otherwise return error code. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QTTSSessionEnd(IntPtr sessionID, string hints);
        /// <summary>
        /// set params related with msc.
        /// </summary>
        /// <param name="sessionID">session id of related param, set null to got global param.</param>
        /// <param name="paramName">param name,could pass more than one param split by ','';'or'\n'.</param>
        /// <param name="paramValue">param value buffer, malloced by user.</param>
        /// <returns>return 0 if success, otherwise return errcode.</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QTTSSetParam(IntPtr sessionID, string paramName, byte[] paramValue);
        /// <summary>
        /// get params related with msc. 获取当前语音合成信息,如当前合成音频对应文本结束位置、上行流量、下行流量等
        /// </summary>
        /// <param name="sessionID">session id of related param, set NULL to got global param. 由QTTSSessionBegin返回的句柄,如果为NULL,获取msc的设置信息</param>
        /// <param name="paramName">param name,could pass more than one param split by ','';'or'\n'. 参数名,一次调用只支持查询一个参数</param>
        ///     在线  sid         服务端会话ID 长度为32字节
        ///     在线  upflow      上行数据量
        ///     在线  downflow    下行数据量
        ///     通用  ced         当前合成音频对应文本结束位置
        /// <param name="paramValue">param value buffer, malloced by user.</param>
        ///     输入: buffer首地址
        ///     输出: 向该buffer写入获取到的信息
        /// <param name="valueLen">pass in length of value buffer, and return length of value string</param>
        ///     输入: buffer的大小
        ///     输出: 信息实际长度(不含’\0’)
        /// <returns>return 0 if success, otherwise return errcode. 函数调用成功则其值为MSP_SUCCESS,否则返回错误代码</returns>
        [DllImport("msc_x64", CallingConvention = CallingConvention.StdCall)]
        public static extern int QTTSGetParam(IntPtr sessionID, string paramName, ref byte[] paramValue, ref uint valueLen);
        #endregion
    }

    #region QISR
    /// <summary>
    /// 用来告知msc音频发送是否完成
    /// </summary>
    public enum AudioStatus
    {
        MSP_AUDIO_SAMPLE_INIT = 0x00,
        MSP_AUDIO_SAMPLE_FIRST = 0x01, //第一块音频
        MSP_AUDIO_SAMPLE_CONTINUE = 0x02, //还有后继音频
        MSP_AUDIO_SAMPLE_LAST = 0x04, //最后一块音频
    }
    /// <summary>
    /// 端点检测（End-point detected）器所处的状态
    /// </summary>
    public enum EpStatus
    {
        MSP_EP_LOOKING_FOR_SPEECH = 0,    //还没有检测到音频的前端点
        MSP_EP_IN_SPEECH = 1,    //已经检测到了音频前端点,正在进行正常的音频处理
        MSP_EP_AFTER_SPEECH = 3,    //检测到音频的后端点,后记的音频会被msc忽略
        MSP_EP_TIMEOUT = 4,    //超时
        MSP_EP_ERROR = 5,    //出现错误
        MSP_EP_MAX_SPEECH = 6,    //音频过大
        MSP_EP_IDLE = 7,    // internal state after stop and before start
    }
    /// <summary>
    /// 识别器返回的状态,提醒用户及时开始\停止获取识别结果
    /// </summary>
    public enum RecogStatus
    {
        MSP_REC_STATUS_SUCCESS = 0,    //识别成功,此时用户可以调用QISRGetResult来获取(部分结果)
        MSP_REC_STATUS_NO_MATCH = 1,    //识别结束,没有识别结果
        MSP_REC_STATUS_INCOMPLETE = 2,    //未完成 正在识别中
        MSP_REC_STATUS_NON_SPEECH_DETECTED = 3,
        MSP_REC_STATUS_SPEECH_DETECTED = 4,
        MSP_REC_STATUS_COMPLETE = 5,    //识别结束
        MSP_REC_STATUS_MAX_CPU_TIME = 6,
        MSP_REC_STATUS_MAX_SPEECH = 7,
        MSP_REC_STATUS_STOPPED = 8,
        MSP_REC_STATUS_REJECTED = 9,
        MSP_REC_STATUS_NO_SPEECH_FOUND = 10,
        MSP_REC_STATUS_FAILURE = MSP_REC_STATUS_NO_MATCH,
    }
    #endregion

    #region QTTS
    /// <summary>
    /// 合成状态
    /// </summary>
    public enum SynthStatus
    {
        MSP_TTS_FLAG_CMD_CANCLEED = 0,
        MSP_TTS_FLAG_STILL_HAVE_DATA = 1,   //音频还没获取完 还有后续的音频
        MSP_TTS_FLAG_DATA_END = 2,   //音频已经获取完
    }

    /// <summary>
    /// 语音音频头
    /// </summary>
    public struct WaveHeader
    {
        public int RIFFID;
        public int FileSize;
        public int RIFFType;
        public int FMTID;
        public int FMTSize;
        public short FMTTag;
        public ushort FMTChannel;
        public int FMTSamplesPerSec;
        public int AvgBytesPerSec;
        public ushort BlockAlign;
        public ushort BitsPerSample;
        public int DataID;
        public int DataSize;
    }

    public enum TtsVoice
    {
        XiaoYan = 0,    //讯飞小燕
        XuJiu = 1,    //讯飞许久
        XiaoPing = 2,    //讯飞小萍
        XiaoJing = 3,    //讯飞小婧
        XuXiaoBao = 4,    //讯飞许小宝
    }
    #endregion

    public class WAV
    {
        // convert two bytes to one float in the range -1 to 1
        static float BytesToFloat(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0F;
        }
        static int BytesToInt(byte[] bytes, int offset = 0)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value |= ((int)bytes[offset + i]) << (i * 8);
            }
            return value;
        }
        // properties
        public float[] LeftChannel { get; internal set; }
        public float[] RightChannel { get; internal set; }
        public int ChannelCount { get; internal set; }
        public int SampleCount { get; internal set; }
        public int Frequency { get; internal set; }
        public WAV(byte[] wav)
        {
            // Determine if mono or stereo
            ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels
            // Get the frequency
            Frequency = BytesToInt(wav, 24);
            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16
            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;
            // Pos is now positioned to start of actual sound data.
            SampleCount = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
            if (ChannelCount == 2) SampleCount /= 2;        // 4 bytes per sample (16 bit stereo)
            // Allocate memory (right will be null if only mono sound)
            LeftChannel = new float[SampleCount];
            if (ChannelCount == 2) RightChannel = new float[SampleCount];
            else RightChannel = null;
            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length)
            {
                LeftChannel[i] = BytesToFloat(wav[pos], wav[pos + 1]);
                pos += 2;
                if (ChannelCount == 2)
                {
                    RightChannel[i] = BytesToFloat(wav[pos], wav[pos + 1]);
                    pos += 2;
                }
                i++;
            }
        }
        public override string ToString()
        {
            return string.Format("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", LeftChannel, RightChannel, ChannelCount, SampleCount, Frequency);
        }
    }