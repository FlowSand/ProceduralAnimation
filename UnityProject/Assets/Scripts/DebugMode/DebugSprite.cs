using UnityEngine;

//功能：逐帧跟随的图片

public class DebugSprite : CosmeticSprite
{
    public FSprite sprite;

    public DebugSprite(Vector2 ps, FSprite sp, Room rm)
    {
        this.pos = ps;
        this.sprite = sp;
    }

    //添加Sprite到HUD Layer
    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[1];
        sLeaser.sprites[0] = this.sprite;
        rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[0]);
    }

    //逐帧设置唯一Sprite位置
    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        sLeaser.sprites[0].x = this.pos.x - camPos.x;
        sLeaser.sprites[0].y = this.pos.y - camPos.y;
        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }
}
