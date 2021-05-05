using PASystem;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;

public class Room
{
    #region Room属性

    public Game game;          

    public string Name;             //房间名字
    public Texture2D BGTexture;     //背景纹理贴图

    public float gravity = 1f;          //重力系数

    public Vector2[] cameraPositions;   //摄像机位置列表

    //per tile = 20 * pixel
    private int Width;
    private int Height;

    //以Tile为单位的宽度
    public int TileWidth
    {
        get
        {
            return this.Width;
        }
    }
    public int TileHeight
    {
        get
        {
            return this.Height;
        }
    }

    //以像素为单位的宽度
    public float PixelWidth
    {
        get
        {
            return Width * 20f;
        }
    }
    public float PixelHeight
    {
        get
        {
            return Height * 20f;
        }
    }

    //检测坐标是否在房间边界内
    public bool IsPositionInsideBoundries(IntVector2 pos)
    {
        return pos.x >= 0 && pos.x < this.TileWidth && pos.y >= 0 && pos.y < this.TileHeight;
    }

    //房间边界Rect（原点为0,0）
    public FloatRect RoomRect
    {
        get
        {
            return new FloatRect(0f, 0f, this.PixelWidth, this.PixelHeight);
        }
    }

    #endregion

    #region 生命周期

    //构造函数
    public Room(Game game)
    {
        this.game = game;

        this.physicalObjects = new List<PhysicalObject>[3];
        for (int i = 0; i < this.physicalObjects.Length; i++)
        {
            this.physicalObjects[i] = new List<PhysicalObject>();
        }
        this.drawableObjects = new List<IDrawable>();
        this.updateList = new List<UpdatableAndDeletable>();

        this.LoadRoomConfig();
    }

    public void Update()
    {
        if (this.game == null) return;

        //遍历并更新整个UpdatableAndDeletable 列表
        this.updateIndex = this.updateList.Count - 1;
        while (this.updateIndex >= 0)
        {
            UpdatableAndDeletable updateObj = this.updateList[this.updateIndex];
            if (updateObj.slatedForDeletetion || updateObj.room != this)
            {
                //准备删除 or 不属于本房间
                this.CleanOutObjectNotInThisRoom(updateObj);
            }
            else
            {
                //调用抽象接口Update
                updateObj.Update(this.game.evenUpdate);

                if (updateObj.slatedForDeletetion || updateObj.room != this)
                {
                    //有可能Update之后信息发生变化，因此需要再次检测
                    this.CleanOutObjectNotInThisRoom(updateObj);
                }
                else if (updateObj is PhysicalObject)
                {
                    //如果是物理对象
                    if ((updateObj as PhysicalObject).graphicsModule != null)
                    {
                        //表现层更新
                        (updateObj as PhysicalObject).graphicsModule.Update();
                        //变现层更新后  更新
                        (updateObj as PhysicalObject).GraphicsModuleUpdated(true, this.game.evenUpdate);
                    }
                    else
                    {
                        (updateObj as PhysicalObject).GraphicsModuleUpdated(false, this.game.evenUpdate);
                    }
                }
            }
            this.updateIndex--;
        }
        this.updateIndex = int.MaxValue;

        //物理碰撞检测
        this.CheckPhycicalCollision();
    }

