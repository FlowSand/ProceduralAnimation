public class WorldEntity
{
    public WorldCoordinate pos;
    public EntityID ID;

    public WorldEntity(WorldCoordinate pos, EntityID ID)
    {
        this.pos = pos;
        this.ID = ID;
    }
}

public struct EntityID
{
    public int ID;

    public EntityID(int number)
    {
        this.ID = number;
    }

    public int RandomSeed
    {
        get
        {
            return ID;
        }
    }

    public override string ToString()
    {
        return "ID." + ID;
    }

    public static bool operator ==(EntityID x, EntityID y)
    {
        return x.ID == y.ID;
    }
    public static bool operator !=(EntityID x, EntityID y)
    {
        return !(x == y);
    }
}

public struct WorldCoordinate
{
    public int x;
    public int y;

    public WorldCoordinate(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public IntVector2 Tile
    {
        get
        {
            return new IntVector2(x, y);
        }
        set
        {
            x = value.x;
            y = value.y;
        }
    }

    public override string ToString()
    {
        return "坐标：" + " x: " + x + " y: " + y;
    }
}