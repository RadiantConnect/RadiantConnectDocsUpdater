using System.Text;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.Regex;

namespace RadiantConnectDocsUpdater
{
	public class DataTypeHandler
	{
		private readonly List<DataTypeBind> _directoryBinds;
		
		public record DataTypeBind(string RecordStorage, string DocsPath, Func<string, string> ReplaceFunction, Func<string, bool>? FilePattern = null);

		public DataTypeHandler(List<DataTypeBind> directoryBinds)
		{
			foreach (DataTypeBind bind in directoryBinds)
			{
				if (!Directory.Exists(bind.DocsPath))
					throw new DirectoryNotFoundException($"The specified documentation path does not exist: {bind.DocsPath}");

				if (!Directory.Exists(bind.RecordStorage))
					throw new DirectoryNotFoundException($"The specified record storage path does not exist: {bind.RecordStorage}");

			}

			_directoryBinds = directoryBinds;
		}

		public async Task Parse()
		{
			foreach (DataTypeBind bind in _directoryBinds)
			{
				Dictionary<string, List<string>> fileInfo = GetDataFiles(bind);

				foreach ((string key, List<string> files) in fileInfo)
				{
					if (key.ToLowerInvariant() == "client") continue;

					StringBuilder sb = new();
					sb.AppendLine($"# {key}\n\n");

					foreach (string file in files)
					{
						sb.AppendLine(BuildMd(file));
					}

					string finalOut = sb.ToString();
					string normalized = Replace(finalOut, @"(\r?\n){3,}", "\n\n")[..^1];
					
					await File.WriteAllTextAsync($"{bind.DocsPath}\\{key.ToLowerInvariant()}.md", normalized);
				}
			}
		}

		private string BuildMd(string file)
		{
			string code = File.ReadAllText(file);

			const string recordPattern = @"public\s+record\s+(\w+)\s*\(([\s\S]*?)\);";

			MatchCollection recordMatches = Matches(code, recordPattern);

			string output = "";

			foreach (Match recordMatch in recordMatches)
			{
				string recordName = recordMatch.Groups[1].Value;
				string parameters = recordMatch.Groups[2].Value;

				StringBuilder markdown = new();

				markdown.AppendLine($"---\n\n## {recordName}\n");

				string paramPattern = parameters.Contains("JsonPropertyName") 
					? @"\]\s*(?<type>[\w<>,\s\.\?]+)\s+(?<name>\w+)" 
					: @"\s*(?<type>[\w<>,\s\.\?]+?)\s+(?<name>\w+)(?:,|$)";

				MatchCollection paramMatches = Matches(parameters, paramPattern);

				markdown.AppendLine("""
				                    | **Type** | **Parameter** | **Nullable** |
				                    |---------------|----------|--------------|
				                    """);

				foreach (Match param in paramMatches)
				{
					string type = param.Groups["type"].Value.Trim();
					string name = param.Groups["name"].Value.Trim();
					markdown.AppendLine($"| `{type}` | {name} | {(type.Contains('?') ? "true" : "false")} |");
				}

				markdown.AppendLine("");

				output += markdown.ToString();
			}

			return output;
		}

		private Dictionary<string, List<string>> GetDataFiles(DataTypeBind input)
		{
			Dictionary<string, List<string>> dataTypeFiles = [];

			Func<string,bool> filePattern = input.FilePattern ?? (_ => true);

			string[] files = Directory.GetFiles(input.RecordStorage, "*.cs", SearchOption.AllDirectories).Where(filePattern).ToArray();

			foreach (string file in files)
			{
				string endpointName = input.ReplaceFunction(file);

				if (!dataTypeFiles.ContainsKey(endpointName))
					dataTypeFiles.Add(endpointName, []);

				dataTypeFiles[endpointName].Add(file);
			}

			return dataTypeFiles;
		}
	}
}
