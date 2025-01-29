namespace MondaySharp.NET.Application.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class MondayColumnHeaderAttribute(string columnId) : Attribute
{
    public string ColumnId { get; } = columnId;
}