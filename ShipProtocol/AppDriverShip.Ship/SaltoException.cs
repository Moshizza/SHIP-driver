using System;

namespace AppDriverShip.Ship;

public class SaltoException : Exception
{
	public const int SaltoException_Header = -1000;

	public const int SaltoException_RequestName = -1001;

	public int Code;
}
