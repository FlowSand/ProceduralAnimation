using PASystem;
using System.Collections.Generic;
using UnityEngine;

//功能：触手生物

public class TentaclePlant : Creature, PhysicalObject.IHaveAppendages
{
    public Tentacle tentacle;       //触手体

    public float rootRad = 8f;      //根半径
    public float tipRad = 1f;       //顶尖半径
    public Vector2 rootPos;         //根部位置
    public float extended;          //伸展系数

    public Vector2 stickOutDir;
    public Vector2 idlePos;
    public float attack;
    public Vector2 attackDir;
    
    //Debug AI模块
    public Vector2? mousePos;    //鼠标位置

    public TentaclePlant(WorldEntity entity)
      : base(entity)
    {
        //随机生物个性种子
        int seed = Random.seed;
        Random.seed = entity.ID.RandomSeed;
        //初始化BodyChunk：两个肢节
        this.bodyChunks = new BodyChunk[2];
        this.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0.0f, 0.0f), this.tipRad, 0.2f);
        this.bodyChunks[1] = new BodyChunk(this, 0, new Vector2(0.0f, 0.0f), this.rootRad, 0.2f);
        this.bodyChunks[1].collideWithTerrain = false;
        this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];

        //初始化触手相关属性：连接基座，长度300，Chunk=8
        this.tentacle = new Tentacle(this, this.bodyChunks[1], Random.Range(200,300));
        this.tentacle.tProps = new Tentacle.TentacleProps(false, true, true, 0.5f, 0.0f, 0.5f, 0.05f, 0.05f, 2.2f, 12f, 0.33f, 5f, 15, 60, 12, 20);
        this.tentacle.tChunks = new Tentacle.TentacleChunk[8];
        for (int index = 0; index < this.tentacle.tChunks.Length; ++index)
        {
            this.tentacle.tChunks[index] = new Tentacle.TentacleChunk(this.tentacle, index, (index + 1) / (float)this.tentacle.tChunks.Length, Mathf.Lerp(this.rootRad, this.tipRad, index / (float)(this.tentacle.tChunks.Length - 1)));
        }  
        this.tentacle.stretchAndSqueeze = 0.1f;
        
        //TODO 附属物
        this.appendages = new List<PhysicalObject.Appendage>();
        this.appendages.Add(new PhysicalObject.Appendage(this, 0, this.tentacle.tChunks.Length + 1));

        //初始化生物 物理参数
        this.GoThroughFloors = true;
        this.airFriction = 1f;
        this.gravity = 0.9f;
        this.bounce = 0.1f;
        this.surfaceFriction = 0.5f;
        this.collisionLayer = 1;
        this.extended = 1f;

        Random.seed = seed;
    }

    public override void InitiateGraphicsModule()
    {

    }

    public float Rad(float ratio)
    {
        ratio = Mathf.Max(1f - ratio, Mathf.Sin(Mathf.PI * Mathf.InverseLerp(0.7f, 1f, ratio)));
        return Mathf.Lerp(this.tipRad, this.rootRad, ratio);
    }

    public void ResetPlant(Vector2 rootPos,Vector2 stickOutDir)
    {
        this.stickOutDir = stickOutDir;
        this.rootPos = room.MiddleOfTile(rootPos);
        this.idlePos = this.rootPos + this.stickOutDir * Mathf.Lerp(200f, 300f, Random.value);
    }

    //迁移到新房间
    public override void NewRoom(Room room)
    {
        //触手迁移到新房间
        this.tentacle.CreateInRoom(room);
        //基类迁移新房间
        base.NewRoom(room);
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
        if (this.room == null) return;

        //根据Room查询当前生物位置的Tile
        this.entity.pos.Tile = this.room.GetTilePosition(this.rootPos);

        //更新触手、及其状态标志位
        this.tentacle.Update();
        //触手瘫软：生物是否有意识
        this.tentacle.limp = !this.Consious;
        //根据伸展系数计算撤回系数
        this.tentacle.retractFac = 1f - this.extended;

        //根据攻击状态实时计算 MainBody的 半径
        this.mainBodyChunk.rad = this.attack <= 1.0 ? this.tipRad : 9f;
        //完全伸展：触手未与其他地形碰撞
        this.mainBodyChunk.collideWithTerrain = extended == 1.0 && this.tentacle.backtrackFrom == -1;
        //如果两块Chunk的距离 超过理想长度 * 2 * 伸展系数，修改Chunk0的位置 在极限长度位置
        if (!Utils.DistLess(this.bodyChunks[1].pos, this.bodyChunks[0].pos, this.tentacle.idealLength * 2f * this.extended))
        {
            this.bodyChunks[0].pos = this.bodyChunks[1].pos + Utils.DirVec(this.bodyChunks[1].pos, this.bodyChunks[0].pos) * this.tentacle.idealLength * 2f * this.extended;
        }
            
        //Debug模式实时获取鼠标位置
        if (Configs.DEV_TOOL_ACTIVE)
        {
            mousePos = (Vector2)Input.mousePosition + this.room.game.Camera.pos;
        }

        //如果当前没有抓取目标点： 抓取理想停留点
        if (!this.tentacle.GrabDest.HasValue)
        {
            this.tentacle.MoveGrabDest(this.idlePos);
        }

        //计算Attack系数
        float attack = this.attack;
        if (this.Consious)
        {
            if (attack >= 1)
            {
                //累增Attack系数 ，修改Tip和 MainBody的速度 朝向 攻击方向
                this.attack += 0.1f;
                this.tentacle.Tip.vel += this.attackDir * 20f;
                this.mainBodyChunk.vel += this.attackDir * 20f;
                //攻击系数重置为0
                if (this.attack > 2.0)
                    this.attack = 0.0f;
            }
            else
            {
                //如果开启Debug模式获取到鼠标位置
                if (mousePos.HasValue)
                {
                    Vector2 destPos = mousePos.Value;
                    if (Utils.DistLess(destPos, this.rootPos, this.tentacle.idealLength))
                    {
                        this.attackDir = Utils.DirVec(this.mainBodyChunk.pos, destPos);
                        this.attack += 0.1f;
                    }
                    this.tentacle.MoveGrabDest(destPos);
                }
                else
                {
                    this.attack -= 0.005f;
                }
            }

            //如果有抓取目标点：修改TipChunk的速度向目标点
            if (this.tentacle.floatGrabDest.HasValue)
                this.bodyChunks[0].vel += Vector2.ClampMagnitude(this.tentacle.floatGrabDest.Value - this.bodyChunks[0].pos, 20f) / 20f;
        }
        else
        {
            attack = 0f;
        }

        //遍历触手所有Chunk
        for (int index = 0; index < this.tentacle.tChunks.Length; ++index)
        {
            float t = index / (float)(this.tentacle.tChunks.Length - 1);
            //速度衰弱
            this.tentacle.tChunks[index].vel *= 0.96f;

            if (this.tentacle.backtrackFrom == -1 || this.tentacle.backtrackFrom > index - 2)
            {
                if (this.attack > 0.5 && this.attack < 1.0)
                {
                    this.tentacle.tChunks[index].vel += Utils.DirVec(this.tentacle.floatGrabDest.Value, Vector2.Lerp(this.mainBodyChunk.pos, this.rootPos + this.stickOutDir * 200f, 0.5f)) * this.attack * 0.8f;
                }
                    
                Vector2 preChunkPos = this.rootPos - this.stickOutDir * 30f;
                if (index == 1)
                {
                    preChunkPos = this.rootPos;
                }
                else if (index > 1)
                {
                    preChunkPos = this.tentacle.tChunks[index - 2].pos;
                    this.tentacle.tChunks[index - 2].vel += Utils.DirVec(this.tentacle.tChunks[index].pos, this.tentacle.tChunks[index - 2].pos);
                }
                this.tentacle.tChunks[index].vel += Utils.DirVec(preChunkPos, this.tentacle.tChunks[index].pos);
                if (this.Consious)
                {
                    this.tentacle.tChunks[index].vel += this.stickOutDir * Mathf.Lerp(0.3f, 0.0f, t);
                }
            }
        }
        this.tentacle.retractFac = 1f - this.extended;

        //如果伸展系数为0，重置Chunk位置和速度
        if (extended == 0.0)
        {
            for (int index = 0; index < 2; ++index)
            {
                this.bodyChunks[index].collideWithTerrain = false;
                this.bodyChunks[index].HardSetPosition(this.rootPos + new Vector2(0.0f, -50f));
                this.bodyChunks[index].vel *= 0.0f;
            }
        }
        else
        {
            //设置RootChunk 位置和速度
            this.bodyChunks[1].pos = this.rootPos;
            this.bodyChunks[1].vel *= 0.0f;

            //判断两Chunk与地形碰撞
            for (int index = 0; index < 2; ++index)
            {
                this.bodyChunks[index].collideWithTerrain = this.tentacle.backtrackFrom == -1;
            }

            float ratio = 0.0f;
            if (this.tentacle.backtrackFrom == -1)
            {
                ratio = this.Consious ? 0.5f : 0.7f;
            }
            //计算Chunk0和触手尖 方向和距离，彼此拉近
            Vector2 dir = Utils.DirVec(this.bodyChunks[0].pos, this.tentacle.Tip.pos);
            float dist = Vector2.Distance(this.bodyChunks[0].pos, this.tentacle.Tip.pos);
            this.bodyChunks[0].pos +=  dist * dir * (1f - ratio);
            this.bodyChunks[0].vel +=  dist * dir * (1f - ratio);
            this.tentacle.Tip.pos -=  dist * dir * ratio;
            this.tentacle.Tip.vel -=  dist * dir * ratio;
            //重力影响
            if (this.Consious)
            {
                this.bodyChunks[0].vel.y += this.gravity;
            }    
        }

        this.extended = 1f;
    }

    #region IHaveAppendages

    //根据Segment索引返回附属物连接的Chunk位置
    public Vector2 AppendagePosition(int appendage, int segment)
    {
        if (segment == 0)
            return this.rootPos;
        return this.tentacle.tChunks[segment - 1].pos;
    }

    public void ApplyForceOnAppendage(PhysicalObject.Appendage.Pos pos, Vector2 momentum)
    {
        if (pos.prevSegment > 0)
        {
            this.tentacle.tChunks[pos.prevSegment - 1].pos += momentum / 0.2f * (1f - pos.distanceToNext);
            this.tentacle.tChunks[pos.prevSegment - 1].vel += momentum / 0.1f * (1f - pos.distanceToNext);
        }
        this.tentacle.tChunks[pos.prevSegment].pos += momentum / 0.2f * pos.distanceToNext;
        this.tentacle.tChunks[pos.prevSegment].vel += momentum / 0.1f * pos.distanceToNext;
    }

    #endregion
}
