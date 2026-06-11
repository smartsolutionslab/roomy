var builder = DistributedApplication.CreateBuilder(args);

builder.AddPostgres("postgres").AddDatabase("identity");

builder.Build().Run();
