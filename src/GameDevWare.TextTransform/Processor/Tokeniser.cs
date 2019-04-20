// 
// Tokeniser.cs
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

namespace GameDevWare.TextTransform.Processor
{
	public class Tokeniser
	{
		private readonly string content;
		private int position;
		private string value;
		private State nextState = State.Content;
		private Location nextStateLocation;
		private Location nextStateTagStartLocation;

		public Tokeniser(string fileName, string content)
		{
			this.State = State.Content;
			this.content = content;
			this.Location = this.nextStateLocation = this.nextStateTagStartLocation = new Location(fileName, 1, 1);
		}

		public bool Advance()
		{
			this.value = null;
			this.State = this.nextState;
			this.Location = this.nextStateLocation;
			this.TagStartLocation = this.nextStateTagStartLocation;
			if (this.nextState == State.EOF)
				return false;
			this.nextState = this.GetNextStateAndCurrentValue();
			return true;
		}

		private State GetNextStateAndCurrentValue()
		{
			switch (this.State)
			{
				case State.Block:
				case State.Expression:
				case State.Helper:
					return this.GetBlockEnd();

				case State.Directive:
					return this.NextStateInDirective();

				case State.Content:
					return this.NextStateInContent();

				case State.DirectiveName:
					return this.GetDirectiveName();

				case State.DirectiveValue:
					return this.GetDirectiveValue();

				default:
					throw new InvalidOperationException("Unexpected state '" + this.State + "'");
			}
		}

		private State GetBlockEnd()
		{
			var start = this.position;
			for (; this.position < this.content.Length; this.position++)
			{
				var c = this.content[this.position];
				this.nextStateTagStartLocation = this.nextStateLocation;
				this.nextStateLocation = this.nextStateLocation.AddCol();
				if (c == '\r')
				{
					if (this.position + 1 < this.content.Length && this.content[this.position + 1] == '\n')
						this.position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
				{
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '>' && this.content[this.position - 1] == '#' && this.content[this.position - 2] != '\\')
				{
					this.value = this.content.Substring(start, this.position - start - 1);
					this.position++;
					this.TagEndLocation = this.nextStateLocation;

					//skip newlines directly after blocks, unless they're expressions
					if (this.State != State.Expression && (this.position += this.IsNewLine()) > 0)
					{
						this.nextStateLocation = this.nextStateLocation.AddLine();
					}
					return State.Content;
				}
			}
			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		private State GetDirectiveName()
		{
			var start = this.position;
			for (; this.position < this.content.Length; this.position++)
			{
				var c = this.content[this.position];
				if (!Char.IsLetterOrDigit(c))
				{
					this.value = this.content.Substring(start, this.position - start);
					return State.Directive;
				}
				this.nextStateLocation = this.nextStateLocation.AddCol();
			}
			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		private State GetDirectiveValue()
		{
			var start = this.position;
			int delimiter = '\0';
			for (; this.position < this.content.Length; this.position++)
			{
				var c = this.content[this.position];
				this.nextStateLocation = this.nextStateLocation.AddCol();
				if (c == '\r')
				{
					if (this.position + 1 < this.content.Length && this.content[this.position + 1] == '\n')
						this.position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
					this.nextStateLocation = this.nextStateLocation.AddLine();
				if (delimiter == '\0')
				{
					if (c == '\'' || c == '"')
					{
						start = this.position;
						delimiter = c;
					}
					else if (!Char.IsWhiteSpace(c))
					{
						throw new ParserException("Unexpected character '" + c + "'. Expecting attribute value.", this.nextStateLocation);
					}
					continue;
				}
				if (c == delimiter)
				{
					this.value = this.content.Substring(start + 1, this.position - start - 1);
					this.position++;
					return State.Directive;
				}
			}
			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		private State NextStateInContent()
		{
			var start = this.position;
			for (; this.position < this.content.Length; this.position++)
			{
				var c = this.content[this.position];
				this.nextStateTagStartLocation = this.nextStateLocation;
				this.nextStateLocation = this.nextStateLocation.AddCol();
				if (c == '\r')
				{
					if (this.position + 1 < this.content.Length && this.content[this.position + 1] == '\n')
						this.position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
				{
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '<' && this.position + 2 < this.content.Length && this.content[this.position + 1] == '#')
				{
					this.TagEndLocation = this.nextStateLocation;
					var type = this.content[this.position + 2];
					if (type == '@')
					{
						this.nextStateLocation = this.nextStateLocation.AddCols(2);
						this.value = this.content.Substring(start, this.position - start);
						this.position += 3;
						return State.Directive;
					}
					if (type == '=')
					{
						this.nextStateLocation = this.nextStateLocation.AddCols(2);
						this.value = this.content.Substring(start, this.position - start);
						this.position += 3;
						return State.Expression;
					}
					if (type == '+')
					{
						this.nextStateLocation = this.nextStateLocation.AddCols(2);
						this.value = this.content.Substring(start, this.position - start);
						this.position += 3;
						return State.Helper;
					}
					this.value = this.content.Substring(start, this.position - start);
					this.nextStateLocation = this.nextStateLocation.AddCol();
					this.position += 2;
					return State.Block;
				}
			}
			//EOF is only valid when we're in content
			this.value = this.content.Substring(start);
			return State.EOF;
		}

		private int IsNewLine()
		{
			var found = 0;

			if (this.position < this.content.Length && this.content[this.position] == '\r')
			{
				found++;
			}
			if (this.position + found < this.content.Length && this.content[this.position + found] == '\n')
			{
				found++;
			}
			return found;
		}

		private State NextStateInDirective()
		{
			for (; this.position < this.content.Length; this.position++)
			{
				var c = this.content[this.position];
				if (c == '\r')
				{
					if (this.position + 1 < this.content.Length && this.content[this.position + 1] == '\n')
						this.position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
				{
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (Char.IsLetter(c))
				{
					return State.DirectiveName;
				}
				else if (c == '=')
				{
					this.nextStateLocation = this.nextStateLocation.AddCol();
					this.position++;
					return State.DirectiveValue;
				}
				else if (c == '#' && this.position + 1 < this.content.Length && this.content[this.position + 1] == '>')
				{
					this.position += 2;
					this.TagEndLocation = this.nextStateLocation.AddCols(2);
					this.nextStateLocation = this.nextStateLocation.AddCols(3);

					//skip newlines directly after directives
					if ((this.position += this.IsNewLine()) > 0)
					{
						this.nextStateLocation = this.nextStateLocation.AddLine();
					}

					return State.Content;
				}
				else if (!Char.IsWhiteSpace(c))
				{
					throw new ParserException("Directive ended unexpectedly with character '" + c + "'", this.nextStateLocation);
				}
				else
				{
					this.nextStateLocation = this.nextStateLocation.AddCol();
				}
			}
			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		public State State { get; private set; }

		public int Position
		{
			get { return this.position; }
		}

		public string Content
		{
			get { return this.content; }
		}

		public string Value
		{
			get { return this.value; }
		}

		public Location Location { get; private set; }
		public Location TagStartLocation { get; private set; }
		public Location TagEndLocation { get; private set; }
	}

	public enum State
	{
		Content = 0,
		Directive,
		Expression,
		Block,
		Helper,
		DirectiveName,
		DirectiveValue,
		Name,
		EOF
	}

	public class ParserException : Exception
	{
		public ParserException(string message, Location location) : base(message)
		{
			this.Location = location;
		}

		public Location Location { get; private set; }
	}
}
