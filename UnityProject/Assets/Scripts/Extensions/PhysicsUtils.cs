using PASystem;
using System;
using System.Collections.Generic;
using UnityEngine;
//DEL
//功能：物理检测模块

public static class PhysicsUtils
{
    public static int MaxRepeats = 1000;

    #region 射线检测函数

    //如果A、B两点直线距离上存在Solid地形：false ， 不存在：true
    public static bool RayTraceTilesForTerrain(Room room, IntVector2 a, IntVector2 b)
    {
        return RayTraceTilesForTerrain(room, a.x, a.y, b.x, b.y);
    }
    public static bool RayTraceTilesForTerrain(Room room, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int x = x0;
        int y = y0;
        int sum = 1 + dx + dy;
        int xStep = x1 <= x0 ? -1 : 1;
        int yStep = y1 <= y0 ? -1 : 1;
        int sign = dx - dy;
        int dx2 = dx * 2;
        int dy2 = dy * 2;
        for (; sum > 0; --sum)
        {
            if (room.GetTile(x, y).Solid)
                return false;
            if (sign > 0)
            {
                x += xStep;
                sign -= dy2;
            }
            else
            {
                y += yStep;
                sign += dx2;
            }
        }
        return true;
    }
    public static bool RayTraceTilesForTerrain(Room room, Vector2 a, Vector2 b)
    {
        float x0 = a.x / 20f;
        float y0 = a.y / 20f;
        float x1 = b.x / 20f;
        float y1 = b.y / 20f;
        float dx = Mathf.Abs(x1 - x0);
        float dy = Mathf.Abs(y1 - y0);
        int intX = Mathf.FloorToInt(x0);
        int intY = Mathf.FloorToInt(y0);
        float kx = 1f / dx;
        float ky = 1f / dy;
        int sum = 1;
        int xStep;
        float xInc;
        if (dx == 0)
        {
            xStep = 0;
            xInc = kx;
        }
        else if (x1 > x0)
        {
            xStep = 1;
            sum += Mathf.FloorToInt(x1) - intX;
            xInc = (Mathf.FloorToInt(x0) + 1 - x0) * kx;
        }
        else
        {
            xStep = -1;
            sum += intX - Mathf.FloorToInt(x1);
            xInc = (x0 - Mathf.FloorToInt(x0)) * kx;
        }
        int yStep;
        float yInc;
        if (dy == 0)
        {
            yStep = 0;
            yInc = ky;
        }
        else if (y1 > y0)
        {
            yStep = 1;
            sum += Mathf.FloorToInt(y1) - intY;
            yInc = (Mathf.FloorToInt(y0) + 1 - y0) * ky;
        }
        else
        {
            yStep = -1;
            sum += intY - Mathf.FloorToInt(y1);
            yInc = (y0 - Mathf.FloorToInt(y0)) * ky;
        }
        for (; sum > 0; --sum)
        {
            if (room.GetTile(intX, intY).Solid)
                return false;
            if (yInc < (double)xInc)
            {
                intY += yStep;
                yInc += ky;
            }
            else
            {
                intX += xStep;
                xInc += kx;
            }
        }
        return true;
    }

    //从A向B进行直线检测，返回第一个Solid类型的Tile
    public static IntVector2? RayTraceTilesForTerrainReturnFirstSolid(Room room, Vector2 A, Vector2 B)
    {
        float x0 = A.x / 20f;
        float y0 = A.y / 20f;
        float x1 = B.x / 20f;
        float y1 = B.y / 20f;
        float dx = Mathf.Abs(x1 - x0);
        float dy = Mathf.Abs(y1 - y0);
        int intX = Mathf.FloorToInt(x0);
        int intY = Mathf.FloorToInt(y0);
        float kx = 1f / dx;
        float ky = 1f / dy;
        int sum = 1;
        int xStep;
        float xInc;
        if (dx == 0.0)
        {
            xStep = 0;
            xInc = kx;
        }
        else if (x1 > x0)
        {
            xStep = 1;
            sum += Mathf.FloorToInt(x1) - intX;
            xInc = (Mathf.FloorToInt(x0) + 1 - x0) * kx;
        }
        else
        {
            xStep = -1;
            sum += intX - Mathf.FloorToInt(x1);
            xInc = (x0 - Mathf.FloorToInt(x0)) * kx;
        }
        int yStep;
        float yInc;
        if (dy == 0.0)
        {
            yStep = 0;
            yInc = ky;
        }
        else if (y1 > y0)
        {
            yStep = 1;
            sum += Mathf.FloorToInt(y1) - intY;
            yInc = (Mathf.FloorToInt(y0) + 1 - y0) * ky;
        }
        else
        {
            yStep = -1;
            sum += intY - Mathf.FloorToInt(y1);
            yInc = (y0 - Mathf.FloorToInt(y0)) * ky;
        }
        for (; sum > 0; --sum)
        {
            if (room.GetTile(intX, intY).Solid)
                return new IntVector2?(new IntVector2(intX, intY));
            if (yInc < xInc)
            {
                intY += yStep;
                yInc += ky;
            }
            else
            {
                intX += xStep;
                xInc += kx;
            }
        }
        return new IntVector2?();
    }

