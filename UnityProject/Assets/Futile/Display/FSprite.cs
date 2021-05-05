using UnityEngine;
using System;

public class FSprite : FFacetElementNode
{
	public static float defaultAnchorX = 0.5f;      //默认Anchor
    public static float defaultAnchorY = 0.5f;
	
	protected Color _color = Futile.white;          //颜色
    protected Color _alphaColor = Futile.white;

    protected bool _facetTypeQuad = true;
	protected Vector2[] _localVertices;             //顶点数组

    protected float _anchorX = defaultAnchorX;      //Andhor
    protected float _anchorY = defaultAnchorY;
	
	protected Rect _localRect; //localRect is the TRIMMED rect
	protected Rect _textureRect; //textureRect is the UN-TRIMMED rect

	protected bool _isMeshDirty = false;            //网格脏数据标识
    protected bool _areLocalVerticesDirty = false;  //顶点脏数据标识

    //构造函数
    protected FSprite() : base() //for overriding
	{
		_localVertices = new Vector2[4];
	}
	
	public FSprite (string elementName, bool quadType = true) : this(Futile.atlasManager.GetElementWithName(elementName))
	{
	}
	
	public FSprite (FAtlasElement element, bool quadType = true) : base()
	{
        _facetTypeQuad = quadType;
		_localVertices = new Vector2[4];

        if (_facetTypeQuad)
            Init(FFacetType.Quad, element, 1);
        else
            Init(FFacetType.Triangle, element, 2);
		
		_isAlphaDirty = true;
		
		UpdateLocalVertices();
	}

    //处理Atlas元素变化
    override public void HandleElementChanged()
	{
		_areLocalVerticesDirty = true;
        //更新顶点数组
        UpdateLocalVertices();
	}

    //重绘
    override public void Redraw(bool shouldForceDirty, bool shouldUpdateDepth)
	{
		bool wasMatrixDirty = _isMatrixDirty;
		bool wasAlphaDirty = _isAlphaDirty;

        //更新深度矩阵
        UpdateDepthMatrixAlpha(shouldForceDirty, shouldUpdateDepth);
		
		if(shouldUpdateDepth)
		{
			UpdateFacets();
		}
		
		if(wasMatrixDirty || shouldForceDirty || shouldUpdateDepth)
		{
            //标识网格脏数据
            _isMeshDirty = true;
		}
		
		if(wasAlphaDirty || shouldForceDirty)
		{
			_isMeshDirty = true;
			_color.ApplyMultipliedAlpha(ref _alphaColor, _concatenatedAlpha);	
		}

        //检测顶点脏标识
        if (_areLocalVerticesDirty)
		{
			UpdateLocalVertices();
		}

        //检测网格脏标识
        if (_isMeshDirty) 
		{
			PopulateRenderLayer();
		}
	}

    //更新顶点数据
    virtual public void UpdateLocalVertices()
	{
        //重置脏标识
        _areLocalVerticesDirty = false;
		
		_textureRect.width = _element.sourceSize.x;
		_textureRect.height = _element.sourceSize.y;
		_textureRect.x = -_anchorX*_textureRect.width;
		_textureRect.y = -_anchorY*_textureRect.height;
		
		float sourceWidth = _element.sourceRect.width;
		float sourceHeight = _element.sourceRect.height;
		float left = _textureRect.x + _element.sourceRect.x;
		float bottom = _textureRect.y + (_textureRect.height - _element.sourceRect.y - _element.sourceRect.height);
		
		_localRect.x = left;
		_localRect.y = bottom;
		_localRect.width = sourceWidth;
		_localRect.height = sourceHeight;
		
		_localVertices[0].Set(left,bottom + sourceHeight);
		_localVertices[1].Set(left + sourceWidth,bottom + sourceHeight);
		_localVertices[2].Set(left + sourceWidth,bottom);
		_localVertices[3].Set(left,bottom);
		
		_isMeshDirty = true;
	} 
	
