using PASystem;
using System.Collections.Generic;
using UnityEngine;

//功能：绳子物理模拟

public class Rope
{
    public Room room;

    public Vector2 A;               //两端点
    public Vector2 B;
    public Vector2 lastA;
    public Vector2 lastB;
    public float totalLength;       //长度
    private float thickness;        //厚度：碰撞计算

    public RopeDebugVisualizer visualizer;  //debug绳子表现层

    //构造函数
    public Rope(Room room, Vector2 initA, Vector2 initB, float thickness)
    {
        this.room = room;
        A = initA;
        lastA = initA;
        B = initB;
        lastB = initB;
        totalLength = Vector2.Distance(initA, initB);
        bends = new List<Corner>();
        this.thickness = thickness;
    }

    #region Corner

    //NOTE：如果绳子有弯曲，整个连接列表除了AB端点，还会加入弯折点，否则整个绳子只有AB
    public List<Corner> bends;

    //绳子弯折点结构
    public struct Corner
    {
        public FloatRect.CornerLabel dir;   //拐角类型
        public Vector2 pos;                 //位置

        public Corner(FloatRect.CornerLabel dir, Vector2 pos)
        {
            this.dir = dir;
            this.pos = pos;
        }
    }

    //与A连接的Corner位置
    public Vector2 AConnect
    {
        get
        {
            return (bends.Count == 0) ? B : bends[0].pos;
        }
    }
    //与B连接的Corner位置
    public Vector2 BConnect
    {
        get
        {
            return (bends.Count == 0) ? A : bends[bends.Count - 1].pos;
        }
    }

    //总节点个数
    public int TotalPositions
    {
        get
        {
            return 2 + bends.Count;
        }
    }

    //根据索引得到某节点位置
    public Vector2 GetPosition(int index)
    {
        if (index == 0)
            return A;
        if (index - 1 >= bends.Count)
            return B;
        return bends[index - 1].pos;
    }

    //得到绳子所有节点Vec
    public List<Vector2> GetAllPositions()
    {
        List<Vector2> vecLst = new List<Vector2>();
        vecLst.Add(A);
        for (int index = 0; index < bends.Count; ++index)
            vecLst.Add(bends[index].pos);
        vecLst.Add(B);
        return vecLst;
    }

    #endregion


    //重置绳子
    public void Reset()
    {
        bends.Clear();
    }
    public void Reset(Vector2 pos)
    {
        bends.Clear();
        A = pos;
        lastA = pos;
        B = pos;
        lastB = pos;
    }

    public void Update(Vector2 newA, Vector2 newB)
    {
        //更新Pos
        lastA = A;
        lastB = B;
        A = newA;
        B = newB;
        //检测碰撞更新弯折点列表Bends
        if (bends.Count == 0)
        {
            CollideWithCorners(lastA, A, lastB, B, 0, 0);
        }
        else
        {
            CollideWithCorners(BConnect, BConnect, lastB, B, bends.Count, 0);
            CollideWithCorners(lastA, A, AConnect, AConnect, 0, 0);
        }
        //去除多余弯折点
        if (bends.Count > 0)
        {
            List<int> intLst = new List<int>();
            for (int index = 0; index < bends.Count; ++index)
            {
                Vector2 l1 = A;
                Vector2 l2 = B;
                if (index > 0)
                    l1 = bends[index - 1].pos;
                if (index < bends.Count - 1)
                    l2 = bends[index + 1].pos;
                if (!DoesLineOverlapCorner(l1, l2, bends[index]))
                    intLst.Add(index);
            }
            for (int index = intLst.Count - 1; index >= 0; --index)
                bends.RemoveAt(intLst[index]);
        }
        //计算绳子总长度
        if (bends.Count == 0)
        {
            totalLength = Vector2.Distance(A, B);
        }
        else
        {
            totalLength = Vector2.Distance(A, AConnect) + Vector2.Distance(BConnect, B);
            for (int index = 1; index < bends.Count; ++index)
                totalLength += Vector2.Distance(bends[index - 1].pos, bends[index].pos);
        }
        //弯折点个数大于50时重置
        if (bends.Count > 50)
        {
            Reset();
        }
        //更新绳子视觉显示
        if (visualizer != null)
        {
            visualizer.Update();
        }
    }

