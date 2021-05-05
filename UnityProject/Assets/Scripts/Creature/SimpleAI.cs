using UnityEngine;
using PASystem;

public class SimpleAI 
{
    public IntVector2 DestTile;     //目标Tile
    public Creature crit;           //生物

    public bool arrived = false;    //是否到达目标点

    public SimpleAI(Creature crit)
    {
        this.crit = crit;
        DestTile = crit.room.GetTilePosition(new Vector2(530, 550));
    }

    public void Update()
    {
        //如果检测到按键E，则将鼠标位置设置为目标点
        if (Input.GetKey("e"))
        {
            DestTile = crit.room.GetTilePosition((Vector2)Input.mousePosition + crit.room.game.Camera.pos);
        }

        //如果生物已接近目标点，设置AI到达标志位
        if (Utils.DistLess(crit.entity.pos.Tile, DestTile,2))
        {
            arrived = true;
        }
        else
        {
            arrived = false;
        }
    }
}
