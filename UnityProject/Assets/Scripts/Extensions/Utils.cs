using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PASystem
{
    //功能：数学、向量相关运算

    public static class Utils
    {
        #region 八方向向量数组

        //9点钟起点、逆时针
        public static IntVector2[] eightDirections = new IntVector2[8]
        {
            new IntVector2(-1, 0),
            new IntVector2(-1, -1),
            new IntVector2(0, -1),
            new IntVector2(1, -1),
            new IntVector2(1, 0),
            new IntVector2(1, 1),
            new IntVector2(0, 1),
            new IntVector2(-1, 1)
        };

        public static IntVector2[] eightDirectionsDiagonalsLast = new IntVector2[8]
        {
            //上下左右
            new IntVector2(-1, 0),  
            new IntVector2(0, -1),
            new IntVector2(1, 0),
            new IntVector2(0, 1),
            //对角线
            new IntVector2(-1, -1), 
            new IntVector2(1, -1),
            new IntVector2(1, 1),
            new IntVector2(-1, 1)
        };
        public static IntVector2[] eightDirectionsAndZero = new IntVector2[9]
        {
            new IntVector2(0, 0),
            new IntVector2(-1, 0),
            new IntVector2(-1, -1),
            new IntVector2(0, -1),
            new IntVector2(1, -1),
            new IntVector2(1, 0),
            new IntVector2(1, 1),
            new IntVector2(0, 1),
            new IntVector2(-1, 1)
        };
        public static IntVector2[] fourDirections = new IntVector2[4]
        {
            new IntVector2(-1, 0),
            new IntVector2(0, -1),
            new IntVector2(1, 0),
            new IntVector2(0, 1)
        };
        public static IntVector2[] fourDirectionsAndZero = new IntVector2[5]
        {
            new IntVector2(0, 0),
            new IntVector2(-1, 0),
            new IntVector2(0, -1),
            new IntVector2(1, 0),
            new IntVector2(0, 1)
        };

        #endregion

        #region 距离相关

        //计算两点间曼哈顿距离
        public static int ManhattanDistance(IntVector2 a, IntVector2 b)
        {
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        //点V到直线L12的距离
        public static float DistanceToLine(Vector2 V, Vector2 l2, Vector2 l1)
        {
            return ((l2.y - l1.y) * V.x - (l2.x - l1.x) * V.y + l2.x * l1.y - l2.y * l1.x) / Mathf.Sqrt(Mathf.Pow(l2.y - l1.y, 2f) + Mathf.Pow(l2.x - l1.x, 2f));
        }

        //计算两点之间的距离
        public static float Dist(Vector2 p1, Vector2 p2)
        {
            return Mathf.Sqrt(Mathf.Abs(p1.x - p2.x) * Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.y - p2.y) * Mathf.Abs(p1.y - p2.y));
        }
        public static float DistNoSqrt(Vector2 p1, Vector2 p2)
        {
            return Mathf.Abs(p1.x - p2.x) * Mathf.Abs(p1.x - p2.x) + Mathf.Abs(p1.y - p2.y) * Mathf.Abs(p1.y - p2.y);
        }

        //判断两点间距离是否小于某值
        public static bool DistLess(Vector2 p1, Vector2 p2, float dst)
        {
            return (p1 - p2).magnitude < dst;
        }
        public static bool DistLess(IntVector2 p1, IntVector2 p2, float dst)
        {
            return DistLess(p1.ToVector2(), p2.ToVector2(), dst);
        }
        
        //判断A、B哪个点距离目标点近    A近：true
        public static bool VectorIsCloser(Vector2 A, Vector2 B, Vector2 comparePoint)
        {
            double distToA = Mathf.Abs(A.x - comparePoint.x) * Mathf.Abs(A.x - comparePoint.x) + Mathf.Abs(A.y - comparePoint.y) * Mathf.Abs(A.y - comparePoint.y);
            double distToB = Mathf.Abs(B.x - comparePoint.x) * Mathf.Abs(B.x - comparePoint.x) + Mathf.Abs(B.y - comparePoint.y) * Mathf.Abs(B.y - comparePoint.y);
            return distToA < distToB;
        }

        //返回从P1到P2的方向向量
        public static Vector2 DirVec(Vector2 p1, Vector2 p2)
        {
            if (p1 == p2)
            {
                return new Vector2(0.0f, 1f);
            }
            return new Vector2(p2[0] - p1[0], p2[1] - p1[1]).normalized;
        }

        //检测A、B是否四方向相邻
        public static bool AreIntVectorsNeighbors(IntVector2 A, IntVector2 B)
        {
            if (A.x == B.x && A.y == B.y
                || A.x - B.x != 0 && A.y - B.y != 0
                || Math.Abs(A.x - B.x) >= 2)
                return false;
            return Math.Abs(A.y - B.y) < 2;
        }

        //检测A、B是否对角线相邻
        public static bool AreIntVectorsDiagonalNeighbors(IntVector2 A, IntVector2 B)
        {
            if (A.x == B.x || A.y == B.y)
                return false;
            if (Math.Abs(A.x - B.x) >= 2)
                return Math.Abs(A.y - B.y) < 2;
            return true;
        }

        #endregion

        #region 数学方法

        //角度值 与 方向向量 互转换
        public static Vector2 DegToVec(float ang)
        {
            return new Vector2(Mathf.Sin(ang * ((float)Math.PI / 180f)), Mathf.Cos(ang * ((float)Math.PI / 180f)));
        }
        public static float VecToDeg(Vector2 v)
        {
            return (float)(Mathf.Atan2(v.x, v.y) / (Math.PI * 2) * 360.0);
        }

        public static float AimFromOneVectorToAnother(Vector2 p1, Vector2 p2)
        {
            return Utils.VecToDeg(p2 - p1);
        }


        //创建Coord
        public static WorldCoordinate MakeWorldCoordinate(IntVector2 pos, int room)
        {
            return new WorldCoordinate(pos.x, pos.y);
        }
        public static Vector2 IntVector2ToVector2(IntVector2 ivect2)
        {
            return new Vector2(ivect2.x, ivect2.y);
        }

        //限定某值在指定范围内
        public static int IntClamp(int val, int inclMin, int inclMax)
        {
            if (val < inclMin)
                return inclMin;
            if (val > inclMax)
                return inclMax;
            return val;
        }

        //垂直单位向量：向量点乘
        public static Vector2 PerpendicularVector(Vector2 v)
        {
            v.Normalize();
            return new Vector2(-v.y, v.x);
        }

        #endregion

        #region  插值计算

        //贝塞尔曲线插值
        public static Vector2 Bezier(Vector2 A, Vector2 cA, Vector2 B, Vector2 cB, float f)
        {
            Vector2 firstInter = Vector2.Lerp(cA, cB, f);
            cA = Vector2.Lerp(A, cA, f);
            cB = Vector2.Lerp(cB, B, f);
            cA = Vector2.Lerp(cA, firstInter, f);
            cB = Vector2.Lerp(firstInter, cB, f);
            return Vector2.Lerp(cA, cB, f);
        }

        public static float SCurve(float x, float k)
        {
            x = (float)(x * 2.0 - 1.0);
            if (x < 0.0)
            {
                x = Mathf.Abs(1f + x);
                return (float)(k * x / (k - x + 1.0) * 0.5);
            }
            k = -1f - k;
            return (float)(0.5 + k * x / (k - x + 1.0) * 0.5);
        }

        public static Vector2 InverseKinematic(Vector2 va, Vector2 vc, float A, float B, float flip)
        {
            float dist = Vector2.Distance(va, vc);
            float dir = Mathf.Acos(Mathf.Clamp((float)((dist * dist + A * A - B * B) / (2.0 * dist * A)), 0.2f, 0.98f)) * (float)(flip * 180.0 / Math.PI);
            return va + Utils.DegToVec(Utils.AimFromOneVectorToAnother(va, vc) + dir) * A;
        }

        public static Vector2 RNV()
        {
            return Utils.DegToVec(UnityEngine.Random.value * 360f);
        }

        public static float LerpMap(float val, float fromA, float toA, float fromB, float toB)
        {
            return Mathf.Lerp(fromB, toB, Mathf.InverseLerp(fromA, toA, val));
        }

        #endregion

        #region 平面检测

        //计算Pos在Rect空间的区域编码
        public static IntVector2 RectZone(Vector2 pos, FloatRect rect)
        {
            IntVector2 code = new IntVector2(0, 0);
            if (pos.x < (double)rect.left)
                code.x = -1;
            else if (pos.x > (double)rect.right)
                code.x = 1;
            if (pos.y < (double)rect.bottom)
                code.y = -1;
            else if (pos.y > (double)rect.top)
                code.y = 1;
            return code;
        }

        public static FloatRect RectCollision(Vector2 pos, Vector2 lastPos, FloatRect rct)
        {
            //检测上一帧位置与当前位置，连线是否与Rect边界存在交点
            Vector2 leftVec = Utils.VerticalCrossPoint(lastPos, pos, rct.left);
            Vector2 rightVec = Utils.VerticalCrossPoint(lastPos, pos, rct.right);
            Vector2 bottomVec = Utils.HorizontalCrossPoint(lastPos, pos, rct.bottom);
            Vector2 topVec = Utils.HorizontalCrossPoint(lastPos, pos, rct.top);
            //如果X轴未变化，统一 上、下交点的X值
            if (lastPos.x == (double)pos.x)
            {
                bottomVec = new Vector2(lastPos.x, rct.bottom);
                topVec = new Vector2(lastPos.x, rct.top);
            }
            //如果上一帧位置不再Rect中，计算两帧距离变化
            float dst = !rct.Vector2Inside(lastPos) ? Vector2.Distance(lastPos, pos) : float.MaxValue;
            Vector2 upperRight = new Vector2(0.0f, 0.0f);
            Vector2 lowerLeft = pos;
            //如果左边界交点Y值 在Top和Bottom之间  &&  上一帧位置和交点位置小于距离：证明位移与Rect发生碰撞，从左边界进入
            if (leftVec.y >= (double)rct.bottom && leftVec.y <= (double)rct.top && Utils.DistLess(lastPos, leftVec, dst))
            {
                lowerLeft = leftVec;
                dst = Vector2.Distance(lastPos, leftVec);
                upperRight = new Vector2(1f, 0.0f);
            }
            //右边界碰撞
            if (rightVec.y >= (double)rct.bottom && rightVec.y <= (double)rct.top && Utils.DistLess(lastPos, rightVec, dst))
            {
                lowerLeft = rightVec;
                dst = Vector2.Distance(lastPos, rightVec);
                upperRight = new Vector2(-1f, 0.0f);
            }
            //下边界碰撞
            if (bottomVec.x >= (double)rct.left && bottomVec.x <= (double)rct.right && Utils.DistLess(lastPos, bottomVec, dst))
            {
                lowerLeft = bottomVec;
                dst = Vector2.Distance(lastPos, bottomVec);
                upperRight = new Vector2(0.0f, 1f);
            }
            //上边界碰撞
            if (topVec.x >= (double)rct.left && topVec.x <= (double)rct.right && Utils.DistLess(lastPos, topVec, dst))
            {
                lowerLeft = topVec;
                Vector2.Distance(lastPos, topVec);
                upperRight = new Vector2(0.0f, -1f);
            }
            return FloatRect.MakeFromVector2(lowerLeft, upperRight);
        }

        //直线LAB 与 x = X的交点
        public static Vector2 VerticalCrossPoint(Vector2 A, Vector2 B, float X)
        {
            if (A.y == (double)B.y)
                return new Vector2(X, A.y);
            float k = (float)((A.y - (double)B.y) / (A.x - (double)B.x));
            float z0 = A.y - A.x * k;
            return new Vector2(X, z0 + k * X);
        }

        //直线LAB 与 y = Y的交点
        public static Vector2 HorizontalCrossPoint(Vector2 A, Vector2 B, float Y)
        {
            if (A.x == (double)B.x)
                return new Vector2(A.x, Y);
            float k = (float)((A.y - (double)B.y) / (A.x - (double)B.x));
            float z0 = A.y - A.x * k;
            return new Vector2((Y - z0) / k, Y);
        }

        //直线A1B1和直线A2B2的交点
        public static Vector2 LineIntersection(Vector2 A1, Vector2 B1, Vector2 A2, Vector2 B2)
        {
            if (A1.x == (double)B1.x)
                return Utils.VerticalCrossPoint(A2, B2, A1.x);
            if (A2.x == (double)B2.x)
                return Utils.VerticalCrossPoint(A1, B1, A2.x);
            if (A1.y == (double)B1.y)
                return Utils.HorizontalCrossPoint(A2, B2, A1.y);
            if (A2.y == (double)B2.y)
                return Utils.HorizontalCrossPoint(A1, B1, A2.y);
            float k1 = (A1.y - B1.y) / (A1.x - B1.x);
            float z1 = A1.y - A1.x * k1;
            float k2 = (A2.y - B2.y) / (A2.x - B2.x);
            float z2 = A2.y - A2.x * k2;
            float x = (z1 - z2) / (k2 - k1);
            return new Vector2(x, z2 + k2 * x);
        }

        //判断某向量点是否位于矩形中
        public static bool InsideRect(IntVector2 vec, IntRect rect)
        {
            return (vec.x >= rect.left
                    && vec.x <= rect.right
                    && vec.y >= rect.bottom
                    && vec.y <= rect.top);
        }
        public static bool InsideRect(int x, int y, IntRect rect)
        {
            return (x >= rect.left
                    && x <= rect.right
                    && y >= rect.bottom
                    && y <= rect.top);
        }

        //判断Vec到矩形的距离（处于内部则为0）
        public static float VectorRectDistance(Vector2 vec, FloatRect rect)
        {
            return Vector2.Distance(vec, Utils.RestrictInRect(vec, rect));
        }

        //直线LAB上距离P最近的点
        public static Vector2 ClosestPointOnLine(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 dirA2P = new Vector2(P.x - A.x, P.y - A.y);
            Vector2 dirB2P = new Vector2(B.x - A.x, B.y - A.y);
            float den = Mathf.Pow(dirB2P.x, 2f) + Mathf.Pow(dirB2P.y, 2f);
            float angle = (dirA2P.x * dirB2P.x + dirA2P.y * dirB2P.y) / den;
            return new Vector2(A.x + dirB2P.x * angle, A.y + dirB2P.y * angle);
        }

        //将Vector变量限制在Rect范围内
        public static Vector2 RestrictInRect(Vector2 vec, FloatRect rect)
        {
            return new Vector2(Mathf.Clamp(vec.x, rect.left, rect.right), Mathf.Clamp(vec.y, rect.bottom, rect.top));
        }


        //点pt是否在v123三角形内部
        public static bool PointInTriangle(Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
        {
            bool flag1 = Utils.DistanceToLine(pt, v1, v2) <= 0.0;
            bool flag2 = Utils.DistanceToLine(pt, v2, v3) <= 0.0;
            bool flag3 = Utils.DistanceToLine(pt, v3, v1) <= 0.0;
            if (flag1 == flag2 && flag2 == flag3)
                return true;
            return false;
        }

        #endregion

        public static string RootFolderDirectory()
        {
            string[] strArray = Assembly.GetExecutingAssembly().Location.Split(Path.DirectorySeparatorChar);
            string str = string.Empty;
            for (int i = 0; i < strArray.Length - 3; ++i)
                str = str + strArray[i] + Path.DirectorySeparatorChar;
            return str;
        }

        //HSL颜色转换为RGB格式
        public static Color HSL2RGB(float h, float sl, float l)
        {
            float r = l;
            float g = l;
            float b = l;
            float num1 = l > 0.5 ? (float)(l + (double)sl - l * (double)sl) : l * (1f + sl);
            if (num1 > 0.0)
            {
                float num2 = l + l - num1;
                float num3 = (num1 - num2) / num1;
                h *= 6f;
                int num4 = (int)h;
                float num5 = h - num4;
                float num6 = num1 * num3 * num5;
                float num7 = num2 + num6;
                float num8 = num1 - num6;
                switch (num4)
                {
                    case 0:
                        r = num1;
                        g = num7;
                        b = num2;
                        break;
                    case 1:
                        r = num8;
                        g = num1;
                        b = num2;
                        break;
                    case 2:
                        r = num2;
                        g = num1;
                        b = num7;
                        break;
                    case 3:
                        r = num2;
                        g = num8;
                        b = num1;
                        break;
                    case 4:
                        r = num7;
                        g = num2;
                        b = num1;
                        break;
                    case 5:
                        r = num1;
                        g = num2;
                        b = num8;
                        break;
                }
            }
            return new Color(r, g, b);
        }
    }
}
