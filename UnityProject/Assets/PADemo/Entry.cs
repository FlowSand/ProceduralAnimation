using System.Collections.Generic;
using UnityEngine;

public class Entry : MonoBehaviour
{
    //模块管理器
    public ProcessManager processManager;

    //游戏纹理
    private Texture2D tentacleRoom;
    private Texture2D swingRoom;

    //主循环入口
    private void Update()
    {
        float dt = Time.deltaTime;
        processManager.Update(dt);
    }

    private void Start()
    {
        //初始化屏幕设置参数
        Screen.SetResolution((int)Configs.ScreenSize.x, (int)Configs.ScreenSize.y, false);
        Screen.fullScreen = Configs.IsFullScreen;
        Cursor.visible = true;

        //初始化Futile引擎
        FutileParams fParams = new FutileParams(true, true, true, true);
        fParams.AddResolutionLevel(Configs.ScreenSize.x, 1f, 1f, string.Empty);
        fParams.origin = new Vector2(0.0f, 0.0f);
        Futile.instance.Init(fParams);
        Futile.displayScale = 1f;

        //加载资源    
        this.LoadResources();       

        //初始化进程管理器
        this.processManager = new ProcessManager(); 
    }

    private void LoadResources()
    {
        this.tentacleRoom = Resources.Load(Configs.Tentacle_Plant_Level) as Texture2D;
        Futile.atlasManager.LoadAtlasFromTexture(Configs.Tentacle_Plant_Level, this.tentacleRoom);
        this.swingRoom = Resources.Load(Configs.Swing_Crit_Level) as Texture2D;
        Futile.atlasManager.LoadAtlasFromTexture(Configs.Swing_Crit_Level, this.swingRoom);
        Futile.atlasManager.LoadAtlas("Atlases/rainWorld");
        Futile.atlasManager.LoadAtlas("Atlases/fontAtlas");
        Futile.atlasManager.LoadFont("font", "font", "Atlases/font", 0.0f, 0.0f);
    }
}
