using System.Collections.Generic;
using UnityEngine;

//生物身体肢节块

public class BodyChunk
{
    private static int MaxRepeats = 1000;     //碰撞检测次数阈值

    public Vector2 pos;             //位置
    public Vector2 lastPos;
    public Vector2 lastLastPos;
    public Vector2? setPos;
    public Vector2 vel;             //速度
    public float rad;               //碰撞半径
    public float terrainSqueeze;    //地形挤压值
    private float slopeRad;         //斜坡碰撞半径

    public float mass;                  //质量
    public BodyChunk rotationChunk;     //逆向运动学：驱动本Chunk旋转的上一级Chunk
    public bool collideWithObjects = true;  //是否与其他物理对象碰撞
    public bool collideWithTerrain;     //是否与地形碰撞
    public bool collideWithSlopes;      //是否与斜坡碰撞

    public int index;       //Chunk索引

    private IntVector2 contactPoint;
    public IntVector2 lastContactPoint;

    //初始化参数
    public BodyChunk(PhysicalObject owner, int index, Vector2 pos, float rad, float mass)
    {
        this.owner = owner;
        this.index = index;
        this.pos = pos;
        lastPos = pos;
        lastLastPos = pos;
        vel = new Vector2(0f, 0f);
        this.rad = rad;
        this.mass = mass;

        contactPoint = new IntVector2(0, 0);
        onSlope = 0;
        terrainSqueeze = 1f;
        collideWithTerrain = true;
        goThroughFloors = false;
        collideWithSlopes = true;
    }

    public PhysicalObject owner { get; private set; }

    //地形半径
    private float TerrainRad
    {
        get
        {
            return Mathf.Max(rad * terrainSqueeze, 1f);
        }
    }

    //当前Chunk相对于前一Chunk的旋转
    public Vector2 Rotation
    {
        get
        {
            if (rotationChunk == null)
                return new Vector2(0f, 1f);
            return (pos - rotationChunk.pos).normalized;
        }
    }

    public IntVector2 ContactPoint
    {
        get
        {
            return contactPoint;
        }
    }

    //是否正在穿越地板
    public bool goThroughFloors { get; set; }

    public int onSlope { get; private set; }

    public void Update()
    {
        //检测X、Y有效性
        if (float.IsNaN(vel.y))
        {
            Debug.Log("VELY IS NAN");
            vel.y = 0.0f;
        }
        if (float.IsNaN(vel.x))
        {
            Debug.Log("VELX IS NAN");
            vel.x = 0.0f;
        }

        //计算位置变化
        vel.y -= owner.gravity;
        vel *= owner.airFriction;
        lastLastPos = lastPos;
        lastPos = pos;

        //如果SetPos有值，则直接修改Pos到目标位置，否则Pos递增当前速度值
        if (setPos.HasValue)
        {
            pos = setPos.Value;
            setPos = new Vector2?();
        }
        else
        {
            pos += vel;
        }

        //地形碰撞检测
        onSlope = 0;
        slopeRad = TerrainRad;
        lastContactPoint = contactPoint;
        if (collideWithTerrain)
        {
            CheckVerticalCollision();
            if (collideWithSlopes)
                CheckAgainstSlopesVertically();
            CheckHorizontalCollision();
        }
        else
        {
            contactPoint.x = 0;
            contactPoint.y = 0;
        }
    }

