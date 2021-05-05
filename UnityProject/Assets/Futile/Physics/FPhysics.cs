using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//A class for linking Unity physics and Futile
//Set METERS_TO_POINTS at a ratio that makes sense for your game

//Help Url: https://www.asknumbers.com/MetersToPoints.aspx

public class FPhysics
{
	public const float DEFAULT_Z_THICKNESS = 1.0f;
	//DEF: To find out how many points in "x" meters or meters in "x" points
	static public float METERS_TO_POINTS;
	static public float POINTS_TO_METERS;
	
	public FPhysics ()
	{
	}
}

