using System;
using System.Collections.Generic;
using UnityEngine;
using PASystem;
using System.IO;
//DEL
//功能：测试初始化Demo场景

public class Game : MainLoopProcess
{
    public bool evenUpdate;

    public RoomCamera Camera { get; private set; }
    public Room Room { get; private set; }

    public Game(ProcessManager manager) : base(manager, ProcessManager.ProcessID.Game)
    {
        this.nextIssuedId = UnityEngine.Random.Range(1000, 10000);

        //加载Demo配置文件
        this.LoadDemoConfigs();

        //初始化相机
        this.Camera = new RoomCamera(this);

        //初始化Demo房间
        this.Room = new Room(this);

        //相机聚焦房间
        this.Camera.MoveCamera(Room, 0);

        //根据Demo场景生成对应的生物
        if (Configs.Level == 1)
        {
            TentaclePlant plantDown = new TentaclePlant(new WorldEntity(new WorldCoordinate(26, 26), this.GetNewID()));
            plantDown.PlaceInRoom(Room);
            plantDown.ResetPlant(new Vector2(510, 110), Vector2.up);

            TentaclePlant plantRight = new TentaclePlant(new WorldEntity(new WorldCoordinate(26, 26), this.GetNewID()));
            plantRight.PlaceInRoom(Room);
            plantRight.ResetPlant(new Vector2(850, 350), Vector2.left);

            TentaclePlant plantLeft = new TentaclePlant(new WorldEntity(new WorldCoordinate(26, 26), this.GetNewID()));
            plantLeft.PlaceInRoom(Room);
            plantLeft.ResetPlant(new Vector2(110, 490), Vector2.right);

        }
        else
        {
            DaddyLongLegs daddy = new DaddyLongLegs(new WorldEntity(new WorldCoordinate(26, 26), this.GetNewID()));
            daddy.PlaceInRoom(Room);
            daddy.ResetCreature(new IntVector2(36, 5), Vector2.up);
        }
    }

    public override void RawUpdate(float dt)
    {
        if (Configs.DEV_TOOL_ACTIVE)
        {
            //控制台输出鼠标当前点击位置
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log((Vector2)Input.mousePosition + Camera.pos);
                Debug.Log(Room.GetTile((Vector2)Input.mousePosition + Camera.pos).Terrain);
            }
        }

        //调用基类RawUpdate
        base.RawUpdate(dt);
    }

    public override void Update()
    {
        base.Update();

        if (!this.processActive) return;

        //更新camera
        this.Camera.Update();

        this.evenUpdate = !this.evenUpdate;

        //更新Room
        this.Room.Update();
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        
        if (this.processActive)
        {
            this.Camera.DrawUpdate(timeStacker, this.TimeSpeedFac);
        }
    }

    private void LoadDemoConfigs()
    {
        string settings = Utils.RootFolderDirectory() + "Demo" + Path.DirectorySeparatorChar + "Settings.txt";
        string[] lines = File.ReadAllLines(settings);
        Configs.OpenDebugVisual = bool.Parse(lines[0].Split(':')[1]);
        Configs.OpenDebugPath = bool.Parse(lines[1].Split(':')[1]);
        Configs.Level = int.Parse(lines[2].Split(':')[1]);

        //1号场景：触手植物
        if (Configs.Level == 1)
        {
            Configs.LEVEL_PATH = Configs.Tentacle_Plant_Level;
            Configs.CONFIG_PATH = Configs.Tentacle_Plant_Config;
        }
        //2号场景：多触手章鱼怪
        else if (Configs.Level == 2)
        {
            Configs.LEVEL_PATH = Configs.Swing_Crit_Level;
            Configs.CONFIG_PATH = Configs.Swing_Crit_Config;
        }
    }

    //生物ID产生器
    public int nextIssuedId;
    public EntityID GetNewID()
    {
        ++this.nextIssuedId;
        return new EntityID(this.nextIssuedId);
    }
}
