namespace AtcDemo.Server;

using AtcDemo.Server.Data;
using AtcDemo.Shared;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

public class AtcDataService : AtcRecordService.AtcRecordServiceBase
{
    private readonly AtcDbContext _db;

    public AtcDataService(AtcDbContext db)
    {
        _db = db;
    }

    // gRPC
    public override async Task<AtcRecordReply> GetAtcRecords(AtcRecordRequest request, ServerCallContext context)
    {
        var chemicals = _db.Chemicals
            .AsNoTracking()
            .Include(c => c.Doses)
            .OrderBy(c => c.Name)
            .Where(a => a.ModifiedTicks > request.ModifiedSinceTicks);
        var reply = new AtcRecordReply
        {
            ModifiedCount = await chemicals.CountAsync()
        };
        reply.Chemicals.AddRange(await chemicals.Take(request.MaxCount).ToListAsync());
        return reply;
    }

    // WebAPI
    public IEnumerable<Atc.Chemical> GetAtcChemicals()
    {
        var chemicals = _db.Chemicals
            .AsNoTracking()
            .Include(c => c.Doses)
            .OrderBy(c => c.Code)
            .ThenBy(c => c.Name)
            .Select(c => c.ConvertFromProtobuf())
            .ToList();
        return chemicals;
    }
}
