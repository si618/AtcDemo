namespace AtcDemo.Server;

using AtcDemo.Server.Data;
using AtcDemo.Shared;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

public class AtcClassificationService : AtcClassificationRpcService.AtcClassificationRpcServiceBase
{
    private readonly AtcDbContext _db;

    public AtcClassificationService(AtcDbContext db)
    {
        _db = db;
    }

    // gRPC
    public override async Task<AtcClassificationReply> GetAtcClassifications(
        AtcClassificationRequest request,
        ServerCallContext context)
    {
        var classifications = _db.Classifications
            .AsNoTracking()
            .Include(c => c.Doses)
            .OrderBy(c => c.Name)
            .Where(a => a.ModifiedTicks > request.ModifiedSinceTicks);
        var reply = new AtcClassificationReply
        {
            ModifiedCount = await classifications.CountAsync()
        };
        reply.Classifications.AddRange(await classifications.Take(request.MaxCount).ToListAsync());
        return reply;
    }

    // WebAPI
    public async Task<IEnumerable<Atc.Classification>> GetAtcClassifications()
    {
        var classifications = _db.Classifications
            .AsNoTracking()
            .Include(c => c.Doses)
            .OrderBy(c => c.Code)
            .ThenBy(c => c.Name)
            .Select(c => c.ConvertFromProtobuf())
            .ToListAsync();
        return await classifications;
    }
}
