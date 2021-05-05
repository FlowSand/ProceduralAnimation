using PASystem;
using System;
using System.Collections.Generic;
using UnityEngine;

//功能：Daddy生物触手逻辑

public class DaddyTentacle : Tentacle
{
    public int tentacleNumber;          //触手索引
    public Vector2 tentacleDir;         //触手方向
    public float awayFromBodyRotation;  //生物重心  到 触手连接Chunk的方向
    public bool atGrabDest;             //是否处于抓取位置
    private int foundNoGrabPos;         //没有找到抓取位置计时
    public float chunksGripping;        //Chunk抓取地形的比例

    private int secondaryGrabBackTrackCounter;
    private bool lastBackTrack;
    public IntVector2 secondaryGrabPos;

    public Vector2 preliminaryGrabDest; //首要抓取目标
    public Vector2 idealGrabPos;        //理想抓取位置

    //生物体
    public DaddyLongLegs daddy
    {
        get
        {
            return this.owner as DaddyLongLegs;
        }
    }

    public DaddyTentacle(DaddyLongLegs daddy, BodyChunk chunk, float length, int tentacleNumber, Vector2 tentacleDir)
      : base(daddy, chunk, length)
    {
        //触手索引 和 方向
        this.tentacleNumber = tentacleNumber;
        this.tentacleDir = tentacleDir;
        //触手属性
        this.tProps = new Tentacle.TentacleProps(false, true, false, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 3.2f, 10f, 0.25f, 5f, 15, 60, 12, 20);
        //触手Chunk
        this.tChunks = new Tentacle.TentacleChunk[(int)(length / 40.0)];
        for (int index = 0; index < this.tChunks.Length; ++index)
        {
            this.tChunks[index] = new Tentacle.TentacleChunk(this, index, (index + 1) / (float)this.tChunks.Length, 3f);
        }
    }

    //更换房间：切换Task为运动
    public override void CreateInRoom(Room room)
    {
        base.CreateInRoom(room);

        this.UpdateClimbGrabPos();
    }

    public override void Update()
    {
        base.Update();

        //判断触手是否处于瘫软状态
        this.limp = !this.daddy.Consious;
        //遍历BodyChunk
        for (int index = 0; index < this.tChunks.Length; ++index)
        {
            //速度递减
            this.tChunks[index].vel *= 0.9f;
            //瘫软状态下，ChunkY轴向下速度增加
            if (this.limp)
                this.tChunks[index].vel.y -= 0.5f;
        }

        //瘫软状态下，Y轴向下速度增加
        if (this.limp)
        {
            for (int index = 0; index < this.tChunks.Length; ++index)
                this.tChunks[index].vel.y -= 0.7f;
        }
        //非瘫软状态下
        else
        {
            //初始化变量：后续重新计算
            this.atGrabDest = false;
            this.chunksGripping = 0.0f;

            if (this.backtrackFrom > -1)
            {
                ++this.secondaryGrabBackTrackCounter;
                if (!this.lastBackTrack)
                    this.secondaryGrabBackTrackCounter += 20;
            }
            this.lastBackTrack = this.backtrackFrom > -1;


            //计算Daddy Body的几何中心，然后计算 触手附着Chunk距离几何中心的方向
            Vector2 critPos = this.daddy.mainBodyChunk.pos;
            for (int index = 1; index < this.daddy.bodyChunks.Length; ++index)
                critPos += this.daddy.bodyChunks[index].pos;
            Vector2 bodyCenter = critPos / daddy.bodyChunks.Length;
            this.awayFromBodyRotation = Utils.AimFromOneVectorToAnother(bodyCenter, this.connectedChunk.pos);

            //执行攀爬行为
            this.Climb();

            //遍历所有触手Chunk：彼此伸展开
            for (int i = 0; i < this.tChunks.Length; ++i)
            {
                float ratio = i / (float)(this.tChunks.Length - 1);
                if ((double)ratio < 0.2f)
                    this.tChunks[i].vel += Utils.DegToVec(this.awayFromBodyRotation) * Mathf.InverseLerp(0.2f, 0.0f, ratio) * 5f;
                for (int j = i + 1; j < this.tChunks.Length; ++j)
                    this.PushChunksApart(i, j);
            }
        }
    }

