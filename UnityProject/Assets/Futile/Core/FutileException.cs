using System;

//功能：框架代码异常处理

public class FutileException : Exception
{
	public FutileException (string message) : base(message)
	{
	}
	
	public FutileException () : base()
	{
	}
}