    private void CollideWithCorners(Vector2 la, Vector2 a, Vector2 lb, Vector2 b, int bend, int recursion)
    {
        //检测递归次数
        if (recursion > 10) return;

        //创建一个包括a、b、lb的矩形
        IntRect intRect = IntRect.MakeFromIntVector2(room.GetTilePosition(la));
        intRect.ExpandToInclude(room.GetTilePosition(a));
        intRect.ExpandToInclude(room.GetTilePosition(lb));
        intRect.ExpandToInclude(room.GetTilePosition(b));
        intRect.Grow(1);
        List<Corner> cornerLst = new List<Corner>();
        //左->右、下->上 逐Tile遍历
        for (int left = intRect.left; left <= intRect.right; ++left)
        {
            for (int bottom = intRect.bottom; bottom <= intRect.top; ++bottom)
            {
                if (room.GetTile(left, bottom).Solid)
                {
                    if (!room.GetTile(left - 1, bottom).Solid)
                    {
                        if (!room.GetTile(left, bottom - 1).Solid
                            && !room.GetTile(left - 1, bottom - 1).Solid)
                        {
                            //左下角拐角地形
                            cornerLst.Add(new Corner(FloatRect.CornerLabel.D, room.MiddleOfTile(left, bottom) + new Vector2(-10f - thickness, -10f - thickness)));
                        }
                        if (!room.GetTile(left, bottom + 1).Solid
                            && !room.GetTile(left - 1, bottom + 1).Solid)
                        {
                            //左上角拐角地形
                            cornerLst.Add(new Corner(FloatRect.CornerLabel.A, room.MiddleOfTile(left, bottom) + new Vector2(-10f - thickness, 10f + thickness)));
                        }
                    }
                    if (!room.GetTile(left + 1, bottom).Solid)
                    {
                        //右下角拐角地形
                        if (!room.GetTile(left, bottom - 1).Solid && !room.GetTile(left + 1, bottom - 1).Solid)
                            cornerLst.Add(new Corner(FloatRect.CornerLabel.C, room.MiddleOfTile(left, bottom) + new Vector2(10f + thickness, -10f - thickness)));
                        //右上角拐角地形
                        if (!room.GetTile(left, bottom + 1).Solid && !room.GetTile(left + 1, bottom + 1).Solid)
                            cornerLst.Add(new Corner(FloatRect.CornerLabel.B, room.MiddleOfTile(left, bottom) + new Vector2(10f + thickness, 10f + thickness)));
                    }
                }
            }
        }
        Corner? nullable = new Rope.Corner?();
        float f = float.MaxValue;
        foreach (Corner corner in cornerLst)
        {
            if (DoesLineOverlapCorner(a, b, corner) && corner.pos != la && (corner.pos != a && corner.pos != lb) && corner.pos != b && ((Utils.PointInTriangle(corner.pos, a, la, b) || Utils.PointInTriangle(corner.pos, a, lb, b) || (Utils.PointInTriangle(corner.pos, a, la, lb) || Utils.PointInTriangle(corner.pos, la, lb, b))) && Mathf.Abs(Utils.DistanceToLine(corner.pos, la, lb)) < (double)Mathf.Abs(f)))
            {
                nullable = new Rope.Corner?(corner);
                f = Utils.DistanceToLine(corner.pos, lastA, lastB);
            }
        }

        if (!nullable.HasValue)
            return;
        Vector2 pos = nullable.Value.pos;
        //增加绳子与拐点碰撞点
        bends.Insert(bend, nullable.Value);
        Vector2 vector2 = Utils.ClosestPointOnLine(la, lb, pos);
        CollideWithCorners(vector2, pos, lb, b, bend + 1, recursion + 1);
        CollideWithCorners(la, a, vector2, pos, bend, recursion + 1);
    }

