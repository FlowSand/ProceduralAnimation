using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//功能：
//1.Futile预设Shader加载
//2.对外提供创建FShader统一接口，并对资产进行管理

//TODO 添加外部获取FShader的API

public class FShader
{
	static public FShader defaultShader;	//默认Shader
	
	//shader types
	public static FShader Basic;			//基础
	public static FShader Additive;			//附加性
	public static FShader AdditiveColor;
	public static FShader Solid;			//固体
	public static FShader SolidColored;
	
	private static int _nextShaderIndex = 0;
	private static List<FShader> _shaders = new List<FShader>();

    //成员变量
    public int index;       //索引
    public string name;     //名字
    public Shader shader;   //Shader

    private FShader()
	{
		throw new NotSupportedException("Use FShader.CreateShader() instead");
	}

	//only to be constructed inside this class using CreateShader()
	private FShader (string name, Shader shader, int index) 
	{
		this.index = index;
		this.name = name;
		this.shader = shader; 

		if(shader == null)
		{
			throw new FutileException("Couldn't find Futile shader '"+name+"'");
		}
	}
	
	//FShader初始化
	public static void Init() //called by Futile
	{
		Basic = CreateShader("Basic", Shader.Find("Futile/Basic"));	
		Additive = CreateShader("Additive", Shader.Find("Futile/Additive"));	
		AdditiveColor = CreateShader("AdditiveColor", Shader.Find("Futile/AdditiveColor"));	
		Solid = CreateShader("Solid", Shader.Find("Futile/Solid"));	
		SolidColored = CreateShader("SolidColored", Shader.Find("Futile/SolidColored"));	
		
		defaultShader = Basic;
	}
	
	//创建FShader的唯一接口
	//create your own FShaders by creating them here
	
	public static FShader CreateShader(string shaderShortName, Shader shader)
	{
		for(int s = 0; s<_shaders.Count; s++)
		{
			if(_shaders[s].name == shaderShortName) return _shaders[s]; //don't add it if we have it already	
		}
		
		FShader newShader = new FShader(shaderShortName, shader, _nextShaderIndex++);
		_shaders.Add (newShader);
		
		return newShader;
	}
	
}


