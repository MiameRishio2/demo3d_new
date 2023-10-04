namespace GameCreator.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;


    public interface IControl
    {
        void SendInfo(String content);
        void SendInfo2(String content);
    }
}