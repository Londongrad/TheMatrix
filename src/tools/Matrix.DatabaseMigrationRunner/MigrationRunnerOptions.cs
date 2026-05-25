namespace Matrix.DatabaseMigrationRunner
{
    internal sealed class MigrationRunnerOptions
    {
        public required string Service { get; init; }

        public string? Connection { get; init; }

        public bool ShowHelp { get; init; }

        public static MigrationRunnerOptions Parse(string[] args)
        {
            if (args.Any(arg => string.Equals(
                                    a: arg,
                                    b: "--help",
                                    comparisonType: StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(
                                    a: arg,
                                    b: "-h",
                                    comparisonType: StringComparison.OrdinalIgnoreCase)))
                return new MigrationRunnerOptions
                {
                    Service = "all",
                    ShowHelp = true
                };

            string? service = null;
            string? connection = null;

            for (int i = 0; i < args.Length; i++)
                switch (args[i])
                {
                    case "--service":
                        service = RequireValue(
                            args: args,
                            index: ++i,
                            optionName: "--service");
                        break;
                    case "--connection":
                        connection = RequireValue(
                            args: args,
                            index: ++i,
                            optionName: "--connection");
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown argument '{args[i]}'. Use --help to see supported options.");
                }

            if (string.IsNullOrWhiteSpace(service))
                throw new InvalidOperationException("Missing required argument --service.");

            return new MigrationRunnerOptions
            {
                Service = service,
                Connection = connection,
                ShowHelp = false
            };
        }

        public static void PrintHelp()
        {
            Console.WriteLine("Matrix.DatabaseMigrationRunner");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine(
                "  dotnet run --project src/tools/Matrix.DatabaseMigrationRunner -- --service <identity|economy|population|resources|simulationcore|simulationsystems|all> [--connection <connection-string>]");
            Console.WriteLine();
            Console.WriteLine(
                "Connection strings are read from configuration or environment variables like ConnectionStrings__IdentityDb.");
            Console.WriteLine("--connection is supported only for a single --service value.");
        }

        private static string RequireValue(
            string[] args,
            int index,
            string optionName)
        {
            if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
                throw new InvalidOperationException($"Missing value for {optionName}.");

            return args[index];
        }
    }
}
