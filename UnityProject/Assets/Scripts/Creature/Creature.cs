using PASystem;
using System;
using System.Collections.Generic;
using UnityEngine;

// 生物基类

public abstract class Creature : PhysicalObject
{
    public Creature(WorldEntity entity) : base(entity)
    {
        this.mainBodyChunkIndex = 0;
    }

    // 身体主要躯干索引
    public int mainBodyChunkIndex { get; set; }

    // 身体主要躯干
    public BodyChunk mainBodyChunk
    {
        get
        {
            return this.bodyChunks[this.mainBodyChunkIndex];
        }
    }

    //根据索引获取显示模块的BodyPart
    public BodyPart BodyPartByIndex(int index)
    {
        if (this.graphicsModule == null || this.graphicsModule.bodyParts == null
            || index < 0 || index >= this.graphicsModule.bodyParts.Length)
            return null;
        return this.graphicsModule.bodyParts[index];
    }


    //是否有意识
    public bool Consious
    {
        get
        {
            return true;
        }
    }



    public override void PlaceInRoom(Room placeRoom)
    {
        this.room = placeRoom;
        //Room添加UpdateObj
        placeRoom.AddObject(this);
        //遍历BodyChunk,初始化位置和速度 = 生物Entity位置
        foreach (BodyChunk bodyChunk in this.bodyChunks)
        {
            bodyChunk.pos = placeRoom.MiddleOfTile(this.entity.pos.Tile) + Utils.DegToVec(UnityEngine.Random.value * 360f);
            bodyChunk.lastPos = bodyChunk.pos;
            bodyChunk.lastLastPos = bodyChunk.pos;
            bodyChunk.setPos = new Vector2?();
            bodyChunk.vel *= 0.0f;
        }
        this.NewRoom(placeRoom);
    }

    public override void NewRoom(Room newRoom)
    {
        if (this.graphicsModule != null)
        {
            this.graphicsModule.Reset();
        }      
    }

    public override void Update(bool eu)
    {
        base.Update(eu);
    }

    public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
    {
        base.TerrainImpact(chunk, direction, speed, firstContact);
    }

    public override void PushOutOf(Vector2 pos, float rad, int exceptedChunk)
    {
        base.PushOutOf(pos, rad, exceptedChunk);
    } 
}
