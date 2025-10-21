using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
var db = builder.AddPostgres("pg").AddDatabase("lockDemo");

var apiService = builder.AddProject<LockDemo_ApiService>("apiservice")
    .WithReference(redis)
    .WithReference(db)
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Swagger";
        url.Url = "/swagger";
    });

var frontend = builder.AddNpmApp("frontend", "../lockdemo.web", "dev")
    .WithHttpEndpoint(5173, env: "VITE_PORT")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEnvironment("VITE_API_BASE", apiService.GetEndpoint("https"));

builder.Build().Run();
