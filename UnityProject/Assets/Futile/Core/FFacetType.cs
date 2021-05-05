using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FFacetType
{
	//Ĭ��FacetType
	static public FFacetType defaultFacetType;
	
	//����FacetType
	public static FFacetType Quad;
	public static FFacetType Triangle;
	
	private static int _nextFacetTypeIndex = 0;
	private static List<FFacetType> _facetTypes = new List<FFacetType>();
	
	//ʵ���ֶ�
	public int index;
	public string name;
	
	public int initialAmount;
	public int expansionAmount;
	public int maxEmptyAmount;
	
	public delegate FFacetRenderLayer CreateRenderLayerDelegate(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader);
	
	public CreateRenderLayerDelegate createRenderLayer;

	//��ʼ��Ĭ��FacetType
	public static void Init() //called by Futile
	{
		Quad = CreateFacetType("Quad", 10, 10, 60, CreateQuadLayer);	
		Triangle = CreateFacetType("Triangle", 16, 16, 64,CreateTriLayer);	
		
		defaultFacetType = Quad;
	}
	
	//����FFacetTypeΨһ�ӿ�
	public static FFacetType CreateFacetType(string facetTypeShortName, int initialAmount, int expansionAmount, int maxEmptyAmount, CreateRenderLayerDelegate createRenderLayer)
	{
		for(int s = 0; s<_facetTypes.Count; s++)
		{
			//don't add it if we have it already
			if (_facetTypes[s].name == facetTypeShortName) return _facetTypes[s]; 	
		}
		
		FFacetType newFacetType = new FFacetType(facetTypeShortName, _nextFacetTypeIndex++, initialAmount, expansionAmount, maxEmptyAmount, createRenderLayer);
		_facetTypes.Add (newFacetType);
		
		return newFacetType;
	}

	//only to be constructed by using CreateFacetType()
	private FFacetType(string name, int index, int initialAmount, int expansionAmount, int maxEmptyAmount, CreateRenderLayerDelegate createRenderLayer)
	{
		this.index = index;
		this.name = name;

		this.initialAmount = initialAmount;
		this.expansionAmount = expansionAmount;
		this.maxEmptyAmount = maxEmptyAmount;

		this.createRenderLayer = createRenderLayer;
	}

	static private FFacetRenderLayer CreateQuadLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader)
	{
		return new FQuadRenderLayer(stage,facetType,atlas,shader);
	}
	
	static private FFacetRenderLayer CreateTriLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader)
	{
		return new FTriangleRenderLayer(stage,facetType,atlas,shader);
	}
	
}


