using UnityEngine;

//TODO 替换生物表现层为测试模式

//功能：测试模式的表现层

public class DebugGraphicsModule : GraphicsModule
{
    public DebugGraphicsModule(PhysicalObject ow)
      : base(ow)
    {
    }

    public override void Update()
    {
        base.Update();
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        //物理对象的每一个Chunk都需要对应一个Sprite
        sLeaser.sprites = new FSprite[this.owner.bodyChunks.Length];
        for (int index = 0; index < this.owner.bodyChunks.Length; ++index)
        {
            sLeaser.sprites[index] = new FSprite("Circle20", true);
            sLeaser.sprites[index].scale = this.owner.bodyChunks[index].rad / 10f;
            sLeaser.sprites[index].color = new Color(1f, index != 0 ? 0.0f : 0.5f, index != 0 ? 0.0f : 0.5f);
        }
        this.AddToContainer(sLeaser, rCam, null);
        base.InitiateSprites(sLeaser, rCam);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        //插值计算Sprite位置：跟随Chunk位置
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        for (int index = 0; index < this.owner.bodyChunks.Length; ++index)
        {
            sLeaser.sprites[index].x = Mathf.Lerp(this.owner.bodyChunks[index].lastPos.x, this.owner.bodyChunks[index].pos.x, timeStacker) - camPos.x;
            sLeaser.sprites[index].y = Mathf.Lerp(this.owner.bodyChunks[index].lastPos.y, this.owner.bodyChunks[index].pos.y, timeStacker) - camPos.y;
        }
    }

    public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
    {
        base.AddToContainer(sLeaser, rCam, newContatiner);
    }
}
