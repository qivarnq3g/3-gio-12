using ThreeTwelve.Api.Services;

namespace ThreeTwelve.Api.Domain;

public sealed record DateRange(DateTimeOffset From, DateTimeOffset To)
{
    public static DateRange Resolve(DateTimeOffset? from, DateTimeOffset? to, TimeProvider clock)
    {
        var resolvedTo = to?.ToUniversalTime() ?? clock.GetUtcNow();
        var resolvedFrom = from?.ToUniversalTime() ?? resolvedTo.AddDays(-30);

        if (resolvedFrom >= resolvedTo)
            throw new BadRequestException("Mốc bắt đầu (from) phải trước mốc kết thúc (to).");

        if (resolvedTo - resolvedFrom > TimeSpan.FromDays(366))
            throw new BadRequestException("Khoảng thời gian không được dài hơn 366 ngày.");

        return new DateRange(resolvedFrom, resolvedTo);
    }
}
