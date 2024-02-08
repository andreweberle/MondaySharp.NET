using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Infrastructure.Extensions;
using MondaySharp.NET.Infrastructure.Persistence;
using MondaySharp.NET.Infrastructure.Utilities;
using System.Text.Json;

namespace MondaySharp.Functional.Tests;

[TestClass]
public class UnitTest1
{
    IMondayClient? MondayClient { get; set; }
    IConfiguration? Configuration { get; set; } = null!;
    IServiceProvider? ServiceProvider { get; set; } = null!;
    IServiceCollection? Services { get; set; } = null!;

    ulong BoardId { get; set; }

    [TestInitialize]
    public void Init()
    {
        // Load appsettings.json
        this.Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        this.BoardId = ulong.Parse(Configuration["boardId"]!);

        // Create service collection
        this.Services = new ServiceCollection();
        this.Services.AddLogging();
        this.Services.TryAddMondayClient(options =>
        {
            options.EndPoint = new System.Uri(Configuration["mondayUrl"]!);
            options.Token = Configuration["mondayToken"]!;
        });

        // Build service provider
        this.ServiceProvider = this.Services.BuildServiceProvider();
        this.MondayClient = this.ServiceProvider.GetRequiredService<IMondayClient>();
    }

    [TestMethod]
    public void GetItemsByColumnValues_Should_Be_Ok()
    {
        // Arrange
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

        // Act
        List<NET.Application.MondayResponse<TestRow?>> items = 
            this.MondayClient!.GetBoardItemsAsEnumerableAsync<TestRow>(this.BoardId, columnValues).ToBlockingEnumerable().ToList();

        // Assert
        Assert.IsTrue(items.Count > 0);
    }

    [TestMethod]
    public void GetItems_Should_Be_Ok()
    {
        // Arrange
        // Act
        var items = this.MondayClient!.GetBoardItemsAsEnumerableAsync<TestRow>(this.BoardId).ToBlockingEnumerable().ToList();

        // Assert
        Assert.IsTrue(items.Count > 0);
    }

    [TestMethod]
    public void GetItemsByColumnValuesWithGroup_Should_Be_Ok()
    {
        // Arrange
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

        // Act
        List<NET.Application.MondayResponse<TestRowWithGroup?>> items = this.MondayClient!.GetBoardItemsAsEnumerableAsync<TestRowWithGroup>(this.BoardId, columnValues).ToBlockingEnumerable().ToList();

        // Assert
        Assert.IsTrue(items.Count > 0);
        Assert.IsTrue(items.FirstOrDefault()?.Data?.Group != null);
    }

    [TestMethod]
    public void GetItemsByColumnValuesWithAssets_Should_Be_Ok()
    {
        // Arrange
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

        // Act
        List<NET.Application.MondayResponse<TestRowWithAssets?>> mondayResponses = 
            this.MondayClient!.GetBoardItemsAsEnumerableAsync<TestRowWithAssets>(this.BoardId, columnValues).ToBlockingEnumerable().ToList();

        // Assert

        Assert.IsTrue(mondayResponses.Count > 0);
        Assert.IsTrue(mondayResponses.FirstOrDefault()?.Data?.Assets?.Count > 0);
    }

    [TestMethod]
    public void GetItemsByColumnValuesWithUpdates_Should_Be_Ok()
    {
        // Arrange
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

        // Act
        List<NET.Application.MondayResponse<TestRowWithUpdates?>> items = this.MondayClient!.GetBoardItemsAsEnumerableAsync<TestRowWithUpdates>(this.BoardId, columnValues)
            .ToBlockingEnumerable().ToList();

        // Assert
        Assert.IsTrue(items.Count > 0);
        Assert.IsTrue(items.FirstOrDefault()?.Data.Updates?.Count > 0);
    }

    [TestMethod]
    public async Task GetItemById_Should_Be_Ok()
    {
        // Act
        NET.Application.MondayResponse<TestRow> item = await this.MondayClient!.GetBoardItemAsync<TestRow>(3641816676);

        // Assert
        Assert.IsTrue(item.Data != null);
    }

