using System;

//���ܣ���ܴ����쳣����

public class FutileException : Exception
{
	public FutileException (string message) : base(message)
	{
	}
	
	public FutileException () : base()
	{
	}
}