    //行为：攀爬移动
    private void Climb()
    {
        //1.在触手体方向+生物移动方向上，寻找可抓取位置，修改当前抓取目标点
        float fac = Utils.LerpMap(daddy.stuckCounter, 50f, 200f, 0.5f, 0.95f);
        //理想抓取位置  通过触手体当前方向  和 生物体MoveDirection 球形插值计算出来
        this.idealGrabPos = this.FloatBase + (Vector2)Vector3.Slerp(tentacleDir, daddy.moveDirection, fac) * this.idealLength * 0.7f;
        //理想抓取方向
        Vector2 idealGrabDir = this.FloatBase + (Vector2)Vector3.Slerp(Vector3.Slerp(tentacleDir, daddy.moveDirection, fac), Utils.RNV(), Mathf.InverseLerp(20f, 200f, foundNoGrabPos)) * this.idealLength * Utils.LerpMap(Math.Max(this.foundNoGrabPos, this.daddy.stuckCounter), 20f, 200f, 0.7f, 1.2f);
        //获取生物体  到理想抓取位置  直线距离上的Tile
        List<IntVector2> intVecLst = PhysicsUtils.RayTracedTilesArray(this.FloatBase, idealGrabDir);
        bool isFind = false;
        //遍历Tile
        for (int i = 0; i < intVecLst.Count - 1; ++i)
        {
            //如果是Solid地形，考虑抓取打分
            if (this.room.GetTile(intVecLst[i + 1]).Solid)
            {
                this.ConsiderGrabPos(Utils.RestrictInRect(idealGrabDir, this.room.TileRect(intVecLst[i]).Shrink(1f)), this.idealGrabPos);
                isFind = true;
                break;
            }
            //垂直或水平竖杆
            if (this.room.GetTile(intVecLst[i]).horizontalBeam || this.room.GetTile(intVecLst[i]).verticalBeam)
            {
                this.ConsiderGrabPos(this.room.MiddleOfTile(intVecLst[i]), this.idealGrabPos);
                isFind = true;
            }
        }
        //设置 是否发现抓取位置 计时
        if (isFind)
            this.foundNoGrabPos = 0;
        else
            ++this.foundNoGrabPos;

        bool flag2 = this.secondaryGrabBackTrackCounter < 200 && this.SecondaryGrabPosScore(this.secondaryGrabPos) > 0.0;
        for (int index = 0; index < this.tChunks.Length; ++index)
        {
            if (this.backtrackFrom == -1 || this.backtrackFrom > index)
            {
                //粘附在地形上
                this.StickToTerrain(this.tChunks[index]);
                //如果存在抓取目标点
                if (this.GrabDest.HasValue)
                {
                    //如果不在抓取目标点 &&  Chunk距离目标点距离小于 20： 抓取目标点=true
                    if (!this.atGrabDest && Utils.DistLess(this.tChunks[index].pos, this.floatGrabDest.Value, 20f))
                        this.atGrabDest = true;

                    //修改Chunk的速度
                    if (this.tChunks[index].currentSegment <= this.grabPath.Count || !flag2)
                        this.tChunks[index].vel += Vector2.ClampMagnitude(this.floatGrabDest.Value - this.tChunks[index].pos, 20f) / 20f * 1.2f;
                    else if (index > 1 && this.segments.Count > this.grabPath.Count && flag2)
                    {
                        float ratio = Mathf.InverseLerp(grabPath.Count, segments.Count, tChunks[index].currentSegment);
                        Vector2 dir = Utils.DirVec(this.tChunks[index - 2].pos, this.tChunks[index].pos) * (1f - ratio) * 0.6f + Utils.DirVec(this.tChunks[index].pos, this.room.MiddleOfTile(this.GrabDest.Value)) * Mathf.Pow(1f - ratio, 4f) * 2f + Utils.DirVec(this.tChunks[index].pos, this.room.MiddleOfTile(this.secondaryGrabPos)) * Mathf.Pow(ratio, 4f) * 2f + Utils.DirVec(this.tChunks[index].pos, this.FloatBase) * Mathf.Sin(ratio * (float)Math.PI) * 0.3f;
                        this.tChunks[index].vel += dir.normalized * 1.2f;
                        if (index == this.tChunks.Length - 1)
                            this.tChunks[index].vel += Vector2.ClampMagnitude(this.room.MiddleOfTile(this.secondaryGrabPos) - this.tChunks[index].pos, 20f) / 20f * 4.2f;
                    }
                }
            }
        }
        //如果抓取目标点存在值
        if (this.GrabDest.HasValue)
            this.ConsiderSecondaryGrabPos(this.GrabDest.Value + new IntVector2(UnityEngine.Random.Range(-20, 21), UnityEngine.Random.Range(-20, 21)));

        //如果没有抓取目标点  or  不在抓取目标点：执行更新抓取目标点
        if (!this.GrabDest.HasValue || !this.atGrabDest)
        {
            this.UpdateClimbGrabPos();
        }
    }


