using System;
using System.Collections.Generic;

//페이지 1장의 구성 데이터
[Serializable]
public class TrainingPageInfo
{
    public string pageTitle;
    public List<TrainingButtonData> buttons = new List<TrainingButtonData>();
}