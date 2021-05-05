using PASystem;
using System.Collections.Generic;
using UnityEngine;

public abstract class PhysicalObject : UpdatableAndDeletable
{
    public const float impactTreshhold = 1f;      //物理对象影响地形阈值
    public BodyChunkConnection[] bodyChunkConnections;   //肢节Joint      
    public float collisionRange;        //碰撞半径
    public int collisionLayer;          //碰撞层

    public WorldEntity entity;          //标记实体（位置、ID）

    public List<PhysicalObject.Appendage> appendages;   //附属物

    public PhysicalObject(WorldEntity worldEntity)
    {
        this.entity = worldEntity;
        this.collisionRange = 50f;
    }

    #region 物理属性

    private float g;    //重力
    public float surfaceFriction;   //表面摩擦力
    public float bounce;    //弹性

    //空气阻力
    public float airFriction { get; protected set; }

    //BodyChunk数组
    public BodyChunk[] bodyChunks { get; protected set; }

    //视觉表现模块
    public GraphicsModule graphicsModule { get; protected set; }

    //第一块BodyChunk
    public BodyChunk firstChunk
    {
        get
        {
            return this.bodyChunks[0];
        }
    }

    //计算BodyChunk总质量
    public float TotalMass
    {
        get
        {
            float mass = 0f;
            foreach (var chunk in bodyChunks)
            {
                mass += chunk.mass;
            }
            return mass;
        }
    }

    //重力：个性化数值 * 房间重力系数
    public float gravity
    {
        get
        {
            return this.g * this.room.gravity;
        }
        protected set
        {
            this.g = value;
        }
    }

    //检测BodyChunk的状态：是否正在穿越地板
    public bool GoThroughFloors
    {
        get
        {
            for (int index = 0; index < this.bodyChunks.Length; ++index)
            {
                if (!this.bodyChunks[index].goThroughFloors)
                    return false;
            }
            return true;
        }
        protected set
        {
            for (int index = 0; index < this.bodyChunks.Length; ++index)
                this.bodyChunks[index].goThroughFloors = value;
        }
    }

    //检测BodyChunk的状态：是否与地形发生碰撞
    public bool CollideWithTerrain
    {
        get
        {
            for (int index = 0; index < this.bodyChunks.Length; ++index)
            {
                if (this.bodyChunks[index].collideWithTerrain)
                    return true;
            }
            return false;
        }
        set
        {
            for (int index = 0; index < this.bodyChunks.Length; ++index)
                this.bodyChunks[index].collideWithTerrain = value;
        }
    }

    //检测BodyChunk的状态：是否与斜坡发生碰撞
    public bool CollideWithSlopes
    {
        get
        {
            for (int i = 0; i < this.bodyChunks.Length; ++i)
            {
                if (this.bodyChunks[i].collideWithSlopes)
                    return true;
            }
            return false;
        }
        protected set
        {
            for (int index = 0; index < this.bodyChunks.Length; ++index)
                this.bodyChunks[index].collideWithSlopes = value;
        }
    }

    //检测BodyChunk的状态：是否与其他物体发生碰撞
    public bool CollideWithObjects
    {
        get
        {
            for (int i = 0; i < this.bodyChunks.Length; ++i)
            {
                if (this.bodyChunks[i].collideWithObjects)
                    return true;
            }
            return false;
        }
        set
        {
            for (int i = 0; i < this.bodyChunks.Length; ++i)
                this.bodyChunks[i].collideWithObjects = value;
        }
    }

    #endregion

    public override void Update(bool eu)
    {
        //更新BodyChunk
        foreach (var chunk in this.bodyChunks)
        {
            chunk.Update();
        }
        //设置抽象生物Tile 等于 FirstChunk的位置
        this.entity.pos.Tile = this.room.GetTilePosition(this.firstChunk.pos);
        //更新Body的Joint
        for (int index = 0; index < this.bodyChunkConnections.Length; ++index)
        {
            this.bodyChunkConnections[index].Update();
        }
        base.Update(eu);
        //更新附属物
        if (this.appendages != null)
        {
            for (int index = 0; index < this.appendages.Count; ++index)
                this.appendages[index].Update();
        }
    }

    #region 物理对象虚接口

    //更换新房间
    public virtual void NewRoom(Room newRoom)
    {
    }

