namespace RadiantConnectDocsUpdater
{
    internal class Program
    {
	    private static readonly List<DataTypeHandler.DataTypeBind> Binds =
	    [
			new (
				RecordStorage: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\ValorantApi",
				DocsPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectDocsUpdater\bin\Debug\net8.0\docs\data-types\valorant-api",
				ReplaceFunction: input =>
				{
					input = input[input.IndexOf("ValorantApi", StringComparison.InvariantCultureIgnoreCase)..];
					input = input.Replace("ValorantApi\\", "");
					input = input.Replace(".cs", "");
					return input;
				}
			),
			new (
				RecordStorage: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\Network",
				DocsPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectDocsUpdater\bin\Debug\net8.0\docs\data-types",
				ReplaceFunction: input =>
				{
					input = input[input.IndexOf("Network", StringComparison.InvariantCultureIgnoreCase)..];
					input = input.Replace("Network\\", "");
					input = input[..input.IndexOf('\\')];
					input = input.Replace("Endpoints", "");

					return input;
				},
				FilePattern: f => Path.GetFileName(Path.GetDirectoryName(f)) == "DataTypes"
			),
			new (
			    RecordStorage: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\RConnect",
			    DocsPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectDocsUpdater\bin\Debug\net8.0\docs\data-types",
			    ReplaceFunction: input =>
			    {
				    input = input[input.IndexOf("RConnect", StringComparison.InvariantCultureIgnoreCase)..];
				    input = input[..input.IndexOf('\\')];

				    return input;
			    },
			    FilePattern: f => Path.GetFileName(Path.GetDirectoryName(f)) == "DataTypes"
		    )
		];

	    private static readonly List<MethodHandler.MethodBind> MethodBinds =
		[
			new (
				ClassLocation: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\Network",
				OutputPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectSite\Docs\C#\docs\api",
				MethodType: MethodHandler.MethodType.Endpoint
			),
			new (
				ClassLocation: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\ValorantApi",
				OutputPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectSite\Docs\C#\docs\api\valorant-api",
				MethodType: MethodHandler.MethodType.ValorantApi
			),
			//new (
			//	ClassLocation: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\Network\ValorantNet.cs",
			//	OutputPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectDocsUpdater\bin\Debug\net8.0\docs\api",
			//	MethodType: MethodHandler.MethodType.ExternalSystem
			//),
			//new (
			//	ClassLocation: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\Services\LogService.cs",
			//	OutputPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectDocsUpdater\bin\Debug\net8.0\docs\services",
			//	MethodType: MethodHandler.MethodType.ExternalSystem
			//),
			new (
				ClassLocation: @"C:\Users\Riis\Documents\GitHub\RadiantConnect\RConnect",
				OutputPath: @"C:\Users\Riis\Documents\GitHub\RadiantConnectSite\Docs\C#\docs\api",
				MethodType: MethodHandler.MethodType.RConnect
			)
		];


		static async Task Main(string[] _)
        {
	        DataTypeHandler handler = new(Binds);
			MethodHandler handler2 = new(MethodBinds);

			await handler.Parse();
			await handler2.Parse();
		}
    }
}
