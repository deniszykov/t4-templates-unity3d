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
using System.Reflection;

// ReSharper disable once CheckNamespace
namespace Assets.Editor.GameDevWare.TextTransform.Utils
{
	internal static class ObjectExtensions
	{
		public static object Invoke(this object target, string methodName, params object[] args)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.InvokeMember(
					  methodName,
					  BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  args
				  );
			}
			else
			{
				return target.GetType().InvokeMember(
					  methodName,
					  BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  args
				  );
			}
		}

		public static bool HasField(this object target, string fieldName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.GetField(
					  fieldName,
					  BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
				  ) != null;
			}
			else
			{
				return target.GetType().GetField(
					  fieldName,
					  BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				  ) != null;
			}
		}

		public static object GetFieldValue(this object target, string fieldName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.InvokeMember(
					  fieldName,
					  BindingFlags.GetField | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  null
				  );
			}
			else
			{
				return target.GetType().InvokeMember(
					  fieldName,
					  BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  null
				  );
			}
		}

		public static void SetFieldValue(this object target, string fieldName, object value)
		{
			var type = target as Type;
			if (type != null)
			{
				type.InvokeMember(
					  fieldName,
					  BindingFlags.SetField | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  new object[] { value }
				  );
			}
			else
			{
				target.GetType().InvokeMember(
					  fieldName,
					  BindingFlags.SetField | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  new object[] { value }
				  );
			}
		}

		public static bool HasProperty(this object target, string fieldName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.GetProperty (
					fieldName,
					BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public
				) != null;
			}
			else
			{
				return target.GetType().GetProperty (
					fieldName,
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
				) != null;
			}
		}

		public static object GetPropertyValue(this object target, string propertyName)
		{
			var type = target as Type;
			if (type != null)
			{
				return type.InvokeMember(
					  propertyName,
					  BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  null
				  );
			}
			else
			{
				return target.GetType().InvokeMember(
					  propertyName,
					  BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  null
				  );
			}
		}

		public static void SetPropertyValue(this object target, string propertyName, object value)
		{
			var type = target as Type;
			if (type != null)
			{
				type.InvokeMember(
					  propertyName,
					  BindingFlags.SetProperty | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  null,
					  new object[] { value }
				  );
			}
			else
			{
				target.GetType().InvokeMember(
					  propertyName,
					  BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
					  null,
					  target,
					  new object[] { value }
				  );
			}
		}
	}
}
