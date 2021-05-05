using UnityEngine;

//功能：相机可绘制物体接口

public interface IDrawable
{
    void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam);

    void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos);

    void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner);
}
