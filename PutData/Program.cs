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

        // データベースを取得する
        var database = await client.CreateDatabaseIfNotExistsAsync("MyDatabase");
        var container = await database.Database.CreateContainerIfNotExistsAsync("MyContainer", "/city");

        // 件数を受け付ける
        while (true)
        {
            string cityName = SelectName("City");
            int count;
            Console.WriteLine("How many records do you want to insert?");
            while (!int.TryParse(Console.ReadLine(), out count))
            {
                Console.WriteLine("Please enter a number.");
            }

            for (int i = 0; i < count; i++)
            {
                // データを作成する
                var data = new Data { id = Guid.NewGuid().ToString(), city = cityName, name = "TestUser" };

                // データを挿入する
                await container.Container.CreateItemAsync(data);
            }
        }
    }

    private static string SelectName(string message)
    {
        string? results;
        while (true)
        {
            Console.WriteLine($"Select {message} Name");
            Console.WriteLine($"1. Tokyo");
            Console.WriteLine($"2. Osaka");
            results = Console.ReadLine();
            if (!string.IsNullOrEmpty(results) && (results == "1" || results == "2"))
            {
                return results == "1" ? "Tokyo" : "Osaka";
            }
        }
    }

}

class Data
{
    public string id { get; init; } = "";
    public string city { get; init; } = "";
    public string name { get; init; } = "";
    public DateTime created { get; init; } = DateTime.Now;
}