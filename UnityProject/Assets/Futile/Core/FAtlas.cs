using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//功能：单个图集元素数据定义

public class FAtlasElement
{
	public string name;
	
	public int indexInAtlas;

	public FAtlas atlas;
	public int atlasIndex;
	
	public Rect uvRect;
	public Vector2 uvTopLeft;
	public Vector2 uvTopRight;
	public Vector2 uvBottomRight;
	public Vector2 uvBottomLeft;
	
	public Rect sourceRect;
	public Vector2 sourceSize;
	public Vector2 sourcePixelSize;
	public bool isTrimmed;
	//public bool isRotated;
	
	public FAtlasElement Clone()
	{
		FAtlasElement element = new FAtlasElement();
		
		element.name = name;
		
		element.indexInAtlas = indexInAtlas;
		
		element.atlas = atlas;
		element.atlasIndex = atlasIndex;
		
		element.uvRect = uvRect;
		element.uvTopLeft = uvTopLeft;
		element.uvTopRight = uvTopRight;
		element.uvBottomRight = uvBottomRight;
		element.uvBottomLeft = uvBottomLeft;
		
		element.sourceRect = sourceRect;
		element.sourceSize = sourceSize;
		element.isTrimmed = isTrimmed;
		
		return element;
	}
}

public class FAtlas
{
	private List<FAtlasElement> _elements = new List<FAtlasElement>();
	public List<FAtlasElement> elements
	{
		get { return _elements; }
	}

	private int _index;
	public int index
	{
		get { return _index; }
	}

	private Texture _texture;
	public Texture texture
	{
		get { return _texture; }
	}

	private Vector2 _textureSize;
	public Vector2 textureSize
	{
		get { return _textureSize; }
	}

	private string _name;
	public string name
	{
		get { return _name; }
	}

	private string _imagePath;
	public string imagePath
	{
		get { return _imagePath; }
	}

	private string _dataPath;
	public string dataPath
	{
		get { return _dataPath; }
	}

	private bool _isSingleImage;
	public bool isSingleImage
	{
		get { return _isSingleImage; }
	}
	
	private Dictionary<string, FAtlasElement> _elementsByName = new Dictionary<string, FAtlasElement>();	
	
	//TODO: allow users to pass a dictionary of pre-built atlas data if they want

	//功能：使用SingleImage创建一个图集
	public FAtlas (string name, Texture texture, int index) //single image
	{
		_name = name;
		_imagePath = "";
		_dataPath = "";
		_index = index;
		
		_texture = texture;
		_textureSize = new Vector2(_texture.width,_texture.height);
		
		CreateAtlasFromSingleImage();
	}
	
	//功能：根据dataPath加载一个图集
	public FAtlas (string name, string dataPath, Texture texture, int index) //atlas with data path
	{
		_name = name;
		_imagePath = "";
		_dataPath = dataPath;
		_index = index;
		
		_texture = texture;
		_textureSize = new Vector2(_texture.width,_texture.height);
		
		_isSingleImage = false;
		LoadAtlasData();
	}
	
	public FAtlas (string name, string imagePath, string dataPath, int index, bool shouldLoadAsSingleImage)
	{
		_name = name;
		_imagePath = imagePath;
		_dataPath = dataPath;
		
		_index = index;
		
		LoadTexture();
		
		//Load as single image or atlas
		if(shouldLoadAsSingleImage)
		{
			_isSingleImage = true;
			CreateAtlasFromSingleImage();
		}
		else
		{
			_isSingleImage = false;
			LoadAtlasData();
		}
	}
	
	//根据ImagePath加载Texture
	private void LoadTexture()
	{
		_texture = Resources.Load (_imagePath, typeof(Texture)) as Texture;
		 
		if(_texture == null)
		{
			throw new FutileException("Couldn't load the atlas texture from: " + _imagePath);	
		}
		
		_isTextureAnAsset = true;
		
		_textureSize = new Vector2(_texture.width,_texture.height);
	}
	
