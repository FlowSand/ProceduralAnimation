using UnityEngine;
using System;

public class FFacetNode : FNode
{
    protected FAtlas _atlas = null;         //图集引用
    protected FShader _shader = null;       //Shader引用

    protected int _firstFacetIndex = -1;    //首个面片索引
    protected int _numberOfFacetsNeeded;    //所需面片个数

    protected FFacetRenderLayer _renderLayer;   //面片渲染器

    protected FFacetType _facetType;    //面片类型

    private bool _hasInited = false;    //是否已初始化

    public FFacetNode()
    {

    }

    virtual protected void Init(FFacetType facetType, FAtlas atlas, int numberOfFacetsNeeded)
    {
        _facetType = facetType;

        _atlas = atlas;
        if (_shader == null) _shader = FShader.defaultShader;
        _numberOfFacetsNeeded = numberOfFacetsNeeded;

        _hasInited = true;
    }

    //更新所有面片
    protected void UpdateFacets()
    {
        if (!_hasInited) return;

        //通过Stage渲染器，获取渲染层级和索引
        _stage.renderer.GetFacetRenderLayer(out _renderLayer, out _firstFacetIndex, _facetType, _atlas, _shader, _numberOfFacetsNeeded);
    }

    virtual public int firstFacetIndex
    {
        get { return _firstFacetIndex; }
    }

    virtual public void PopulateRenderLayer()
    {
        //override in parent, this is when you set the quads of the render layer
    }

    override public void HandleAddedToStage()
    {
        if (!_isOnStage)
        {
            base.HandleAddedToStage();
            //Stage处理面片改变
            _stage.HandleFacetsChanged();
        }
    }

    override public void HandleRemovedFromStage()
    {
        if (_isOnStage)
        {
            base.HandleRemovedFromStage();
            _stage.HandleFacetsChanged();
        }
    }

    public FShader shader
    {
        get { return _shader; }
        set
        {
            if (_shader != value)
            {
                _shader = value;
                if (_isOnStage) _stage.HandleFacetsChanged();
            }
        }
    }

    public FFacetType facetType
    {
        get { return _facetType; }
    }
}

//FFacetNode handles only a single element
public class FFacetElementNode : FFacetNode
{
    protected FAtlasElement _element;

    protected void Init(FFacetType facetType, FAtlasElement element, int numberOfFacetsNeeded)
    {
        _element = element;

        base.Init(facetType, _element.atlas, numberOfFacetsNeeded);

        HandleElementChanged();
    }

    //根据名字修改AtlasElement
    public void SetElementByName(string elementName)
    {
        this.element = Futile.atlasManager.GetElementWithName(elementName);
    }

    public FAtlasElement element
    {
        get { return _element; }
        set
        {
            if (_element != value)
            {
                bool isAtlasDifferent = (_element.atlas != value.atlas);

                _element = value;

                if (isAtlasDifferent)
                {
                    _atlas = _element.atlas;
                    if (_isOnStage) _stage.HandleFacetsChanged();
                }

                HandleElementChanged();
            }
        }
    }

    //处理AtlasElement元素变化时
    virtual public void HandleElementChanged()
    {
        //override by parent things
    }
}


