using PASystem;
using System;
using System.Collections.Generic;
using UnityEngine;

//功能：触手组件物理逻辑层

public class Tentacle
{
    public Room room;
    public PhysicalObject owner;            //物理对象
    public TentacleProps tProps;            //触手属性

    public Vector2? floatGrabDest;          //抓取目标位置
    public List<IntVector2> grabPath;       //抓取路径
    public List<IntVector2> segments;       //片段

    public BodyChunk connectedChunk;    //连接生物的Chunk
    public float idealLength;           //理想长度
    public TentacleChunk[] tChunks;     //肢节块

    public bool limp;           //瘫软
    public float stretchAndSqueeze = 0.5f;      //伸展压缩系数

    public int backtrackFrom;


    #region  初始化

    //构造函数
    public Tentacle(PhysicalObject owner, BodyChunk connectedChunk, float length)
    {
        this.owner = owner;
        this.idealLength = length;
        this.connectedChunk = connectedChunk;
    }

    public virtual void CreateInRoom(Room room)
    {
        this.room = room;
        //重置Segments、Chunk、抓取路径
        segments = new List<IntVector2>();
        for (int i = 0; i < (int)(idealLength / 20); i++)
        {
            segments.Add(room.GetTilePosition(owner.firstChunk.pos));
        }
        foreach (var chunk in tChunks)
        {
            chunk.Reset();
        }
        grabPath = new List<IntVector2>();

        //Debug视觉效果
        if (debugViz)
        {
            foreach (var chunk in tChunks)
            {
                chunk.CreateDebugSprites();
            }
        }
    }

    //重置触手
    public void Reset(Vector2 resetPos)
    {
        if (room == null) return;

        //Segment
        segments = new List<IntVector2>() { room.GetTilePosition(resetPos) };
        //Chunk
        for (int index = 0; index < tChunks.Length; ++index)
        {
            tChunks[index].pos = resetPos;
            tChunks[index].vel *= 0.0f;
            tChunks[index].Reset();
        }
        //更新Debug视觉
        UpdateDebugSprites();
    }

    #endregion

    #region 触手属性

    //NOTE：每个Seg占据一个Tile

    //当前触手长度
    public float CurrentLength()
    {
        //节数*20
        float length = segments.Count * 20f;
        //存在对角线 -6：勾股定理
        for (int index = 0; index < segments.Count - 2; ++index)
        {
            if (Utils.AreIntVectorsDiagonalNeighbors(segments[index], segments[index + 2]))
                length -= 5.9f;
        }
        return length;
    }

    //触手末端  Tip：顶尖
    public TentacleChunk Tip
    {
        get
        {
            return tChunks[tChunks.Length - 1];
        }
    }

    //基座位置：生物连接体位置  or  Segment[0]
    public Vector2 FloatBase
    {
        get
        {
            if (connectedChunk == null)
                return owner.room.MiddleOfTile(BasePos);
            return connectedChunk.pos;
        }
    }

    //触手基座位置
    public IntVector2 BasePos
    {
        get
        {
            CheckIfAnySegmentsLeft();
            return segments[0];
        }
    }

    //检查是否有可用Segment
    private void CheckIfAnySegmentsLeft()
    {
        if (segments.Count > 0)
            return;
        //没有可用Segment，就添加生物连接体，或者生物主Chunk 作为基座
        if (connectedChunk != null)
            segments.Add(room.GetTilePosition(connectedChunk.pos));
        else
            segments.Add(room.GetTilePosition(owner.firstChunk.pos));
    }

    public float TotalRope
    {
        get
        {
            float length = 0.0f;
            for (int index = 0; index < tChunks.Length; ++index)
                length += tChunks[index].rope.totalLength;
            return length;
        }
    }

    //撤回因子
    private float rf;
    public float retractFac
    {
        get
        {
            return rf;
        }
        set
        {
            rf = Mathf.Clamp(value, 0.0f, 1f);
        }
    }

    #endregion

    #region 抓取目标点

    //获取抓取目标Tile ： floatGrabDest 的 IntTile
    public IntVector2? GrabDest
    {
        get
        {
            if (floatGrabDest.HasValue)
                return new IntVector2?(room.GetTilePosition(floatGrabDest.Value));
            return new IntVector2?();
        }
    }

    //更换新的抓取目标点
    public void MoveGrabDest(Vector2 newGrabDest)
    {
        //如果当前目标点等于新目标点
        if (GrabDest.HasValue && GrabDest.Value == room.GetTilePosition(newGrabDest))
            return;

        floatGrabDest = new Vector2?(newGrabDest);
        FindGrabPath();
    }

    #endregion

    #region 基础行为算法流程

    private float updateCounter;    //触手运算虚拟帧计数
    public virtual void Update()
    {
        //如果存在连接生物Chunk，移动BasePos
        if (connectedChunk != null)
        {
            this.MoveBase(owner.room.GetTilePosition(connectedChunk.pos));
        }

        updateCounter += tProps.tileTentacleUpdateSpeed;
        if (updateCounter >= 1.0)
        {
            pullsThisTick = tProps.maxPullTilesPerTick;

            //如果瘫软：只受重力影响
            if (limp)
            {
                ApplyGravity();
            }
            //如果有抓取路径：进行对齐
            else if (grabPath.Count > 0)
            {
                AlignWithGrabPath();
            }
            //调整触手长度
            AdjustLength();

            --updateCounter;
        }
        //重置参数
        backtrackFrom = -1;
        //更新所有Chunk
        for (int index = 0; index < tChunks.Length; ++index)
        {
            tChunks[index].Update();
        }
    }

