using PASystem;
using System.Collections.Generic;
using UnityEngine;

//DaddyLongLeg生物

public class DaddyLongLegs : Creature, PhysicalObject.IHaveAppendages
{
    public SimpleAI AI;                 //AI模块
    public DaddyTentacle[] tentacles;   //触手列表

    private float unconditionalSupport;     //出生时无条件支持力
    public Vector2 moveDirection = new Vector2(0.0f, -1f);      //移动方向
    public bool moving;                     //是否正在移动

    public List<IntVector2> pastPositions;
    public int stuckCounter;
    public bool squeeze;
    public float squeezeFac;

    public DaddyLongLegs(WorldEntity entity)
      : base(entity)
    {
        //随机生物个性种子
        int seed = Random.seed;
        Random.seed = entity.ID.RandomSeed;
        //随机长度Chunk、质量
        float totalMass =  8f;
        this.bodyChunks = new BodyChunk[Random.Range(4, 7)];
        List<Vector2> vecLst = new List<Vector2>();
        for (int index = 0; index < this.bodyChunks.Length; ++index)
        {
            //Body质量递减算法
            float t = index / (float)(this.bodyChunks.Length - 1);
            float mass = Mathf.Lerp(totalMass * 0.2f, totalMass * Mathf.Lerp(0.3f, 1f, t), Mathf.Pow(Random.value, 1f - t));
            totalMass -= mass;
            //创建BodyChunk（Owner：this  索引  初始Pos  半径  质量）
            this.bodyChunks[index] = new BodyChunk(this, index, new Vector2(0.0f, 0.0f), (float)(mass * 3.5 + 3.5), mass);
            vecLst.Add(Utils.RNV() * this.bodyChunks[index].rad);
        }
        //调整Chunk彼此之间的相对位置
        for (int index1 = 0; index1 < 5; ++index1)
        {
            //遍历Chunk列表，
            for (int index2 = 0; index2 < this.bodyChunks.Length; ++index2)
            {
                for (int index3 = 0; index3 < this.bodyChunks.Length; ++index3)
                {
                    //彼此之间距离小于 半径和 * 0.85，彼此分离
                    if (index2 != index3 && Vector2.Distance(vecLst[index2], vecLst[index3]) < (bodyChunks[index2].rad + (double)this.bodyChunks[index3].rad) * 0.85f)
                        vecLst[index3] -= Utils.DirVec(vecLst[index3], vecLst[index2]) * ((float)((bodyChunks[index2].rad + (double)this.bodyChunks[index3].rad) * 0.85f) - Vector2.Distance(vecLst[index2], vecLst[index3]));
                }
            }
            for (int index2 = 0; index2 < this.bodyChunks.Length; ++index2)
                vecLst[index2] *= 0.9f;
        }
        //创建Chunk之间的物理Joint：任意Chunk之间都存在
        this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[this.bodyChunks.Length * (this.bodyChunks.Length - 1) / 2];
        int index4 = 0;
        for (int index1 = 0; index1 < this.bodyChunks.Length; ++index1)
        {
            for (int index2 = index1 + 1; index2 < this.bodyChunks.Length; ++index2)
            {
                this.bodyChunkConnections[index4] = new PhysicalObject.BodyChunkConnection(this.bodyChunks[index1], this.bodyChunks[index2], Vector2.Distance(vecLst[index1], vecLst[index2]), BodyChunkConnection.Type.Normal, 1f, -1f);
                ++index4;
            }
        }
        //创建触手
        this.tentacles = new DaddyTentacle[Random.Range(5,  10)];
        float length = Mathf.Lerp( 1600f , tentacles.Length * ( 300f), 0.5f);
        List<float> lengthLst = new List<float>();
        for (int i = 0; i < this.tentacles.Length; ++i)
        {
            lengthLst.Add(length / tentacles.Length);
        } 

        //初始化触手列表
        this.appendages = new List<PhysicalObject.Appendage>();
        for (int i = 0; i < this.tentacles.Length; ++i)
        {
            this.tentacles[i] = new DaddyTentacle(this, this.bodyChunks[i % this.bodyChunks.Length], lengthLst[i], i, Utils.DegToVec(Mathf.Lerp(0.0f, 360f, i / (float)this.tentacles.Length)));
            this.appendages.Add(new PhysicalObject.Appendage(this, i, this.tentacles[i].tChunks.Length + 1));
        }
        Random.seed = seed;
        //初始化物理参数
        this.airFriction = 1f;
        this.gravity = 0.9f;
        this.bounce = 0.1f;
        this.surfaceFriction = 0.4f;
        this.collisionLayer = 1;
    }