    //检测线是否和折角重叠
    public bool DoesLineOverlapCorner(Vector2 l1, Vector2 l2, Corner corner)
    {
        IntVector2 intVec = new IntVector2(corner.dir == FloatRect.CornerLabel.A || corner.dir == FloatRect.CornerLabel.D ? -1 : 1,
                                               corner.dir == FloatRect.CornerLabel.A || corner.dir == FloatRect.CornerLabel.B ? 1 : -1);
        return (l1.y == (double)l2.y || (intVec.x >= 0 || Utils.HorizontalCrossPoint(l1, l2, corner.pos.y).x >= (double)corner.pos.x) && (intVec.x <= 0 || Utils.HorizontalCrossPoint(l1, l2, corner.pos.y).x <= (double)corner.pos.x))
            && (l1.x == (double)l2.x || (intVec.y >= 0 || Utils.VerticalCrossPoint(l1, l2, corner.pos.x).y >= (double)corner.pos.y) && (intVec.y <= 0 || Utils.VerticalCrossPoint(l1, l2, corner.pos.x).y <= (double)corner.pos.y));
    }



    //绳子Debug视觉
    public class RopeDebugVisualizer
    {
        private Rope rope;
        private DebugSprite mainDebugSprite;  //A端点Sprite
        private List<DebugSprite> sprts;

        public RopeDebugVisualizer(Rope rope)
        {
            this.rope = rope;
            mainDebugSprite = new DebugSprite(rope.A, new FSprite("pixel", true), rope.room);
            mainDebugSprite.sprite.anchorY = 0.0f;
            mainDebugSprite.sprite.color = new Color(0.0f, 1f, 0.1f);
            mainDebugSprite.sprite.scaleX = 3f;
            mainDebugSprite.sprite.alpha = 0.5f;
            rope.room.AddObject(mainDebugSprite);
            sprts = new List<DebugSprite>();
        }

        public void Update()
        {
            //MainSprite属性设置
            mainDebugSprite.pos = rope.A;
            mainDebugSprite.sprite.rotation = Utils.AimFromOneVectorToAnother(rope.A, rope.B);
            mainDebugSprite.sprite.scaleY = Vector2.Distance(rope.A, rope.B);
            //根据Rope.bends列表动态调整Sprite数量
            while (sprts.Count > rope.bends.Count + 1)
            {
                sprts[sprts.Count - 1].Destroy();
                sprts.RemoveAt(sprts.Count - 1);
            }
            while (sprts.Count < rope.bends.Count + 1)
            {
                sprts.Add(new DebugSprite(rope.A, new FSprite("pixel", true), rope.room));
                rope.room.AddObject(sprts[sprts.Count - 1]);
                sprts[sprts.Count - 1].sprite.anchorY = 0.0f;
                sprts[sprts.Count - 1].sprite.scaleX = 2f;
            }

            //根据前后节点的位置：修改各节点Sprite的旋转、Scale、颜色
            for (int i = 0; i < rope.bends.Count + 1; ++i)
            {
                //得到前一Bends位置和当前bend位置
                Vector2 preBend = i != 0 ? rope.bends[i - 1].pos : rope.A;
                Vector2 curBend = i != rope.bends.Count ? rope.bends[i].pos : rope.B;
                //修改当前索引Sprite属性
                sprts[i].pos = preBend;
                sprts[i].sprite.rotation = Utils.AimFromOneVectorToAnother(preBend, curBend);
                sprts[i].sprite.scaleY = Vector2.Distance(preBend, curBend);
                sprts[i].sprite.color = !PhysicsUtils.RayTraceTilesForTerrain(rope.room, preBend, curBend) ? new Color(1f, 0.0f, 0.0f) : new Color(0.0f, 1f, 0.1f);
            }
        }

        //释放DebugSprite资源
        public void ClearSprites()
        {
            Debug.Log("clearRopeSprites");
            mainDebugSprite.Destroy();
            for (int i = 0; i < sprts.Count; ++i)
            {
                sprts[i].Destroy();
            }
        }
    }
}