    private void MoveBase(IntVector2 newPos)
    {
        //检测newPos是否符合条件
        if (room == null || room.GetTile(newPos).Solid || newPos == BasePos)
            return;

        //如果新位置和旧位置相邻
        if (Utils.AreIntVectorsNeighbors(segments[0], newPos))
        {
            int seg = -1;
            //检查Segment列表是否存在等于newPos
            for (int index = 1; index < segments.Count - 1; ++index)
            {
                if (segments[index] == newPos)
                    seg = index;
            }
            //如果找到重合新位置的关节：移除0-seg、然后增长seg长度的触手
            if (seg > 0)
            {
                segments.RemoveRange(0, seg);
                Grow(seg);
            }
            //没有找到重合的关节
            else
            {
                PrivateMoveBase(newPos - segments[0]);
                ++pullCounter;
                if (pullCounter > 4)
                    PullAtTentacle(0, false);
            }
        }
        //新位置和旧位置不相邻
        else
        {
            List<IntVector2> grabPath = !room.IsPositionInsideBoundries(segments[0])
                                    || !room.IsPositionInsideBoundries(newPos)
                                    || Visual(segments[0], newPos) ?
                                    room.RayTraceTilesList(segments[0].x, segments[0].y, newPos.x, newPos.y) : FindTentaclePath(segments[0], newPos);
            IntVector2 segment = segments[0];
            for (int index = 1; index < grabPath.Count; ++index)
            {
                PrivateMoveBase(grabPath[index] - segment);
                segment = grabPath[index];
            }
            //计算是否施加拉力
            pullCounter += grabPath.Count - 1;
            if (pullCounter > 4)
                PullAtTentacle(0, false);
        }

        //Base位置移动后，如果存在抓取目标，需要寻找新的抓取路径并更新视觉显示
        if (GrabDest.HasValue)
        {
            FindGrabPath();
            UpdateGrabPathDebugSprites();
        }
        //更新Debug视觉层
        UpdateDebugSprites();
    }

    //在Segment 0号索引位置插入新Seg
    private void PrivateMoveBase(IntVector2 movement)
    {
        segments.Insert(0, segments[0] + movement);
    }

    private void ApplyGravity()
    {
        //获取重力方向
        IntVector2 dir = GravityDirection();
        bool flag = false;
        for (int s = 0; s < segments.Count && !flag; ++s)
        {
            if (MoveSegment(s, segments[s] + dir, false, false))
                flag = true;
        }
        if (flag)
            return;
        int seg = 0;
        while (seg < segments.Count && !MoveSegment(seg, segments[seg] + dir, true, false))
            ++seg;
    }

    //随机重力方向：下、左下、右下
    protected virtual IntVector2 GravityDirection()
    {
        if (UnityEngine.Random.value < 0.5)
            return new IntVector2(UnityEngine.Random.value >= 0.5 ? 1 : -1, -1);
        return new IntVector2(0, -1);
    }

    //与抓取路径进行对其
    private void AlignWithGrabPath()
    {
        bool flag = false;
        for (int i = Math.Min(segments.Count - 2, grabPath.Count - 1); i > 0; --i)
        {
            if (segments[i] != grabPath[i]
                && Visual(segments[i], grabPath[i])
                && MoveSegment(i + 1, room.RayTraceTilesList(segments[i].x, segments[i].y, grabPath[i].x, grabPath[i].y)[1], false, false))
                flag = true;
        }
        if (flag) return;

        for (int s = 1; s < segments.Count && s < grabPath.Count; ++s)
        {
            if (segments[s] != grabPath[s])
            {
                MoveSegment(s, grabPath[s], true, false);
                break;
            }
        }
    }

    //寻找抓取路径
    private void FindGrabPath()
    {
        //1.计算新的抓取路径
        //抓取目标点不在房间内 or 基点不在房间内 or 基点和目标点视觉可达 -----------直接返回两点间直线Tile
        //或者------------使用A*获得抓取路径（视觉不可见）
        grabPath = !room.IsPositionInsideBoundries(GrabDest.Value)
                || !room.IsPositionInsideBoundries(BasePos)
                || Visual(BasePos, GrabDest.Value) ?
                room.RayTraceTilesList(BasePos.x, BasePos.y, GrabDest.Value.x, GrabDest.Value.y) : FindTentaclePath(BasePos, GrabDest.Value);
        //2.根据GrabPath对Segment产生一个修正作用拉力
        MoveAlignedSegmentsWithPath();
        //3.更新抓取路径视觉显示
        UpdateGrabPathDebugSprites();
    }

    //对齐Segment和GrabPath
    private void MoveAlignedSegmentsWithPath()
    {
        //获取Segment和抓取路径 较短的长度
        int index = Math.Min(segments.Count - 1, grabPath.Count - 1);
        //长度小于minLen  &&  Seg和Path距离小于 触手属性（Seg远离路径距离） &&  两点间视觉可见
        for (int index2 = 0; index2 < Math.Min(segments.Count - 1, grabPath.Count - 1)
                            && (segments[index2 + 1].FloatDist(grabPath[index2 + 1]) <= (double)tProps.tileTentacleSnapWithPathDistance
                            && Visual(segments[index2 + 1], grabPath[index2 + 1])); ++index2)
        {
            //退出循环时：Index1保存了首个不满足对齐条件的索引
            index = index2 + 1;
            //对齐Segment和Grabpath
            segments[index2] = grabPath[index2];
        }
        //获取同索引下
        List<IntVector2> intVecLst = room.RayTraceTilesList(grabPath[index].x, grabPath[index].y, segments[index].x, segments[index].y);
        for (int index2 = intVecLst.Count - 2; index2 >= 0; --index2)
            segments.Insert(index, intVecLst[index2]);
        pullCounter += intVecLst.Count - 1;
        //在该索引位置，对触手产生简单拉力
        PullAtTentacle(index, true);
        //更新Debug视觉
        UpdateDebugSprites();
    }

    //最终影响Seg长度、更新Debug视觉