    //放置该对象到指定房间：对应房间添加一个对此对象的引用
    public virtual void PlaceInRoom(Room placeRoom)
    {
        placeRoom.AddObject(this);
        this.room = placeRoom;
    }

    public virtual void PushOutOf(Vector2 pos, float rad, int exceptedChunk)
    {
        //遍历BodyChunk，从某Pos向其施加一个推理，排除指定索引CHunk
        foreach (BodyChunk bodyChunk in this.bodyChunks)
        {
            if (bodyChunk.index != exceptedChunk && Utils.DistLess(bodyChunk.pos, pos, rad + bodyChunk.rad))
            {
                float num = Vector2.Distance(bodyChunk.pos, pos);
                Vector2 vector2 = Utils.DirVec(bodyChunk.pos, pos);
                bodyChunk.pos -= (rad + bodyChunk.rad - num) * vector2;
                bodyChunk.vel -= (rad + bodyChunk.rad - num) * vector2;
            }
        }
        //视觉层做推动表现
        if (this.graphicsModule != null)
        {
            this.graphicsModule.PushOutOf(pos, rad);
        }
    }

    //与其他物理对象发生碰撞
    public virtual void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
    {
    }


    //地形影响
    public virtual void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        if (!firstContact) return;
    }


    #endregion

    #region Graphic模块

    public virtual void InitiateGraphicsModule()
    {
    }

    public virtual void GraphicsModuleUpdated(bool actuallyViewed, bool eu)
    {
    }

    public virtual void RemoveGraphicsModule()
    {
        this.graphicsModule = null;
    }

    #endregion

    #region 公开API

    //改变此物理对象碰撞层
    public void ChangeCollisionLayer(int newCollisionLayer)
    {
        if (this.room == null)
            this.collisionLayer = newCollisionLayer;
        else
            this.room.ChangeCollisionLayerForObject(this, newCollisionLayer);
    }

    //检测指定Chunk关联地形是否为Solid
    public bool IsTileSolid(int bChunk, int relativeX, int relativeY)
    {
        switch (this.room.GetTile(this.room.GetTilePosition(this.bodyChunks[bChunk].pos) + new IntVector2(relativeX, relativeY)).Terrain)
        {
            case Room.Tile.TerrainType.Solid:
                return true;
            case Room.Tile.TerrainType.Floor:
                if (relativeY < 0 && !this.bodyChunks[bChunk].goThroughFloors)
                    return true;
                break;
        }
        return false;
    }

    //以A、BChunk的重量为权重，推动  ABChunk
    public void WeightedPush(int A, int B, Vector2 dir, float frc)
    {
        float ratio = this.bodyChunks[B].mass / (this.bodyChunks[A].mass + this.bodyChunks[B].mass);
        this.bodyChunks[A].vel += dir * frc * ratio;
        this.bodyChunks[B].vel -= dir * frc * (1f - ratio);
    }

    #endregion

    #region Joint

    //肢节连接器  ：物理Joint：让两连接体倾向于保持 正常距离
    public class BodyChunkConnection
    {
        public BodyChunk chunk1;    //连接的两个BodyChunk
        public BodyChunk chunk2;
        public float distance;          //正常距离
        public float elasticity;        //弹性
        public float weightSymmetry;    //重量对称
        public bool active;             //是否激活
        public BodyChunkConnection.Type type;   //类型

        public BodyChunkConnection(BodyChunk chunk1, BodyChunk chunk2, float distance, PhysicalObject.BodyChunkConnection.Type type, float elasticity, float weightSymmetry)
        {
            this.chunk1 = chunk1;
            this.chunk2 = chunk2;
            this.distance = distance;
            this.type = type;
            this.elasticity = elasticity;
            this.weightSymmetry = weightSymmetry != -1.0 ? weightSymmetry : chunk2.mass / (chunk1.mass + chunk2.mass);
            this.active = true;
            chunk1.rotationChunk = chunk2;
            chunk2.rotationChunk = chunk1;
        }

        public void Update()
        {
            //检测是否激活
            if (!this.active) return;

            float dist = Vector2.Distance(this.chunk1.pos, this.chunk2.pos);
            //检测条件：让两连接体倾向于保持 正常距离
            if (this.type != PhysicalObject.BodyChunkConnection.Type.Normal
                && (this.type != PhysicalObject.BodyChunkConnection.Type.Pull || dist <= (double)this.distance)
                && (this.type != PhysicalObject.BodyChunkConnection.Type.Push || dist >= (double)this.distance))
            {
                return;
            }

            Vector2 dir = Utils.DirVec(this.chunk1.pos, this.chunk2.pos);
            this.chunk1.pos -= (this.distance - dist) * dir * this.weightSymmetry * this.elasticity;
            this.chunk1.vel -= (this.distance - dist) * dir * this.weightSymmetry * this.elasticity;
            this.chunk2.pos += (this.distance - dist) * dir * (1f - this.weightSymmetry) * this.elasticity;
            this.chunk2.vel += (this.distance - dist) * dir * (1f - this.weightSymmetry) * this.elasticity;
        }

        public enum Type
        {
            Normal, //正常
            Pull,   //拉力
            Push,   //推开
        }
    }

    #endregion

    #region 附属物

    //拥有附属物行为接口
    public interface IHaveAppendages
    {
        Vector2 AppendagePosition(int appendage, int segment);

        void ApplyForceOnAppendage(PhysicalObject.Appendage.Pos pos, Vector2 momentum);
    }

    //附属物 数据结构
    public class Appendage
    {
        public bool canBeHit = true;    //是否可以被击中
        public PhysicalObject owner;    //附属对象
        public PhysicalObject.IHaveAppendages ownerApps;    //持有者接口
        public Vector2[] segments;      //肢节
        public int appIndex;
        public float totalLength;

        public Appendage(PhysicalObject owner, int appIndex, int totSegs)
        {
            this.owner = owner;
            this.appIndex = appIndex;
            this.ownerApps = owner as PhysicalObject.IHaveAppendages;
            this.segments = new Vector2[totSegs];
            for (int index = 0; index < totSegs; ++index)
            {
                this.segments[index] = owner.firstChunk.pos;
            }
        }

        public void Update()
        {
            //实时计算总长度
            this.totalLength = 0.0f;
            for (int segment = 0; segment < this.segments.Length; ++segment)
            {
                this.segments[segment] = this.ownerApps.AppendagePosition(this.appIndex, segment);
                if (segment > 0)
                    this.totalLength += Vector2.Distance(this.segments[segment - 1], this.segments[segment]);
            }
        }

        public Vector2 OnAppendagePosition(PhysicalObject.Appendage.Pos pos)
        {
            return Vector2.Lerp(this.segments[pos.prevSegment], this.segments[pos.prevSegment + 1], pos.distanceToNext);
        }

        public Vector2 OnAppendageDirection(PhysicalObject.Appendage.Pos pos)
        {
            Vector2 vector2 = pos.prevSegment >= this.segments.Length - 2 ? Utils.DirVec(this.segments[pos.prevSegment], this.segments[pos.prevSegment + 1]) : Utils.DirVec(this.segments[pos.prevSegment + 1], this.segments[pos.prevSegment + 2]);
            return Vector3.Slerp(Utils.DirVec(this.segments[pos.prevSegment], this.segments[pos.prevSegment + 1]), vector2, pos.distanceToNext);
        }

        public bool LineCross(Vector2 A, Vector2 B)
        {
            for (int index = 1; index < this.segments.Length; ++index)
            {
                Vector2 vector2 = Utils.LineIntersection(A, B, this.segments[index - 1], this.segments[index]);
                Mathf.InverseLerp(0.0f, Vector2.Distance(A, B), Vector2.Distance(A, vector2));
                if (Utils.DistLess(vector2, A, Vector2.Distance(A, B)) && Utils.DistLess(vector2, B, Vector2.Distance(A, B)) && (Utils.DistLess(vector2, this.segments[index - 1], Vector2.Distance(this.segments[index - 1], this.segments[index])) && Utils.DistLess(vector2, this.segments[index], Vector2.Distance(this.segments[index - 1], this.segments[index]))))
                    return true;
            }
            return false;
        }

        public class Pos
        {
            public PhysicalObject.Appendage appendage;
            public int prevSegment;
            public float distanceToNext;

            public Pos(PhysicalObject.Appendage appendage, int prevSegment, float distanceToNext)
            {
                this.appendage = appendage;
                this.prevSegment = prevSegment;
                this.distanceToNext = distanceToNext;
            }
        }
    }

    #endregion
}
