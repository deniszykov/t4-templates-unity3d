//
// Template.cs
//
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using GameDevWare.TextTransform.Editor.Utils;

namespace GameDevWare.TextTransform.Editor.Processor
{
	public class ParsedTemplate
	{
		private readonly List<ISegment> importedHelperSegments = new();
		private readonly string rootFileName;

		public List<ISegment> RawSegments { get; } = new();

		public IEnumerable<Directive> Directives
		{
			get
			{
				foreach (var seg in this.RawSegments)
				{
					var dir = seg as Directive;
					if (dir != null)
						yield return dir;
				}
			}
		}

		public IEnumerable<TemplateSegment> Content
		{
			get
			{
				foreach (var seg in this.RawSegments)
				{
					var ts = seg as TemplateSegment;
					if (ts != null)
						yield return ts;
				}
			}
		}

		public CompilerErrorCollection Errors { get; } = new();

		public ParsedTemplate(string rootFileName)
		{
			this.rootFileName = rootFileName;
		}

		public static ParsedTemplate FromText(string content, ITextTemplatingEngineHost host)
		{
			var template = new ParsedTemplate(host.TemplateFile);
			try
			{
				template.Parse(host, new Tokeniser(host.TemplateFile, content));
			}
			catch (ParserException ex)
			{
				template.LogError(ex.Message, ex.Location);
			}

			return template;
		}

		public void Parse(ITextTemplatingEngineHost host, Tokeniser tokeniser)
		{
			this.Parse(host, tokeniser, true);
		}

		public void ParseWithoutIncludes(Tokeniser tokeniser)
		{
			this.Parse(null, tokeniser, false);
		}

		private void Parse(ITextTemplatingEngineHost host, Tokeniser tokeniser, bool parseIncludes)
		{
			this.Parse(host, tokeniser, parseIncludes, false);
		}

		private void Parse(ITextTemplatingEngineHost host, Tokeniser tokeniser, bool parseIncludes, bool isImport)
		{
			var skip = false;
			var addToImportedHelpers = false;
			while ((skip || tokeniser.Advance()) && tokeniser.State != State.EOF)
			{
				skip = false;
				ISegment seg = null;
				switch (tokeniser.State)
				{
					case State.Block:
						if (!string.IsNullOrEmpty(tokeniser.Value))
							seg = new TemplateSegment(SegmentType.Block, tokeniser.Value, tokeniser.Location);
						break;
					case State.Content:
						if (!string.IsNullOrEmpty(tokeniser.Value))
							seg = new TemplateSegment(SegmentType.Content, tokeniser.Value, tokeniser.Location);
						break;
					case State.Expression:
						if (!string.IsNullOrEmpty(tokeniser.Value))
							seg = new TemplateSegment(SegmentType.Expression, tokeniser.Value, tokeniser.Location);
						break;
					case State.Helper:
						addToImportedHelpers = isImport;
						if (!string.IsNullOrEmpty(tokeniser.Value))
							seg = new TemplateSegment(SegmentType.Helper, tokeniser.Value, tokeniser.Location);
						break;
					case State.Directive:
						Directive directive = null;
						string attName = null;
						while (!skip && tokeniser.Advance())
						{
							switch (tokeniser.State)
							{
								case State.DirectiveName:
									if (directive == null)
									{
										directive = new Directive(tokeniser.Value, tokeniser.Location);
										directive.TagStartLocation = tokeniser.TagStartLocation;
										if (!parseIncludes || !string.Equals(directive.Name, "include", StringComparison.OrdinalIgnoreCase))
											this.RawSegments.Add(directive);
									}
									else
										attName = tokeniser.Value;

									break;
								case State.DirectiveValue:
									if (attName != null && directive != null)
										directive.Attributes[attName] = tokeniser.Value;
									else
										this.LogError("Directive value without name", tokeniser.Location);
									attName = null;
									break;
								case State.Directive:
									if (directive != null)
										directive.EndLocation = tokeniser.TagEndLocation;
									break;
								case State.Content:
								case State.Expression:
								case State.Block:
								case State.Helper:
								case State.Name:
								case State.EOF:
								default:
									skip = true;
									break;
							}
						}

						if (parseIncludes && directive != null && string.Equals(directive.Name, "include", StringComparison.OrdinalIgnoreCase))
							this.Import(host, directive, Path.GetDirectoryName(tokeniser.Location.FileName));
						break;
					case State.DirectiveName:
					case State.DirectiveValue:
					case State.Name:
					case State.EOF:
					default:
						throw new InvalidOperationException();
				}

				if (seg != null)
				{
					seg.TagStartLocation = tokeniser.TagStartLocation;
					seg.EndLocation = tokeniser.TagEndLocation;
					if (addToImportedHelpers)
						this.importedHelperSegments.Add(seg);
					else
						this.RawSegments.Add(seg);
				}
			}

			if (!isImport)
				this.AppendAnyImportedHelperSegments();
		}

