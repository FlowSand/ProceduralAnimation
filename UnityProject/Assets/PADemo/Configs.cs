using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Configs
{
    //DEBUG开关
    public static bool ON_DEBUG = true;
    public static bool DEV_TOOL_ACTIVE = true;

    //配置信息
    public static bool OpenDebugVisual = false;
    public static bool OpenDebugPath = false;
    public static int Level = 1;

    //屏幕设置
    public static bool IsFullScreen = true;
    public static Vector2 ScreenSize = new Vector2(1360f, 768f);

    //关卡和纹理
    public static string Tentacle_Plant_Level = "TPlantDemo_1";     
    public static string Tentacle_Plant_Config = "tentaclePlants";

    public static string Swing_Crit_Level = "swingDemo_1";   
    public static string Swing_Crit_Config = "swingDemo";

    public static string LEVEL_PATH = Swing_Crit_Level;        
    public static string CONFIG_PATH = Swing_Crit_Config;
}
