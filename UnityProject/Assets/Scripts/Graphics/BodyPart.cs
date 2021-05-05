using PASystem;
using UnityEngine;
//DEL
//功能：生物身体肢节表现层

public class BodyPart
{
    protected GraphicsModule owner; //生物表现层

    public Vector2 lastPos;         //前一帧的Pos
    public Vector2 pos;             //当前Pos
    public Vector2 vel;             //速度
    public float rad;               //碰撞体半径

    protected float surfaceFric;    //表面摩擦力
    protected float airFric;        //空气阻力
    public bool terrainContact;     //是否接触地形

    public BodyPart(GraphicsModule owner)
    {
        this.owner = owner;
    }

    public virtual void Update()
    {
    }

    public virtual void Reset(Vector2 resetPoint)
    {
        pos = resetPoint + Utils.DegToVec(Random.value * 360f);
        lastPos = pos;
        vel = new Vector2(0f, 0f); 
    }

    //缓慢连接到目标点：距离平衡在connectionRad
    public void ConnectToPoint(Vector2 pnt, float connectionRad, bool push, float elasticMovement, Vector2 hostVel, float adaptVel, float exaggerateVel)
    {
        //弹性运动：修改速度向目标点
        if (elasticMovement > 0.0)
        {
            vel += Utils.DirVec(pos, pnt) * Vector2.Distance(pos, pnt) * elasticMovement;
        } 
        vel += hostVel * exaggerateVel;
        //推力 or 距离检测小于连接半径（拉力）：距离平衡在connectionRad
        if (push || !Utils.DistLess(pos, pnt, connectionRad))
        {
            float dist = Vector2.Distance(pos, pnt);
            Vector2 dir = Utils.DirVec(pos, pnt);
            pos -= (connectionRad - dist) * dir;
            vel -= (connectionRad - dist) * dir;
        }
        //速度调整
        vel -= hostVel;
        vel *= 1f - adaptVel;
        vel += hostVel;
    }

    //从pnt点对BodyPart产生一个推力
    public void PushFromPoint(Vector2 pnt, float pushRad, float elasticity)
    {
        //检测推力有效距离
        if (!Utils.DistLess(pos, pnt, pushRad)) return;

        float dist = Vector2.Distance(pos, pnt);
        Vector2 dir = Utils.DirVec(pos, pnt);
        pos -= (pushRad - dist) * dir * elasticity;
        vel -= (pushRad - dist) * dir * elasticity;
    }

    //检测是否与地形相邻
    public bool OnOtherSideOfTerrain(Vector2 conPos, float minAffectRadius)
    {
        //检测conPos是否进入影响距离
        if (Utils.DistLess(pos, conPos, minAffectRadius))
            return false;
        if (owner.owner.room.GetTile(pos).Solid)
            return true;
        IntVector2 intVec = IntVector2.ClampAtOne(owner.owner.room.GetTilePosition(conPos) - owner.owner.room.GetTilePosition(pos));
        if (intVec.x != 0 && intVec.y != 0)
        {
            if (Mathf.Abs(conPos.x - pos.x) > Mathf.Abs(conPos.y - pos.y))
                intVec.y = 0;
            else
                intVec.x = 0;
        }
        return owner.owner.room.GetTile(owner.owner.room.GetTilePosition(pos) + intVec).Solid;
    }

    //与周围地形进行碰撞检测并校正位置
    public void PushOutOfTerrain(Room room)
    {
        terrainContact = false;
        //遍历当前位置九宫格
        for (int i = 0; i < 9; ++i)
        {
            //检测该位置是地址
            if (room.GetTile(room.GetTilePosition(pos) + Utils.eightDirectionsAndZero[i]).Terrain == Room.Tile.TerrainType.Solid)
            {
                Vector2 tempTile = room.MiddleOfTile(room.GetTilePosition(pos) + Utils.eightDirectionsAndZero[i]);
                float dx = 0f;
                float dy = 0f;
                //位于同一水平轴
                if (pos.y >= tempTile.y - 10f && pos.y <= tempTile.y + 10f)
                {
                    //左半侧
                    if (lastPos.x < tempTile.x)
                    {
                        //推开地形距离 = 地形Center - 10 - 碰撞体半径
                        if (pos.x > tempTile.x - 10f - rad && room.GetTile(room.GetTilePosition(pos) + Utils.eightDirectionsAndZero[i] + new IntVector2(-1, 0)).Terrain != Room.Tile.TerrainType.Solid)
                            dx = tempTile.x - 10f - rad; 
                    }
                    //右半侧
                    else if (pos.x < tempTile.x + 10f + rad && room.GetTile(room.GetTilePosition(pos) + Utils.eightDirectionsAndZero[i] + new IntVector2(1, 0)).Terrain != Room.Tile.TerrainType.Solid)
                        dx = tempTile.x + 10f + rad;
                }
                //位于同一垂直Y轴(算法同上)
                if (pos.x >= tempTile.x - 10f && pos.x <= tempTile.x + 10f)
                {
                    //下半侧
                    if (lastPos.y < tempTile.y)
                    {
                        if (pos.y > tempTile.y - 10f - rad && room.GetTile(room.GetTilePosition(pos) + Utils.eightDirectionsAndZero[i] + new IntVector2(0, -1)).Terrain != Room.Tile.TerrainType.Solid)
                            dy = tempTile.y - 10f - rad;
                    }
                    //上半侧
                    else if (pos.y < tempTile.y + 10f + rad && room.GetTile(room.GetTilePosition(pos) + Utils.eightDirectionsAndZero[i] + new IntVector2(0, 1)).Terrain != Room.Tile.TerrainType.Solid)
                        dy = tempTile.y + 10f + rad;
                }

                //校正位置和速度
                if (Mathf.Abs(pos.x - dx) < Mathf.Abs(pos.y - dy) && dx != 0f)
                {
                    pos.x = dx;
                    vel.x = dx - pos.x;
                    vel.y *= surfaceFric;
                    terrainContact = true;
                }
                else if (dy != 0f)
                {
                    pos.y = dy;
                    vel.y = dy - pos.y;
                    vel.x *= surfaceFric;
                    terrainContact = true;
                }
                else
                {
                    Vector2 newPos = new Vector2(Mathf.Clamp(pos.x, tempTile.x - 10f, tempTile.x + 10f), Mathf.Clamp(pos.y, tempTile.y - 10f, tempTile.y + 10f));
                    if (Utils.DistLess(pos, newPos, rad))
                    {
                        float dist = Vector2.Distance(pos, newPos);
                        Vector2 dir = Utils.DirVec(pos, newPos);
                        vel *= surfaceFric;
                        pos -= (rad - dist) * dir;
                        vel -= (rad - dist) * dir;
                        terrainContact = true;
                    }
                }
            }
        }
    }
}