    //在某索引点对触手产生拉力
    private int pullCounter;     //应用拉力计数
    private int pullsThisTick;
    private void PullAtTentacle(int startPullingPoint, bool onlySimplePull)
    {
        //复杂拉力
        if (!onlySimplePull)
        {
            //存在多个拉力点
            while (pullCounter > 0)
            {
                bool flag = false;
                //从起始点开始遍历---起点和倒数第3个点之间
                for (int from = startPullingPoint; from < segments.Count - 3 && !flag; ++from)
                {
                    //从最后一个点 -- 起点 +3 的点之间
                    for (int to = segments.Count - 1; to >= from + 3; --to)
                    {
                        //终点 - 起点 - 两点曼哈顿距离
                        int num = to - from - Utils.ManhattanDistance(segments[from], segments[to]);
                        //              两点视觉可见
                        if (num > 0 && num <= pullCounter && Visual(segments[from], segments[to]))
                        {
                            //获取两点之间直线Tile
                            List<IntVector2> straightPath = room.RayTraceTilesList(segments[from].x, segments[from].y, segments[to].x, segments[to].y);
                            bool canPull = true;
                            if (num > 1 && num <= pullsThisTick)
                            {
                                //遍历直线Tile列表：去头去尾
                                for (int index1 = 1; index1 < straightPath.Count - 1; ++index1)
                                {
                                    //遍历from、 to 两点之间的Seg
                                    for (int index2 = from + 1; index2 < to; ++index2)
                                    {
                                        //如果Seg和Tile距离大于2  且  两点间视觉不可见
                                        if (Utils.ManhattanDistance(segments[index2], straightPath[index1]) > 2 && !Visual(segments[index2], straightPath[index1]))
                                        {
                                            canPull = false;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (canPull)
                            {
                                //减去此次拉动的肢节数量
                                pullsThisTick -= num;
                                pullCounter -= num;
                                //缩短触手
                                Shorten(from, to, straightPath);
                                flag = true;   //flag1用来跳外层循环
                                break;
                            }
                        }
                    }
                }
                if (!flag)
                    break;
            }
        }

        //简单拉力：从末尾缩减Seg
        for (; pullCounter > 0 && pullsThisTick > 0 && segments.Count > 1; --pullsThisTick)
        {
            segments.RemoveAt(segments.Count - 1);
            --pullCounter;
        }
        //更新视觉显示
        UpdateDebugSprites();
    }

    //迁移S索引的Segment到Dest坐标，并返回是否移动成功
    private bool MoveSegment(int s, IntVector2 dest, bool allowMovingToOccupiedSpace, bool allowSolidTile)
    {
        //Seg = 0   or  当前Seg等于目标点
        //前一Seg不等于目标点  且 不相邻
        //目标点时Solid，且不允许Solid地形
        if (s == 0 || segments[s] == dest
            || !Utils.AreIntVectorsNeighbors(segments[s - 1], dest) && segments[s - 1] != dest
            || !allowSolidTile && room.GetTile(dest).Solid)
            return false;
        //前一个Seg已经等于目标点，
        if (dest == segments[s - 1])
        {
            //不允许移动到已占据的Space ： false
            if (!allowMovingToOccupiedSpace)
            {
                return false;
            }
            //允许移动到已占据Space
            else
            {
                segments.Insert(s, dest);
                segments.RemoveAt(segments.Count - 1);
            }
        }
        //Seg与目标点X、Y距离大于1
        else if (Math.Abs(segments[s].x - dest.x) > 1 || Math.Abs(segments[s].y - dest.y) > 1)
        {
            //不允许移动到已占据的Space ： false
            if (!allowMovingToOccupiedSpace)
                return false;
            //允许移动到已占据Space
            segments.Insert(s, segments[s - 1]);
            segments.Insert(s, dest);
            segments.RemoveAt(segments.Count - 1);
            segments.RemoveAt(segments.Count - 1);
        }
        else
        {
            //不允许移动到已占据的Space ： 遍历Seg列表如果和目标点重合：返回false
            if (!allowMovingToOccupiedSpace)
            {
                for (int index = 0; index < segments.Count; ++index)
                {
                    if (segments[index] == dest)
                        return false;
                }
            }
            //允许移动到已占据Space
            List<IntVector2> intVecLst = new List<IntVector2>();
            intVecLst.Add(new IntVector2(segments[s].x, dest.y));
            intVecLst.Add(new IntVector2(dest.x, segments[s].y));
            IntVector2 minCostVec = intVecLst[0];
            float minCost = float.MaxValue;
            for (int i = 0; i < intVecLst.Count; ++i)
            {
                float cost = 1f;
                //Solid代价+1000
                if (room.GetTile(intVecLst[i]).Solid)
                    cost += 1000f;
                //与Seg列表重叠代价+1
                for (int j = 0; j < segments.Count; ++j)
                {
                    if (intVecLst[i] == segments[j])
                        ++cost;
                }
                if (cost < (double)minCost)
                {
                    minCostVec = intVecLst[i];
                    minCost = cost;
                }
            }
            //不允许移动到已占据的Space ： 遍历Seg列表如果和代价点重合：返回false
            if (!allowMovingToOccupiedSpace)
            {
                for (int index = 0; index < segments.Count; ++index)
                {
                    if (segments[index] == minCostVec)
                        return false;
                }
            }
            segments.Insert(s, minCostVec);
            segments.Insert(s, dest);
            segments.RemoveAt(segments.Count - 1);
            segments.RemoveAt(segments.Count - 1);
        }
        UpdateDebugSprites();
        return true;
    }

    //调整触手长度
    private void AdjustLength()
    {
        float curLength = CurrentLength();
        float idealLength = this.idealLength;
        //触手可以变短 && 存在抓取路径 ----重新计算理想长度
        if (tProps.shorten && grabPath.Count > 0)
            idealLength = Mathf.Min(this.idealLength, grabPath.Count * 20f);
        //当前长度和理想长度的差值
        int diffVal = (int)(idealLength / 20.0 - curLength / 20.0);
        //当前长度小于理想长度 &&（抓取路径为空  or  segment长度小于抓取路径）---触手增长一节
        if (diffVal > 0 && (grabPath.Count == 0 || segments.Count < grabPath.Count))
        {
            Grow(1);
        }
        //当前长度大于理想长度：施加拉力
        else if (diffVal < 0)
        {
            ++pullCounter;
            PullAtTentacle(0, false);
        }
    }

    //缩短触手
    private void Shorten(int from, int to, List<IntVector2> straightPath)
    {
        segments.RemoveRange(from + 1, to - from - 1);
        for (int index = straightPath.Count - 2; index > 0; --index)
            segments.Insert(from + 1, straightPath[index]);
        //更新视觉显示
        UpdateDebugSprites();
    }

    //增长触手长度 ：增加Segment
    private void Grow(int add)
    {
        for (int addNum = 0; addNum < add; ++addNum)
        {
            float minCost = float.MaxValue;
            IntVector2 tempVec = segments[segments.Count - 1];
            //遍历五个方向
            for (int i = 0; i < 5; ++i)
            {
                float cost = UnityEngine.Random.value;
                //新位置 = 触手尖+4方向位置
                IntVector2 pos = segments[segments.Count - 1] + Utils.fourDirectionsAndZero[i];
                //如果新位置是Solid
                if (room.GetTile(pos).Solid)
                    cost += 10000f;
                //检测新位置是否与其他触手节相交
                for (int seg = 0; seg < segments.Count; ++seg)
                {
                    if (segments[seg] == pos)
                        ++cost;
                }
                //寻找权重最小的Pos添加到触手节末尾
                if (cost < (double)minCost)
                {
                    minCost = cost;
                    tempVec = pos;
                }
            }
            segments.Add(tempVec);
        }
        //更新DebugSprite显示
        UpdateDebugSprites();
    }


    //检测AB两点间是否直线可达：视觉可见
    protected bool Visual(IntVector2 a, IntVector2 b)
    {
        return room.RayTraceTilesForTerrain(a.x, a.y, b.x, b.y);
    }

    //推动指定索引两Chunk彼此分离
    protected void PushChunksApart(int a, int b)
    {
        //方向、距离、阈值
        Vector2 dir = Utils.DirVec(tChunks[a].pos, tChunks[b].pos);
        float dist = Vector2.Distance(tChunks[a].pos, tChunks[b].pos);
        float threshold = 10f;
        if (dist < (double)threshold)
        {
            //修改Chunk的Pos、Vel属性：距离越远推动系数越小，推力越弱
            tChunks[a].pos -= dir * (threshold - dist) * 0.5f;
            tChunks[a].vel -= dir * (threshold - dist) * 0.5f;
            tChunks[b].pos += dir * (threshold - dist) * 0.5f;
            tChunks[b].vel += dir * (threshold - dist) * 0.5f;
        }
    }


    //A*寻路：根据起点和目标点 得到触手路径
    private List<IntVector2> FindTentaclePath(IntVector2 start, IntVector2 goal)
    {
        //开放列表
        List<PCell> openLst = new List<PCell>() { new PCell(goal, 0, 0.0f, null) };
        //关闭列表
        bool[,] closeTags = new bool[room.TileWidth, room.TileHeight];

        //从目标点向起始点寻路
        PCell tempCell = null;
        while (openLst.Count > 0 && tempCell == null)
        {
            //选取开放列表中启发代价最小的单元
            PCell parent = openLst[0];
            float minH = float.MaxValue;
            for (int index = 0; index < openLst.Count; ++index)
            {
                if (openLst[index].heuristic < (double)minH)
                {
                    parent = openLst[index];
                    minH = openLst[index].heuristic;
                }
            }
            openLst.Remove(parent);
            //四方向检测
            for (int index = 0; index < 4; ++index)
            {
                //左右边界、上下边界和关闭列表检测
                if (parent.pos.x + Utils.fourDirections[index].x >= 0 && parent.pos.x + Utils.fourDirections[index].x < room.TileWidth
                    && (parent.pos.y + Utils.fourDirections[index].y >= 0 && parent.pos.y + Utils.fourDirections[index].y < room.TileHeight)
                    && !closeTags[parent.pos.x + Utils.fourDirections[index].x, parent.pos.y + Utils.fourDirections[index].y])
                {
                    //计算寻路代价（Solid：1000）
                    float heuristic = Vector2.Distance(IntVector2.ToVector2(parent.pos + Utils.fourDirections[index]), IntVector2.ToVector2(start)) + (!room.GetTile(parent.pos + Utils.fourDirections[index]).Solid ? 0.0f : 1000f);
                    //创建新的Pcell
                    PCell pcell = new PCell(parent.pos + Utils.fourDirections[index], parent.generation + 1, heuristic, parent);
                    //设置重复检测标志位
                    closeTags[parent.pos.x + Utils.fourDirections[index].x, parent.pos.y + Utils.fourDirections[index].y] = true;
                    //寻到路径：退出循环
                    if (pcell.pos == start)
                        tempCell = pcell;
                    else
                        openLst.Add(pcell);
                }
            }
        }
        //逆转列表获得寻路结果
        List<IntVector2> pathLst = new List<IntVector2>();
        for (; tempCell.parent != null; tempCell = tempCell.parent)
        {
            pathLst.Add(tempCell.pos);
        }
        return pathLst;
    }

    //寻路单元
    private class PCell
    {
        public IntVector2 pos;
        public int generation;
        public float heuristic;     //启发式寻路代价
        public PCell parent;

        public PCell(IntVector2 pos, int generation, float heuristic, PCell parent)
        {
            this.pos = pos;
            this.generation = generation;
            this.heuristic = heuristic;
            this.parent = parent;
        }
    }

    #endregion


    #region Debug 表现层

    public bool debugViz = true;                //Debug视觉模式开关
    public List<DebugSprite> sprites;           //Chunk Sprite
    public List<DebugSprite> grabPathSprites;   //抓取路径Sprite

    //根据GrabPath调整<grabPathSprite>的视觉显示
    private void UpdateGrabPathDebugSprites()
    {
        //如果未开启DebugViz
        if (grabPathSprites != null && !Configs.OpenDebugPath)
        {
            for (int index = 0; index < grabPathSprites.Count; ++index)
                grabPathSprites[index].RemoveFromRoom();
        }

        //如果开启DebugViz
        if (!Configs.OpenDebugPath) return;

        if (grabPathSprites == null)
            grabPathSprites = new List<DebugSprite>();

        //根据GrabPath长度调整 Sprite长度
        if (grabPathSprites.Count > grabPath.Count)
        {
            int num = grabPathSprites.Count - grabPath.Count;
            for (int index = 0; index < num; ++index)
            {
                grabPathSprites[grabPathSprites.Count - 1].RemoveFromRoom();
                grabPathSprites.RemoveAt(grabPathSprites.Count - 1);
            }
        }
        if (grabPathSprites.Count < grabPath.Count)
        {
            int num = grabPath.Count - grabPathSprites.Count;
            for (int index = 0; index < num; ++index)
            {
                grabPathSprites.Add(new DebugSprite(new Vector2(0.0f, 0.0f), new FSprite("pixel", true), room));
                room.AddObject(grabPathSprites[grabPathSprites.Count - 1]);
            }
        }

        //遍历Sprite列表
        for (int index = 0; index < grabPathSprites.Count; ++index)
        {
            //根据GrabPath调整Sprite：位置、颜色
            grabPathSprites[index].pos = room.MiddleOfTile(grabPath[index]);
            grabPathSprites[index].sprite.color = new Color(1f, 0.0f, 0.0f);
            //触手肢节参数：Scale、旋转
            if (index < grabPathSprites.Count - 1)
            {
                grabPathSprites[index].sprite.scaleX = 1f;
                grabPathSprites[index].sprite.scaleY = 20f;
                grabPathSprites[index].sprite.anchorY = 0.0f;
                grabPathSprites[index].sprite.rotation = Utils.AimFromOneVectorToAnother(room.MiddleOfTile(grabPath[index]), room.MiddleOfTile(grabPath[index + 1]));
            }
            //触手顶点参数：scale
            else
            {
                grabPathSprites[index].sprite.scale = 5f;
            }
        }
    }

    //根据Segment调整DebugSprite视觉显示
    private void UpdateDebugSprites()
    {
        //检测是否开启Debug视觉
        if (!debugViz)
        {
            ClearDebugSprites();
            return;
        }

        if (sprites == null)
        {
            sprites = new List<DebugSprite>();
        }

        //根据Segments长度调整DebugSprite长度
        if (sprites.Count > segments.Count)
        {
            int num = sprites.Count - segments.Count;
            for (int index = 0; index < num; ++index)
            {
                sprites[sprites.Count - 1].RemoveFromRoom();
                sprites.RemoveAt(sprites.Count - 1);
            }
        }
        if (sprites.Count < segments.Count)
        {
            int num = segments.Count - sprites.Count;
            for (int index = 0; index < num; ++index)
            {
                sprites.Add(new DebugSprite(new Vector2(0.0f, 0.0f), new FSprite("pixel", true), room));
                room.AddObject(sprites[sprites.Count - 1]);
            }
        }
        //遍历Segment列表
        for (int index = 0; index < segments.Count; ++index)
        {
            //根据Segment索引选择Sprite颜色
            float f = (float)(index / (double)segments.Count * 0.7);
            float h = f - Mathf.Floor(f);
            sprites[index].sprite.color = Utils.HSL2RGB(h, 1f, 0.5f);

            if (index < segments.Count - 1)
            {
                //触手节参数
                sprites[index].sprite.scaleX = 12f;
                sprites[index].sprite.scaleY = 18f;
                sprites[index].sprite.anchorY = 0.0f;
                sprites[index].sprite.rotation = Utils.AimFromOneVectorToAnother(room.MiddleOfTile(segments[index]), room.MiddleOfTile(segments[index + 1]));
            }
            else
            {
                //触手顶尖参数
                sprites[index].sprite.scale = 14f;
            }

            sprites[index].pos = room.MiddleOfTile(segments[index]);
            sprites[index].sprite.alpha = index >= segments.Count - 1 ? 0.5f : 0.2f;
        }
    }

    //清理DebugSprite引用
    private void ClearDebugSprites()
    {
        if (sprites == null) return;

        for (int index = 0; index < sprites.Count; ++index)
            sprites[index].RemoveFromRoom();
    }

    #endregion

    #region 触手肢节

    public class TentacleChunk
    {
        public Tentacle tentacle;   //父级触手
        public int tentacleIndex;   //肢节索引

        public Vector2 pos;         //位置
        public Vector2 vel;         //速度
        public float rad;           //半径

        public Vector2 lastPos;             //上一帧位置
        public IntVector2 lastContactPoint; //上一个关联Point
        public IntVector2 contactPoint;     //当前关联Point


        public float tPos;
        private DebugSprite[] dbSprites;

        public float lockInPosition;
        public Vector2 phaseFrom;
        public int phasesToSameLocation;    //是否定相到同一位置
        public IntVector2 afterPhaseStuckPos;//定相后的位置
        public float phase;
        public List<IntVector2> currentSegmentTrail;
        public bool collideWithTerrain;     //是否接触地形
        public int phaseAttempts;
        public float stretchedFac;          //拉伸系数
        public Rope rope;

        //构造函数
        public TentacleChunk(Tentacle tentacle, int tentacleIndex, float tPos, float rad)
        {
            this.tentacle = tentacle;
            this.tPos = tPos;
            this.rad = rad;
            this.tentacleIndex = tentacleIndex;
            collideWithTerrain = true;
        }

        //当前Chunk对应的Segment索引
        public int currentSegment
        {
            get
            {
                //[0-Segment.Count]
                return Utils.IntClamp((int)(tPos * (tentacle.segments.Count - 1)), 0, tentacle.segments.Count - 1);
            }
        }

        //粘连的Seg位置
        public IntVector2 StuckPos
        {
            get
            {
                return tentacle.segments[currentSegment];
            }
        }

        //粘连Rect
        public FloatRect StuckRect
        {
            get
            {
                //每个Rect 边长为18
                return FloatRect.MakeFromVector2(tentacle.room.MiddleOfTile(StuckPos) - new Vector2(9f, 9f), tentacle.room.MiddleOfTile(StuckPos) + new Vector2(9f, 9f));
            }
        }

        //触手属性
        public TentacleProps tp
        {
            get
            {
                return tentacle.tProps;
            }
        }

        //可拉伸半径
        public float stretchedRad
        {
            get
            {
                return rad * Mathf.Clamp(Mathf.Pow(stretchedFac, tentacle.stretchAndSqueeze), 0.5f, 1.5f);
            }
        }

        //是否激活使用绳子物理
        public bool RopeActive
        {
            get
            {
                if (rope == null || phase != -1.0 || tentacleIndex != 0 && tentacle.tChunks[tentacleIndex - 1].phase != -1.0)
                    return false;
                if (tentacle.backtrackFrom >= tentacleIndex)
                    return tentacle.backtrackFrom == -1;
                return true;
            }
        }

        //重置触手关节
        public void Reset()
        {
            currentSegmentTrail = new List<IntVector2>();
            for (int index = 0; index < tp.tileTentacleRecordFrames; ++index)
                currentSegmentTrail.Add(StuckPos);
            phase = -1f;
            pos = tentacle.room.MiddleOfTile(StuckPos);
            lastPos = pos;
            vel = new Vector2(0.0f, 0.0f);

            if (tentacle.tProps.rope)
            {
                rope = new Rope(tentacle.room, tentacle.FloatBase, tentacle.FloatBase + new Vector2(1f, 1f), 1f);
            }
        }

        public void Update()
        {
            lastPos = pos;
            //更新绳子物理
            if (rope != null)
            {
                rope.Update(tentacleIndex != 0 ? tentacle.tChunks[tentacleIndex - 1].pos : tentacle.FloatBase, pos);
                if (!RopeActive || phase > -1.0 || tentacleIndex > 0 && tentacle.tChunks[tentacleIndex - 1].phase > -1.0 || rope.totalLength > tentacle.idealLength / (double)tentacle.tChunks.Length * 5.0)
                    rope.Reset();
            }

            //如果当前Chunk已定相
            if (phase == -1.0)
            {
                if (phasesToSameLocation > 0 && tentacle.segments[this.currentSegment] == afterPhaseStuckPos)
                    pos = Utils.RestrictInRect(pos, StuckRect);

                //如果是第一个触手节（靠近基座）
                if (tentacleIndex == 0)
                {
                    //计算距离基座的方向和距离
                    Vector2 dir = Utils.DirVec(pos, tentacle.FloatBase);
                    float dist = Vector2.Distance(pos, tentacle.FloatBase);
                    if (RopeActive)
                    {
                        dir = Utils.DirVec(pos, rope.BConnect);
                        dist = rope.totalLength;
                    }
                    //计算收缩长度 =（理想长度 * 撤回因子）/ 触手关节数目
                    float length = tentacle.idealLength / tentacle.tChunks.Length * Mathf.Lerp(1f, 0.1f, tentacle.retractFac);
                    if (tp.stiff || dist > (double)length)
                    {
                        //更新当前Chunk的Pos、Vel属性
                        pos -= dir * (length - dist) * (1f - tp.pullAtConnectionChunk);
                        vel -= dir * (length - dist) * (1f - tp.pullAtConnectionChunk);
                        //如果触手属性对生物连接体有拉力：更新连接Chunk的Pos、Vel属性
                        if (tentacle.connectedChunk != null && tp.pullAtConnectionChunk > 0.0)
                        {
                            if (RopeActive)
                                dir = Utils.DirVec(rope.AConnect, tentacle.connectedChunk.pos);
                            tentacle.connectedChunk.pos += dir * (length - dist) * tp.pullAtConnectionChunk;
                            tentacle.connectedChunk.vel += dir * (length - dist) * tp.pullAtConnectionChunk;
                        }
                    }
                    //计算当前拉伸系数 = 理想长度 /（ 触手关节数 * 与上一关节距离）
                    stretchedFac = tentacle.idealLength / tentacle.tChunks.Length / Mathf.Max(1f, dist);
                }
                //（计算逻辑基本同上）
                else
                {
                    //计算与前一触手节的方向和距离
                    Vector2 dir = Utils.DirVec(pos, tentacle.tChunks[tentacleIndex - 1].pos);
                    float dist = Vector2.Distance(pos, tentacle.tChunks[tentacleIndex - 1].pos);
                    if (RopeActive)
                    {
                        dir = Utils.DirVec(pos, rope.BConnect);
                        dist = rope.totalLength;
                    }
                    float num = tentacle.idealLength / tentacle.tChunks.Length * Mathf.Lerp(1f, 0.1f, tentacle.retractFac);
                    if (tp.stiff || dist > (double)num)
                    {
                        pos -= dir * (num - dist) * (1f - tp.massDeteriorationPerChunk);
                        vel -= dir * (num - dist) * (1f - tp.massDeteriorationPerChunk);
                        if (RopeActive)
                            dir = Utils.DirVec(rope.AConnect, tentacle.tChunks[tentacleIndex - 1].pos);
                        tentacle.tChunks[tentacleIndex - 1].pos += dir * (num - dist) * tp.massDeteriorationPerChunk;
                        tentacle.tChunks[tentacleIndex - 1].vel += dir * (num - dist) * tp.massDeteriorationPerChunk;
                    }
                    stretchedFac = tentacle.idealLength / tentacle.tChunks.Length / Mathf.Max(1f, dist);
                }

                //StuckPos不等于trail首个位置时，trail首位插入StuckPos
                if (StuckPos != currentSegmentTrail[0])
                {
                    currentSegmentTrail.Insert(0, StuckPos);
                    currentSegmentTrail.RemoveAt(currentSegmentTrail.Count - 1);
                }

                //检测撤回关节索引不存在  && 当前Chunk位置和Segment直线不可达
                if (tentacle.backtrackFrom == -1 && !tentacle.Visual(tentacle.room.GetTilePosition(pos), tentacle.segments[this.currentSegment]))
                    tentacle.backtrackFrom = tentacleIndex;
                //如果当前触手体存在撤回索引，且当前Chunk索引大于撤回索引
                if (tentacle.backtrackFrom != -1 && tentacleIndex >= tentacle.backtrackFrom)
                {
                    bool flag = false;
                    for (int index = 1; index <= currentSegment && !flag; ++index)
                    {
                        if (!PhysicsUtils.RayTraceTilesForTerrain(tentacle.room, tentacle.room.GetTilePosition(pos), tentacle.segments[index])
                            && PhysicsUtils.RayTraceTilesForTerrain(tentacle.room, tentacle.room.GetTilePosition(pos), tentacle.segments[index - 1]))
                        {
                            vel += Vector2.ClampMagnitude(tentacle.room.MiddleOfTile(tentacle.segments[index - 1]) - pos, 20f) * tp.backtrackSpeed / 20f;
                            flag = true;
                        }
                    }
                    if (!flag)
                    {
                        //获取前一触手节位置
                        Vector2 tentacleBase = tentacle.FloatBase;
                        if (tentacleIndex > 0)
                            tentacleBase = tentacle.tChunks[tentacleIndex - 1].pos;
                        //更新当前Chunk速度 =（ 矢量距离 * 撤回速度）/ 20f
                        vel += Vector2.ClampMagnitude(tentacleBase - pos, 20f) * tp.backtrackSpeed / 20f;
                        //更新前一Chunk速度
                        if (tentacleIndex > 0)
                            tentacle.tChunks[tentacleIndex - 1].vel += Vector2.ClampMagnitude(tentacleBase - pos, 20f) * tp.backtrackSpeed / 20f;
                    }
                    //当前关联Point不为空
                    if (!flag && (contactPoint.x != 0 || contactPoint.y != 0))
                    {
                        //定相尝试次数递增1
                        ++phaseAttempts;
                        //定相尝试次数 > 触手属性值
                        if (phaseAttempts > tentacle.tProps.terrainHitsBeforePhase)
                        {
                            //开始定相
                            PhaseToSegment();
                            phaseAttempts = 0;
                        }
                    }
                }
                //不存在撤回索引  or  撤回索引大于当前Chunk索引
                else
                {
                    phaseAttempts = 0;
                    //瘫软状态
                    if (tentacle.limp)
                    {
                        //从当前Seg往前遍历
                        for (int currentSegment = this.currentSegment; currentSegment >= 0; --currentSegment)
                        {
                            //如果当前Chunk位置和Seg视觉可见
                            if (tentacle.Visual(tentacle.room.GetTilePosition(pos), tentacle.segments[currentSegment]))
                            {
                                if (tentacle.room.GetTile(tentacle.segments[currentSegment].x, tentacle.segments[currentSegment].y - 1).Solid)
                                {
                                    vel += Vector2.ClampMagnitude(tentacle.room.MiddleOfTile(tentacle.segments[currentSegment].x, tentacle.segments[currentSegment].y) - pos, 20f) * 0.005f;
                                    break;
                                }
                                vel += Vector2.ClampMagnitude(tentacle.room.MiddleOfTile(tentacle.segments[currentSegment]) - pos, 20f) * 0.005f;
                                break;
                            }
                        }
                    }
                    //触手非瘫软状态
                    else
                    {
                        //触手存在抓取目标
                        if (tentacle.floatGrabDest.HasValue)
                        {
                            //计算抓取目标对Chunk的吸引力 ： 修改速度
                            if (tentacleIndex == tentacle.tChunks.Length - 1)
                                vel += Vector2.ClampMagnitude(tentacle.floatGrabDest.Value - pos, 20f) * tp.goalAttractionSpeedTip / 20f;
                            else
                                vel += Vector2.ClampMagnitude(tentacle.floatGrabDest.Value - pos, 20f) * tp.goalAttractionSpeed / 20f;
                        }
                        //Chunk速度 对齐Segment
                        vel += Vector2.ClampMagnitude(tentacle.room.MiddleOfTile(tentacle.segments[currentSegment]) - pos, 20f) * tp.alignToSegmentSpeed / 20f;
                    }
                }

                //Clamp速度、更新位置
                vel = Vector2.ClampMagnitude(vel, tp.chunkVelocityCap);
                pos += vel;
                //与地形发生碰撞、计算碰撞后的位置、速度、碰撞点
                if (collideWithTerrain)
                {
                    lastContactPoint = contactPoint;
                    PhysicsUtils.TerrainCollisionData terrainCollisionData = PhysicsUtils.HorizontalCollision(tentacle.room, PhysicsUtils.SlopesVertically(tentacle.room, PhysicsUtils.VerticalCollision(tentacle.room, new PhysicsUtils.TerrainCollisionData(pos, pos - vel, vel, rad, new IntVector2(0, 0), true))));
                    contactPoint = terrainCollisionData.contactPoint;
                    pos = terrainCollisionData.pos;
                    vel = terrainCollisionData.vel;
                }
            }
            //如果当前Chunk为定相
            else
            {
                //定相逐帧计数（后面的数值基本都设置为：15）
                phase += 1f / tp.segmentPhaseThroughTerrainFrames;
                //根据当前定相进度，插值改变Pos属性（PhaseFrom->StuckPos）
                if (phase <= 1.0)
                    pos = Vector2.Lerp(phaseFrom, tentacle.room.MiddleOfTile(StuckPos), Mathf.Min(1f, phase));
                //速度设置为0
                vel *= 0.0f;
                //定相完成
                if (phase >= 1.0)
                {
                    phase = -1f;
                    if (afterPhaseStuckPos == StuckPos)
                        ++phasesToSameLocation;
                    else
                        phasesToSameLocation = 0;
                    afterPhaseStuckPos = StuckPos;
                }
                //如果是触手关节、且前一触手节未定相
                if (tentacleIndex > 0 && tentacle.tChunks[tentacleIndex - 1].phase == -1.0)
                {
                    //计算与前一触手节的方向和距离
                    Vector2 dir = Utils.DirVec(pos, tentacle.tChunks[tentacleIndex - 1].pos);
                    float dist = Vector2.Distance(pos, tentacle.tChunks[tentacleIndex - 1].pos);
                    //计算收缩长度 =（理想长度 * 撤回因子）/ 触手关节数目
                    float length = tentacle.idealLength / tentacle.tChunks.Length * Mathf.Lerp(1f, 0.1f, tentacle.retractFac);
                    if (tp.stiff || dist > (double)length)
                    {
                        //对前一触手节产生影响
                        tentacle.tChunks[tentacleIndex - 1].pos += dir * (length - dist) * tp.massDeteriorationPerChunk;
                        tentacle.tChunks[tentacleIndex - 1].vel += dir * (length - dist) * tp.massDeteriorationPerChunk;
                    }
                    //计算拉伸系数
                    stretchedFac = length / dist;
                }
            }

            UpdateDebugSprites();
        }

        //定相到指定坐标
        public void PhaseToSegment()
        {
            phaseFrom = pos;
            phase = 0.0f;
        }

        #region Debug表现层

        //初始化DebugSprite数组
        public void CreateDebugSprites()
        {
            if (dbSprites != null)
            {
                for (int index = 0; index < 1; ++index)
                    dbSprites[index].RemoveFromRoom();
            }

            if (!tentacle.debugViz) return;

            dbSprites = new DebugSprite[3];
            dbSprites[0] = new DebugSprite(pos, new FSprite("Circle20", true), tentacle.room);
            dbSprites[0].sprite.scale = 0.5f;
            dbSprites[0].sprite.alpha = 1f;
            dbSprites[1] = new DebugSprite(pos, new FSprite("pixel", true), tentacle.room);
            dbSprites[1].sprite.color = new Color(1f, 0.0f, 1f);
            dbSprites[1].sprite.alpha = 0.3f;
            dbSprites[1].sprite.anchorY = 0.0f;
            dbSprites[2] = new DebugSprite(pos, new FSprite("pixel", true), tentacle.room);
            dbSprites[2].sprite.alpha = 1f;
            dbSprites[2].sprite.anchorY = 0.0f;
            dbSprites[2].sprite.scaleX = 2f;
            for (int index = 0; index < 3; ++index)
                tentacle.room.AddObject(dbSprites[index]);
        }

        //更新DebugSprite视觉显示
        private void UpdateDebugSprites()
        {
            if (dbSprites == null) return;

            //更改位置属性
            dbSprites[0].pos.x = pos.x;
            dbSprites[0].pos.y = pos.y;
            dbSprites[1].pos.x = pos.x;
            dbSprites[1].pos.y = pos.y;
            dbSprites[2].pos.x = pos.x;
            dbSprites[2].pos.y = pos.y;
            //根据索引计算DBSprite颜色
            float f = (float)(tentacleIndex / (double)tentacle.tChunks.Length * 0.7f);
            float h = f - Mathf.Floor(f);
            if (tentacle.backtrackFrom > -1 && tentacleIndex >= tentacle.backtrackFrom)
                h = 0.0f;
            dbSprites[0].sprite.color = Utils.HSL2RGB(h, 1f, 0.5f);
            dbSprites[2].sprite.color = Utils.HSL2RGB(h, 1f, 0.5f);
            //获取前一触手节的位置
            Vector2 tentacleBase = tentacle.FloatBase;
            if (tentacleIndex > 0)
                tentacleBase = tentacle.tChunks[tentacleIndex - 1].pos;
            //计算scaleY和朝向
            dbSprites[2].sprite.scaleY = Vector2.Distance(pos, tentacleBase);
            dbSprites[2].sprite.rotation = Utils.AimFromOneVectorToAnother(pos, tentacleBase);
        }

        #endregion
    }

    #endregion

    //触手属性
    public struct TentacleProps
    {
        public bool stiff;      //僵硬
        public bool rope;       //绳子
        public bool shorten;    //缩短
        public float massDeteriorationPerChunk;     //质量逐节退化系数
        public float pullAtConnectionChunk;         //连接块拉力
        public float goalAttractionSpeedTip;        //目标对触手尖吸引系数
        public float goalAttractionSpeed;           //目标吸引系数
        public float alignToSegmentSpeed;           //对齐Segment的速度
        public float backtrackSpeed;                //原路返回速度
        public float chunkVelocityCap;              //Chunk速度限制
        public float tileTentacleUpdateSpeed;       //运算帧率
        public float tileTentacleSnapWithPathDistance;  //Chunk与路径的偏移
        public int segmentPhaseThroughTerrainFrames;
        public int tileTentacleRecordFrames;
        public int maxPullTilesPerTick;
        public int terrainHitsBeforePhase;

        public TentacleProps(bool stiff, bool rope, bool shorten, float massDeteriorationPerChunk, float pullAtConnectionChunk, float goalAttractionSpeedTip, float goalAttractionSpeed, float alignToSegmentSpeed, float backtrackSpeed, float chunkVelocityCap, float tileTentacleUpdateSpeed, float tileTentacleSnapWithPathDistance, int segmentPhaseThroughTerrainFrames, int tileTentacleRecordFrames, int maxPullTilesPerTick, int terrainHitsBeforePhase)
        {
            this.stiff = stiff;
            this.rope = rope;
            this.shorten = shorten;
            this.massDeteriorationPerChunk = massDeteriorationPerChunk;
            this.pullAtConnectionChunk = pullAtConnectionChunk;
            this.goalAttractionSpeedTip = goalAttractionSpeedTip;
            this.goalAttractionSpeed = goalAttractionSpeed;
            this.alignToSegmentSpeed = alignToSegmentSpeed;
            this.backtrackSpeed = backtrackSpeed;
            this.chunkVelocityCap = chunkVelocityCap;
            this.tileTentacleUpdateSpeed = tileTentacleUpdateSpeed;
            this.tileTentacleSnapWithPathDistance = tileTentacleSnapWithPathDistance;
            this.segmentPhaseThroughTerrainFrames = segmentPhaseThroughTerrainFrames;
            this.tileTentacleRecordFrames = tileTentacleRecordFrames;
            this.maxPullTilesPerTick = maxPullTilesPerTick;
            this.terrainHitsBeforePhase = terrainHitsBeforePhase;
        }
    }
}