    //查找A、B两点直线上的所有Tile
    public static List<IntVector2> RayTracedTilesArray(Vector2 A, Vector2 B)
    {
        List<IntVector2> intVecLst = new List<IntVector2>();
        float x0 = A.x / 20f;
        float y0 = A.y / 20f;
        float x1 = B.x / 20f;
        float y1 = B.y / 20f;
        float dx = Mathf.Abs(x1 - x0);
        float dy = Mathf.Abs(y1 - y0);
        int intX = Mathf.FloorToInt(x0);
        int intY = Mathf.FloorToInt(y0);
        float kx = 1f / dx;
        float ky = 1f / dy;
        int sum = 1;
        int xStep;
        float xInc;
        if (dx == 0.0)
        {
            xStep = 0;
            xInc = kx;
        }
        else if (x1 > x0)
        {
            xStep = 1;
            sum += Mathf.FloorToInt(x1) - intX;
            xInc = (Mathf.FloorToInt(x0) + 1 - x0) * kx;
        }
        else
        {
            xStep = -1;
            sum += intX - Mathf.FloorToInt(x1);
            xInc = (x0 - Mathf.FloorToInt(x0)) * kx;
        }
        int yStep;
        float yInc;
        if (dy == 0.0)
        {
            yStep = 0;
            yInc = ky;
        }
        else if (y1 > y0)
        {
            yStep = 1;
            sum += Mathf.FloorToInt(y1) - intY;
            yInc = (Mathf.FloorToInt(y0) + 1 - y0) * ky;
        }
        else
        {
            yStep = -1;
            sum += intY - Mathf.FloorToInt(y1);
            yInc = (y0 - Mathf.FloorToInt(y0)) * ky;
        }
        for (; sum > 0; --sum)
        {
            intVecLst.Add(new IntVector2(intX, intY));
            if (yInc < xInc)
            {
                intY += yStep;
                yInc += ky;
            }
            else
            {
                intX += xStep;
                xInc += kx;
            }
        }
        return intVecLst;
    }

    #endregion

    #region 地形碰撞检测

