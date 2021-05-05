using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RoomCamera
{
    public Game game;    

    //相机位置
    public int curCameraPos;
    public Vector2 lastPos;
    public Vector2 pos;
    private Vector2 seekPos;

    //背景纹理
    public FSprite levelGraphic;

    public RoomCamera(Game game)
    {
        this.game = game;

        //初始化相机位置
        room = null;
        pos = new Vector2(0.0f, 0.0f);
        lastPos = pos;

        //FContainer，添加到Futile引擎的Stage
        SpriteLayers = new FContainer[6];
        for (int index = 0; index < SpriteLayers.Length; ++index)
        {
            SpriteLayers[index] = new FContainer();
            Futile.stage.AddChild(SpriteLayers[index]);
        }
        //Sprite层级和索引字典
        SpriteLayerIndex = new Dictionary<string, int>();
        SpriteLayerIndex.Add("Background", 0);
        SpriteLayerIndex.Add("Midground", 1);
        SpriteLayerIndex.Add("Items", 2);
        SpriteLayerIndex.Add("Foreground", 3);
        SpriteLayerIndex.Add("HUD", 4);
        SpriteLayerIndex.Add("HUD2", 5);
        //实例化关卡纹理
        levelGraphic = new FSprite(Configs.LEVEL_PATH, true);
        levelGraphic.anchorX = 0.0f;
        levelGraphic.anchorY = 0.0f;
        ReturnFContainer("Foreground").AddChild(levelGraphic);
    }

    public Room room { get; private set; }

    //根据索引获取房间相机位置
    private Vector2 CamPos(int index)
    {
        return room.cameraPositions[index];
    }

    //游戏屏幕比例
    public Vector2 sSize
    {
        get
        {
            return Configs.ScreenSize;
        }
    }

    public float hDisplace
    {
        get
        {
            return (float)((1400.0 - sSize.x) / 2.0 - 8.0);
        }
    }

    #region 生命周期

    public void Update()
    {
        //如果Room为空则不做后续处理
        if (room == null) return;

        lastPos = pos;

        //Y轴像素 768
        //正常模式：相机位置计算
        seekPos = CamPos(curCameraPos);
        seekPos.x += hDisplace + 8f;
        seekPos.y += 18f;
        pos = Vector2.Lerp(pos, seekPos, 0.1f);

        //坐标限制
        pos.x = Mathf.Clamp(pos.x, (float)(CamPos(curCameraPos).x + hDisplace + 8.0 - 20.0), (float)(CamPos(curCameraPos).x + hDisplace + 8.0 + 20.0));
        pos.y = Mathf.Clamp(pos.y, (float)(CamPos(curCameraPos).y + 8.0 - 7.0), (float)(CamPos(curCameraPos).y + 33.0));
    }

    public void DrawUpdate(float timeStacker, float timeSpeed)
    {
        if (room == null) return;

        //计算上一帧和当前帧的位置差值（timeStacker 一般情况下等于1，所以返回值基本是Pos）
        Vector2 deltPos = Vector2.Lerp(lastPos, pos, timeStacker);

        deltPos.x = Mathf.Clamp(deltPos.x, (float)(CamPos(curCameraPos).x + hDisplace + 8.0 - 20.0), (float)(CamPos(curCameraPos).x + hDisplace + 8.0 + 20.0));
        deltPos.y = Mathf.Clamp(deltPos.y, (float)(CamPos(curCameraPos).y + 8.0 - 7.0 ), (float)(CamPos(curCameraPos).y + 33.0 ));
        levelGraphic.isVisible = true;

        //计算相机位置偏移 
        deltPos = new Vector2(Mathf.Floor(deltPos.x), Mathf.Floor(deltPos.y));
        deltPos.x -= 0.02f;
        deltPos.y -= 0.02f;

        //遍历SpriteLeaser列表  并绘制精灵  需要传入当前相机位置
        for (int index = spriteLeasers.Count - 1; index >= 0; --index)
        {
            spriteLeasers[index].Update(timeStacker, this, deltPos);
            if (spriteLeasers[index].deleteMeNextFrame)
                spriteLeasers.RemoveAt(index);
        }

        levelGraphic.x = CamPos(curCameraPos).x - deltPos.x;
        levelGraphic.y = CamPos(curCameraPos).y - deltPos.y;
    }

    #endregion

    #region 移动相机位置

    //切换新房间，修改相机到指定位置
    public void MoveCamera(Room newRoom, int camPos)
    {
        Debug.Log("Change room. Camera position: " + camPos);

        room = newRoom;

        camPos = (camPos == -1) ? -1 : camPos;

        //应用位置改变

        //修改加载相机位置和加载房间
        if (newRoom != null)
        {
            ChangeRoom(newRoom, camPos);
        }
        curCameraPos = camPos;

        //设置新位置
        seekPos = CamPos(curCameraPos);
        seekPos.x += hDisplace + 8f;
        seekPos.y += 18f;
        pos = seekPos;
        lastPos = seekPos;
    }


    private void ChangeRoom(Room newRoom, int cameraPosition)
    {
        //当前房间不为空，执行清理操作
        if (room != null)
        {
            //清理当前SpriteLeaser引用
            for (int index = 0; index < spriteLeasers.Count; ++index)
                spriteLeasers[index].CleanSpritesAndRemove();
            spriteLeasers.Clear();
            spriteLeasers.TrimExcess();
        }

        room = newRoom;

        //Debug模式添加一个DebugMouse
        if (Configs.DEV_TOOL_ACTIVE)
            room.AddObject(new DebugMouse());

        curCameraPos = cameraPosition;

        for (int index = 0; index < room.drawableObjects.Count; ++index)
        {
            NewObjectInRoom(room.drawableObjects[index]);
        }     
    }

    #endregion

    #region FContainer管理

    //FContainer
    private List<SpriteLeaser> spriteLeasers = new List<SpriteLeaser>();    //Sprite持有者列表
    private FContainer[] SpriteLayers;
    private Dictionary<string, int> SpriteLayerIndex;

    public void NewObjectInRoom(IDrawable obj)
    {
        spriteLeasers.Add(new SpriteLeaser(obj, this));
    }

    //根据层级名返回FContainer
    public FContainer ReturnFContainer(string layerName)
    {
        return SpriteLayers[SpriteLayerIndex[layerName]];
    }

    public void MoveObjectToContainer(IDrawable obj, FContainer container)
    {
        foreach (SpriteLeaser sl in spriteLeasers)
        {
            if (sl.drawableObject == obj)
            {
                sl.AddSpritesToContainer(container, this);
                break;
            }
        }
    }

    public class SpriteLeaser
    {
        public IDrawable drawableObject;
        public FSprite[] sprites;
        public bool deleteMeNextFrame;
        public FContainer[] containers;

        public SpriteLeaser(IDrawable obj, RoomCamera rCam)
        {
            drawableObject = obj;
            drawableObject.InitiateSprites(this, rCam);
        }

        public void Update(float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            drawableObject.DrawSprites(this, rCam, timeStacker, camPos);
        }

        public void CleanSpritesAndRemove()
        {
            deleteMeNextFrame = true;
            RemoveAllSpritesFromContainer();
        }

        //清除Sprite和Container引用
        public void RemoveAllSpritesFromContainer()
        {
            for (int index = 0; index < sprites.Length; ++index)
                sprites[index].RemoveFromContainer();
            if (containers == null)
                return;
            for (int index = 0; index < containers.Length; ++index)
                containers[index].RemoveFromContainer();
        }

        public void AddSpritesToContainer(FContainer newContainer, RoomCamera rCam)
        {
            drawableObject.AddToContainer(this, rCam, newContainer);
        }
    }

    #endregion
}
