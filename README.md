# MondaySharp.NET
<!---
[![Build Status](https://your-ci-service.com/your-username/your-repo/badge.svg)](https://your-ci-service.com/your-username/your-repo)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)
-->

MondaySharp.NET is a powerful and intuitive C# library for interacting with the Monday.com API. With this library, developers can seamlessly integrate Monday.com functionalities into their C# applications, making it easier than ever to manage and automate workflows.

## Features

- **Easy Integration**: Quickly integrate Monday.com features into your C# projects with a clean and straightforward API.
- **Full API Coverage**: Access the complete range of Monday.com API endpoints, allowing you to interact with boards, items, columns, and more.
- **Type-Safe Models**: Benefit from type-safe models that reflect the structure of Monday.com entities, providing a robust development experience.
- **Asynchronous Support**: Leverage asynchronous methods for non-blocking communication with the Monday.com API, ensuring optimal performance.

## Getting Started

The library can be injected via Dependency Injection or you can initialize it manually.

**Initializing** 
```csharp
services.TryAddMondayClient(options =>
{
    options.EndPoint = new System.Uri(configuration["mondayUrl"]!);
    options.Token = configuration["mondayToken"]!;
});

IMondayClient mondayClient = new MondayClient(this.Logger, options =>
{
    options.EndPoint = new System.Uri(configuration["mondayUrl"]!);
    options.Token = configuration["mondayToken"]!;
});
```

**Reading**<br>
When binding your row to an object, you can create a record inheriting `MondayRow`
 
```csharp
public record TestRow : MondayRow
{
    [MondayColumnHeader("text0")]
    public ColumnText? Text { get; set; }

    [MondayColumnHeader("numbers9")]
    public ColumnNumber? Number { get; set; }
    public ColumnCheckBox? Checkbox { get; set; }
    public ColumnStatus? Priority { get; set; }
}
```
If you have a property that doesn't conform to your naming convention, 
you can simply add the `MondayColumnHeader` attribute, this will tell the client when attempting to bind the properties at runtime to look for this `columnId` instead of using the property name.

If you need to include `Groups, Assets, Updates`  you can add them as a property.

*Here are some example records that were used during testing to validate Assets, Updates and Group were successfully binding.*
```csharp
public record TestRowWithAssets : TestRow
{
    public List<Asset>? Assets { get; set; }
}
public record TestRowWithUpdates : TestRow
{
    public List<Update>? Updates { get; set; }
}
public record TestRowWithGroup : TestRow
{
    public Group? Group { get; set; }
}
```

when the library is creating the query to read the item(s),
it will detect if there are assets, updates or groups, it will then modify the query as needed.

This way, we are not always requesting the assets, updates or groups, only when you need them.

When required to read a column based from [ColumnValues](https://developer.monday.com/api-reference/docs/column-values-v2), you can do the following.
```csharp
ColumnValue[] columnValues =
[
    new ColumnValue()
    {
        Id = "text0",
        Text = "123"
    },
    new ColumnValue()
    {
        Id = "numbers9",
        Text = "1"
    },
];
List<TestRow?> items = await this.MondayClient!.GetBoardItemsAsync<TestRow>(this.BoardId, columnValues).ToListAsync();
```
This will attempt to find any items for the `boardId` along with the `columnValues`.
If you need items without using the `columnValues`, you can simply do the following

*This will enumerate each result asynchronously*
```csharp
List<TestRow?> items = await this.MondayClient!.GetBoardItemsAsync<TestRow>(this.BoardId).ToListAsync();
```
**Creating**<br>
when required to create an item, you can do the following
```csharp
Item[] items =[
    new Item()
    {
        Name = "Test Item 1",
        ColumnValues =
        [
            new ColumnValue()
            {
                ColumnBaseType = new ColumnText()
                {
                    Id = "text0",
                    Text = "Andrew Eberle"
                },
            },
            new ColumnValue()
            {
                ColumnBaseType = new ColumnNumber()
                {
                    Id = "numbers9",
                    Number = 10
                },
            },
        ]
    },
     new Item()
    {
        Name = "Test Item 2",
        ColumnValues =
        [
            new ColumnValue()
            {
                ColumnBaseType = new ColumnText()
                {
                    Id = "text0",
                    Text = "Eberle Andrew"
                },
            },
            new ColumnValue()
            {
                ColumnBaseType = new ColumnNumber()
                {
                    Id = "numbers9",
                    Number = 11
                },
            },
        ]
    }
];

Dictionary<string, Item>? keyValuePairs = await this.MondayClient!.CreateBoardItemsAsync(BoardId, items);
```

This will create items into monday with a single request similar to this
https://developer.monday.com/api-reference/docs/introduction-to-graphql#sample-mutation-1

There are any column types, some supported, some not as I'm still building the library.
Here is a small example pulled from some unit tests.
```csharp
// Arrange
List<ColumnBaseType> columnValues =
[
    new ColumnDateTime("date", new DateTime(2023, 11, 29)),
    new ColumnText("text0", "Andrew Eberle"),
    new ColumnNumber("numbers", 10),
    new ColumnLongText("long_text7", "hello,world!"),
    new ColumnStatus("status_19", "Test"),
    new ColumnStatus("label", "Test"),
    new ColumnLongText("long_text", "long text with return \n"),
    new ColumnDropDown("dropdown", ["Hello", "World"]),
    new ColumnLink("link", "https://www.google.com", "google!"),
    new ColumnTag("tags", "21057674,21057675"),
    new ColumnTimeline("timeline", new DateTime(2023, 11, 29), new DateTime(2023, 12, 29)),
];
```
First provide the `columnId` and then filling the rest.

Here is the interface thusfar.
For detailed usage instructions and examples, refer to the [Documentation](./docs/).

## Installation

Install the MondaySharp.NET library using NuGet Package Manager:

```bash
nuget install MondaySharp.NET
```

## Contributing

We welcome contributions! Please check out our [Contributing Guidelines](./CONTRIBUTING.md) for details on how to get started.

## License

MondaySharp.NET is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

---

Feel free to customize this description to better fit the specific features and goals of your MondaySharp.NET library.