    //水平碰撞检测
    public static TerrainCollisionData HorizontalCollision(Room room, TerrainCollisionData cd)
    {
        //获取前一帧Tile位置
        IntVector2 lastTile = room.GetTilePosition(cd.lastPos);
        int count = 0;
        //速度大于零：向右移动
        if (cd.vel.x > 0.0)
        {
            //获取当前帧、前一帧（位置+半径）的Tile信息
            int x1 = room.GetTilePosition(new Vector2(cd.pos.x + cd.rad, 0.0f)).x;
            int x2 = room.GetTilePosition(new Vector2(cd.lastPos.x + cd.rad, 0.0f)).x;
            int y1 = room.GetTilePosition(new Vector2(0.0f, (float)(cd.pos.y + (double)cd.rad - 1.0))).y;
            int y2 = room.GetTilePosition(new Vector2(0.0f, (float)(cd.pos.y - (double)cd.rad + 1.0))).y;
            bool @checked = false;
            //从左到右、从上到下遍历地形
            for (int x = x2; x <= x1 && !@checked; ++x)
            {
                for (int y = y2; y <= y1 && !@checked; ++y)
                {
                    if (room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid 
                        && room.GetTile(x - 1, y).Terrain != Room.Tile.TerrainType.Solid 
                        && (lastTile.x < x || room.GetTile(cd.lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        //修改碰撞数据
                        cd.pos.x = x * 20f - cd.rad;
                        cd.vel.x = 0.0f;
                        cd.contactPoint.x = 1;
                        @checked = true;
                    }
                    //碰撞次数阈值检测
                    ++count;
                    if (count > MaxRepeats)
                    {
                        Debug.Log("!!!!! sharedphysics emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
        //速度小于零：向左移动 ： 检测方式同上
        else if (cd.vel.x < 0.0)
        {
            int x1 = room.GetTilePosition(new Vector2(cd.pos.x - cd.rad, 0.0f)).x;
            int x2 = room.GetTilePosition(new Vector2(cd.lastPos.x - cd.rad, 0.0f)).x;
            int y1 = room.GetTilePosition(new Vector2(0.0f, (float)(cd.pos.y + cd.rad - 1.0))).y;
            int y2 = room.GetTilePosition(new Vector2(0.0f, (float)(cd.pos.y - cd.rad + 1.0))).y;
            bool @checked = false;
            for (int x = x2; x >= x1 && !@checked; --x)
            {
                for (int y = y2; y <= y1 && !@checked; ++y)
                {
                    if (room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid && room.GetTile(x + 1, y).Terrain != Room.Tile.TerrainType.Solid && (lastTile.x > x || room.GetTile(cd.lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        cd.pos.x = (float)((x + 1.0) * 20.0) + cd.rad;
                        cd.vel.x = 0.0f;
                        cd.contactPoint.x = -1;
                        @checked = true;
                    }
                    ++count;
                    if (count > MaxRepeats)
                    {
                        Debug.Log("!!!!! sharedphysics emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
        return cd;
    }

    //垂直碰撞检测：原理同上
    public static TerrainCollisionData VerticalCollision(Room room, TerrainCollisionData cd)
    {
        IntVector2 lastTile = room.GetTilePosition(cd.lastPos);
        int count = 0;
        //速度大于0：向上移动
        if (cd.vel.y > 0.0)
        {
            int y1 = room.GetTilePosition(new Vector2(0.0f, (float)(cd.pos.y + (double)cd.rad))).y;
            int y2 = room.GetTilePosition(new Vector2(0.0f, cd.lastPos.y + cd.rad)).y;
            int x1 = room.GetTilePosition(new Vector2((float)(cd.pos.x - (double)cd.rad + 1.0), 0.0f)).x;
            int x2 = room.GetTilePosition(new Vector2((float)(cd.pos.x + (double)cd.rad - 1.0), 0.0f)).x;
            bool @checked = false;
            for (int y = y2; y <= y1 && !@checked; ++y)
            {
                for (int x = x1; x <= x2 && !@checked; ++x)
                {
                    if (room.GetTile(x, y).Terrain == Room.Tile.TerrainType.Solid && room.GetTile(x, y - 1).Terrain != Room.Tile.TerrainType.Solid && (lastTile.y < y || room.GetTile(cd.lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        cd.pos.y = y * 20f - cd.rad;
                        cd.vel.y = 0.0f;
                        cd.contactPoint.y = 1;
                        @checked = true;
                    }
                    ++count;
                    if (count > MaxRepeats)
                    {
                        Debug.Log("!!!!! sharedphysics emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
        //速度小于0：向下移动
        else if (cd.vel.y < 0.0)
        {
            int y1 = room.GetTilePosition(new Vector2(0.0f, (float)(cd.pos.y - (double)cd.rad))).y;
            int y2 = room.GetTilePosition(new Vector2(0.0f, cd.lastPos.y - cd.rad)).y;
            int x1 = room.GetTilePosition(new Vector2((float)(cd.pos.x - (double)cd.rad + 1.0), 0.0f)).x;
            int x2 = room.GetTilePosition(new Vector2((float)(cd.pos.x + (double)cd.rad - 1.0), 0.0f)).x;
            bool @checked = false;
            for (int y = y2; y >= y1 && !@checked; --y)
            {
                for (int x = x1; x <= x2 && !@checked; ++x)
                {
                    if (SolidFloor(room, x, y, cd.goThroughFloors, cd.lastPos) && !SolidFloor(room, x, y + 1, cd.goThroughFloors, cd.lastPos) && (lastTile.y > y || room.GetTile(cd.lastPos).Terrain == Room.Tile.TerrainType.Solid))
                    {
                        cd.pos.y = (float)((y + 1.0) * 20.0) + cd.rad;
                        cd.vel.y = 0.0f;
                        cd.contactPoint.y = -1;
                        @checked = true;
                    }
                    ++count;
                    if (count > MaxRepeats)
                    {
                        Debug.Log("!!!!! sharedphysics emergency breakout of terrain check!");
                        @checked = true;
                    }
                }
            }
        }
        return cd;
    }

    public static TerrainCollisionData SlopesVertically(Room room, TerrainCollisionData cd)
    {
        IntVector2 tilePos = room.GetTilePosition(cd.pos);
        IntVector2 intVec = new IntVector2(0, 0);
        Room.SlopeDirection slopeDir = room.IdentifySlope(cd.pos);
        //如果目标地形不是Slope
        if (room.GetTile(cd.pos).Terrain != Room.Tile.TerrainType.Slope)
        {
            //左边Tile斜坡类型不是Broken && 碰撞体与左边Tile发生碰撞：（碰撞体Center - 碰撞半径).X < 当前Tile边界Pos.X
            if (room.IdentifySlope(tilePos.x - 1, tilePos.y) != Room.SlopeDirection.Broken && cd.pos.x - (double)cd.rad <= room.MiddleOfTile(cd.pos).x - 10.0)
            {
                slopeDir = room.IdentifySlope(tilePos.x - 1, tilePos.y);
                intVec.x = -1;
            }
            //右边检测同上
            else if (room.IdentifySlope(tilePos.x + 1, tilePos.y) != Room.SlopeDirection.Broken && cd.pos.x + (double)cd.rad >= room.MiddleOfTile(cd.pos).x + 10.0)
            {
                slopeDir = room.IdentifySlope(tilePos.x + 1, tilePos.y);
                intVec.x = 1;
            }
            //下边检测同上
            else if (cd.pos.y - (double)cd.rad < room.MiddleOfTile(cd.pos).y - 10.0 && room.IdentifySlope(tilePos.x, tilePos.y - 1) != Room.SlopeDirection.Broken)
            {
                slopeDir = room.IdentifySlope(tilePos.x, tilePos.y - 1);
                intVec.y = -1;
            }
            //上边检测同上
            else if (cd.pos.y + (double)cd.rad > room.MiddleOfTile(cd.pos).y + 10.0 && room.IdentifySlope(tilePos.x, tilePos.y + 1) != Room.SlopeDirection.Broken)
            {
                slopeDir = room.IdentifySlope(tilePos.x, tilePos.y + 1);
                intVec.y = 1;
            }
        }
        //如果斜坡类型不是损坏类型
        if (slopeDir != Room.SlopeDirection.Broken)
        {
            //得到碰撞的tile
            Vector2 tile = room.MiddleOfTile(room.GetTilePosition(cd.pos) + intVec);
            float dist;
            int sign;
            switch (slopeDir)
            {
                //上左、上右斜坡，碰撞由上到下
                case Room.SlopeDirection.UpLeft:
                    dist = (float)(cd.pos.x - (tile.x - 10.0) + (tile.y - 10.0));
                    sign = -1;
                    break;
                case Room.SlopeDirection.UpRight:
                    dist = (float)(20.0 - (cd.pos.x - (tile.x - 10.0)) + (tile.y - 10.0));
                    sign = -1;
                    break;
                case Room.SlopeDirection.DownLeft:
                    dist = (float)(20.0 - (cd.pos.x - (tile.x - 10.0)) + (tile.y - 10.0));
                    sign = 1;
                    break;
                default:
                    dist = (float)(cd.pos.x - (tile.x - 10.0) + (tile.y - 10.0));
                    sign = 1;
                    break;
            }
            if (sign == -1 && cd.pos.y <= dist + (double)cd.rad + cd.rad)
                cd.pos.y = dist + cd.rad + cd.rad;
            else if (sign == 1 && cd.pos.y >= dist - (double)cd.rad - cd.rad)
                cd.pos.y = dist - cd.rad - cd.rad;
        }
        return cd;
    }

    //检测是否是固体、地板地形
    private static bool SolidFloor(Room room, int X, int Y, bool goThroughFloors, Vector2 lastPos)
    {
        return room.GetTile(X, Y).Terrain == Room.Tile.TerrainType.Solid
            || room.GetTile(X, Y).Terrain == Room.Tile.TerrainType.Floor && !goThroughFloors && room.GetTilePosition(new Vector2(0.0f, lastPos.y)).y > Y;
    }

    //地形碰撞数据
    public class TerrainCollisionData
    {
        public Vector2 pos;
        public Vector2 vel;
        public Vector2 lastPos;
        public IntVector2 contactPoint;
        public float rad;
        public bool goThroughFloors;

        public TerrainCollisionData(Vector2 pos, Vector2 lastPos, Vector2 vel, float rad, IntVector2 contactPoint, bool goThroughFloors)
        {
            this.pos = pos;
            this.vel = vel;
            this.lastPos = lastPos;
            this.rad = rad;
            this.contactPoint = contactPoint;
            this.goThroughFloors = goThroughFloors;
        }
    }

    #endregion
}
