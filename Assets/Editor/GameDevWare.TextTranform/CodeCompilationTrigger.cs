/*
	Copyright (c) 2016 Denis Zykov, GameDevWare.com

	This a part of "T4 Transform" Unity Asset - https://www.assetstore.unity3d.com/en/#!/content/56706
	
	This source code is distributed via Unity Asset Store, 
	to use it in your project you should accept Terms of Service and EULA 
	https://unity3d.com/ru/legal/as_terms
*/

using System;
using Assets.Editor.GameDevWare.TextTranform.Editors;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.GameDevWare.TextTranform
{
	[InitializeOnLoad, Serializable]
	internal class CodeCompilationTrigger : ScriptableObject
	{
		public static CodeCompilationTrigger Instance;

		static CodeCompilationTrigger()
		{
			Instance = ScriptableObject.CreateInstance<CodeCompilationTrigger>();
		}

		protected void Awake()
		{
			System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(TemplateInspector).TypeHandle);

			foreach (var templatePath in TemplateSettings.ListTemplatesInProject())
			{
				var settings = TemplateSettings.Load(templatePath);
				if ((settings.Trigger & (int)TemplateSettings.Triggers.CodeCompilation) == 0)
					continue;

				if (Menu.VerboseLogs)
					Debug.Log(string.Format("Code compilation in project is triggered T4 template's generator at '{0}'.", templatePath));

				UnityTemplateGenerator.RunForTemplateWithDelay(templatePath, TimeSpan.FromMilliseconds(settings.TriggerDelay));
			}
		}
	}
}