		private void Import(ITextTemplatingEngineHost host, Directive includeDirective, string relativeToDirectory)
		{
			string fileName;
			if (includeDirective.Attributes.Count > 1 || !includeDirective.Attributes.TryGetValue("file", out fileName))
			{
				this.LogError("Unexpected attributes in include directive", includeDirective.StartLocation);
				return;
			}

			//try to resolve path relative to the file that included it
			if (relativeToDirectory != null && !Path.IsPathRooted(fileName))
			{
				var possible = Path.Combine(relativeToDirectory, fileName);
				if (File.Exists(possible))
					fileName = Path.GetFullPath(possible, PathUtils.ProjectPath);
			}

			string content, resolvedName;
			if (host.LoadIncludeText(fileName, out content, out resolvedName))
				this.Parse(host, new Tokeniser(resolvedName, content), true, true);
			else
				this.LogError("Could not resolve include file '" + fileName + "'.", includeDirective.StartLocation);
		}

		private void AppendAnyImportedHelperSegments()
		{
			this.RawSegments.AddRange(this.importedHelperSegments);
			this.importedHelperSegments.Clear();
		}

		private void LogError(string message, Location location, bool isWarning)
		{
			var err = new CompilerError();
			err.ErrorText = message;
			if (location.FileName != null)
			{
				err.Line = location.Line;
				err.Column = location.Column;
				err.FileName = location.FileName ?? string.Empty;
			}
			else
				err.FileName = this.rootFileName ?? string.Empty;

			err.IsWarning = isWarning;
			this.Errors.Add(err);
		}

		public void LogError(string message)
		{
			this.LogError(message, Location.Empty, false);
		}

		public void LogWarning(string message)
		{
			this.LogError(message, Location.Empty, true);
		}

		public void LogError(string message, Location location)
		{
			this.LogError(message, location, false);
		}

		public void LogWarning(string message, Location location)
		{
			this.LogError(message, location, true);
		}
	}

	public interface ISegment
	{
		Location StartLocation { get; }
		Location EndLocation { get; set; }
		Location TagStartLocation { get; set; }
	}

	public class TemplateSegment : ISegment
	{
		public SegmentType Type { get; private set; }
		public string Text { get; private set; }
		public Location TagStartLocation { get; set; }
		public Location StartLocation { get; }
		public Location EndLocation { get; set; }
		public TemplateSegment(SegmentType type, string text, Location start)
		{
			this.Type = type;
			this.StartLocation = start;
			this.Text = text;
		}
	}

	public class Directive : ISegment
	{
		public string Name { get; }
		public Dictionary<string, string> Attributes { get; }
		public Location TagStartLocation { get; set; }
		public Location StartLocation { get; }
		public Location EndLocation { get; set; }
		public Directive(string name, Location start)
		{
			this.Name = name;
			this.Attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			this.StartLocation = start;
		}

		public string Extract(string key)
		{
			string value;
			if (!this.Attributes.TryGetValue(key, out value))
				return null;

			this.Attributes.Remove(key);
			return value;
		}
	}

	public enum SegmentType
	{
		Block,
		Expression,
		Content,
		Helper
	}

	public struct Location : IEquatable<Location>
	{
		public Location(string fileName, int line, int column) : this()
		{
			this.FileName = fileName;
			this.Column = column;
			this.Line = line;
		}

		public int Line { get; }
		public int Column { get; }
		public string FileName { get; }

		public static Location Empty => new(null, -1, -1);

		public Location AddLine()
		{
			return new Location(this.FileName, this.Line + 1, 1);
		}

		public Location AddCol()
		{
			return this.AddCols(1);
		}

		public Location AddCols(int number)
		{
			return new Location(this.FileName, this.Line, this.Column + number);
		}

		public override string ToString()
		{
			return string.Format("[{0} ({1},{2})]", this.FileName, this.Line, this.Column);
		}

		public bool Equals(Location other)
		{
			return other.Line == this.Line && other.Column == this.Column && other.FileName == this.FileName;
		}
	}
}
