// See https://aka.ms/new-console-template for more information
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;


internal class Program
{
    private static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
#if DEBUG
            .AddJsonFile("appSettings.Development.json")
#else
            .AddJsonFile("appSettings.json")
#endif
    .Build();

        var connectionString = configuration["ConnectionStrings:CosmosDB"];
        // CosmosDB Cilnet を作成する
        var client = new Microsoft.Azure.Cosmos.CosmosClient(connectionString);

        string processorName = SelectName("Processor");
        string instanceName = SelectName("Instance");

        var changeFeedProcessor = await StartChangeFeedProcessorAsync(client, processorName, instanceName);

        Console.WriteLine("Press any key to stop the processor...");
        Console.ReadKey();
        await changeFeedProcessor.StopAsync();
        Console.WriteLine("Change Feed Processor stopped.");
    }

    private static string SelectName(string message)
    {
        string? results;
        while (true)
        {
            Console.WriteLine($"Select {message} Name");
            Console.WriteLine($"1. {message}-1");
            Console.WriteLine($"2. {message}-2");
            results = Console.ReadLine();
            if (!string.IsNullOrEmpty(results) && (results == "1" || results == "2"))
            {
                return results == "1" ? $"{message}-1" : $"{message}-2";
            }
        }
    }



    /// <summary>
    /// Start the Change Feed Processor to listen for changes and process them with the HandleChangesAsync implementation.
    /// </summary>
    private static async Task<ChangeFeedProcessor> StartChangeFeedProcessorAsync(
            CosmosClient cosmosClient,
            string processorName, string instanceName)
    {
        string databaseName = "MyDatabase";
        string sourceContainerName = "MyContainer";
        string leaseContainerName = "leases";

        //Container leaseContainer = cosmosClient.GetContainer(databaseName, leaseContainerName);
        var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
        var leaseContainer = await database.Database.CreateContainerIfNotExistsAsync(leaseContainerName, "/id");

        ChangeFeedProcessor changeFeedProcessor = cosmosClient.GetContainer(databaseName, sourceContainerName)
            .GetChangeFeedProcessorBuilder<Data>(processorName: processorName, onChangesDelegate: HandleChangesAsync)
                .WithInstanceName(instanceName)
                .WithLeaseContainer(leaseContainer)
                //                .WithStartTime(DateTime.MinValue.ToUniversalTime())
                .Build();

        Console.WriteLine("Starting Change Feed Processor...");
        await changeFeedProcessor.StartAsync();
        Console.WriteLine("Change Feed Processor started.");
        return changeFeedProcessor;
    }

    /// <summary>
    /// The delegate receives batches of changes as they are generated in the change feed and can process them.
    /// </summary>
    static async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<Data> changes,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Started handling changes for lease {context.LeaseToken}...");
        Console.WriteLine($"Change Feed request consumed {context.Headers.RequestCharge} RU.");
        // SessionToken if needed to enforce Session consistency on another client instance
        Console.WriteLine($"SessionToken ${context.Headers.Session}");

        // We may want to track any operation's Diagnostics that took longer than some threshold
        // if (context.Diagnostics.GetClientElapsedTime() > TimeSpan.FromSeconds(1))
        // {
        //     Console.WriteLine($"Change Feed request took longer than expected. Diagnostics:" + context.Diagnostics.ToString());
        // }

        foreach (Data item in changes)
        {
            Console.WriteLine($"Detected operation for item with id {item.id}, created at {item.created}.");
            // Simulate some asynchronous operation
            await Task.Delay(10);
        }

        Console.WriteLine("Finished handling changes.");
    }

    class Data
    {
        public string id { get; init; } = "";
        public string city { get; init; } = "";
        public string name { get; init; } = "";
        public DateTime created { get; init; } = DateTime.Now;
    }
}