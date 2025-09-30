using System.Text;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.Regex;

namespace RadiantConnectDocsUpdater
{
	internal class MethodHandler
	{
		private readonly Dictionary<string, string> _csTypes = new()
		{
			{ "bool", "https://learn.microsoft.com/en-us/dotnet/api/system.boolean" },
			{ "byte", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "sbyte", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "short", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "ushort", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "int", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "uint", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "long", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "ulong", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/integral-numeric-types" },
			{ "char", "https://learn.microsoft.com/en-us/dotnet/api/system.char" },
			{ "float", "https://learn.microsoft.com/en-us/dotnet/api/system.single" },
			{ "double", "https://learn.microsoft.com/en-us/dotnet/api/system.double" },
			{ "decimal", "https://learn.microsoft.com/en-us/dotnet/api/system.decimal" },
			{ "string", "https://learn.microsoft.com/en-us/dotnet/api/system.string" },
			{ "object", "https://learn.microsoft.com/en-us/dotnet/api/system.object" },
			{ "void", "https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/built-in-types" },
			{ "task", "https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task" },
			{ "t", "https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics" }
		};

		private static readonly List<Method> Methods = [];

		[Flags]
		public enum ExtractFlags
		{
			Static = 1,
			ClassName = 2
		}

		public enum MethodType
		{
			Endpoint,
			ExternalSystem,
			RConnect,
			ValorantApi,
		}

		public record MethodBind(string ClassLocation, string OutputPath, MethodType MethodType);

		public record Method(string Name, string Namespace, string ReturnType, List<string> Parameters);

		private readonly List<MethodBind> _directoryBinds;

		private static readonly Dictionary<string, string> EndpointRoutes = new() {
			{ "ChatEndpoints", "initiator.Endpoints.ChatEndpoints" },
			{ "ContractEndpoints", "initiator.Endpoints.ContractEndpoints" },
			{ "CurrentGameEndpoints", "initiator.Endpoints.CurrentGameEndpoints" },
			{ "LocalEndpoints", "initiator.Endpoints.LocalEndpoints" },
			{ "PartyEndpoints", "initiator.Endpoints.PartyEndpoints" },
			{ "PreGameEndpoints", "initiator.Endpoints.PreGameEndpoints" },
			{ "PVPEndpoints", "initiator.Endpoints.PVPEndpoints" },
			{ "StoreEndpoints", "initiator.Endpoints.StoreEndpoints" }
		};

		private static readonly Dictionary<string, string> ExternalSystemsRoute = new() {
			{ "ValorantNet", "initiator.ExternalSystems.Net" },
			{ "LogService", "initiator.ExternalSystems.LogService" },
		};

		private static readonly Dictionary<string, string> RConnectRoutes = new() {
			{ "RConnectMethods", "initiator" }
		};

		private static readonly Dictionary<string, string> ValorantApiRoutes = new() {
			{ "Agents", "RadiantConnect.ValorantApi.Agents" },
			{ "Buddies", "RadiantConnect.ValorantApi.Buddies" },
			{ "Bundles", "RadiantConnect.ValorantApi.Bundles" },
			{ "Ceremonies", "RadiantConnect.ValorantApi.Ceremonies" },
			{ "CompetitiveTiers", "RadiantConnect.ValorantApi.CompetitiveTiers" },
			{ "ContentTiers", "RadiantConnect.ValorantApi.ContentTiers" },
			{ "Contracts", "RadiantConnect.ValorantApi.Contracts" },
			{ "Currencies", "RadiantConnect.ValorantApi.Currencies" },
			{ "Events", "RadiantConnect.ValorantApi.Events" },
			{ "Flexes", "RadiantConnect.ValorantApi.Flexes" },
			{ "Gamemodes", "RadiantConnect.ValorantApi.Gamemodes" },
			{ "Gears", "RadiantConnect.ValorantApi.Gears" },
			{ "LevelBorders", "RadiantConnect.ValorantApi.LevelBorders" },
			{ "Maps", "RadiantConnect.ValorantApi.Maps" },
			{ "PlayerCards", "RadiantConnect.ValorantApi.PlayerCards" },
			{ "PlayerTitles", "RadiantConnect.ValorantApi.PlayerTitles" },
			{ "Seasons", "RadiantConnect.ValorantApi.Seasons" },
			{ "Sprays", "RadiantConnect.ValorantApi.Sprays" },
			{ "Themes", "RadiantConnect.ValorantApi.Themes" },
			{ "Versions", "RadiantConnect.ValorantApi.Versions" },
			{ "Weapons", "RadiantConnect.ValorantApi.Weapons" }
		};


		public MethodHandler(List<MethodBind> directoryBinds)
		{
			foreach (MethodBind bind in directoryBinds)
			{
				if (!Directory.Exists(bind.OutputPath))
					throw new DirectoryNotFoundException($"The specified documentation path does not exist: {bind.OutputPath}");

				if (!Directory.Exists(bind.ClassLocation) && !File.Exists(bind.ClassLocation))
					throw new DirectoryNotFoundException($"The specified record storage path does not exist: {bind.ClassLocation}");
			}

			_directoryBinds = directoryBinds;
		}

		public async Task Parse()
		{
			foreach (MethodBind bind in _directoryBinds)
			{
				switch (bind.MethodType)
				{
					case MethodType.Endpoint:
						string[] files = Directory.GetFiles(bind.ClassLocation, "*.cs", SearchOption.AllDirectories).Where(f => Path.GetFileNameWithoutExtension(f).EndsWith("Endpoints")).ToArray();
						await HandleMethods(files, EndpointRoutes ,ExtractFlags.ClassName);
						break;
					case MethodType.ExternalSystem:
						await HandleMethods([bind.ClassLocation], ExternalSystemsRoute);
						break;
					case MethodType.RConnect:
						await HandleMethods(Directory.GetFiles(bind.ClassLocation, "*.cs", SearchOption.AllDirectories), RConnectRoutes, ExtractFlags.Static);
						break;
					case MethodType.ValorantApi:
						await HandleMethods(Directory.GetFiles(bind.ClassLocation, "*.cs", SearchOption.AllDirectories), ValorantApiRoutes, ExtractFlags.Static);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			Dictionary<string, StringBuilder> stringBuilderBind = [];
			
			foreach (Method method in Methods)
			{
				string stringBuilderName = method.Namespace[(method.Namespace.LastIndexOf('.') + 1)..];

				if (!stringBuilderBind.TryGetValue(stringBuilderName, out StringBuilder? sb))
				{
					stringBuilderBind[stringBuilderName] = new StringBuilder();

					stringBuilderBind[stringBuilderName].AppendLine($"# {stringBuilderName}");
					stringBuilderBind[stringBuilderName].AppendLine();
					stringBuilderBind[stringBuilderName].AppendLine($"Access via: {method.Namespace}\n\n");

					sb = stringBuilderBind[stringBuilderName];
				}
				
				sb.AppendLine(BuildMd(method));

				string finalOut = sb.ToString();
				string normalized = Replace(finalOut, @"(\r?\n){3,}", "\n\n");

				sb.Clear().Append(normalized);
			}

			foreach ((string key, StringBuilder value) in stringBuilderBind)
			{
				string fileName = key.ToLowerInvariant().Replace("endpoints", "", StringComparison.OrdinalIgnoreCase);

				if (fileName == "logservice") fileName = "../services/logservice";
				if (fileName == "net") fileName = "../services/valorantnet";
				if (fileName == "initiator") fileName = "rconnect";

				await File.WriteAllTextAsync($"{_directoryBinds[0].OutputPath}\\{fileName}.md", value.ToString()[..^2]);
			}
		}


		private static async Task HandleMethods(string[] files, Dictionary<string, string> routes, ExtractFlags? flags = null)
		{
			foreach (string file in files)
			{
				string fileData = await File.ReadAllTextAsync(file);

				bool staticClass = flags?.HasFlag(ExtractFlags.Static) ?? false;
				bool hasClassName = flags?.HasFlag(ExtractFlags.ClassName) ?? false;

				StringBuilder patternBuilder = new();

				patternBuilder.Append("public");
				if (staticClass) patternBuilder.Append(@"\sstatic");
				patternBuilder.Append(@"\s+class\s+");
				patternBuilder.Append(hasClassName ? @"(\w+Endpoints)" : @"(\w+)");

				MatchCollection classMatches = Matches(fileData, patternBuilder.ToString());

				foreach (Match classMatch in classMatches)
				{
					if (!classMatch.Success)
						continue;

					string className = classMatch.Groups[1].Value;

					if (!routes.TryGetValue(className, out string? route))
						continue;

					MatchCollection methodMatches = Matches(fileData, $@"public{(staticClass ? @"\s+static" : "")}\s+async\s+Task(?:<([\w?<>,\s]+)>)?\s+(\w+)(?:<([\w\s,<>?]+)>)?\s*\(([^)]*)\)", RegexOptions.IgnorePatternWhitespace);

					foreach (Match methodMatch in methodMatches)
					{
						string returnType = methodMatch.Groups[1].Value;
						string methodName = methodMatch.Groups[2].Value;
						string methodGenerics = methodMatch.Groups[3].Value;
						string parameters = methodMatch.Groups[4].Value;

						List<string> parameterList = SplitParameters(parameters);

						if (!string.IsNullOrEmpty(methodGenerics) && string.IsNullOrEmpty(returnType))
							returnType = "Task";

						Methods.Add(new Method(methodName, route, returnType, parameterList));
					}
				}
			}
		}

		private static List<string> SplitParameters(string parameters)
		{
			List<string> result = [];
			StringBuilder current = new();
			int angleDepth = 0;

			foreach (char c in parameters)
			{
				switch (c)
				{
					case '<':
						angleDepth++;
						break;
					case '>':
						angleDepth--;
						break;
				}

				if (c == ',' && angleDepth == 0)
				{
					result.Add(current.ToString().Trim());
					current.Clear();
				}
				else current.Append(c);
			}

			if (current.Length > 0) result.Add(current.ToString().Trim());

			return result;
		}

		private string BuildMd(Method method)
		{
			string recordName = method.Name;

			StringBuilder markdown = new();

			markdown.AppendLine($"---\n\n## {recordName}\n");


			markdown.AppendLine("""
				                    | **Type** | **Parameter** | **Nullable** |
				                    |---------------|----------|--------------|
				                    """);

			foreach (string param in method.Parameters)
			{
				string paramTrimmed = param.Trim();

				if (paramTrimmed.Contains('='))
					paramTrimmed = paramTrimmed[..paramTrimmed.IndexOf('=')].Trim();

				string type = paramTrimmed[..paramTrimmed.LastIndexOf(' ')];
				string name = paramTrimmed[(paramTrimmed.LastIndexOf(' ')+1)..];

				if (type.Contains("this "))
					continue;
				
				markdown.AppendLine($"| `{type}` | {name} | {(type.Contains('?') ? "true" : "false")} |");
			}

			markdown.AppendLine("");

			string typeReturn = method.ReturnType;

			if (!string.IsNullOrEmpty(typeReturn))
			{
				string dataTypeLocation = 
					_csTypes.TryGetValue(typeReturn.ToLowerInvariant().Replace("?", ""), out string? link) ?
						link : 
						GetDataLoc(method);

				markdown.AppendLine($"""
				                     |  Return Type     | Nullable | Docs Source |
				                     |-----------------|----------|-------------|
				                     | `{typeReturn}` | {(typeReturn.Contains('?') ? "true" : "false")}      | [{typeReturn.Replace("<", "&lt;").Replace(">","&gt;")}]({dataTypeLocation}) |
				                     """);
			}
				
			markdown.AppendLine("");
			markdown.AppendLine($"\nFull code method:\n\n```csharp\n{typeReturn} {method.Name}({string.Join(' ', method.Parameters)});\n```");
			return markdown.ToString();
		}

		private static string GetDataLoc(Method method)
		{
			string basePath = method.Namespace[(method.Namespace.LastIndexOf('.') + 1)..].ToLowerInvariant();
			basePath = basePath.Replace("endpoints", "", StringComparison.OrdinalIgnoreCase);
			basePath = $"data-types/{basePath}.md";
			return basePath;
		}
	}
}