    //附着在地形上
    private void StickToTerrain(Tentacle.TentacleChunk chunk)
    {
        //如果有抓取目标点  &&  当前Chunk与目标点距离大于200
        if (this.floatGrabDest.HasValue && !Utils.DistLess(chunk.pos, this.floatGrabDest.Value, 200f))
            return;

        //Sign：返回正负号
        int sign = (int)Mathf.Sign(chunk.pos.x - this.room.MiddleOfTile(chunk.pos).x);
        Vector2 contact = new Vector2(0.0f, 0.0f);
        IntVector2 tilePos = this.room.GetTilePosition(chunk.pos);
        //遍历8方向
        for (int index = 0; index < 8; ++index)
        {
            //目标方向Tile是Solid地形
            if (this.room.GetTile(tilePos + new IntVector2(Utils.eightDirectionsDiagonalsLast[index].x * sign, Utils.eightDirectionsDiagonalsLast[index].y)).Solid)
            {
                if (Utils.eightDirectionsDiagonalsLast[index].x != 0)
                    contact.x = this.room.MiddleOfTile(chunk.pos).x + Utils.eightDirectionsDiagonalsLast[index].x * sign * (20f - chunk.rad);
                if (Utils.eightDirectionsDiagonalsLast[index].y != 0)
                {
                    contact.y = this.room.MiddleOfTile(chunk.pos).y + Utils.eightDirectionsDiagonalsLast[index].y * (20f - chunk.rad);
                    break;
                }
                break;
            }
        }
        if (contact.x == 0.0 && this.room.GetTile(chunk.pos).verticalBeam)
            contact.x = this.room.MiddleOfTile(chunk.pos).x;
        if (contact.y == 0.0 && this.room.GetTile(chunk.pos).horizontalBeam)
            contact.y = this.room.MiddleOfTile(chunk.pos).y;

        
        //未处于相同X轴
        if (contact.x != 0.0)
        {
            chunk.vel.x += (float)((contact.x - (double)chunk.pos.x) * 0.1f);
            chunk.vel.y *= 0.9f;
        }
        //未处于相同Y轴
        if (contact.y != 0.0)
        {
            chunk.vel.y += (float)((contact.y - (double)chunk.pos.y) * 0.1f);
            chunk.vel.x *= 0.9f;
        }

        //触手抓取比例增加
        if (contact.x != 0.0 || contact.y != 0.0)
        {
            this.chunksGripping += 1f / tChunks.Length;
        }
    }

    #region  抓取位置打分