    //质心点
    public Vector2 MiddleOfBody
    {
        get
        {
            Vector2 vector2 = this.mainBodyChunk.pos * this.mainBodyChunk.mass;
            for (int index = 1; index < this.bodyChunks.Length; ++index)
                vector2 += this.bodyChunks[index].pos * this.bodyChunks[index].mass;
            return vector2 / this.TotalMass;
        }
    }

    //初始化Graphic模块
    public override void InitiateGraphicsModule()
    {
    }

    #region 在房间中实例化

    public override void NewRoom(Room newRoom)
    {
        base.NewRoom(newRoom);
        //迁移所有触手到新的房间
        for (int index = 0; index < this.tentacles.Length; ++index)
            this.tentacles[index].CreateInRoom(newRoom);
        //清空PastPos
        this.pastPositions = new List<IntVector2>();
    }

    public override void PlaceInRoom(Room placeRoom)
    {
        base.PlaceInRoom(placeRoom);
        this.AI = new  SimpleAI(this);
        //遍历所有触手
        Vector2 middleOfBody = this.MiddleOfBody;
        for (int index = 0; index < this.tentacles.Length; ++index)
        {
            IntVector2 connectedTile = this.room.GetTilePosition(this.tentacles[index].connectedChunk.pos);
            //生物质心 到 触手连接Chunk的方向
            Vector2 dir = Utils.DirVec(middleOfBody, this.tentacles[index].connectedChunk.pos);
            //远方距离 = 方向 * 理想长度
            IntVector2 dist = this.room.GetTilePosition(this.tentacles[index].connectedChunk.pos + dir * this.tentacles[index].idealLength);
            //获取触手基座 到 顶尖的所有Tile,移除碰撞到Solid的
            List<IntVector2> vecLst = this.room.RayTraceTilesList(connectedTile.x, connectedTile.y, dist.x, dist.y);
            for (int i = 1; i < vecLst.Count; ++i)
            {
                if (this.room.GetTile(vecLst[i]).Solid)
                {
                    vecLst.RemoveRange(i, vecLst.Count - i);
                    break;
                }
            }
            //赋值触手的Segment
            this.tentacles[index].segments = vecLst;
            //Reset触手的所有Chunk
            for (int i = 0; i < this.tentacles[index].tChunks.Length; ++i)
            {
                this.tentacles[index].tChunks[i].Reset();
            }
            //触手抓取最后一个最后一个Tile
            this.tentacles[index].MoveGrabDest(this.room.MiddleOfTile(vecLst[vecLst.Count - 1]));
        }
        //初始化变量：无条件的支持力
        this.unconditionalSupport = 1f;
    }

    public void ResetCreature(IntVector2 pos, Vector2 direction)
    {
        for (int i = 0; i < this.bodyChunks.Length; i++)
        {
            this.bodyChunks[i].pos = this.room.MiddleOfTile(pos) + Utils.RNV();
            this.bodyChunks[i].lastPos = this.bodyChunks[i].pos;
            this.bodyChunks[i].vel = direction * 4f;
        }
        this.squeezeFac = 1f;
        for (int i = 0; i < this.tentacles.Length; i++)
        {
            this.tentacles[i].Reset(this.tentacles[i].connectedChunk.pos);
        }
    }

