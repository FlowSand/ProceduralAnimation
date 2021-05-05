using UnityEngine;

public struct FloatRect
{
    public enum CornerLabel
    {
        A,  //左上
        B,  //右上
        C,  //右下
        D,  //左下
        None,
    }

    public float left;
    public float bottom;
    public float right;
    public float top;

    public FloatRect(float left, float bottom, float right, float top)
    {
        this.left = left;
        this.bottom = bottom;
        this.right = right;
        this.top = top;
    }

    public Vector2 GetCorner(int corner)
    {
        switch (corner)
        {
            case 0: //左上
                return new Vector2(left, top);
            case 1: //右上
                return new Vector2(right, top);
            case 2: //右下
                return new Vector2(right, bottom);
            case 3: //左下
                return new Vector2(left, bottom);
            default:
                return new Vector2(0f, 0f);
        }
    }

    public Vector2 GetCorner(CornerLabel corner)
    {
        switch (corner)
        {
            case CornerLabel.A:
                return new Vector2(left, top);
            case CornerLabel.B:
                return new Vector2(right, top);
            case CornerLabel.C:
                return new Vector2(right, bottom);
            case CornerLabel.D:
                return new Vector2(left, bottom);
            default:
                return new Vector2(0f, 0f);
        }
    }

    public Vector2 Center
    {
        get
        {
            return new Vector2((left + right) / 2f, (bottom + top) / 2f);
        }
    }

    public static FloatRect MakeFromVector2(Vector2 lowerLeft, Vector2 upperRight)
    {
        return new FloatRect(lowerLeft.x, lowerLeft.y, upperRight.x, upperRight.y);
    }

    public bool Vector2Inside(Vector2 vec)
    {
        return (vec.x > left && vec.x < right && vec.y > bottom && vec.y < top);
    }

    public FloatRect Shrink(float shrink)
    {
        left += shrink;
        bottom += shrink;
        right -= shrink;
        top -= shrink;
        return this;
    }

    public override string ToString()
    {
        return "rect: " + left + ", " + bottom + ", " + right + ", " + top;
    }
}