    //抓取位置打分策略：传入测试位置 和 理想抓取位置
    private void ConsiderGrabPos(Vector2 testPos, Vector2 idealGrabPos)
    {
        //如果测试点打分 大于初步点，修改初步点为测试点
        if (this.GrabPosScore(testPos, idealGrabPos) > (double)this.GrabPosScore(this.preliminaryGrabDest, idealGrabPos))
        {
            this.preliminaryGrabDest = testPos;
        }
    }
    private float GrabPosScore(Vector2 testPos, Vector2 idealGrabPos)
    {
        //分数 = 100/距离值
        float score = 100f / Vector2.Distance(testPos, idealGrabPos);
        //如果有抓取目标点，且目标点等于测试点  分数*1.5倍
        if (this.GrabDest.HasValue && this.room.GetTilePosition(testPos) == this.GrabDest.Value)
            score *= 1.5f;
        //遍历测试点四个方向如果有Solid地形，分数*2倍
        for (int index = 0; index < 4; ++index)
        {
            if (this.room.GetTile(testPos + Utils.fourDirections[index].ToVector2() * 20f).Solid)
            {
                score *= 2f;
                break;
            }
        }
        return score;
    }

    private void ConsiderSecondaryGrabPos(IntVector2 testPos)
    {
        //如果测试点地形为Solid  or  测试点打分 小于当前 次级抓取点打分
        if (this.room.GetTile(testPos).Solid || this.SecondaryGrabPosScore(testPos) <= (double)this.SecondaryGrabPosScore(this.secondaryGrabPos))
            return;
        this.secondaryGrabBackTrackCounter = 0;
        this.secondaryGrabPos = testPos;
    }

    private float SecondaryGrabPosScore(IntVector2 testPos)
    {
        //没有抓取目标  or  测试点太近
        if (!this.GrabDest.HasValue || testPos.FloatDist(this.BasePos) < 7.0)
        {
            return 0.0f;
        }
           
        float num1 = this.idealLength - grabPath.Count * 20f;
        if (Vector2.Distance(this.room.MiddleOfTile(testPos), this.floatGrabDest.Value) > (double)num1 
            || !PhysicsUtils.RayTraceTilesForTerrain(this.room, this.GrabDest.Value, testPos))
            return 0.0f;
        //测试点周围8方向存在Solid +分
        float num2 = 0.0f;
        for (int index = 0; index < 8; ++index)
        {
            if (this.room.GetTile(testPos + Utils.eightDirections[index]).Solid)
                ++num2;
        }
        //如果测试点地形是Beam  加分
        if (this.room.GetTile(testPos).horizontalBeam || this.room.GetTile(testPos).verticalBeam)
            ++num2;
        //如果测试分 大于0 且测试点等于次级抓取点 加分
        if (num2 > 0.0 && testPos == this.secondaryGrabPos)
            ++num2;
        if (num2 == 0.0)
            return 0.0f;
        //复杂计算：分数、距离、索引节等等
        return (num2 + testPos.FloatDist(this.BasePos) / 10f) / (1f + Mathf.Abs(num1 * 0.75f - Vector2.Distance(this.room.MiddleOfTile(testPos), this.floatGrabDest.Value)) + Vector2.Distance(this.room.MiddleOfTile(testPos), this.room.MiddleOfTile(this.segments[this.segments.Count - 1])));
    }

    //释放打分
    public float ReleaseScore()
    {
        float minDist = float.MaxValue;
        //遍历后半段Chunk
        for (int index = this.tChunks.Length / 2; index < this.tChunks.Length; ++index)
        {
            //寻找一个与理想抓取位置最近的Chunk 距离
            if (Utils.DistLess(this.tChunks[index].pos, this.idealGrabPos, minDist))
                minDist = Vector2.Distance(this.tChunks[index].pos, this.idealGrabPos);
        }
        return minDist;
    }

    #endregion

    //更新攀爬抓取位置
    //preliminary：初步的
    public void UpdateClimbGrabPos()
    {
        this.MoveGrabDest(this.preliminaryGrabDest);
    }

    //随机重力方向
    protected override IntVector2 GravityDirection()
    {
        if (UnityEngine.Random.value < 0.5)
            //判断Tip是在连接的BodyChunk的左边或者右边
            return new IntVector2(Tip.pos.x >= (double)this.connectedChunk.pos.x ? 1 : -1, -1);
        return new IntVector2(0, -1);
    }
}
