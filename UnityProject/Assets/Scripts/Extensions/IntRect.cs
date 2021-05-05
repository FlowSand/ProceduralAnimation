using System;
//DEL
public struct IntRect
{
    public int left;
    public int bottom;
    public int right;
    public int top;

    public IntRect(int left, int bottom, int right, int top)
    {
        this.left = left;
        this.bottom = bottom;
        this.right = right;
        this.top = top;
    }

    public int Height
    {
        get
        {
            return Math.Abs(top - bottom);
        }
    }

    public int Width
    {
        get
        {
            return Math.Abs(right - left);
        }
    }

    public FloatRect ToFloatRect()
    {
        return new FloatRect(left, bottom, right, top);
    }

    public static IntRect MakeFromIntVector2(IntVector2 intVec2)
    {
        return new IntRect(intVec2.x, intVec2.y, intVec2.x, intVec2.y);
    }

    public void ExpandToInclude(IntVector2 intVec)
    {
        if (intVec.x < left)
            left = intVec.x;
        if (intVec.x > right)
            right = intVec.x;
        if (intVec.y < bottom)
            bottom = intVec.y;
        if (intVec.y > top)
            top = intVec.y;
    }

    public void Grow(int grow)
    {
        left -= grow;
        right += grow;
        bottom -= grow;
        top += grow;
    }
}