    #endregion

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.room == null) return;

        //无条件支持力递减
        this.unconditionalSupport = Mathf.Max(0.0f, this.unconditionalSupport - 0.025f);

        //计算收缩因子，更新触手Chunk的位置:缩回mainBodyChunk
        this.squeezeFac = this.squeeze  ? Mathf.Min(1f, this.squeezeFac + 0.02f) : Mathf.Max(0.0f, this.squeezeFac - 0.03f);
        if (squeezeFac > 0.8)
        {
            for (int i = 0; i < this.tentacles.Length; ++i)
            {
                for (int j = 0; j < this.tentacles[i].tChunks.Length; ++j)
                    this.tentacles[i].tChunks[j].pos = Vector2.Lerp(this.tentacles[i].tChunks[j].pos, this.mainBodyChunk.pos, Utils.LerpMap(this.squeezeFac, 0.8f, 1f, 0.0f, 0.5f));
            }
        }
        //计算生物Chunk的地形收缩因子
        for (int i = 0; i < this.bodyChunks.Length; ++i)
        {
            this.bodyChunks[i].terrainSqueeze = 1f - this.squeezeFac;
        }
        //根据收缩因子，更新Joint作用力类型
        for (int index = 0; index < this.bodyChunkConnections.Length; ++index)
        {
            this.bodyChunkConnections[index].type = !this.squeeze ? BodyChunkConnection.Type.Normal : BodyChunkConnection.Type.Pull;
        }
            
        this.squeeze = false;

        //遍历触手列表
        //1.Update
        //2.计算抓握地形数量
        //3.更新触手撤回系数
        int legsGrabbing = 0;     
        for (int index = 0; index < this.tentacles.Length; ++index)
        {
            //Update
            this.tentacles[index].Update();
            //检测是否抓握目标点
            if (this.tentacles[index].atGrabDest)
                ++legsGrabbing;
            this.tentacles[index].retractFac = this.squeezeFac;
        }

        //如果生物存在意识
        if (this.Consious)
        {
            //执行Act行为：传参抓握点数量
            this.Act(legsGrabbing);
        }
    }

    private void Act(int legsGrabbing)
    {
        //更新AI模块：主要是根据AI模块选择行为，然后修改寻路目标点
        //Debug模式下使用鼠标作为目标点，就不必了，可以设置一个Update检测生物是否到达目标点
        this.AI.Update();

        int count = 0;
        //生物位置和AI目标点大于3  且抓取地形腿 数量大于0
        if ((Utils.ManhattanDistance(this.entity.pos.Tile, this.AI.DestTile) > 3 && legsGrabbing > 0))
        {
            this.pastPositions.Insert(0, this.entity.pos.Tile);
            if (this.pastPositions.Count > 80)
                this.pastPositions.RemoveAt(this.pastPositions.Count - 1);
            //遍历PasePos,如果生物距离此小于4，num++
            for (int index = 40; index < this.pastPositions.Count; ++index)
            {
                if (Utils.DistLess(this.entity.pos.Tile, this.pastPositions[index], 4f))
                    ++count;
            }
        }
        //计算StuckCounter
        if (count > 30)
            ++this.stuckCounter;
        else
            this.stuckCounter -= 2;
        //StuckCount限制在  0-200
        this.stuckCounter = Utils.IntClamp(this.stuckCounter, 0, 200);
        //如果StuckCounter > 100,遍历所有Chunk，给其施加一个  随机方向速度
        if (this.stuckCounter > 100)
        {
            for (int index = 0; index < this.bodyChunks.Length; ++index)
            {
                this.bodyChunks[index].vel += Utils.RNV() * 3f * Random.value * Mathf.InverseLerp(100f, 200f, stuckCounter);
            }
        }

        //寻找一个触手放开并抓取新的目标点

        //如果有一半的Leg处于抓取状态  && 正在移动  或者 Stuck计时大于100
        if (legsGrabbing > this.tentacles.Length / 2 && this.moving || this.stuckCounter > 100)
        {
            //遍历触手列表：寻找 触手在目标抓取点 && 释放打分最大
            float maxScore = float.MinValue;
            int maxScoreIndex = -1;
            for (int i = 0; i < this.tentacles.Length; ++i)
            {
                if (this.tentacles[i].atGrabDest && this.tentacles[i].ReleaseScore() > (double)maxScore)
                {
                    maxScore = this.tentacles[i].ReleaseScore();
                    maxScoreIndex = i;
                }
            }
            //执行抓取新的目标点
            if (maxScoreIndex > -1)
                this.tentacles[maxScoreIndex].UpdateClimbGrabPos();
        }
        //经过复杂计算出f和num1
        float ratio = 0.0f;
        float force = 0.0f;
        //遍历触手列表
        for (int index = 0; index < this.tentacles.Length; ++index)
        {
            //Chunk抓握比例 开根号
            float from = Mathf.Pow(this.tentacles[index].chunksGripping, 0.5f);
            //如果触手在目标抓握点 
            if (this.tentacles[index].atGrabDest && this.tentacles[index].GrabDest.HasValue)
            {
                force += Mathf.Pow(Mathf.InverseLerp(Utils.LerpMap(stuckCounter, 0.0f, 100f, -0.1f, -1f), 0.85f, Vector2.Dot((this.tentacles[index].floatGrabDest.Value - this.mainBodyChunk.pos).normalized, this.moveDirection)), 0.8f) / tentacles.Length;
                from = Mathf.Lerp(from, 1f, 0.75f);
            }
            ratio += from / tentacles.Length;
        }
        float a1 = Mathf.Pow(force * ratio, Utils.LerpMap(stuckCounter, 100f, 200f, 0.8f, 0.1f));
        float a2 = Mathf.Pow(ratio, 0.3f);
        float a3 = Mathf.Max(a1, this.squeezeFac);
        float pullForce = Mathf.Max(Mathf.Max(a2, this.squeezeFac), this.unconditionalSupport);
        float supportForce = Mathf.Max(a3, this.unconditionalSupport);

        //TODO 更加智能的选择部分触手执行移动任务

        //修改所有Chunk速度 ：施加一个物理因素
        for (int index = 0; index < this.bodyChunks.Length; ++index)
        {
            this.bodyChunks[index].vel *= Mathf.Lerp(1f, Mathf.Lerp(0.95f, 0.8f, this.squeezeFac), pullForce);
            this.bodyChunks[index].vel.y += this.gravity;
        }

        //如果生物位置 和目标点 曼哈顿距离小于3，给所有Chunk施加一个拉向目标点的力
        if (Utils.ManhattanDistance(this.entity.pos.Tile, this.AI.DestTile) < 3)
        {
            //给所有Chunk速度施加一个 指向目标点的力
            for (int index = 0; index < this.bodyChunks.Length; ++index)
                this.bodyChunks[index].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.AI.DestTile) - this.bodyChunks[0].pos, 30f) / 30f * 0.6f * supportForce;
        }

        this.moving = !this.AI.arrived;
        if (this.moving)
        {
            this.GoThroughFloors = false;
            this.squeeze = false;

            //所有Chunk施加一个向目标点的力
            for (int index = 0; index < this.bodyChunks.Length; ++index)
                this.bodyChunks[index].vel += Utils.DirVec(this.bodyChunks[0].pos, this.room.MiddleOfTile(this.AI.DestTile)) * 0.6f * supportForce;
            //计算移动方向 : 质心点 朝向目标点
            Vector2 dir = Utils.DirVec(this.MiddleOfBody, Utils.IntVector2ToVector2(this.AI.DestTile));
            //移动方向偏移
            this.moveDirection = (this.moveDirection + dir.normalized * 0.2f).normalized;
        }
        //设置默认移动方向：向下偏移
        else
        {
            this.moveDirection = (this.moveDirection + new Vector2(0.0f, -0.1f)).normalized;
        }
    }

    public override void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
        base.Collide(otherObject, myChunk, otherChunk);
    }

    #region 暂时无用 IHaveAppendage

    public Vector2 AppendagePosition(int appendage, int segment)
    {
        --segment;
        if (segment < 0)
            return this.tentacles[appendage].connectedChunk.pos;
        return this.tentacles[appendage].tChunks[segment].pos;
    }

    public void ApplyForceOnAppendage(PhysicalObject.Appendage.Pos pos, Vector2 momentum)
    {
        if (pos.prevSegment > 0)
        {
            this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment - 1].pos += momentum / 0.04f * (1f - pos.distanceToNext);
            this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment - 1].vel += momentum / 0.04f * (1f - pos.distanceToNext);
        }
        else
        {
            this.tentacles[pos.appendage.appIndex].connectedChunk.pos += momentum / this.tentacles[pos.appendage.appIndex].connectedChunk.mass * (1f - pos.distanceToNext);
            this.tentacles[pos.appendage.appIndex].connectedChunk.vel += momentum / this.tentacles[pos.appendage.appIndex].connectedChunk.mass * (1f - pos.distanceToNext);
        }
        this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment].pos += momentum / 0.04f * pos.distanceToNext;
        this.tentacles[pos.appendage.appIndex].tChunks[pos.prevSegment].vel += momentum / 0.04f * pos.distanceToNext;
    }

    #endregion
}
