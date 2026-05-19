# MondaySharp.NET

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT)

A C# library for interacting with the [Monday.com API](https://developer.monday.com/api-reference/docs). Map Monday.com boards to strongly-typed C# records, then read, create, update, and delete items with minimal boilerplate.

## Table of Contents

- [Installation](#installation)
- [Setup](#setup)
- [Defining a Row](#defining-a-row)
- [Reading Items](#reading-items)
  - [All items on a board](#all-items-on-a-board)
  - [Filter by column values](#filter-by-column-values)
  - [By item ID(s)](#by-item-ids)
  - [By cursor (pagination)](#by-cursor-pagination)
  - [Including Groups, Assets, and Updates](#including-groups-assets-and-updates)
  - [Including Sub-Items](#including-sub-items)
- [Creating Items](#creating-items)
  - [Using Item objects](#using-item-objects)
  - [Using a MondayRow](#using-a-mondayrow)
- [Updating Items](#updating-items)
- [Deleting Items](#deleting-items)
- [Sub-Items](#sub-items)
  - [Reading sub-items](#reading-sub-items)
  - [Creating sub-items](#creating-sub-items)
- [Item Updates (Comments)](#item-updates-comments)
- [File Uploads](#file-uploads)
- [Boards](#boards)
- [Users](#users)
- [Supported Column Types](#supported-column-types)
- [Contributing](#contributing)
- [License](#license)

---

## Installation

```bash
nuget install MondaySharp.NET
```

---

## Setup

Register the client with dependency injection, or instantiate it directly.

**Dependency Injection**

```csharp
services.TryAddMondayClient(options =>
{
    options.EndPoint = new Uri(configuration["mondayUrl"]!);
    options.Token = configuration["mondayToken"]!;
});
```

**Manual instantiation**

```csharp
IMondayClient mondayClient = new MondayClient(logger, options =>
{
    options.EndPoint = new Uri(configuration["mondayUrl"]!);
    options.Token = configuration["mondayToken"]!;
});
```

---

## Defining a Row

Create a record that inherits `MondayRow`. Each property maps to a Monday.com column by its **column ID**.

```csharp
public record ProjectRow : MondayRow
{
    // Property name matches the column ID (case-insensitive)
    public ColumnStatus? Status { get; set; }

    // Use [MondayColumnHeader] when the property name differs from the column ID
    [MondayColumnHeader("text_abc123")]
    public ColumnText? Description { get; set; }

    [MondayColumnHeader("date_xyz789")]
    public ColumnDateTime? DueDate { get; set; }

    [MondayColumnHeader("numbers_def456")]
    public ColumnNumber? Budget { get; set; }
}
```

`MondayRow` provides `Id` (`ulong`) and `Name` (`string?`) automatically.

---

## Reading Items

All read methods return a `MondayResponse<T>` which contains:

| Property | Description |
|---|---|
| `IsSuccessful` | Whether the request succeeded |
| `Response` | `List<MondayData<T>>` — each item wrapped in `MondayData<T>.Data` |
| `Cursor` | Pagination cursor for the next page |
| `HasMore` | `true` when a next page is available |
| `Errors` | `HashSet<string>` of error messages, or `null` |

### All items on a board

```csharp
MondayResponse<ProjectRow> response = await mondayClient.GetBoardItemsAsync<ProjectRow>(boardId);

foreach (MondayData<ProjectRow> entry in response.Response ?? [])
{
    ProjectRow row = entry.Data!;
    Console.WriteLine($"{row.Id}: {row.Name}");
}
```

An optional `limit` parameter controls page size (default `25`, max `500`).

```csharp
MondayResponse<ProjectRow> response = await mondayClient.GetBoardItemsAsync<ProjectRow>(boardId, limit: 100);
```

### Filter by column values

```csharp
ColumnValue[] filters =
[
    new() { Id = "text_abc123", Text = "Andrew Eberle" },
];

MondayResponse<ProjectRow> response =
    await mondayClient.GetBoardItemsAsync<ProjectRow>(boardId, filters);
```

### By item ID(s)

```csharp
MondayResponse<ProjectRow> response =
    await mondayClient.GetBoardItemsAsync<ProjectRow>([itemId1, itemId2]);
```

### By cursor (pagination)

Use `HasMore` and `Cursor` to page through large boards.

```csharp
MondayResponse<ProjectRow> page = await mondayClient.GetBoardItemsAsync<ProjectRow>(boardId, limit: 50);

while (page.HasMore)
{
    page = await mondayClient.GetBoardItemsAsync<ProjectRow>(page.Cursor, limit: 50);
    // process page.Response ...
}
```

### Including Groups, Assets, and Updates

Add typed properties to your row record and the library will automatically include the relevant fields in the query.

```csharp
public record ProjectRowWithExtras : ProjectRow
{
    public Group? Group { get; set; }
    public List<Asset>? Assets { get; set; }
    public List<Update>? Updates { get; set; }
}

MondayResponse<ProjectRowWithExtras> response =
    await mondayClient.GetBoardItemsAsync<ProjectRowWithExtras>(boardId);
```

---

## Creating Items

### Using Item objects

```csharp
Item[] items =
[
    new()
    {
        Name = "Project Alpha",
        ColumnValues =
        [
            new() { ColumnBaseType = new ColumnText()     { Id = "text_abc123", Text = "Andrew Eberle" } },
            new() { ColumnBaseType = new ColumnNumber()   { Id = "numbers_def456", Number = 10 } },
            new() { ColumnBaseType = new ColumnStatus()   { Id = "status", Status = "In Progress" } },
            new() { ColumnBaseType = new ColumnDateTime() { Id = "date_xyz789", Date = new DateTime(2024, 6, 1) } },
        ]
    },
    new()
    {
        Name = "Project Beta",
        ColumnValues =
        [
            new() { ColumnBaseType = new ColumnText() { Id = "text_abc123", Text = "Eberle Andrew" } },
        ]
    }
];

MondayResponse<Item> response = await mondayClient.CreateBoardItemsAsync(boardId, items);
// response.Response[i].Data.Id is populated after creation
```

### Using a MondayRow

When your record already holds the values you want to write, pass it directly. The library maps each property back to its column ID automatically.

```csharp
ProjectRow newRow = new()
{
    Name = "Project Gamma",
    Description = new ColumnText() { Text = "Created via MondaySharp.NET" },
    Budget      = new ColumnNumber() { Number = 5000 },
    Status      = new ColumnStatus() { Status = "In Progress" },
    DueDate     = new ColumnDateTime() { Date = new DateTime(2024, 12, 31) },
};

MondayResponse<ProjectRow> response =
    await mondayClient.CreateBoardItemsAsync<ProjectRow>(boardId, [newRow]);
```

---

## Updating Items

Use `UpdateBoardItemsAsync` with a populated row that already has its `Id` set.

```csharp
// Modify the row retrieved earlier
existingRow.Status = new ColumnStatus() { Status = "Done" };
existingRow.Name   = "Project Gamma (Completed)";

MondayResponse<ProjectRow> response =
    await mondayClient.UpdateBoardItemsAsync<ProjectRow>(boardId, [existingRow]);
```

---

## Deleting Items

```csharp
// Delete by Item object (Id must be set)
MondayResponse<Item> response = await mondayClient.DeleteItemsAsync([item1, item2]);
```

---

## Sub-Items

### Reading sub-items

Define a record inheriting `MondaySubRow` to map the sub-item's columns, then add a `List<T>` property to your parent row. The library detects the property and includes `subitems { ... }` in the query automatically.

```csharp
public record TaskSubRow : MondaySubRow
{
    [MondayColumnHeader("status")]
    public ColumnStatus? Status { get; set; }

    [MondayColumnHeader("date0")]
    public ColumnDateTime? DueDate { get; set; }

    [MondayColumnHeader("numbers8")]
    public ColumnNumber? Estimate { get; set; }
}

public record ProjectRowWithSubItems : ProjectRow
{
    public List<TaskSubRow> SubItems { get; set; } = [];
}
```

```csharp
MondayResponse<ProjectRowWithSubItems> response =
    await mondayClient.GetBoardItemsAsync<ProjectRowWithSubItems>(boardId);

foreach (MondayData<ProjectRowWithSubItems> entry in response.Response ?? [])
{
    foreach (TaskSubRow subItem in entry.Data!.SubItems)
    {
        Console.WriteLine($"  SubItem: {subItem.Name}, Status: {subItem.Status?.Status}");
    }
}
```

`MondaySubRow` provides `Id` and `Name` automatically, just like `MondayRow`.

### Creating sub-items

**Using Item objects**

```csharp
Item[] subItems =
[
    new()
    {
        Name = "Task 1",
        ColumnValues =
        [
            new() { ColumnBaseType = new ColumnStatus()   { Id = "status", Status = "In Progress" } },
            new() { ColumnBaseType = new ColumnDateTime() { Id = "date0",  Date = new DateTime(2024, 6, 1) } },
        ]
    },
    new() { Name = "Task 2" }
];

MondayResponse<Item> response =
    await mondayClient.CreateBoardSubItemsAsync(parentItemId, subItems);
```

**Using a MondayRow**

```csharp
TaskSubRow[] subRows =
[
    new() { Name = "Task 1", Status = new ColumnStatus() { Status = "In Progress" } },
    new() { Name = "Task 2", Status = new ColumnStatus() { Status = "Done" } },
];

MondayResponse<TaskSubRow> response =
    await mondayClient.CreateBoardSubItemsAsync<TaskSubRow>(parentItemId, subRows);
```

---

## Item Updates (Comments)

```csharp
Update[] updates =
[
    new() { ItemId = itemId, TextBody = "First comment" },
    new() { ItemId = itemId, TextBody = "Second comment" },
];

MondayResponse<Update> response = await mondayClient.CreateItemsUpdateAsync(updates);
```

---

## File Uploads

**Upload to a file column**

```csharp
item.FileUpload = new FileUpload()
{
    ColumnId      = "file_col123",
    FileName      = "report.pdf",
    StreamContent = new StreamContent(File.OpenRead("report.pdf")),
};

MondayResponse<Asset> response = await mondayClient.UploadFileToColumnAsync([item]);
```

**Upload to an item update**

```csharp
Update update = new()
{
    ItemId     = updateId,
    FileUpload = new FileUpload()
    {
        FileName      = "attachment.txt",
        StreamContent = new StreamContent(File.OpenRead("attachment.txt")),
    }
};

MondayResponse<Asset> response = await mondayClient.UploadFileToUpdateAsync([update]);
```

---

## Boards

```csharp
// Fetch specific boards
MondayResponse<Board> response = await mondayClient.GetBoardsAsync([boardId1, boardId2]);

// Fetch up to 10 boards (default)
MondayResponse<Board> allBoards = await mondayClient.GetBoardsAsync();
```

---

## Users

Define a record inheriting `MondayUser` (`Id` and `Name` are provided automatically).

```csharp
public record AppUser : MondayUser { }

// Fetch all users
MondayResponse<AppUser> allUsers = await mondayClient.GetUsersAsync<AppUser>();

// Fetch specific users by ID
MondayResponse<AppUser> specificUsers = await mondayClient.GetUsersAsync<AppUser>([userId1, userId2]);
```

---

## Supported Column Types

| Type | Class |
|---|---|
| Text | `ColumnText` |
| Number | `ColumnNumber` |
| Status | `ColumnStatus` |
| Date | `ColumnDateTime` |
| Checkbox | `ColumnCheckBox` |
| Long Text | `ColumnLongText` |
| Dropdown | `ColumnDropDown` |
| Link | `ColumnLink` |
| Tags | `ColumnTag` |
| Timeline | `ColumnTimeline` |
| Email | `ColumnEmail` |
| Phone | `ColumnPhone` |
| Rating | `ColumnRating` |
| Color Picker | `ColumnColorPicker` |
| People & Teams | `ColumnPeopleAndTeams` |
| File | `ColumnFile` |
| LastUpdated | `LastUpdated`

---

## Contributing

Contributions are welcome! Please read the [Contributing Guidelines](./CONTRIBUTING.md) before opening a pull request.

## License

MondaySharp.NET is licensed under the MIT License — see the [LICENSE](./LICENSE) file for details.
