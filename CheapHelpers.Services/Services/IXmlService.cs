﻿using System.Threading.Tasks;

namespace CheapHelpers.Services
{
	public interface IXmlService
	{
		Task Export(string filePath, dynamic data);
	}
}
