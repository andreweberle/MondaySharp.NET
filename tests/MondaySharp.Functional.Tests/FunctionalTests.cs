using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Domain.Common.Enums;
using MondaySharp.NET.Infrastructure.Extensions;
using MondaySharp.NET.Infrastructure.Persistence;
using MondaySharp.NET.Infrastructure.Utilities;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using MondaySharp.NET.Application;
using static MondaySharp.Functional.Tests.FunctionalTests;

namespace MondaySharp.Functional.Tests;

[TestClass]
public class FunctionalTests
{
    private IMondayClient? MondayClient { get; set; }
    private IConfiguration? Configuration { get; set; } = null!;
    private IServiceProvider? ServiceProvider { get; set; } = null!;
    private IServiceCollection? Services { get; set; } = null!;

    private ulong BoardId { get; set; }

    [TestInitialize]
    public void Init()
    {
        // Load appsettings.json
        Configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        // Get the board id
        BoardId = ulong.Parse(Configuration["boardId"]!);

        // Create service collection
        Services = new ServiceCollection();
        Services.AddLogging();
        Services.TryAddMondayClient(options =>
        {
            options.EndPoint = new Uri(Configuration["mondayUrl"]!);
            options.Token = Configuration["mondayToken"]!;
        });

        // Build service provider
        ServiceProvider = Services.BuildServiceProvider();
        MondayClient = ServiceProvider.GetRequiredService<IMondayClient>();
    }

    [TestMethod]
    public async Task GetItemsByColumnValues_Should_Be_OkAsync()
    {
        // Arrange
        await this.TestBoard_CreateItem_Should_Be_Ok();
        
        // Arrange
        ColumnValue[] columnValues =
        [
            new()
            {
                Id = "text_mkmn7km4",
                Text = "FROM UNIT TEST"
            }
        ];

        // Act
        NET.Application.MondayResponse<TestRow> items =
            await MondayClient!.GetBoardItemsAsync<TestRow>(BoardId, columnValues);

        // Assert
        Assert.IsTrue(items.Response?.Count > 0);
    }