    //检测物理对象碰撞
    private void CheckPhycicalCollision()
    {
        //遍历物理层 物理对象列表：检测两两之间的关系
        for (int j = 1; j < this.physicalObjects.Length; j++)
        {
            //遍历该层所有物理对象
            for (int k = 0; k < this.physicalObjects[j].Count; k++)
            {
                for (int l = k + 1; l < this.physicalObjects[j].Count; l++)
                {
                    //如果两物理对象之间的距离小于其碰撞半径之和
                    if (Mathf.Abs(this.physicalObjects[j][k].bodyChunks[0].pos.x - this.physicalObjects[j][l].bodyChunks[0].pos.x) < this.physicalObjects[j][k].collisionRange + this.physicalObjects[j][l].collisionRange
                        && Mathf.Abs(this.physicalObjects[j][k].bodyChunks[0].pos.y - this.physicalObjects[j][l].bodyChunks[0].pos.y) < this.physicalObjects[j][k].collisionRange + this.physicalObjects[j][l].collisionRange)
                    {
                        bool contacted = false;
                        //遍历两对象的BodyChunk
                        for (int index1 = 0; index1 < this.physicalObjects[j][k].bodyChunks.Length; index1++)
                        {
                            for (int index2 = 0; index2 < this.physicalObjects[j][l].bodyChunks.Length; index2++)
                            {
                                //如果两BodyChunk存在碰撞
                                if (this.physicalObjects[j][k].bodyChunks[index1].collideWithObjects && this.physicalObjects[j][l].bodyChunks[index2].collideWithObjects 
                                    && Utils.DistLess(this.physicalObjects[j][k].bodyChunks[index1].pos, this.physicalObjects[j][l].bodyChunks[index2].pos, this.physicalObjects[j][k].bodyChunks[index1].rad + this.physicalObjects[j][l].bodyChunks[index2].rad))
                                {
                                    float radSum = this.physicalObjects[j][k].bodyChunks[index1].rad + this.physicalObjects[j][l].bodyChunks[index2].rad;
                                    float dist = Vector2.Distance(this.physicalObjects[j][k].bodyChunks[index1].pos, this.physicalObjects[j][l].bodyChunks[index2].pos);
                                    Vector2 dir = Utils.DirVec(this.physicalObjects[j][k].bodyChunks[index1].pos, this.physicalObjects[j][l].bodyChunks[index2].pos);
                                    float massRation = this.physicalObjects[j][l].bodyChunks[index2].mass / (this.physicalObjects[j][k].bodyChunks[index1].mass + this.physicalObjects[j][l].bodyChunks[index2].mass);
                                    //更新两BodyChunk的速度和位置属性
                                    this.physicalObjects[j][k].bodyChunks[index1].pos -= (radSum - dist) * dir * massRation;
                                    this.physicalObjects[j][k].bodyChunks[index1].vel -= (radSum - dist) * dir * massRation;
                                    this.physicalObjects[j][l].bodyChunks[index2].pos += (radSum - dist) * dir * (1f - massRation);
                                    this.physicalObjects[j][l].bodyChunks[index2].vel += (radSum - dist) * dir * (1f - massRation);
                                    //相等时随机抖动
                                    if (this.physicalObjects[j][k].bodyChunks[index1].pos.x == this.physicalObjects[j][l].bodyChunks[index2].pos.x)
                                    {
                                        this.physicalObjects[j][k].bodyChunks[index1].vel += Utils.DegToVec(Random.value * 360f) * 0.0001f;
                                        this.physicalObjects[j][l].bodyChunks[index2].vel += Utils.DegToVec(Random.value * 360f) * 0.0001f;
                                    }
                                    //记录一次碰撞
                                    if (!contacted)
                                    {
                                        this.physicalObjects[j][k].Collide(this.physicalObjects[j][l], index1, index2);
                                        this.physicalObjects[j][l].Collide(this.physicalObjects[j][k], index2, index1);
                                    }
                                    contacted = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region 抽象Object管理

    //物理对象
    public List<PhysicalObject>[] physicalObjects;
    //UpdateObj
    private List<UpdatableAndDeletable> updateList;
    //Drawable
    public List<IDrawable> drawableObjects;

    private int updateIndex = int.MaxValue;

    //TODO 优化
    public void AddObject(UpdatableAndDeletable obj)
    {
        if (this.game == null) return;

        Debug.Log("添加列表管理");
        this.updateList.Add(obj);
        obj.room = this;

        IDrawable drawable = null;
        if (obj is IDrawable)
        {
            drawable = (obj as IDrawable);
        }

        //添加物理对象管理
        PhysicalObject phyObj = null;
        if (obj is PhysicalObject)
        {
            phyObj = obj as PhysicalObject;
            this.physicalObjects[phyObj.collisionLayer].Add(phyObj);
            if (phyObj.graphicsModule != null)
            {
                drawable = phyObj.graphicsModule;
            }
            else if (true)
            //else if (this.BeingViewed)
            {
                phyObj.InitiateGraphicsModule();
                if (phyObj.graphicsModule != null)
                {
                    drawable = phyObj.graphicsModule;
                }
            }
        }
        //添加drawable
        if (drawable != null)
        {
            this.drawableObjects.Add(drawable);
            if (this.game.Camera.room == this)
            {
                this.game.Camera.NewObjectInRoom(drawable);
            }
        }
    }

    //销毁指定ID的Obj
    public void DestroyObject(EntityID ID)
    {
        for (int i = 0; i < this.updateList.Count; i++)
        {
            //  物理对象 && ID相等
            if (this.updateList[i] is PhysicalObject && (this.updateList[i] as PhysicalObject).entity.ID == ID)
            {
                this.updateList[i].Destroy();
                return;
            }
        }
    }

    public void RemoveObject(UpdatableAndDeletable obj)
    {
        //移除房间引用
        if (obj.room == this)
        {
            obj.RemoveFromRoom();
        }
        //更新UpdateLst
        if (this.updateList.IndexOf(obj) > this.updateIndex)
        {
            this.CleanOutObjectNotInThisRoom(obj);
        }
    }

    private void CleanOutObjectNotInThisRoom(UpdatableAndDeletable obj)
    {
        //移除UpdateLst
        this.updateList.Remove(obj);
        //移除Drawable引用
        if (obj is IDrawable)
        {
            this.drawableObjects.Remove(obj as IDrawable);
        }

        //移除物理对象
        if (obj is PhysicalObject)
        {
            this.physicalObjects[(obj as PhysicalObject).collisionLayer].Remove(obj as PhysicalObject);
            if ((obj as PhysicalObject).graphicsModule != null)
            {
                this.drawableObjects.Remove((obj as PhysicalObject).graphicsModule);
            }
        }
        this.RemoveObject(obj);
    }
    
    //为物理对象更换碰撞层
    public void ChangeCollisionLayerForObject(PhysicalObject obj, int newLayer)
    {
        int collisionLayer = obj.collisionLayer;
        if (collisionLayer == newLayer)
        {
            return;
        }
        this.physicalObjects[collisionLayer].Remove(obj);
        this.physicalObjects[newLayer].Add(obj);
        obj.collisionLayer = newLayer;
    }

    #endregion

    #region Tile

    private Tile[,] Tiles;

    //计算Vec2所位于TilePos
    public IntVector2 GetTilePosition(Vector2 pos)
    {
        //（10,10）计算得（0,0）
        return new IntVector2((int)((pos.x + 20f) / 20f) - 1, (int)((pos.y + 20f) / 20f) - 1);
    }

    //Tile地格中心点
    public Vector2 MiddleOfTile(int x, int y)
    {
        return new Vector2(10f + x * 20f, 10f + y * 20f);
    }
    public Vector2 MiddleOfTile(IntVector2 pos)
    {
        return this.MiddleOfTile(pos.x, pos.y);
    }
    public Vector2 MiddleOfTile(Vector2 pos)
    {
        return this.MiddleOfTile(this.GetTilePosition(pos));
    }

    public FloatRect TileRect(IntVector2 pos)
    {
        return FloatRect.MakeFromVector2(this.MiddleOfTile(pos) - new Vector2(10f, 10f), this.MiddleOfTile(pos) + new Vector2(10f, 10f));
    }

    public Tile GetTile(Vector2 pos)
    {
        return this.GetTile(this.GetTilePosition(pos));
    }

    //获取目标位置Tile属性
    public Tile GetTile(IntVector2 pos)
    {
        return this.GetTile(pos.x, pos.y);
    }

    public Tile GetTile(WorldCoordinate pos)
    {
        return this.GetTile(pos.x, pos.y);
    }

    public Tile GetTile(int x, int y)
    {
        if (x > -1 && y > -1 && x < this.Tiles.GetLength(0) && y < this.Tiles.GetLength(1))
        {
            return this.Tiles[x, y];
        }

        return this.Tiles[Utils.IntClamp(x, 0, this.Width - 1), Utils.IntClamp(y, 0, this.Height - 1)];
    }

    //判断该Tile 四个Corner的地形属性是否为Solid
    public bool IsCornerFree(int x, int y, FloatRect.CornerLabel corner)
    {
        switch (corner)
        {
            case FloatRect.CornerLabel.A:
                return !this.GetTile(x - 1, y).Solid && !this.GetTile(x - 1, y + 1).Solid && !this.GetTile(x, y + 1).Solid;
            case FloatRect.CornerLabel.B:
                return !this.GetTile(x + 1, y).Solid && !this.GetTile(x + 1, y + 1).Solid && !this.GetTile(x, y + 1).Solid;
            case FloatRect.CornerLabel.C:
                return !this.GetTile(x + 1, y).Solid && !this.GetTile(x + 1, y - 1).Solid && !this.GetTile(x, y - 1).Solid;
            case FloatRect.CornerLabel.D:
                return !this.GetTile(x - 1, y).Solid && !this.GetTile(x - 1, y - 1).Solid && !this.GetTile(x, y - 1).Solid;
            default:
                return false;
        }
    }
    public bool IsCornerFree(int x, int y, int corner)
    {
        switch (corner)
        {
            case 0:
                return !this.GetTile(x - 1, y).Solid && !this.GetTile(x - 1, y + 1).Solid && !this.GetTile(x, y + 1).Solid;
            case 1:
                return !this.GetTile(x + 1, y).Solid && !this.GetTile(x + 1, y + 1).Solid && !this.GetTile(x, y + 1).Solid;
            case 2:
                return !this.GetTile(x + 1, y).Solid && !this.GetTile(x + 1, y - 1).Solid && !this.GetTile(x, y - 1).Solid;
            case 3:
                return !this.GetTile(x - 1, y).Solid && !this.GetTile(x - 1, y - 1).Solid && !this.GetTile(x, y - 1).Solid;
            default:
                return false;
        }
    }

    //返回两坐标间所有Tile坐标
    public List<IntVector2> RayTraceTilesList(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int x = x0;
        int y = y0;
        int sum = 1 + dx + dy;
        int xStep = (x1 <= x0) ? -1 : 1;
        int yStep = (y1 <= y0) ? -1 : 1;
        int sign = dx - dy;
        dx *= 2;
        dy *= 2;
        List<IntVector2> vecLst = new List<IntVector2>();
        while (sum > 0)
        {
            vecLst.Add(new IntVector2(x, y));
            if (sign > 0)
            {
                x += xStep;
                sign -= dy;
            }
            else
            {
                y += yStep;
                sign += dx;
            }
            sum--;
        }
        return vecLst;
    }
    //检测两坐标之间是否直线可达（没有Solid地形Tile）
    public bool RayTraceTilesForTerrain(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int x = x0;
        int y = y0;
        int sum = 1 + dx + dy;
        int xStep = (x1 <= x0) ? -1 : 1;
        int yStep = (y1 <= y0) ? -1 : 1;
        int sign = dx - dy;
        dx *= 2;
        dy *= 2;
        while (sum > 0)
        {
            if (this.GetTile(x, y).Solid)
            {
                return false;
            }
            if (sign > 0)
            {
                x += xStep;
                sign -= dy;
            }
            else
            {
                y += yStep;
                sign += dx;
            }
            sum--;
        }
        return true;
    }

    //判断两点是否视觉可见（Tile之间无Solid地形）
    public bool VisualContact(IntVector2 a, IntVector2 b)
    {
        return this.VisualContact(this.MiddleOfTile(a), this.MiddleOfTile(b));
    }
    public bool VisualContact(Vector2 a, Vector2 b)
    {
        //如果两点地形都是Solid则False
        if (this.GetTile(a).Solid || this.GetTile(b).Solid)
        {
            return false;
        }
        float dist = Vector2.Distance(a, b);
        //插值遍历两点之间的Tile地形类型
        for (float i = 20f; i < dist; i += 20f)
        {
            if (this.GetTile(Vector2.Lerp(a, b, i / dist)).Solid)
            {
                return false;
            }
        }
        return true;
    }

    //判断当前Pos的斜坡类型
    public SlopeDirection IdentifySlope(Vector2 pos)
    {
        return this.IdentifySlope(this.GetTilePosition(pos));
    }
    public SlopeDirection IdentifySlope(int X, int Y)
    {
        return this.IdentifySlope(new IntVector2(X, Y));
    }
    public SlopeDirection IdentifySlope(IntVector2 pos)
    {
        if (this.GetTile(pos.x, pos.y).Terrain == Tile.TerrainType.Slope)
        {
            if (this.GetTile(pos.x - 1, pos.y).Terrain == Tile.TerrainType.Solid)
            {
                //左、下方为Solid
                if (this.GetTile(pos.x, pos.y - 1).Terrain == Tile.TerrainType.Solid)
                {
                    return SlopeDirection.UpRight;
                }
                //左、上方为Solid
                if (this.GetTile(pos.x, pos.y + 1).Terrain == Tile.TerrainType.Solid)
                {
                    return SlopeDirection.DownRight;
                }
            }
            else if (this.GetTile(pos.x + 1, pos.y).Terrain == Tile.TerrainType.Solid)
            {
                if (this.GetTile(pos.x, pos.y - 1).Terrain == Tile.TerrainType.Solid)
                {
                    return SlopeDirection.UpLeft;
                }
                if (this.GetTile(pos.x, pos.y + 1).Terrain == Tile.TerrainType.Solid)
                {
                    return SlopeDirection.DownLeft;
                }
            }
        }
        return SlopeDirection.Broken;
    }

    //Tile数据类型定义
    public class Tile
    {
        public Tile(int x, int y, TerrainType tType, bool vBeam, bool hBeam, bool wbhnd)
        {
            this.X = x;
            this.Y = y;
            this.verticalBeam = vBeam;
            this.horizontalBeam = hBeam;
            this.Terrain = tType;
            this.wallbehind = wbhnd;
        }

        public TerrainType Terrain { get; set; }

        public bool AnyBeam
        {
            get
            {
                return this.verticalBeam || this.horizontalBeam;
            }
        }

        public bool Solid
        {
            get
            {
                return this.Terrain == TerrainType.Solid;
            }
        }

        public bool verticalBeam;

        public bool horizontalBeam;

        public bool wallbehind;

        public int X;

        public int Y;

        //地形枚举
        public enum TerrainType
        {
            Air,        //空气
            Solid,      //固体
            Slope,      //斜坡
            Floor,      //地板
            ShortcutEntrance
        }
    }

    //斜坡类型
    public enum SlopeDirection
    {
        UpLeft,
        UpRight,
        DownLeft,
        DownRight,
        Broken
    }

    #endregion

    #region 配置加载流程

    //加载Room文本配置
    private void LoadRoomConfig()
    {
        string roomConfig = Utils.RootFolderDirectory() +"Demo"+ Path.DirectorySeparatorChar + Configs.CONFIG_PATH+".txt";
        string[] lines = File.ReadAllLines(roomConfig);
        this.LoadFromDataString(lines);
    }

    private static Dictionary<string, int> TilePropMap = new Dictionary<string, int>()
    {
        {"1",0 },{"2",1 },{"6",7 }
    };

    //解析数据流并加载Room数据
    private  void LoadFromDataString(string[] lines)
    {
        //第一行：房间名
        this.Name = lines[0];

        //1.房间宽度*高度
        //第2行以“|”分割符，第一个元素代表房间以Tile为单位的宽 * 高
        string[] array = lines[1].Split('|' )[0].Split( '*' );
        this.Width = Convert.ToInt32(array[0]);
        this.Height = Convert.ToInt32(array[1]);

        //2.房间相机位置
        //第4行以“|”分割符，代表房间的摄像机位置
        string[] array3 = lines[3].Split('|' );
        this.cameraPositions = new Vector2[array3.Length];
        for (int i = 0; i < array3.Length; i++)
        {
            this.cameraPositions[i] = new Vector2(Convert.ToSingle(array3[i].Split(',' )[0]), -(800f - Height * 20f + Convert.ToSingle(array3[i].Split(',' )[1])));
        }

        //3.加载Tile配置
        //第12行数据存储房间Tile定义：Solid、Slop、等等
        this.Tiles = new Tile[this.Width, this.Height];
        for (int x = 0; x < this.Width; x++)
        {
            for (int y = 0; y < this.Height; y++)
            {
                this.Tiles[x, y] = new Tile(x, y, Tile.TerrainType.Air, false, false, false);
            }
        }

        int key = -1;
        IntVector2 tileVec = new IntVector2(0, this.Height - 1);
        string[] array11 = lines[11].Split( '|' );
        for (int m = 0; m < array11.Length - 1; m++)
        {
            //首位代表地形元素
            string[] tileStr = array11[m].Split( ',' );
            this.Tiles[tileVec.x, tileVec.y].Terrain = (Tile.TerrainType)int.Parse(tileStr[0]);
            //检查剩余Tile属性
            for (int n = 1; n < tileStr.Length; n++)
            {
                string text = tileStr[n];
                if (text != null)
                {
                    if (TilePropMap.TryGetValue(text, out key))
                    {
                        switch (key)
                        {
                            case 0:
                                this.Tiles[tileVec.x, tileVec.y].verticalBeam = true;
                                break;
                            case 1:
                                this.Tiles[tileVec.x, tileVec.y].horizontalBeam = true;
                                break;
                            case 7:
                                this.Tiles[tileVec.x, tileVec.y].wallbehind = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            //左上为原点，从上到下，从左到右初始化Tile
            tileVec.y--;
            if (tileVec.y< 0)
            {
                tileVec.x++;
                tileVec.y = this.Height - 1;
            }
        }
    }

    #endregion
}