	override public void PopulateRenderLayer()
	{
		if(_isOnStage && _firstFacetIndex != -1) 
		{
			_isMeshDirty = false;

            Vector3[] vertices = _renderLayer.vertices;
            Vector2[] uvs = _renderLayer.uvs;
            Color[] colors = _renderLayer.colors;

            if (_facetTypeQuad)
            {
                int vertexIndex0 = _firstFacetIndex * 4;
                int vertexIndex1 = vertexIndex0 + 1;
                int vertexIndex2 = vertexIndex0 + 2;
                int vertexIndex3 = vertexIndex0 + 3;

                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex0], _localVertices[0], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex1], _localVertices[1], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex2], _localVertices[2], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex3], _localVertices[3], 0);

                uvs[vertexIndex0] = _element.uvTopLeft;
                uvs[vertexIndex1] = _element.uvTopRight;
                uvs[vertexIndex2] = _element.uvBottomRight;
                uvs[vertexIndex3] = _element.uvBottomLeft;

                colors[vertexIndex0] = _alphaColor;
                colors[vertexIndex1] = _alphaColor;
                colors[vertexIndex2] = _alphaColor;
                colors[vertexIndex3] = _alphaColor;
            }
            else
            {
                int vertexIndex0 = _firstFacetIndex * 4;
                int vertexIndex1 = vertexIndex0 + 1;
                int vertexIndex2 = vertexIndex0 + 2;
                int vertexIndex3 = vertexIndex0 + 3;
                int vertexIndex4 = vertexIndex0 + 4;
                int vertexIndex5 = vertexIndex0 + 5;

                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex0], _localVertices[0], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex1], _localVertices[1], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex2], _localVertices[2], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex3], _localVertices[0], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex4], _localVertices[2], 0);
                _concatenatedMatrix.ApplyVector3FromLocalVector2(ref vertices[vertexIndex5], _localVertices[3], 0);

                uvs[vertexIndex0] = _element.uvTopLeft;
                uvs[vertexIndex1] = _element.uvTopRight;
                uvs[vertexIndex2] = _element.uvBottomRight;
                uvs[vertexIndex3] = _element.uvTopLeft;
                uvs[vertexIndex4] = _element.uvBottomRight;
                uvs[vertexIndex5] = _element.uvBottomLeft;

                colors[vertexIndex0] = _alphaColor;
                colors[vertexIndex1] = _alphaColor;
                colors[vertexIndex2] = _alphaColor;
                colors[vertexIndex3] = _alphaColor;
                colors[vertexIndex4] = _alphaColor;
                colors[vertexIndex5] = _alphaColor;
            }

			_renderLayer.HandleVertsChange();
		}
	}
	
	//Note: this does not consider rotation at all!
	public Rect GetTextureRectRelativeToContainer()
	{
		return _textureRect.CloneAndScaleThenOffset(_scaleX,_scaleY,_x,_y);
	}
	
	virtual public Rect textureRect //the full rect as if the sprite hadn't been trimmed
	{
		get {return _textureRect;}	
	}
	
	[Obsolete("FSprite's boundsRect is obsolete, use textureRect instead")]
	public Rect boundsRect
	{
		get {throw new NotSupportedException("boundsRect is obsolete! Use textureRect instead");}
	}
	
	virtual public Rect localRect //the rect of the actual trimmed quad drawn on screen
	{
		get {return _localRect;}	
	}

	virtual public Color color 
	{
		get { return _color; }
		set 
		{ 
			if(_color != value)
			{
				_color = value; 
				_isAlphaDirty = true;
			}
		}
	}
	
	virtual public float width
	{
		get { return _scaleX * _textureRect.width; }
		set { _scaleX = value/_textureRect.width; _isMatrixDirty = true; } 
	}
	
	virtual public float height
	{
		get { return _scaleY * _textureRect.height; }
		set { _scaleY = value/_textureRect.height; _isMatrixDirty = true; } 
	}
	
	virtual public float anchorX 
	{
		get { return _anchorX;}
		set 
		{ 
			if(_anchorX != value)
			{
				_anchorX = value; 
				_areLocalVerticesDirty = true; 
			}
		}
	}
	
	virtual public float anchorY 
	{
		get { return _anchorY;}
		set 
		{ 
			if(_anchorY != value)
			{
				_anchorY = value; 
				_areLocalVerticesDirty = true; 
			}
		}
	}
	
	//for convenience
	public void SetAnchor(float newX, float newY)
	{
		this.anchorX = newX;
		this.anchorY = newY;
	}
	
	public void SetAnchor(Vector2 newAnchor)
	{
		this.anchorX = newAnchor.x;
		this.anchorY = newAnchor.y;
	}
	
	public Vector2 GetAnchor()
	{
		return new Vector2(_anchorX,_anchorY);	
	}
}

