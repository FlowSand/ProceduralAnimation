using PASystem;
using System.Collections.Generic;
using UnityEngine;

//功能：表现层模块

public abstract class GraphicsModule : IDrawable
{
    public PhysicalObject owner { get; private set; }   //显示层持有者：物理对象

    public BodyPart[] bodyParts;//显示层由多个Part组成

    public GraphicsModule(PhysicalObject ow)
    {
        owner = ow;
    }

    public virtual void Update()
    {

    }

    //重置BodyPart位置：主ChunkPos
    public virtual void Reset()
    {
        if (bodyParts == null) return;

        for (int index = 0; index < bodyParts.Length; ++index)
        {
            bodyParts[index].Reset(owner.firstChunk.pos);
        } 
    }

    //遍历全部BodyPart从目标Pos推开 ：维持距离Rad + Part.rad
    public virtual void PushOutOf(Vector2 pos, float rad)
    {
        if (bodyParts == null) return;

        foreach (BodyPart bodyPart in bodyParts)
        {
            //检测Chunk位置和目标Pos是否小于半径和
            if (Utils.DistLess(bodyPart.pos, pos, rad + bodyPart.rad))
            {
                float dist = Vector2.Distance(bodyPart.pos, pos);
                Vector2 dir = Utils.DirVec(bodyPart.pos, pos);
                bodyPart.pos -= (rad + bodyPart.rad - dist) * dir;
                bodyPart.vel -= (rad + bodyPart.rad - dist) * dir;
            }
        }
    }

    #region IDrawable

    public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {

    }

    public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        //检测清除
        if (owner.slatedForDeletetion || owner.room != rCam.room) 
        {
            sLeaser.CleanSpritesAndRemove();
        }
    }

    public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        //默认层：Midground
        sLeaser.RemoveAllSpritesFromContainer();
        if (newContatiner == null)
            newContatiner = rCam.ReturnFContainer("Midground");
        foreach (FSprite sprite in sLeaser.sprites)
        {
            newContatiner.AddChild(sprite);
        }
           
        if (sLeaser.containers != null)
        {
            foreach (FContainer container in sLeaser.containers)
                newContatiner.AddChild(container);
        }  
    }

    #endregion
}
