/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "T4 Templates" Unity Asset - https://www.assetstore.unity3d.com/#!/content/63294
	
	THIS SOFTWARE IS DISTRIBUTED "AS-IS" WITHOUT ANY WARRANTIES, CONDITIONS AND 
	REPRESENTATIONS WHETHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION THE 
	IMPLIED WARRANTIES AND CONDITIONS OF MERCHANTABILITY, MERCHANTABLE QUALITY, 
	FITNESS FOR A PARTICULAR PURPOSE, DURABILITY, NON-INFRINGEMENT, PERFORMANCE 
	AND THOSE ARISING BY STATUTE OR FROM CUSTOM OR USAGE OF TRADE OR COURSE OF DEALING.
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Assets.Editor.GameDevWare.TextTransform.Utils
{
	static class FileUtils
	{
		private readonly static char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

		public static string MakeProjectRelative(string path)
		{
			if (String.IsNullOrEmpty(path)) return null;
			var fullPath = Path.GetFullPath(Environment.CurrentDirectory).Replace("\\", "/");
			path = Path.GetFullPath(path).Replace("\\", "/");

			if (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.DirectorySeparatorChar)
				path = path.Substring(0, path.Length - 1);
			if (fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar || fullPath[fullPath.Length - 1] == Path.DirectorySeparatorChar)
				fullPath = fullPath.Substring(0, fullPath.Length - 1);

			if (path == fullPath)
				path = ".";
			else if (path.StartsWith(fullPath, StringComparison.Ordinal))
				path = path.Substring(fullPath.Length + 1);
			else
				path = null;

			return path;
		}
		public static string ComputeMd5Hash(string path, int tries = 5)
		{
			if (path == null) throw new ArgumentNullException("path");
			if (tries <= 0) throw new ArgumentOutOfRangeException("tries");

			foreach (var attempt in Enumerable.Range(1, tries))
			{
				try
				{
					using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
					using (var md5 = new MD5CryptoServiceProvider())
					{
						var hashBytes = md5.ComputeHash(fs);
						return BitConverter.ToString(hashBytes).Replace("-", "");
					}
				}
				catch (IOException exception)
				{
					Debug.LogWarning("Attempt #" + attempt + " to compute hash of " + path + " has failed with IO error: " + exception);

					if (attempt == tries)
						throw;
				}
				Thread.Sleep(100);
			}

			return new string('0', 32); // never happens
		}
		public static string SanitizeFileName(string path)
		{
			var fileName = new StringBuilder(path);
			for (var c = 0; c < fileName.Length; c++)
			{
				if (Array.IndexOf(InvalidFileNameChars, fileName[c]) != -1)
					fileName[c] = '_';
			}
			return fileName.ToString();
		}
	}
}
