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

namespace GameDevWare.TextTransform.Editor.Processor
{
	public class Tokeniser
	{
		private State nextState = State.Content;
		private Location nextStateLocation;
		private Location nextStateTagStartLocation;

		public State State { get; private set; }

		public int Position { get; private set; }

		public string Content { get; }

		public string Value { get; private set; }

		public Location Location { get; private set; }
		public Location TagStartLocation { get; private set; }
		public Location TagEndLocation { get; private set; }

		public Tokeniser(string fileName, string content)
		{
			this.State = State.Content;
			this.Content = content;
			this.Location = this.nextStateLocation = this.nextStateTagStartLocation = new Location(fileName, 1, 1);
		}

		public bool Advance()
		{
			this.Value = null;
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

				case State.Name:
				case State.EOF:
				default:
					throw new InvalidOperationException("Unexpected state '" + this.State + "'");
			}
		}

		private State GetBlockEnd()
		{
			var start = this.Position;
			for (; this.Position < this.Content.Length; this.Position++)
			{
				var c = this.Content[this.Position];
				this.nextStateTagStartLocation = this.nextStateLocation;
				this.nextStateLocation = this.nextStateLocation.AddCol();
				if (c == '\r')
				{
					if (this.Position + 1 < this.Content.Length && this.Content[this.Position + 1] == '\n')
						this.Position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
					this.nextStateLocation = this.nextStateLocation.AddLine();
				else if (c == '>' && this.Content[this.Position - 1] == '#' && this.Content[this.Position - 2] != '\\')
				{
					this.Value = this.Content.Substring(start, this.Position - start - 1);
					this.Position++;
					this.TagEndLocation = this.nextStateLocation;

					//skip newlines directly after blocks, unless they're expressions
					if (this.State != State.Expression && (this.Position += this.IsNewLine()) > 0) this.nextStateLocation = this.nextStateLocation.AddLine();
					return State.Content;
				}
			}

			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		private State GetDirectiveName()
		{
			var start = this.Position;
			for (; this.Position < this.Content.Length; this.Position++)
			{
				var c = this.Content[this.Position];
				if (!char.IsLetterOrDigit(c))
				{
					this.Value = this.Content.Substring(start, this.Position - start);
					return State.Directive;
				}

				this.nextStateLocation = this.nextStateLocation.AddCol();
			}

			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		private State GetDirectiveValue()
		{
			var start = this.Position;
			int delimiter = '\0';
			for (; this.Position < this.Content.Length; this.Position++)
			{
				var c = this.Content[this.Position];
				this.nextStateLocation = this.nextStateLocation.AddCol();
				if (c == '\r')
				{
					if (this.Position + 1 < this.Content.Length && this.Content[this.Position + 1] == '\n')
						this.Position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
					this.nextStateLocation = this.nextStateLocation.AddLine();

				if (delimiter == '\0')
				{
					if (c == '\'' || c == '"')
					{
						start = this.Position;
						delimiter = c;
					}
					else if (!char.IsWhiteSpace(c))
						throw new ParserException("Unexpected character '" + c + "'. Expecting attribute value.", this.nextStateLocation);

					continue;
				}

				if (c == delimiter)
				{
					this.Value = this.Content.Substring(start + 1, this.Position - start - 1);
					this.Position++;
					return State.Directive;
				}
			}

			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}

		private State NextStateInContent()
		{
			var start = this.Position;
			for (; this.Position < this.Content.Length; this.Position++)
			{
				var c = this.Content[this.Position];
				this.nextStateTagStartLocation = this.nextStateLocation;
				this.nextStateLocation = this.nextStateLocation.AddCol();
				if (c == '\r')
				{
					if (this.Position + 1 < this.Content.Length && this.Content[this.Position + 1] == '\n')
						this.Position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
					this.nextStateLocation = this.nextStateLocation.AddLine();
				else if (c == '<' && this.Position + 2 < this.Content.Length && this.Content[this.Position + 1] == '#')
				{
					this.TagEndLocation = this.nextStateLocation;
					var type = this.Content[this.Position + 2];
					if (type == '@')
					{
						this.nextStateLocation = this.nextStateLocation.AddCols(2);
						this.Value = this.Content.Substring(start, this.Position - start);
						this.Position += 3;
						return State.Directive;
					}

					if (type == '=')
					{
						this.nextStateLocation = this.nextStateLocation.AddCols(2);
						this.Value = this.Content.Substring(start, this.Position - start);
						this.Position += 3;
						return State.Expression;
					}

					if (type == '+')
					{
						this.nextStateLocation = this.nextStateLocation.AddCols(2);
						this.Value = this.Content.Substring(start, this.Position - start);
						this.Position += 3;
						return State.Helper;
					}

					this.Value = this.Content.Substring(start, this.Position - start);
					this.nextStateLocation = this.nextStateLocation.AddCol();
					this.Position += 2;
					return State.Block;
				}
			}

			//EOF is only valid when we're in content
			this.Value = this.Content.Substring(start);
			return State.EOF;
		}

		private int IsNewLine()
		{
			var found = 0;

			if (this.Position < this.Content.Length && this.Content[this.Position] == '\r') found++;
			if (this.Position + found < this.Content.Length && this.Content[this.Position + found] == '\n') found++;
			return found;
		}

		private State NextStateInDirective()
		{
			for (; this.Position < this.Content.Length; this.Position++)
			{
				var c = this.Content[this.Position];
				if (c == '\r')
				{
					if (this.Position + 1 < this.Content.Length && this.Content[this.Position + 1] == '\n')
						this.Position++;
					this.nextStateLocation = this.nextStateLocation.AddLine();
				}
				else if (c == '\n')
					this.nextStateLocation = this.nextStateLocation.AddLine();
				else if (char.IsLetter(c))
					return State.DirectiveName;
				else if (c == '=')
				{
					this.nextStateLocation = this.nextStateLocation.AddCol();
					this.Position++;
					return State.DirectiveValue;
				}
				else if (c == '#' && this.Position + 1 < this.Content.Length && this.Content[this.Position + 1] == '>')
				{
					this.Position += 2;
					this.TagEndLocation = this.nextStateLocation.AddCols(2);
					this.nextStateLocation = this.nextStateLocation.AddCols(3);

					//skip newlines directly after directives
					if ((this.Position += this.IsNewLine()) > 0) this.nextStateLocation = this.nextStateLocation.AddLine();

					return State.Content;
				}
				else if (!char.IsWhiteSpace(c))
					throw new ParserException("Directive ended unexpectedly with character '" + c + "'", this.nextStateLocation);
				else
					this.nextStateLocation = this.nextStateLocation.AddCol();
			}

			throw new ParserException("Unexpected end of file.", this.nextStateLocation);
		}
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
		public Location Location { get; private set; }
		public ParserException(string message, Location location) : base(message)
		{
			this.Location = location;
		}
	}
}