	private void LoadAtlasData()
	{
		TextAsset dataAsset = Resources.Load (_dataPath, typeof(TextAsset)) as TextAsset;
		
		if(dataAsset == null)
		{
			throw new FutileException("Couldn't load the atlas data from: " + _dataPath);
		}
		
		Dictionary<string,object> dict = dataAsset.text.dictionaryFromJson();
		
		if(dict == null)
		{
			throw new FutileException("The atlas at " + _dataPath + " was not a proper JSON file. Make sure to select \"Unity3D\" in TexturePacker.");
		}
		
		Dictionary<string,object> frames = (Dictionary<string,object>) dict["frames"];
		
		float scaleInverse = Futile.resourceScaleInverse;
		
		int index = 0;
		
		foreach(KeyValuePair<string,object> item in frames)
		{
			FAtlasElement element = new FAtlasElement();
			 
			element.indexInAtlas = index++;
			
			string name = item.Key;
			
			if(Futile.shouldRemoveAtlasElementFileExtensions)
			{
				int extensionPosition = name.LastIndexOf(".");
				if (extensionPosition >= 0) name = name.Substring(0, extensionPosition);
			}

			element.name = name;
			
			IDictionary itemDict = (IDictionary)item.Value;
			
			element.isTrimmed = (bool)itemDict["trimmed"];
			
			if((bool)itemDict["rotated"]) 
			{
				throw new NotSupportedException("Futile no longer supports TexturePacker's \"rotated\" flag. Please disable it when creating the "+_dataPath+" atlas.");
			}

			//the uv coordinate rectangle within the atlas
			IDictionary frame = (IDictionary)itemDict["frame"];
			
			float frameX = float.Parse(frame["x"].ToString());
			float frameY = float.Parse(frame["y"].ToString());
			float frameW = float.Parse(frame["w"].ToString());
			float frameH = float.Parse(frame["h"].ToString()); 
			
			Rect uvRect = new Rect
			(
				frameX/_textureSize.x,
				((_textureSize.y - frameY - frameH)/_textureSize.y),
				frameW/_textureSize.x,
				frameH/_textureSize.y
			);
				
			element.uvRect = uvRect;
		
			element.uvTopLeft.Set(uvRect.xMin,uvRect.yMax);
			element.uvTopRight.Set(uvRect.xMax,uvRect.yMax);
			element.uvBottomRight.Set(uvRect.xMax,uvRect.yMin);
			element.uvBottomLeft.Set(uvRect.xMin,uvRect.yMin);


			//the source size is the untrimmed size
			IDictionary sourcePixelSize = (IDictionary)itemDict["sourceSize"];

			element.sourcePixelSize.x = float.Parse(sourcePixelSize["w"].ToString());	
			element.sourcePixelSize.y = float.Parse(sourcePixelSize["h"].ToString());	

			element.sourceSize.x = element.sourcePixelSize.x * scaleInverse;	
			element.sourceSize.y = element.sourcePixelSize.y * scaleInverse;


			//this rect is the trimmed size and position relative to the untrimmed rect
			IDictionary sourceRect = (IDictionary)itemDict["spriteSourceSize"];

			float rectX = float.Parse(sourceRect["x"].ToString()) * scaleInverse;
			float rectY = float.Parse(sourceRect["y"].ToString()) * scaleInverse;
			float rectW = float.Parse(sourceRect["w"].ToString()) * scaleInverse;
			float rectH = float.Parse(sourceRect["h"].ToString()) * scaleInverse;
			
			element.sourceRect = new Rect(rectX,rectY,rectW,rectH);

			_elements.Add (element);
			_elementsByName.Add(element.name, element);
		}
		
		Resources.UnloadAsset(dataAsset);
	}
	
	private void CreateAtlasFromSingleImage()
	{
		FAtlasElement element = new FAtlasElement();
		
		element.name = _name;
		element.indexInAtlas = 0;
		
		//TODO: may have to offset the rect slightly
		float scaleInverse = Futile.resourceScaleInverse;
		
		Rect uvRect = new Rect(0.0f,0.0f,1.0f,1.0f);
		
		element.uvRect = uvRect;
		
		element.uvTopLeft.Set(uvRect.xMin,uvRect.yMax);
		element.uvTopRight.Set(uvRect.xMax,uvRect.yMax);
		element.uvBottomRight.Set(uvRect.xMax,uvRect.yMin);
		element.uvBottomLeft.Set(uvRect.xMin,uvRect.yMin);
		
		
		element.sourceSize = new Vector2(_textureSize.x*scaleInverse,_textureSize.y*scaleInverse);
		element.sourcePixelSize = new Vector2(_textureSize.x,_textureSize.y);

		element.sourceRect = new Rect(0,0,_textureSize.x*scaleInverse,_textureSize.y*scaleInverse);


		element.isTrimmed = false;
		
		_elements.Add (element);
		_elementsByName.Add (element.name, element);
	}

	//卸载图集Texture资源引用
	private bool _isTextureAnAsset = false;
	public void Unload ()
	{
		if(_isTextureAnAsset)
		{
			Resources.UnloadAsset(_texture);
		}
	}
}


