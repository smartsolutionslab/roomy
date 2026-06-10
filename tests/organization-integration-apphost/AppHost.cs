var builder = DistributedApplication.CreateBuilder(args);

builder.AddPostgres("postgres").AddDatabase("organization");

builder.Build().Run();