    [TestMethod]
    public void ConvertColumnValuesToJson_Should_Be_Ok()
    {
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

        // Act
        string json = MondayUtilties.ToColumnValuesJson(columnValues);

        // Assert
        Assert.IsTrue(!string.IsNullOrWhiteSpace(json));
        JsonDocument jsonDocument = JsonDocument.Parse(json);

        Assert.IsTrue(jsonDocument.RootElement.EnumerateObject().Count() == 11);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("date").GetProperty("date").GetString() == "2023-11-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("text0").GetString() == "Andrew Eberle");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("numbers").GetString() == "10");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("long_text7").GetProperty("text").GetString() == "hello,world!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("status_19").GetProperty("label").GetString() == "Test");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("label").GetProperty("label").GetString() == "Test");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("long_text").GetProperty("text").GetString() == "long text with return ");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("dropdown").GetProperty("labels").EnumerateArray().Count() == 2);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("link").GetProperty("url").GetString() == "https://www.google.com");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("link").GetProperty("text").GetString() == "google!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("tags").GetProperty("tag_ids").EnumerateArray().Count() == 2);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("timeline").GetProperty("from").GetString() == "2023-11-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("timeline").GetProperty("to").GetString() == "2023-12-29");
    }

    [TestMethod]
    public async Task CreateMultipleItemsMutation_Should_Be_Ok()
    {
        // Arrange
        Item[] items =[ 
            new Item()
            {
                Name = "Test Item 1",
                Group = new Group() { Id = "new_group53864" },
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
                Group = new Group() { Id = "new_group22583" },
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

        // Act
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponse = 
            await this.MondayClient!.CreateBoardItemsAsync(BoardId, items);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Data?.Count == 2);
        Assert.IsTrue(mondayResponse.Data?.FirstOrDefault().Value.Name == items.FirstOrDefault()?.Name);
        Assert.IsTrue(mondayResponse.Data?.LastOrDefault().Value.Name == items.LastOrDefault()?.Name);
    }

    [TestMethod]
    public async Task CreateItemUpdate_Should_Be_Ok()
    {
        // Arrange
        Update[] updates = [
            new Update()
            {
                Id = 5718383580,
                TextBody = "Test Update 1"
            },
            new Update()
            {
                Id = 5718383580,
                TextBody = "Test Update 2"
            }
        ];

        // Act
        NET.Application.MondayResponse<Dictionary<string, Update>?>? mondayResponse = await this.MondayClient!.CreateItemsUpdateAsync(updates);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Data?.Count == 2);
        Assert.IsTrue(mondayResponse.Data?.FirstOrDefault().Value.TextBody == updates.FirstOrDefault()?.TextBody);
        Assert.IsTrue(mondayResponse.Data?.LastOrDefault().Value.TextBody == updates.LastOrDefault()?.TextBody);
    }

    [TestMethod]
    public async Task DeleteItem_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item 1",
            Group = new Group() { Id = "new_group53864" },
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
        };

        // Act
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponse = 
            await this.MondayClient!.CreateBoardItemsAsync(this.BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Data?.Count == 1);

        // Assign the id to the item
        item.Id = mondayResponse.Data.FirstOrDefault().Value.Id;

        // Act
        mondayResponse = await this.MondayClient!.DeleteItemsAsync([item]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Data?.Count == 1);
        Assert.IsTrue(mondayResponse.Data?.FirstOrDefault().Value.Id == item.Id);
    }

    [TestMethod]
    public async Task DeleteItems_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item 1",
            Group = new Group() { Id = "new_group53864" },
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
        };
        Item item2 = new()
        {
            Name = "Test Item 2",
            Group = new Group() { Id = "new_group22583" },
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
        };

        // Create the items
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponseCreate = 
            await this.MondayClient!.CreateBoardItemsAsync(BoardId, [item, item2]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Data?.Count == 2);
        Assert.IsTrue(mondayResponseCreate.Data?.FirstOrDefault().Value.Name == item.Name);
        Assert.IsTrue(mondayResponseCreate.Data?.LastOrDefault().Value.Name == item2.Name);

        // Assign the ids to the items
        item.Id = mondayResponseCreate.Data.FirstOrDefault().Value.Id;
        item2.Id = mondayResponseCreate.Data.LastOrDefault().Value.Id;

        // Act
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponse =
            await this.MondayClient!.DeleteItemsAsync([item, item2]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Data?.Count == 2);
        Assert.IsTrue(mondayResponse.Data?.FirstOrDefault().Value.Id == item.Id);
        Assert.IsTrue(mondayResponse.Data?.LastOrDefault().Value.Id == item2.Id);
    }

    [TestMethod]
    public async Task GetBoardById_Should_Be_Ok()
    {
        // Arrange
        // Act
        NET.Application.MondayResponse<List<Board>> mondayResponse = 
            await this.MondayClient!.GetBoardsAsync([this.BoardId]);

        // Assert
        Assert.IsTrue(mondayResponse.Data?.Count == 1);
        Assert.IsTrue(mondayResponse.Data.FirstOrDefault()?.Id == this.BoardId);
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Errors is null);
    }

    [TestMethod]
    public async Task GetBoards_Should_Be_Ok()
    {
        // Arrange
        // Act
        NET.Application.MondayResponse<List<Board>> boards = await this.MondayClient!.GetBoardsAsync();

        // Assert
        Assert.IsTrue(boards.Data?.Count <= 10);
    }

    [TestMethod]
    public async Task UploadFileToItemColumn_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item 1",
            Group = new Group() { Id = "new_group53864" },
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
        };
        Item item1 = new()
        {
            Name = "Test Item 2",
            Group = new Group() { Id = "new_group53864" },
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
        };

        // Create the item
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponseCreate = 
            await this.MondayClient!.CreateBoardItemsAsync(BoardId, [item, item1]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Data?.Count == 2);
        Assert.IsTrue(mondayResponseCreate.Data?.FirstOrDefault().Value.Name == item.Name);
        Assert.IsTrue(mondayResponseCreate.Data?.LastOrDefault().Value.Name == item1.Name);

        item.Id = mondayResponseCreate.Data.FirstOrDefault().Value.Id;
        item1.Id = mondayResponseCreate.Data.LastOrDefault().Value.Id;

        // Arrange
        FileUpload fileUpload = new()
        {
            FileName = "test.txt",
            StreamContent = new StreamContent(File.OpenRead("test.txt")),
            ColumnId = "files4"
        };
        FileUpload fileUpload1 = new()
        {
            FileName = "test.txt",
            StreamContent = new StreamContent(File.OpenRead("test.txt")),
            ColumnId = "files4"
        };

        item.FileUpload = fileUpload;
        item1.FileUpload = fileUpload1;

        // Act
        NET.Application.MondayResponse<Dictionary<string, Asset>> uploadFilesMondayResponse =
            await this.MondayClient!.UploadFileToColumnAsync([item, item1]);

        // Assert
        Assert.IsTrue(uploadFilesMondayResponse.Data?.Count == 2);
        Assert.IsTrue(uploadFilesMondayResponse.IsSuccessful);
        Assert.IsTrue(uploadFilesMondayResponse.Errors is null);

        // Delete the item
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponseDelete = 
            await this.MondayClient!.DeleteItemsAsync([item, item1]);

        // Assert
        Assert.IsTrue(mondayResponseDelete.IsSuccessful);
        Assert.IsNull(mondayResponseDelete.Errors);
        Assert.IsTrue(mondayResponseDelete.Data?.Count == 2);
        Assert.IsTrue(mondayResponseDelete.Data?.FirstOrDefault().Value.Id == item.Id);
        Assert.IsTrue(mondayResponseDelete.Data?.LastOrDefault().Value.Id == item1.Id);
    }

    [TestMethod]
    public void Deserialize_Update_Response_Should_Be_Ok()
    {
        const string DATA = "{\"id\":\"1187128743\",\"text_body\":\"Text\"}";
        using JsonDocument jsonDocument = JsonDocument.Parse(DATA);

        Update? update = Newtonsoft.Json.JsonConvert.DeserializeObject<Update>(DATA);
        Assert.IsNotNull(update);
        Assert.IsTrue(update?.Id == 1187128743);
    }

    [TestMethod]
    public async Task UploadFileToUpdate_Should_Be_Ok()
    {
        // Create Item
        Item item = new()
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
        };

        // Create the item
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponseCreate = 
            await this.MondayClient!.CreateBoardItemsAsync(BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Data?.Count == 1);
        Assert.IsTrue(mondayResponseCreate.Data?.FirstOrDefault().Value.Name == item.Name);

        item.Id = mondayResponseCreate.Data.FirstOrDefault().Value.Id;

        // Create Update For The Item
        Update update = new()
        {
            Id = mondayResponseCreate.Data.FirstOrDefault().Value.Id,
            TextBody = "Test Update 1"
        };

        // Act
        NET.Application.MondayResponse<Dictionary<string, Update>?>? mondayResponse = 
            await this.MondayClient!.CreateItemsUpdateAsync([update]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Data?.Count == 1);
        Assert.IsTrue(mondayResponse.Data?.FirstOrDefault().Value.TextBody == update.TextBody);

        // Arrange
        Update update0 = new()
        {
            Id = mondayResponse.Data.FirstOrDefault().Value.Id,
            FileUpload = new FileUpload() { FileName = "test.txt", StreamContent = new StreamContent(File.OpenRead("test.txt")) }
        };
        Update update1 = new()
        {
            Id = mondayResponse.Data.FirstOrDefault().Value.Id,
            FileUpload = new FileUpload() { FileName = "test.txt", StreamContent = new StreamContent(File.OpenRead("test.txt")) }
        };

        // Act
        NET.Application.MondayResponse<Dictionary<string, Asset>> uploadFilesMondayResponse =
            await this.MondayClient!.UploadFileToUpdateAsync([update0, update1]);

        // Assert
        Assert.IsTrue(uploadFilesMondayResponse.Data?.Count == 2);
        Assert.IsTrue(uploadFilesMondayResponse.IsSuccessful);
        Assert.IsTrue(uploadFilesMondayResponse.Errors is null);

        // Delete the item
        NET.Application.MondayResponse<Dictionary<string, Item>?>? mondayResponseDelete = 
            await this.MondayClient!.DeleteItemsAsync([item]);

        // Assert
        Assert.IsTrue(mondayResponseDelete.IsSuccessful);
        Assert.IsNull(mondayResponseDelete.Errors);
        Assert.IsTrue(mondayResponseDelete.Data?.Count == 1);
        Assert.IsTrue(mondayResponseDelete.Data?.FirstOrDefault().Value.Id == item.Id);
    }

    public record TestRowWithGroup : TestRow
    {
        public Group? Group { get; set; }
    }

    public record TestRowWithAssets : TestRow
    {
        public List<Asset>? Assets { get; set; }
    }

    public record TestRowWithUpdates : TestRow
    {
        public List<Update>? Updates { get; set; }
    }

    public record TestRow : MondayRow
    {
        [MondayColumnHeader("text0")]
        public ColumnText? Text { get; set; }

        [MondayColumnHeader("numbers9")]
        public ColumnNumber? Number { get; set; }

        [MondayColumnHeader("checkbox")]
        public ColumnCheckBox? Checkbox { get; set; }

        [MondayColumnHeader("priority")]
        public ColumnStatus? Priority { get; set; }

        [MondayColumnHeader("status")]
        public ColumnStatus? Status { get; set; }

        [MondayColumnHeader("link2")]
        public ColumnLink? Link { get; set; }

        [MondayColumnHeader("dropdown")]
        public ColumnDropDown? Dropdown { get; set; }

        [MondayColumnHeader("date")]
        public ColumnDateTime? Date { get; set; }

        [MondayColumnHeader("long_text")]
        public ColumnLongText? LongText { get; set; }

        [MondayColumnHeader("color_picker")]
        public ColumnColorPicker? ColorPicker { get; set; }

        [MondayColumnHeader("timeline")]
        public ColumnTimeline? Timeline { get; set; }

        [MondayColumnHeader("tags")]
        public ColumnTag? Tags { get; set; }
    }
}