    //检测水平碰撞：计算速度、位置和ContactPoint
    private void CheckHorizontalCollision()
    {
        contactPoint.x = 0;
        //获取上一帧TilePos
        IntVector2 tileLastPos = owner.room.GetTilePosition(lastPos);
        int collideNum = 0;
        //如果水平速度大于0,从左向右移动
        if (vel.x > 0)
        {
            //从左向右，从下到下遍历检测
            int x1 = owner.room.GetTilePosition(new Vector2((pos.x + TerrainRad), 0.0f)).x;
            int x2 = owner.room.GetTilePosition(new Vector2(lastPos.x + TerrainRad, 0.0f)).x;
            int y1 = owner.room.GetTilePosition(new Vector2(0.0f, (float)(pos.y + TerrainRad - 1.0))).y;
            int y2 = owner.room.GetTilePosition(new Vector2(0.0f, (float)(pos.y - TerrainRad + 1.0))).y;
            bool contacted = false;
            for (int x = x2; x <= x1 && !contacted; ++x)
            {
                for (int y = y2; y <= y1 && !contacted; ++y)
                {
                    //检测是否满足条件
                    if (owner.room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid && owner.room.GetTile(x - 1, y).Terrain != Room.Tile.TerrainType.Solid
                        && (tileLastPos.x < x || owner.room.GetTile(lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        pos.x = x * 20f - TerrainRad;
                        //产生地形碰撞影响
                        if (vel.x > PhysicalObject.impactTreshhold)
                            owner.TerrainImpact(index, new IntVector2(1, 0), Mathf.Abs(vel.x), lastContactPoint.x < 1);
                        contactPoint.x = 1;
                        //水平速度计算反弹，小于阈值则为0
                        vel.x = -Mathf.Abs(vel.x) * owner.bounce;
                        if (Mathf.Abs(vel.x) < 1.0 + 9.0 * (1.0 - owner.bounce))
                            vel.x = 0.0f;
                        //垂直速度计算计算表面摩擦力
                        vel.y *= Mathf.Clamp(owner.surfaceFriction * 2f, 0.0f, 1f);
                        //设置碰撞标志位
                        contacted = true;
                    }
                    //碰撞超限计数
                    ++collideNum;
                    if (collideNum > BodyChunk.MaxRepeats)
                    {
                        Debug.Log("!!!!! " + owner + " emergency breakout of terrain check!");
                        contacted = true;
                    }
                }
            }
        }
        //如果水平速度小于0,从右向左移动
        else if (vel.x < 0.0)
        {
            //从右向左，从下到上遍历
            int x1 = owner.room.GetTilePosition(new Vector2(pos.x - TerrainRad, 0.0f)).x;
            int x2 = owner.room.GetTilePosition(new Vector2(lastPos.x - TerrainRad, 0.0f)).x;
            int y1 = owner.room.GetTilePosition(new Vector2(0.0f, (float)(pos.y + TerrainRad - 1.0))).y;
            int y2 = owner.room.GetTilePosition(new Vector2(0.0f, (float)(pos.y - TerrainRad + 1.0))).y;
            bool @checked = false;
            for (int x = x2; x >= x1 && !@checked; --x)
            {
                for (int y = y2; y <= y1 && !@checked; ++y)
                {
                    if (owner.room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid && owner.room.GetTile(x + 1, y).Terrain != Room.Tile.TerrainType.Solid
                        && (tileLastPos.x > x || owner.room.GetTile(lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        //产生地形碰撞效果
                        pos.x = (float)((x + 1.0) * 20.0) + TerrainRad;
                        if (vel.x < -(double)PhysicalObject.impactTreshhold)
                            owner.TerrainImpact(index, new IntVector2(-1, 0), Mathf.Abs(vel.x), lastContactPoint.x > -1);
                        contactPoint.x = -1;
                        //根据弹力计算水平速度，小于阈值则为0
                        vel.x = Mathf.Abs(vel.x) * owner.bounce;
                        if (Mathf.Abs(vel.x) < 1.0 + 9.0 * (1.0 - owner.bounce))
                            vel.x = 0.0f;
                        //根据表面摩擦力修改垂直速度
                        vel.y *= Mathf.Clamp(owner.surfaceFriction * 2f, 0.0f, 1f);
                        @checked = true;
                    }
                    //碰撞计数检测
                    ++collideNum;
                    if (collideNum > BodyChunk.MaxRepeats)
                    {
                        Debug.Log("!!!!! " + owner + " emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
    }

    //检测垂直碰撞：检测方式与水平同理
    private void CheckVerticalCollision()
    {
        contactPoint.y = 0;
        IntVector2 tilePosition = owner.room.GetTilePosition(lastPos);
        int collideNum = 0;
        if (vel.y > 0.0)
        {
            int y1 = owner.room.GetTilePosition(new Vector2(0.0f, pos.y + TerrainRad)).y;
            int y2 = owner.room.GetTilePosition(new Vector2(0.0f, lastPos.y + TerrainRad)).y;
            int x1 = owner.room.GetTilePosition(new Vector2((float)(pos.x - TerrainRad + 1.0), 0.0f)).x;
            int x2 = owner.room.GetTilePosition(new Vector2((float)(pos.x + TerrainRad - 1.0), 0.0f)).x;
            bool @checked = false;
            for (int y = y2; y <= y1 && !@checked; ++y)
            {
                for (int x = x1; x <= x2 && !@checked; ++x)
                {
                    if (owner.room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid && owner.room.GetTile(x, y - 1).Terrain != Room.Tile.TerrainType.Solid && (tilePosition.y < y || owner.room.GetTile(lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        pos.y = y * 20f - TerrainRad;
                        if (vel.y > (double)PhysicalObject.impactTreshhold)
                            owner.TerrainImpact(index, new IntVector2(0, 1), Mathf.Abs(vel.y), lastContactPoint.y < 1);
                        contactPoint.y = 1;
                        vel.y = -Mathf.Abs(vel.y) * owner.bounce;
                        if (Mathf.Abs(vel.y) < 1.0 + 9.0 * (1.0 - owner.bounce))
                            vel.y = 0.0f;
                        vel.x *= Mathf.Clamp(owner.surfaceFriction * 2f, 0.0f, 1f);
                        @checked = true;
                    }
                    ++collideNum;
                    if (collideNum > BodyChunk.MaxRepeats)
                    {
                        Debug.Log("!!!!! " + owner + " emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
        else
        {
            if (vel.y >= 0.0)
                return;
            int y1 = owner.room.GetTilePosition(new Vector2(0.0f, pos.y - TerrainRad)).y;
            int y2 = owner.room.GetTilePosition(new Vector2(0.0f, lastPos.y - TerrainRad)).y;
            int x1 = owner.room.GetTilePosition(new Vector2((float)(pos.x - TerrainRad + 1.0), 0.0f)).x;
            int x2 = owner.room.GetTilePosition(new Vector2((float)(pos.x + TerrainRad - 1.0), 0.0f)).x;
            bool @checked = false;
            for (int y = y2; y >= y1 && !@checked; --y)
            {
                for (int x = x1; x <= x2 && !@checked; ++x)
                {
                    if (SolidFloor(x, y) && !SolidFloor(x, y + 1) && (tilePosition.y > y || owner.room.GetTile(lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        pos.y = (float)((y + 1.0) * 20.0) + TerrainRad;
                        if (vel.y < -PhysicalObject.impactTreshhold)
                            owner.TerrainImpact(index, new IntVector2(0, -1), Mathf.Abs(vel.y), lastContactPoint.y > -1);
                        contactPoint.y = -1;
                        vel.y = Mathf.Abs(vel.y) * owner.bounce;
                        if (vel.y < owner.gravity || vel.y < 1.0 + 9.0 * (1.0 - owner.bounce))
                            vel.y = 0.0f;
                        vel.x *= Mathf.Clamp(owner.surfaceFriction * 2f, 0.0f, 1f);
                        @checked = true;
                    }
                    ++collideNum;
                    if (collideNum > BodyChunk.MaxRepeats)
                    {
                        Debug.Log("!!!!! " + owner + " emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
    }

    private void CheckAgainstSlopesVertically()
    {
        //获取当前Pos对应的Tile
        IntVector2 tilePos = owner.room.GetTilePosition(pos);
        IntVector2 intVec = new IntVector2(0, 0);
        //获取当前Pos斜坡类型
        Room.SlopeDirection slopeDir = owner.room.IdentifySlope(pos);
        //如果当前Pos地形不是斜坡
        if (owner.room.GetTile(pos).Terrain != Room.Tile.TerrainType.Slope)
        {
            if (owner.room.IdentifySlope(tilePos.x - 1, tilePos.y) != Room.SlopeDirection.Broken && pos.x - slopeRad <= owner.room.MiddleOfTile(pos).x - 10.0)
            {
                slopeDir = owner.room.IdentifySlope(tilePos.x - 1, tilePos.y);
                intVec.x = -1;
            }
            else if (owner.room.IdentifySlope(tilePos.x + 1, tilePos.y) != Room.SlopeDirection.Broken && pos.x + slopeRad >= owner.room.MiddleOfTile(pos).x + 10.0)
            {
                slopeDir = owner.room.IdentifySlope(tilePos.x + 1, tilePos.y);
                intVec.x = 1;
            }
            else if (pos.y - (double)slopeRad < owner.room.MiddleOfTile(pos).y - 10.0)
            {
                if (owner.room.IdentifySlope(tilePos.x, tilePos.y - 1) != Room.SlopeDirection.Broken)
                {
                    slopeDir = owner.room.IdentifySlope(tilePos.x, tilePos.y - 1);
                    intVec.y = -1;
                }
            }
            else if (pos.y + (double)slopeRad > owner.room.MiddleOfTile(pos).y + 10.0 && owner.room.IdentifySlope(tilePos.x, tilePos.y + 1) != Room.SlopeDirection.Broken)
            {
                slopeDir = owner.room.IdentifySlope(tilePos.x, tilePos.y + 1);
                intVec.y = 1;
            }
        }
        if (slopeDir == Room.SlopeDirection.Broken)
            return;
        Vector2 tempVec = owner.room.MiddleOfTile(owner.room.GetTilePosition(pos) + intVec);
        int xSign = 0;
        float dist;
        int ySign;
        switch (slopeDir)
        {
            case Room.SlopeDirection.UpLeft:
                xSign = -1;
                dist = (float)(pos.x - (tempVec.x - 10.0) + (tempVec.y - 10.0));
                ySign = -1;
                break;
            case Room.SlopeDirection.UpRight:
                xSign = 1;
                dist = (float)(20.0 - (pos.x - (tempVec.x - 10.0)) + (tempVec.y - 10.0));
                ySign = -1;
                break;
            case Room.SlopeDirection.DownLeft:
                dist = (float)(20.0 - (pos.x - (tempVec.x - 10.0)) + (tempVec.y - 10.0));
                ySign = 1;
                break;
            default:
                dist = (float)(pos.x - (tempVec.x - 10.0) + (tempVec.y - 10.0));
                ySign = 1;
                break;
        }
        if (ySign == -1 && pos.y <= dist + (double)slopeRad + slopeRad)
        {
            pos.y = dist + slopeRad + slopeRad;
            contactPoint.y = -1;
            vel.x *= 1f - owner.surfaceFriction;
            vel.x += (float)(Mathf.Abs(vel.y) * (double)Mathf.Clamp(0.5f - owner.surfaceFriction, 0.0f, 0.5f) * xSign * 0.2);
            vel.y = 0.0f;
            onSlope = xSign;
            slopeRad = TerrainRad - 1f;
        }
        else
        {
            if (ySign != 1 || pos.y < dist - (double)slopeRad - slopeRad)
                return;
            pos.y = dist - slopeRad - slopeRad;
            contactPoint.y = 1;
            vel.y = 0.0f;
            vel.x *= 1f - owner.surfaceFriction;
            slopeRad = TerrainRad - 1f;
        }
    }

    //检测是否是固体、地板
    private bool SolidFloor(int X, int Y)
    {
        if (owner.room.GetTile(X, Y).Terrain == Room.Tile.TerrainType.Solid)
            return true;
        //地形不等于Floor or 正在穿越Floor地形：返回false
        if (owner.room.GetTile(X, Y).Terrain != Room.Tile.TerrainType.Floor || goThroughFloors)
            return false;
        float minY = owner.room.PixelHeight;
        //遍历Body，找到LastPos Y轴最低点
        for (int index = 0; index < owner.bodyChunks.Length; ++index)
        {
            if (owner.bodyChunks[index].lastPos.y < minY)
                minY = owner.bodyChunks[index].lastPos.y;
        }
        return owner.room.GetTilePosition(new Vector2(0.0f, minY)).y > Y;
    }

    #region public API

    //强行设置当前Chunk到目标位置
    public void HardSetPosition(Vector2 newPos)
    {
        pos = newPos;
        lastPos = newPos;
        lastLastPos = newPos;
        if (setPos.HasValue)
        {
            setPos = new Vector2?(newPos);
        }
    }

    //使用SetPos在下一帧循环中修改位置
    public void MoveFromOutsideMyUpdate(bool eu, Vector2 moveTo)
    {
        if (owner.evenUpdate == eu)
            pos = moveTo;
        else
            setPos = new Vector2?(moveTo);
    }

    //从当前位置关联移动到一个位置
    public void RelativeMoveFromOutsideMyUpdate(bool eu, Vector2 move)
    {
        if (owner.evenUpdate == eu)
            pos += move;
        else
            setPos = new Vector2?(pos + move);
    }

    #endregion
}
