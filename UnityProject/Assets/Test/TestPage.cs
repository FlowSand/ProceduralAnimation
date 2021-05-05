using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPage : FContainer
{
	private FSprite _background;
 
	public TestPage()
	{
		ListenForResize(HandleResize);
	}

	public void Start()
	{
		_background = new FSprite("TPlantDemo_1");
		AddChild(_background);
		Debug.Log(_background.x+":"+_background.y);
	}

	private void HandleResize(bool wasOrientationChange)
	{
		_background.scale = Mathf.Max(1.0f, Mathf.Max(Futile.screen.height / _background.textureRect.height, Futile.screen.width / _background.textureRect.width));

	}
}
