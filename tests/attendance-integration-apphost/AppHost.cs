var builder = DistributedApplication.CreateBuilder(args);

builder.AddPostgres("postgres").AddDatabase("attendance");

builder.Build().Run();
