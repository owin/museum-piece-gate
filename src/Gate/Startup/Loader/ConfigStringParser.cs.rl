using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate
{
	interface IConfigStringParserDelegate
	{
		void OnIdentifier(string identifier);
		void OnAssembly(string assembly);
	}

	class ConfigStringParser
	{
		IConfigStringParserDelegate del;
		StringBuilder sb;


		%%{
		machine config_string_parser;
		
		action buf {
			sb.Append((char)fc);
		}

		action clear {
			sb.Length = 0;
		}

		action on_identifier {
			Console.WriteLine("on_identifier " + sb.ToString());
			del.OnIdentifier(sb.ToString());
		}

		action on_assembly {
			Console.WriteLine("on_assembly " + sb.ToString());
			del.OnAssembly(sb.ToString());
		}

		include config_string "config_string.rl";

		}%%

		int cs;
		%% write data;

		public ConfigStringParser(IConfigStringParserDelegate del)
		{
			this.del = del;
			sb = new StringBuilder();
			%% write init;
		}

		public int Execute(ArraySegment<byte> buf)
		{
            byte[] data = buf.Array;
            int p = buf.Offset;
            int pe = buf.Offset + buf.Count;
            int eof = buf.Count == 0 ? buf.Offset : -1;

			%% write exec;

			return p - buf.Offset;
		}
	}
}
