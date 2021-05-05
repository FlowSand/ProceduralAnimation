using UnityEngine;

public class CosmeticSprite : UpdatableAndDeletable, IDrawable
{
    public Vector2 pos;     //当前位置
    public Vector2 lastPos; //上一帧位置
    public Vector2 vel;     //速度

    public override void Update(bool eu)
    {
        lastPos = pos;
        pos += vel;  
        base.Update(eu);
    }
  
    public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
    }

    public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        //逐帧监听删除
        if (slatedForDeletetion || room != rCam.room)
        {
            sLeaser.CleanSpritesAndRemove();
        }
    }

    public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        //默认添加在Midground Layer
        if (newContatiner == null)
            newContatiner = rCam.ReturnFContainer("Midground");
        foreach (FSprite sprite in sLeaser.sprites)
        {
            sprite.RemoveFromContainer();
            newContatiner.AddChild(sprite);
        }
    }
}
