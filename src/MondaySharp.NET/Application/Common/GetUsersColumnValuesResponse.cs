using MondaySharp.NET.Domain.Common;

namespace MondaySharp.NET.Application.Common;

internal sealed class GetUsersColumnValuesResponse
{
    public List<MondayUser>? Users { get; set; } = [];
}