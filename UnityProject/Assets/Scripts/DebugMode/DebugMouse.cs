using System;
using UnityEngine;

public class DebugMouse : CosmeticSprite, IDrawable
{
    public FLabel label;
    private Vector2 dataPos;

    public DebugMouse()
    {
        label = new FLabel("font", "0");
        label.alignment = FLabelAlignment.Left;
        label.color = new Color(1f, 0.0f, 0.0f);
    }

    public override void Update(bool eu)
    {
        //实时位置 = 鼠标位置 + 房间相机偏移
        pos = (Vector2)Input.mousePosition + room.game.Camera.pos;
        //设置DebugStr信息
        string str = "pos: x=" + pos.x + " y=" + pos.y + "     TPOS: X=" + room.GetTilePosition(pos).x + " Y=" + room.GetTilePosition(pos).y + Environment.NewLine;

        label.text = str;
        base.Update(eu);
    }

    public override void Destroy()
    {
        label.RemoveFromContainer();
        base.Destroy();
    }

    public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
    {
        sLeaser.sprites = new FSprite[6];
        sLeaser.sprites[0] = new FSprite("pixel", true);
        sLeaser.sprites[0].color = new Color(1f, 0.0f, 0.0f);
        sLeaser.sprites[0].scale = 10f;
        sLeaser.sprites[0].anchorX = 0.0f;
        sLeaser.sprites[0].anchorY = 1f;
        rCam.ReturnFContainer("HUD2").AddChild(sLeaser.sprites[0]);
        rCam.ReturnFContainer("HUD2").AddChild(label);
        for (int index = 1; index < 5; ++index)
        {
            sLeaser.sprites[index] = new FSprite("pixel", true);
            sLeaser.sprites[index].color = new Color(0.0f, 1f, 0.0f);
            sLeaser.sprites[index].alpha = 1f;
            rCam.ReturnFContainer("HUD2").AddChild(sLeaser.sprites[index]);
        }
        sLeaser.sprites[1].scaleY = 21f;
        sLeaser.sprites[2].scaleX = 21f;
        sLeaser.sprites[3].scaleY = 21f;
        sLeaser.sprites[4].scaleX = 21f;
        sLeaser.sprites[5] = new FSprite("pixel", true);
        sLeaser.sprites[5].color = new Color(0.0f, 1f, 0.0f);
        sLeaser.sprites[5].scale = 7f;
        rCam.ReturnFContainer("HUD2").AddChild(sLeaser.sprites[5]);
    }

    public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        //实时计算label位置
        sLeaser.sprites[0].x = pos.x - camPos.x;
        sLeaser.sprites[0].y = pos.y - camPos.y;
        if (pos.x > camPos.x + 800f)
        {
            label.x = 800f;
            label.y = pos.y - camPos.y;
        }
        else
        {
            label.x = (float)(pos.x - (double)camPos.x + 40f);
            label.y = pos.y - camPos.y;
        }
        Vector2 vector2 = rCam.room.MiddleOfTile(pos);
        sLeaser.sprites[1].x = (float)(vector2.x - camPos.x - 10.0);
        sLeaser.sprites[1].y = vector2.y - camPos.y;
        sLeaser.sprites[2].x = vector2.x - camPos.x;
        sLeaser.sprites[2].y = (float)(vector2.y - camPos.y + 10.0);
        sLeaser.sprites[3].x = (float)(vector2.x - camPos.x + 10.0);
        sLeaser.sprites[3].y = vector2.y - camPos.y;
        sLeaser.sprites[4].x = vector2.x - camPos.x;
        sLeaser.sprites[4].y = (float)(vector2.y - camPos.y - 10.0);
        sLeaser.sprites[5].x = dataPos.x - camPos.x;
        sLeaser.sprites[5].y = (float)(dataPos.y - camPos.y - 10.0);

        base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
    }
}
