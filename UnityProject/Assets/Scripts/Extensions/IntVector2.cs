using UnityEngine;

public struct IntVector2
{
    public int x;
    public int y;

    public IntVector2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static Vector2 ToVector2(IntVector2 intVec)
    {
        return new Vector2(intVec.x, intVec.y);
    }

    public static IntVector2 ClampAtOne(IntVector2 intVec)
    {
        IntVector2 vec = new IntVector2(intVec.x, intVec.y);
        vec.x = Mathf.Clamp(vec.x, -1, 1);
        vec.y = Mathf.Clamp(vec.y, -1, 1);
        return vec;
    }

    public Vector2 ToVector2()
    {
        return new Vector2(x, y);
    }

    public float FloatDist(IntVector2 otherVector)
    {
        return Vector2.Distance(new Vector2(x, y), new Vector2(otherVector.x, otherVector.y));
    }

    public override string ToString()
    {
        return "x: " + x + ", y: " + y;
    }

    public static IntVector2 operator +(IntVector2 a, IntVector2 b)
    {
        return new IntVector2(a.x + b.x, a.y + b.y);
    }

    public static IntVector2 operator -(IntVector2 a, IntVector2 b)
    {
        return new IntVector2(a.x - b.x, a.y - b.y);
    }

    public static bool operator ==(IntVector2 a, IntVector2 b)
    {
        return (a.x == b.x && a.y == b.y) ? true : false;
    }

    public static bool operator !=(IntVector2 a, IntVector2 b)
    {
        return (a.x != b.x ? 0 : (a.y == b.y ? 1 : 0)) == 0;
    }
}

