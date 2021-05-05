using UnityEngine;

//功能：向量运算工具类

public static class VectorUtils
{
    public static bool SegmentsIntersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
	{
		float S, T;
	
		if( VectorUtils.LinesIntersect(A, B, C, D, out S, out T )
		   && (S >= 0.0f && S <= 1.0f && T >= 0.0f && T <= 1.0f) )
			return true;
	
		return false;
	}
	
	public static Vector2 LinesIntersectPoint(Vector2 A, Vector2 B, Vector2 C, Vector2 D, out bool valid)
	{
		float S, T;
	
		if( VectorUtils.LinesIntersect(A, B, C, D, out S, out T) ) {
			// Point of intersection
			Vector2 P;
			P.x = A.x + S * (B.x - A.x);
			P.y = A.y + S * (B.y - A.y);
			valid=true;
			return P;
		}
		valid=false;
		return Vector2.zero;
	}
	
	public static bool LinesIntersect(Vector2 A, Vector2 B,
						  Vector2 C, Vector2 D,
						  out float S, out float T)
	{    
		// FAIL: Line undefined
		if ( (A.x==B.x && A.y==B.y) || (C.x==D.x && C.y==D.y) ) {
			S=T=0;
			return false;
		}
	
		float BAx = B.x - A.x;
		float BAy = B.y - A.y;
		float DCx = D.x - C.x;
		float DCy = D.y - C.y;
		float ACx = A.x - C.x;
		float ACy = A.y - C.y;
	
		float denom = DCy*BAx - DCx*BAy;
	
		S = DCx*ACy - DCy*ACx;
		T = BAx*ACy - BAy*ACx;
	
		if (denom == 0) {
			if (S == 0 || T == 0) { 
				// Lines incident
				return true;   
			}
			// Lines parallel and not incident
			return false;
		}
	
		S = S / denom;
		T = T / denom;
	
		// Point of intersection
		// Vector2 P;
		// P.x = A.x + *S * (B.x - A.x);
		// P.y = A.y + *S * (B.y - A.y);
	
		return true;
	}
	
	public static float Angle (Vector2 vector)
	{
	    Vector2 to = new Vector2(1, 0);
	
	    float result = Vector2.Angle( vector, to );
	    Vector3 cross = Vector3.Cross( vector, to );
	
	    if (cross.z > 0)
	       result = 360f - result;
	
	    return result;
	}
}