    [TestMethod]
    public async Task GetItems_Should_Be_OkAsync()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item Create 2",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text_mkmn7km4",
                        Text = "FROM UNIT TEST"
                    }
                },
            ]
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync(this.BoardId,
                [item]); //hard-coded BoardID to properly match the fields of a test-Board

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == item.Name);
        Assert.IsNull(mondayResponse.Errors);

        // Act
        NET.Application.MondayResponse<TestRow> items = await MondayClient!.GetBoardItemsAsync<TestRow>(BoardId);

        // Assert
        Assert.IsTrue(items.Response?.Count > 0);
    }

    [TestMethod]
    public async Task GetItemsByColumnValuesWithGroup_Should_Be_OkAsync()
    {
        await this.TestBoard_CreateItem_Should_Be_Ok();
        
        // Arrange
        ColumnValue[] columnValues =
        [
            new()
            {
                Id = "text_mkmn7km4",
                Text = "FROM UNIT TEST"
            }
        ];

        // Act
        NET.Application.MondayResponse<TestRowWithGroup> items =
            await MondayClient!.GetBoardItemsAsync<TestRowWithGroup>(BoardId, columnValues);

        // Assert
        Assert.IsTrue(items.Response?.Count > 0);
    }

    [TestMethod]
    public async Task GetItemsByColumnValuesWithAssets_Should_Be_OkAsync()
    {
        // Arrange
        ColumnValue[] columnValues =
        [
            new()
            {
                Id = "text_mkmn7km4",
                Text = "FROM UNIT TEST"
            }
        ];

        // Act
        await MondayClient!.CreateBoardItemsAsync<TestRowWithAssets>(BoardId, [
            new TestRowWithAssets()
            {
                Name = "Test Item 1",
                Text = new ColumnText()
                {
                    Id = "text_mkmn7km4",
                    Text = "FROM UNIT TEST"
                }
            }
        ]);

        NET.Application.MondayResponse<TestRowWithAssets> mondayResponses =
            await MondayClient!.GetBoardItemsAsync<TestRowWithAssets>(BoardId, columnValues);

        // Assert
        Assert.IsTrue(mondayResponses.Response?.Count > 0);
    }

    [TestMethod]
    public async Task GetItemsByColumnValuesWithUpdates_Should_Be_OkAsync()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item Create 2",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text_mkmn7km4",
                        Text = "FROM UNIT TEST"
                    }
                }
            ]
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync(BoardId,
                [item]); //hard-coded BoardID to properly match the fields of a test-Board
        
        // Arrange
        ColumnValue[] columnValues =
        [
            new()
            {
                Id = "text_mkmn7km4",
                Text = "FROM UNIT TEST"
            },
        ];

        // Act
        NET.Application.MondayResponse<TestRowWithUpdates> items =
            await MondayClient!.GetBoardItemsAsync<TestRowWithUpdates>(BoardId, columnValues);

        // Assert
        Assert.IsTrue(items.Response?.Count > 0);
    }

    [TestMethod]
    public async Task GetItemById_Should_Be_Ok()
    {
        // Act
        // Create New Item.
        Item newItem = new()
        {
            Name = "Test Item 1",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text_mkmn7km4",
                        Text = "Andrew Eberle"
                    }
                },
            ]
        };

        // Create the item
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(BoardId, [newItem]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 1);
        Assert.IsTrue(mondayResponseCreate.Response?.FirstOrDefault()?.Data?.Name == newItem.Name);

        // Assign the id to the item
        ulong boardItemId = mondayResponseCreate.Response.FirstOrDefault()?.Data?.Id ?? 0;

        NET.Application.MondayResponse<TestRow> item = await MondayClient!.GetBoardItemAsync<TestRow>(boardItemId);

        // Assert
        Assert.IsTrue(item.Response != null);
        Assert.IsTrue(item.IsSuccessful);
    }

    [TestMethod]
    public async Task GetItemsByCursor_Should_Be_Ok()
    {
        // Arrange

        // Act
        NET.Application.MondayResponse<TestRow> items = await MondayClient!.GetBoardItemsAsync<TestRow>(BoardId, 25);

        // Assert
        Assert.IsTrue(items.Response?.Count > 0);
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
            new ColumnEmail("email", "andreweberle@email.com.au", "hello world!"),
            new ColumnRating("rating", null),
            new ColumnPhone("contact_phone", "1234567890")
        ];

        // Act
        string json = MondayUtilities.ToColumnValuesJson(columnValues);

        // Assert
        Assert.IsTrue(!string.IsNullOrWhiteSpace(json));
        JsonDocument jsonDocument = JsonDocument.Parse(json);

        Assert.IsTrue(jsonDocument.RootElement.EnumerateObject().Count() == columnValues.Count);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("date").GetProperty("date").GetString() == "2023-11-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("text0").GetString() == "Andrew Eberle");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("numbers").GetString() == "10");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("long_text7").GetProperty("text").GetString() ==
                      "hello,world!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("status_19").GetProperty("label").GetString() == "Test");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("label").GetProperty("label").GetString() == "Test");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("long_text").GetProperty("text").GetString() ==
                      "long text with return ");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("dropdown").GetProperty("labels").EnumerateArray().Count() ==
                      2);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("link").GetProperty("url").GetString() ==
                      "https://www.google.com");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("link").GetProperty("text").GetString() == "google!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("tags").GetProperty("tag_ids").EnumerateArray().Count() ==
                      2);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("timeline").GetProperty("from").GetString() == "2023-11-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("timeline").GetProperty("to").GetString() == "2023-12-29");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("email").GetProperty("email").GetString() ==
                      "andreweberle@email.com.au");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("email").GetProperty("text").GetString() == "hello world!");
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("rating").GetProperty("rating").GetInt32() == 0);
        Assert.IsTrue(jsonDocument.RootElement.GetProperty("contact_phone").GetProperty("phone").GetString() == "1234567890");
    }

    [TestMethod]
    public async Task CreateMultipleItemsMutation_Should_Be_Ok()
    {
        // Arrange
        Item[] items =
        [
            new()
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
                        }
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnNumber()
                        {
                            Id = "numbers9",
                            Number = 10
                        }
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnRating()
                        {
                            Id = "rating",
                            Rating = MondayRating.Two
                        }
                    }
                ]
            },
            new()
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
                        }
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnNumber()
                        {
                            Id = "numbers9",
                            Number = 11
                        }
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnEmail()
                        {
                            Id = "email",
                            Email = "andreweberle@email.com.au",
                            Message = "Andrew Eberle"
                        }
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnRating()
                        {
                            Id = "rating",
                            Rating = MondayRating.Five
                        }
                    },
                    new ColumnValue()
                    {
                        ColumnBaseType = new ColumnPhone
                        {
                            Id = "contact_phone",
                            Phone = "1234567890"
                        }
                    }
                ]
            }
        ];

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync(BoardId, items);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 2);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == items.FirstOrDefault()?.Name);
        Assert.IsTrue(mondayResponse.Response?.LastOrDefault()?.Data?.Name == items.LastOrDefault()?.Name);
    }

    [TestMethod]
    public async Task CreateItemUpdate_Should_Be_Ok()
    {
        // Arrange
        Update[] updates =
        [
            new()
            {
                ItemId = 5718383580,
                TextBody = "Test Update 1"
            },
            new()
            {
                ItemId = 5718383580,
                TextBody = "Test Update 2"
            }
        ];

        // Act
        NET.Application.MondayResponse<Update> mondayResponse = await MondayClient!.CreateItemsUpdateAsync(updates);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id > 0));
        Assert.IsTrue(mondayResponse.Response?.Count == 2);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.TextBody == updates.FirstOrDefault()?.TextBody);
        Assert.IsTrue(mondayResponse.Response?.LastOrDefault()?.Data?.TextBody == updates.LastOrDefault()?.TextBody);
    }

    [TestMethod]
    public async Task DeleteItem_Should_Be_Ok()
    {
        await this.TestBoard_CreateItem_Should_Be_Ok();
        
        // Arrange
        Item item = new()
        {
            Name = "Test Item 1",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text_mkmn7km4",
                        Text = "FROM UNIT TEST"
                    }
                },
            ]
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync(BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(item.Id > 0);

        // Act
        mondayResponse = await MondayClient!.DeleteItemsAsync([item]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Id == item.Id);
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
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers9",
                        Number = 10
                    }
                }
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
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers9",
                        Number = 11
                    }
                }
            ]
        };

        // Create the items
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(BoardId, [item, item2]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 2);

        Assert.IsTrue(mondayResponseCreate.Response.FirstOrDefault()?.Data?.Name == item.Name);
        Assert.IsTrue(mondayResponseCreate.Response?.LastOrDefault()?.Data?.Name == item2.Name);

        Assert.IsTrue(item.Id > 0);
        Assert.IsTrue(item2.Id > 0);

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.DeleteItemsAsync([item, item2]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 2);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Id == item.Id);
        Assert.IsTrue(mondayResponse.Response?.LastOrDefault()?.Data?.Id == item2.Id);
    }

    [TestMethod]
    public async Task GetBoardById_Should_Be_Ok()
    {
        // Arrange
        // Act
        NET.Application.MondayResponse<Board> mondayResponse =
            await MondayClient!.GetBoardsAsync([BoardId]);

        // Assert
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(mondayResponse.Response.FirstOrDefault()?.Data?.Id == BoardId);
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Errors is null);
    }

    [TestMethod]
    public async Task GetBoards_Should_Be_Ok()
    {
        // Arrange
        // Act
        NET.Application.MondayResponse<Board> boards = await MondayClient!.GetBoardsAsync();

        // Assert
        Assert.IsTrue(boards.Response?.Count <= 10);
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
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers9",
                        Number = 10
                    }
                }
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
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers9",
                        Number = 10
                    }
                }
            ]
        };

        // Create the item
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(BoardId, [item, item1]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 2);

        Assert.IsTrue(mondayResponseCreate.Response.FirstOrDefault()?.Data?.Name == item.Name);
        Assert.IsTrue(mondayResponseCreate.Response.LastOrDefault()?.Data?.Name == item1.Name);

        Assert.IsTrue(item.Id > 0);
        Assert.IsTrue(item1.Id > 0);

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
        NET.Application.MondayResponse<Asset> uploadFilesMondayResponse =
            await MondayClient!.UploadFileToColumnAsync([item, item1]);

        // Assert
        Assert.IsTrue(uploadFilesMondayResponse.Response?.Count == 2);
        Assert.IsTrue(uploadFilesMondayResponse.IsSuccessful);
        Assert.IsTrue(uploadFilesMondayResponse.Errors is null);

        // Delete the item
        NET.Application.MondayResponse<Item> mondayResponseDelete =
            await MondayClient!.DeleteItemsAsync([item, item1]);

        // Assert
        Assert.IsTrue(mondayResponseDelete.IsSuccessful);
        Assert.IsNull(mondayResponseDelete.Errors);
        Assert.IsTrue(mondayResponseDelete.Response?.Count == 2);
        Assert.IsTrue(mondayResponseDelete.Response.FirstOrDefault()?.Data?.Id == item.Id);
        Assert.IsTrue(mondayResponseDelete.Response.LastOrDefault()?.Data?.Id == item1.Id);
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
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers9",
                        Number = 10
                    }
                }
            ]
        };

        // Create the item
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 1);
        Assert.IsTrue(mondayResponseCreate.Response?.FirstOrDefault()?.Data?.Name == item.Name);

        // Create Update For The Item
        Update update = new()
        {
            ItemId = mondayResponseCreate.Response.FirstOrDefault()?.Data?.Id,
            TextBody = "Test Update 1"
        };

        // Act
        NET.Application.MondayResponse<Update> mondayResponse =
            await MondayClient!.CreateItemsUpdateAsync([update]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.TextBody == update.TextBody);

        // Arrange
        Update update0 = new()
        {
            ItemId = mondayResponse.Response.FirstOrDefault()?.Data?.Id,
            FileUpload = new FileUpload()
                { FileName = "test.txt", StreamContent = new StreamContent(File.OpenRead("test.txt")) }
        };
        Update update1 = new()
        {
            ItemId = mondayResponse.Response.FirstOrDefault()?.Data?.Id,
            FileUpload = new FileUpload()
                { FileName = "test.txt", StreamContent = new StreamContent(File.OpenRead("test.txt")) }
        };

        // Act
        NET.Application.MondayResponse<Asset> uploadFilesMondayResponse =
            await MondayClient!.UploadFileToUpdateAsync([update0, update1]);

        // Assert
        Assert.IsTrue(uploadFilesMondayResponse.Response?.Count == 2);
        Assert.IsTrue(uploadFilesMondayResponse.IsSuccessful);
        Assert.IsTrue(uploadFilesMondayResponse.Errors is null);

        // Delete the item
        NET.Application.MondayResponse<Item> mondayResponseDelete =
            await MondayClient!.DeleteItemsAsync([item]);

        // Assert
        Assert.IsTrue(mondayResponseDelete.IsSuccessful);
        Assert.IsNull(mondayResponseDelete.Errors);
        Assert.IsTrue(mondayResponseDelete.Response?.Count == 1);
        Assert.IsTrue(mondayResponseDelete.Response?.FirstOrDefault()?.Data?.Id == item.Id);
    }

    [TestMethod]
    public async Task CreateItemFromMondayRow_Should_Be_Ok()
    {
        // Arrange
        TestRow testRow = new()
        {
            Name = "Test Item 1",
            Text = new ColumnText()
            {
                Text = "Andrew Eberle"
            },
            Number = new ColumnNumber()
            {
                Number = 10
            },
            Email = new ColumnEmail()
            {
                Email = "andrew.eberle@lithocraft.com.au"
            },
            Rating = new ColumnRating()
            {
                Rating = MondayRating.Five
            },
            Checkbox = new ColumnCheckBox()
            {
                IsChecked = true
            },
            Date = new ColumnDateTime()
            {
                Date = new DateTime(2023, 11, 29)
            },
            Dropdown = new ColumnDropDown()
            {
                Label = "Hello"
            },
            LongText = new ColumnLongText()
            {
                Text = "Hello, World!"
            },
            Link = new ColumnLink()
            {
                Text = "Google",
                Uri = new Uri("https://www.google.com")
            },
            Priority = new ColumnStatus()
            {
                Status = "High"
            },
            Status = new ColumnStatus()
            {
                Status = "Done"
            },
            Timeline = new ColumnTimeline()
            {
                From = new DateTime(2023, 11, 29),
                To = new DateTime(2023, 12, 29)
            },
            Tags = new ColumnTag()
            {
                TagIds = [21057674, 21057675]
            },
            Phone = new ColumnPhone()
            {
                Phone = "1234567890"
            }
        };

        // Act
        NET.Application.MondayResponse<TestRow> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<TestRow>(BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);
    }

    [TestMethod]
    public async Task UpdateItemFromMondayRow_Should_Be_Ok()
    {
        // Arrange
        TestRow testRow = new()
        {
            Name = "Test Item 1",
            Text = new ColumnText()
            {
                Text = "Andrew Eberle"
            },
            Number = new ColumnNumber()
            {
                Number = 10
            },
            Email = new ColumnEmail()
            {
                Email = "andrew.eberle@lithocraft.com.au"
            },
            Rating = new ColumnRating()
            {
                Rating = MondayRating.Five
            },
            Checkbox = new ColumnCheckBox()
            {
                IsChecked = true
            },
            Date = new ColumnDateTime()
            {
                Date = new DateTime(2023, 11, 29)
            },
            Dropdown = new ColumnDropDown()
            {
                Label = "Hello"
            },
            LongText = new ColumnLongText()
            {
                Text = "Hello, World!"
            },
            Link = new ColumnLink()
            {
                Text = "Google",
                Uri = new Uri("https://www.google.com")
            },
            Priority = new ColumnStatus()
            {
                Status = "High"
            },
            Status = new ColumnStatus()
            {
                Status = "Done"
            },
            Timeline = new ColumnTimeline()
            {
                From = new DateTime(2023, 11, 29),
                To = new DateTime(2023, 12, 29)
            },
            Tags = new ColumnTag()
            {
                TagIds = [21057674, 21057675]
            },
            Phone = new ColumnPhone()
            {
                Phone = "1234567890"
            }
        };

        // Act
        NET.Application.MondayResponse<TestRow> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<TestRow>(BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);

        // Change The Text
        testRow.Text.Text = null;
        testRow.Status = null;
        testRow.Priority = null;
        testRow.Checkbox.IsChecked = false;
        testRow.Number.Number = null;
        testRow.Email.Email = null;
        testRow.Link = null;
        testRow.Dropdown = null;
        testRow.Date = null;
        testRow.LongText = null;
        testRow.Timeline = null;
        testRow.Tags = null;
        testRow.Rating = null;
        testRow.Name = "Updated Item";
        testRow.Phone = null;

        // Attempt To Update The Item.
        mondayResponse = await MondayClient!.UpdateBoardItemsAsync<TestRow>(BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);
    }

    [TestMethod]
    public async Task CreateSubItemsGeneric_Should_Be_Ok()
    {
        // Arrange
        TestRow testRow = new()
        {
            Name = "Test Item 1",
            Text = new ColumnText()
            {
                Text = "Andrew Eberle"
            },
            Number = new ColumnNumber()
            {
                Number = 10
            },
            Email = new ColumnEmail()
            {
                Email = "andrew.eberle@lithocraft.com.au"
            },
            Rating = new ColumnRating()
            {
                Rating = MondayRating.Five
            },
            Checkbox = new ColumnCheckBox()
            {
                IsChecked = true
            },
            Date = new ColumnDateTime()
            {
                Date = new DateTime(2023, 11, 29)
            },
            Dropdown = new ColumnDropDown()
            {
                Label = "Hello"
            },
            LongText = new ColumnLongText()
            {
                Text = "Hello, World!"
            },
            Link = new ColumnLink()
            {
                Text = "Google",
                Uri = new Uri("https://www.google.com")
            },
            Priority = new ColumnStatus()
            {
                Status = "High"
            },
            Status = new ColumnStatus()
            {
                Status = "Done"
            },
            Timeline = new ColumnTimeline()
            {
                From = new DateTime(2023, 11, 29),
                To = new DateTime(2023, 12, 29)
            },
            Tags = new ColumnTag()
            {
                TagIds = [21057674, 21057675]
            },
            Phone = new ColumnPhone()
            {
                Phone = "1234567890"
            }
        };

        // Act
        NET.Application.MondayResponse<TestRow> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<TestRow>(BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);

        // Arrange
        TestSubRow testSubRow0 = new()
        {
            Name = "Test Sub Item 1",

            Status = new ColumnText()
            {
                Text = "Andrew Eberle"
            },
            DueDate = new ColumnDateTime()
            {
                Date = new DateTime(2023, 11, 29)
            },
            Priority = new ColumnNumber()
            {
                Number = 10
            }
        };

        // Arrange
        TestSubRow testSubRow1 = new()
        {
            Name = "Test Sub Item 2",

            Status = new ColumnText()
            {
                Text = "Andrew Eberle"
            },
            DueDate = new ColumnDateTime()
            {
                Date = new DateTime(2023, 11, 29)
            },
            Priority = new ColumnNumber()
            {
                Number = 10
            }
        };

        // Act
        NET.Application.MondayResponse<TestSubRow> mondayResponseSubRow =
            await MondayClient!.CreateBoardSubItemsAsync<TestSubRow>(
                mondayResponse.Response?.FirstOrDefault()?.Data?.Id ?? 0, [testSubRow0, testSubRow1]);

        // Assert
        Assert.IsTrue(mondayResponseSubRow.IsSuccessful);
        Assert.IsTrue(mondayResponseSubRow.Response?.Count == 2);
        Assert.IsTrue(mondayResponseSubRow.Response?.FirstOrDefault()?.Data?.Name == testSubRow0.Name);
        Assert.IsTrue(mondayResponseSubRow.Response?.LastOrDefault()?.Data?.Name == testSubRow1.Name);
        Assert.IsTrue(mondayResponseSubRow.Response?.LastOrDefault()?.Data?.Id >
                      mondayResponseSubRow.Response?.FirstOrDefault()?.Data?.Id);
        Assert.IsNull(mondayResponseSubRow.Errors);
    }

    [TestMethod]
    public async Task CreateSubItems_Should_Be_Ok()
    {
        // Arrange
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
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers9",
                        Number = 10
                    }
                }
            ]
        };

        // Create the item
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 1);
        Assert.IsTrue(mondayResponseCreate.Response?.FirstOrDefault()?.Data?.Name == item.Name);

        // Arrange
        Item subItem1 = new()
        {
            Name = "Test Sub Item 1",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "status__1",
                        Text = "Andrew Eberle"
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnDateTime()
                    {
                        Id = "date6",
                        Date = new DateTime(2023, 11, 29)
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers8",
                        Number = 10
                    }
                }
            ]
        };

        // Arrange
        Item subItem2 = new()
        {
            Name = "Test Sub Item 2",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "status__1",
                        Text = "Andrew Eberle"
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnDateTime()
                    {
                        Id = "date6",
                        Date = new DateTime(2023, 11, 29)
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnNumber()
                    {
                        Id = "numbers8",
                        Number = 10
                    }
                }
            ]
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponseSubItem =
            await MondayClient!.CreateBoardSubItemsAsync(mondayResponseCreate.Response?.FirstOrDefault()?.Data?.Id ?? 0,
                [subItem1, subItem2]);

        // Assert
        Assert.IsTrue(mondayResponseSubItem.IsSuccessful);
        Assert.IsTrue(mondayResponseSubItem.Response?.Count == 2);
        Assert.IsTrue(mondayResponseSubItem.Response?.FirstOrDefault()?.Data?.Name == subItem1.Name);
        Assert.IsTrue(mondayResponseSubItem.Response?.LastOrDefault()?.Data?.Name == subItem2.Name);
        Assert.IsTrue(mondayResponseSubItem.Response?.LastOrDefault()?.Data?.Id >
                      mondayResponseSubItem.Response?.FirstOrDefault()?.Data?.Id);
        Assert.IsNull(mondayResponseSubItem.Errors);
    }

    [TestMethod]
    public async Task ZZZCleanup()
    {
        // Get All Items
        NET.Application.MondayResponse<TestRow> items = await MondayClient!.GetBoardItemsAsync<TestRow>(BoardId);

        // Delete All Items
        await MondayClient!.DeleteItemsAsync([
            .. items.Response?.Select(x => new Item()
            {
                Id = x.Data!.Id
            })
        ]);
    }

    // Create Item Using Item Object
    [TestMethod]
    public async Task TestBoard_CreateItem_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item Create 2",
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text_mkmn7km4",
                        Text = "FROM UNIT TEST"
                    }
                }
            ]
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync(BoardId,
                [item]); //hard-coded BoardID to properly match the fields of a test-Board

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == item.Name);
        Assert.IsNull(mondayResponse.Errors);
    }

    // Create Item Using Custom-Row
    [TestMethod]
    public async Task TestBoard_CreateItem_UsingCustomRow_Should_Be_Ok()
    {
        // Arrange
        MondayTestRow testRow = new()
        {
            Name = "Test Item Create",
            Text = new ColumnText()
            {
                Id = "text_mkmn7km4",
                Text = "FROM UNIT TEST"
            }
        };

        // Act
        NET.Application.MondayResponse<MondayTestRow> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<MondayTestRow>(this.BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);
    }

    // Update Item - Test case for issue # 10
    [TestMethod]
    public async Task TestBoard_UpdateItem_Issue10_Should_Be_Ok()
    {
        // Arrange
        MondayTestRow testRow = new()
        {
            Name = "Test Item Update Details For Issue 10",
            Group = new Group()
            {
                Id = "group_title"
            },
            Text = new ColumnText()
            {
                Text = "ITEM CREATED FROM UNIT TEST"
            },
            Date = new ColumnDateTime()
            {
                Date = DateTime.Now
            },
            Dropdown = new ColumnDropDown()
            {
                LabelId = 1
            },
            Status = new ColumnStatus()
            {
                StatusId = 1
            },
            Files = new ColumnFile()
        };

        // Act
        NET.Application.MondayResponse<MondayTestRow> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<MondayTestRow>(this.BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);

        // Change The Text
        testRow.Text.Text = "Updated!";
        testRow.Status.StatusId = 2;
        testRow.Dropdown.LabelId = 2;

        // Attempt To Update The Item.
        mondayResponse = await MondayClient!.UpdateBoardItemsAsync<MondayTestRow>(this.BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);
    }

    // Update Item - Test case for issue # 9
    [TestMethod]
    public async Task TestBoard_UpdateItem_Issue9_Should_Be_Ok()
    {
        // Arrange
        MondayTestRow testRow = new()
        {
            Name = "Test Item Update Details For Issue 9",
            Group = new Group()
            {
                Id = "group_title"
            },
            Text = new ColumnText()
            {
                Text = "ITEM CREATED FROM VS"
            },
            Date = new ColumnDateTime()
            {
                Date = DateTime.Now
            },
            Dropdown = new ColumnDropDown()
            {
                LabelId = 1
            },
            Status = new ColumnStatus()
            {
                StatusId = 1
            },
            Files = new ColumnFile()
        };

        // Act
        NET.Application.MondayResponse<MondayTestRow> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<MondayTestRow>(this.BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);

        // Change The Text
        testRow.Text.Text = "Updated!";
        testRow.Name = null; // <- test issue # 9
        testRow.Status.StatusId = 2;
        testRow.Dropdown.LabelId = 2;

        // Attempt To Update The Item.
        mondayResponse = await MondayClient!.UpdateBoardItemsAsync<MondayTestRow>(this.BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);
    }

    // Update Item and Upload a File - With Debug Console Messages
    [TestMethod]
    public async Task TestBoard_UpdateItem_UpdateFile_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item Create Then Edit Then Upload File 3",
            Group = new Group()
            {
                Id = "group_title"
            },
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text__1",
                        Text = "Created with VS Test Case"
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnStatus()
                    {
                        Id = "status",
                        StatusId = 1
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnDateTime()
                    {
                        Id = "date4",
                        Date = DateTime.Now
                    }
                },

                new ColumnValue()
                {
                    ColumnBaseType = new ColumnDropDown()
                    {
                        Id = "dropdown__1",
                        LabelId = 1
                    }
                }
            ]
        };


        // Create the item
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(this.BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 1);
        Assert.IsTrue(mondayResponseCreate.Response.FirstOrDefault()?.Data?.Name == item.Name);
        Assert.IsTrue(item.Id > 0);

        // Arrange
        FileUpload fileUpload = new()
        {
            FileName = "test.txt",
            StreamContent = new StreamContent(File.OpenRead("test.txt")),
            ColumnId = "files9__1"
        };

        item.FileUpload = fileUpload;

        // Act
        NET.Application.MondayResponse<Asset> uploadFilesMondayResponse =
            await MondayClient!.UploadFileToColumnAsync([item]);

        // Assert
        Assert.IsTrue(uploadFilesMondayResponse.Response?.Count == 1);
        Assert.IsTrue(uploadFilesMondayResponse.IsSuccessful);
        Assert.IsTrue(uploadFilesMondayResponse.Errors is null);
    }

    // Add Update Details with an Uploaded File to an Item
    [TestMethod]
    public async Task TestBoard_CreateThenAddUpdateDetailsWithFile_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item Update Details With File",
            Group = new Group()
            {
                Id = "group_title"
            },
            ColumnValues =
            [
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnText()
                    {
                        Id = "text__1",
                        Text = "Update With File"
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnStatus()
                    {
                        Id = "status",
                        StatusId = 1
                    }
                },
                new ColumnValue()
                {
                    ColumnBaseType = new ColumnDateTime()
                    {
                        Id = "date4",
                        Date = DateTime.Now
                    }
                },

                new ColumnValue()
                {
                    ColumnBaseType = new ColumnDropDown()
                    {
                        Id = "dropdown__1",
                        LabelId = 1
                    }
                }
            ]
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponseCreate =
            await MondayClient!.CreateBoardItemsAsync(this.BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponseCreate.IsSuccessful);
        Assert.IsNull(mondayResponseCreate.Errors);
        Assert.IsTrue(mondayResponseCreate.Response?.Count == 1);
        Assert.IsTrue(mondayResponseCreate.Response?.FirstOrDefault()?.Data?.Name == item.Name);

        // Create Update For The Item
        Update update = new()
        {
            ItemId = mondayResponseCreate.Response.FirstOrDefault()?.Data?.Id,
            TextBody = "Test Updated With File"
        };

        // Act
        NET.Application.MondayResponse<Update> mondayResponse =
            await MondayClient!.CreateItemsUpdateAsync([update]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.TextBody == update.TextBody);


        // Arrange
        Update update0 = new()
        {
            ItemId = mondayResponse.Response.FirstOrDefault()?.Data?.Id,
            FileUpload = new FileUpload()
                { FileName = "test.txt", StreamContent = new StreamContent(File.OpenRead("test.txt")) }
        };

        // Act
        NET.Application.MondayResponse<Asset> uploadFilesMondayResponse =
            await MondayClient!.UploadFileToUpdateAsync([update0]);

        // Assert
        Assert.IsTrue(uploadFilesMondayResponse.Response?.Count == 1);
        Assert.IsTrue(uploadFilesMondayResponse.IsSuccessful);
        Assert.IsTrue(uploadFilesMondayResponse.Errors is null);
    }

    // Create and Delete an Item
    [TestMethod]
    public async Task TestBoard_DeleteItem_Should_Be_Ok()
    {
        // Arrange
        Item item = new()
        {
            Name = "Test Item - FOR DELETION",
            Group = new Group() { Id = "group_title" }
        };

        // Act
        NET.Application.MondayResponse<Item> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync(this.BoardId, [item]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(item.Id > 0);

        // Act
        mondayResponse = await MondayClient!.DeleteItemsAsync([item]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsNull(mondayResponse.Errors);
        Assert.IsTrue(mondayResponse.Response?.Count == 1);
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Id == item.Id);
    }
    
    [TestMethod]
    public async Task Update_Board_Item_Name_Should_Be_Ok()
    {
        // Arrange
        Customer testRow = new()
        {
            Name = "Test Item Create",
            XeroId = new ColumnText()
            {
                Id = "text_mkmn7km4",
                Text = "FROM UNIT TEST"
            }
        };

        // Act
        NET.Application.MondayResponse<Customer> mondayResponse =
            await MondayClient!.CreateBoardItemsAsync<Customer>(this.BoardId, [testRow]);

        // Assert
        Assert.IsTrue(mondayResponse.IsSuccessful);
        Assert.IsTrue(mondayResponse.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(mondayResponse.Response?.FirstOrDefault()?.Data?.Name == testRow.Name);
        Assert.IsNull(mondayResponse.Errors);
        
        // Act
        List<Customer> items = new List<Customer>();
        MondayData<Customer>? item = mondayResponse.Response?.FirstOrDefault();
        
        // Assert
        Assert.IsNotNull(item);
        Assert.IsTrue(item.Data?.Id > 0);
        
        // Arrange
        item.Data.Name = "Updated Item";
        items.Add(item.Data);
        MondayResponse<Customer> updatedItem = await this.MondayClient.UpdateBoardItemsAsync<Customer>(this.BoardId, items.ToArray());
        
        // Assert
        Assert.IsTrue(updatedItem.IsSuccessful);
        Assert.IsTrue(updatedItem.Response?.All(x => x.Data?.Id != 0));
        Assert.IsTrue(updatedItem.Response?.FirstOrDefault()?.Data?.Name == item.Data.Name);
        Assert.IsNull(updatedItem.Errors);
    }
    public record Customer : MondayRow
    {
        [MondayColumnHeader("text_mkmn7km4")]
        public ColumnText? XeroId { get; set; }
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
        [MondayColumnHeader("text0")] public ColumnText? Text { get; set; }

        [MondayColumnHeader("numbers9")] public ColumnNumber? Number { get; set; }

        [MondayColumnHeader("checkbox")] public ColumnCheckBox? Checkbox { get; set; }

        [MondayColumnHeader("priority")] public ColumnStatus? Priority { get; set; }

        [MondayColumnHeader("status")] public ColumnStatus? Status { get; set; }

        [MondayColumnHeader("link2")] public ColumnLink? Link { get; set; }

        [MondayColumnHeader("dropdown")] public ColumnDropDown? Dropdown { get; set; }

        [MondayColumnHeader("date")] public ColumnDateTime? Date { get; set; }

        [MondayColumnHeader("long_text")] public ColumnLongText? LongText { get; set; }

        [MondayColumnHeader("color_picker")] public ColumnColorPicker? ColorPicker { get; set; }

        [MondayColumnHeader("timeline")] public ColumnTimeline? Timeline { get; set; }

        [MondayColumnHeader("tags")] public ColumnTag? Tags { get; set; }

        [MondayColumnHeader("email")] public ColumnEmail? Email { get; set; }

        [MondayColumnHeader("rating")] public ColumnRating? Rating { get; set; }

        [MondayColumnHeader("phone")] public ColumnPhone? Phone { get; set; }
    }

    public record Test2Row : MondayRow
    {
        [MondayColumnHeader("text0")] public ColumnText? Text { get; set; }

        public Group? Group { get; set; }
    }

    public record TestSubRow : MondayRow
    {
        [MondayColumnHeader("status__1")] public ColumnText? Status { get; set; }

        [MondayColumnHeader("date6")] public ColumnDateTime? DueDate { get; set; }

        [MondayColumnHeader("numbers8")] public ColumnNumber? Priority { get; set; }
    }

    // Fields currently on Test-Board
    public record MondayTestRow : MondayRow
    {
        public Group? Group { get; set; }

        [MondayColumnHeader("status")] public ColumnStatus? Status { get; set; }

        [MondayColumnHeader("date4")] public ColumnDateTime? Date { get; set; }

        [MondayColumnHeader("dropdown__1")] public ColumnDropDown? Dropdown { get; set; }

        [MondayColumnHeader("text__1")] public ColumnText? Text { get; set; }

        [MondayColumnHeader("files9__1")] public ColumnFile? Files { get; set; }
    }
}