using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System.IO;

public class TestGame : MonoBehaviour
{
	public static TestGame instance;

	private FStage _stage;

	public Texture2D tentaclePlants;
    
    void Start()
    {
		instance = this;

		//设置引擎参数
		FutileParams fparams = new FutileParams(true, true, true, false);
		fparams.AddResolutionLevel(1400f, 1f, 1f, string.Empty);
		fparams.origin = new Vector2(0.5f, 0.5f);  //RW原点在 0，0

		//初始化引擎
		Futile.instance.Init(fparams);

		//加载纹理资源
		this.tentaclePlants = Resources.Load("TPlantDemo_1") as Texture2D;
		Futile.atlasManager.LoadAtlasFromTexture("TPlantDemo_1", tentaclePlants);

		_stage = Futile.stage;
		Debug.Log("测试运行开始");
		//测试方法
		TestPage testPage = new TestPage();
		_stage.AddChild(testPage);
		testPage.Start();

		TestMethod();

		Debug.Log("测试路径读取：" + RootFolderDirectory());
	}

	private string RootFolderDirectory()
	{
		string[] strArray = Assembly.GetExecutingAssembly().Location.Split(Path.DirectorySeparatorChar);
		string str = string.Empty;
		for (int i = 0; i < strArray.Length-3; i++)
		{
			str = str + strArray[i] + Path.DirectorySeparatorChar;
		}
		return str;
	}

	private void TestMethod()
	{
		TextAsset txt = Resources.Load("tentaclePlants") as TextAsset;
		string[] strs = txt.text.Split('\n');
		Debug.Log("测试输出第一行文本内容：" + strs[0]);
	}
}
