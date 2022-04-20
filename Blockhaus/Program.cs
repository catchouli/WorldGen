using Blockhaus;
using MeshGen;
using Utils;
using WorldGen;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvc();

// Add services to the container.
builder.Services.AddRazorPages();

// Add Blockhaus services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUrlBuilder>((s) => new BlockhausUrlBuilder("", s.GetRequiredService<IHttpContextAccessor>()));
builder.Services.AddScoped<IChunkGenerator>((_) => new BasicChunkGenerator(0));
builder.Services.AddScoped<IChunkMeshGenerator, ChunkMeshGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
  endpoints.MapControllers();
});

app.MapRazorPages();

app.Run();
