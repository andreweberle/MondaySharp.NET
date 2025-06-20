﻿using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using MondaySharp.NET.Application;
using MondaySharp.NET.Application.Attributes;
using MondaySharp.NET.Application.Common;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Infrastructure.Utilities;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MondaySharp.NET.Infrastructure.Persistence;

public partial class MondayClient : IMondayClient, IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    private readonly MondayOptions? _mondayOptions;

    /// <summary>
    /// 
    /// </summary>
    private readonly GraphQLHttpClient? _graphQLHttpClient;

    /// <summary>
    /// 
    /// </summary>
    private bool disposedValue;

    /// <summary>
    /// 
    /// </summary>
    private readonly ILogger<MondayClient>? _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="options"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public MondayClient(ILogger<MondayClient> logger, Action<MondayOptions> options)
    {
        // Create New Monday Options.
        _mondayOptions = new MondayOptions();

        // Invoke Delegate That Will Assign The Options To The Monday Options.
        options.Invoke(_mondayOptions);

        // Build The GraphQL Client.
        _graphQLHttpClient = new GraphQLHttpClient(new GraphQLHttpClientOptions()
            {
                EndPoint = _mondayOptions.EndPoint
            },
            new NewtonsoftJsonSerializer());

        // Create Header For The GraphQL Client.
        _graphQLHttpClient.HttpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _mondayOptions.Token);

        // Set The API Version.
        _graphQLHttpClient.HttpClient.DefaultRequestHeaders.Add("API-Version", _mondayOptions.Version);

        // Set The Logger.
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // Stop The Client From Being Created Without Options.
    public MondayClient()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue && disposing)
        {
            _logger?.LogInformation("Disposing Monday Client.");
            _graphQLHttpClient?.HttpClient?.Dispose();

            _logger?.LogInformation("Disposing GraphQL Client.");
            _graphQLHttpClient?.Dispose();

            disposedValue = true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="boardId"></param>
    /// <param name="columnValues"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<MondayResponse<T>> GetBoardItemsAsync<T>(
        ulong boardId, ColumnValue[] columnValues, int limit = 25,
        CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<T>()
            {
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilities.UnsupportedTypes)
        {
            int count = instance.GetType().GetProperties()
                .Count(propertyInfo => propertyInfo.PropertyType == unSupportedType.Key);
            if (count > 1) throw new NotImplementedException(unSupportedType.Value);
        }

        // Create New
        StringBuilder stringBuilder = new();

        // Get each property in instance.
        foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
        {
            // Attempt to get the type from the GetItemsQueryBuilder
            if (MondayUtilities.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
            {
                // Append the query to the string builder.
                stringBuilder.Append(query);
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"query($limit: Int, $boardId: ID!, $columnValues: [ItemsPageByColumnValuesQuery!]) {{
              items_page_by_column_values(limit: $limit, board_id: $boardId, columns: $columnValues) {{
                cursor
                items {{
                  id
                  name
                  state
                  column_values {{
                    id
                    text
                    type
                    value
                  }}
                    {stringBuilder}
                }}
              }}
            }}",
            Variables = new
            {
                boardId,
                columnValues = columnValues.Select(query => new
                {
                    column_id = query.Id,
                    column_values = new List<string?> { query.Text }
                }),
                limit
            }
        };

        // Execute The Query.
        GraphQLResponse<GetBoardItemsByColumnValuesResponse> graphQLResponse =
            await _graphQLHttpClient.SendQueryAsync<GetBoardItemsByColumnValuesResponse>(keyValuePairs,
                cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.ItemsPageByColumnValue?.Items?.Count > 0)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilities.GetColumnPropertyMap<T>();

            // Create New Instance Of MondayResponse.
            MondayResponse<T> mondayResponse = new()
            {
                IsSuccessful = true,
                Cursor = graphQLResponse.Data.ItemsPageByColumnValue.Cursor,
                Response = []
            };

            // Loop through each item
            foreach (Item item in graphQLResponse.Data.ItemsPageByColumnValue.Items)
            {
                // Check if we need to break out of the loop
                if (cancellationToken.IsCancellationRequested)
                {
                    return new MondayResponse<T>()
                    {
                        IsSuccessful = false,
                        Errors = ["Cancellation Requested."]
                    };
                }

                // Create New Instance Of T Type.
                T datanstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilities.TryBindColumnDataAsync(columnPropertyMap!, item!, ref datanstance))
                {
                    // Add the data to the response
                    mondayResponse.Response.Add(new MondayData<T>()
                    {
                        Data = datanstance
                    });
                }
            }

            return mondayResponse;
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    public async Task<MondayResponse<T>> GetBoardItemsAsync<T>(ulong[] itemIds, int limit = 25,
        CancellationToken cancellationToken = default)  where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<T>()
            {
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilities.UnsupportedTypes)
        {
            int count = instance.GetType().GetProperties().Count(propertyInfo => propertyInfo.PropertyType == unSupportedType.Key);
            if (count > 1) throw new NotImplementedException(unSupportedType.Value);
        }

        // Create New
        StringBuilder stringBuilder = new();

        // Get each property in instance.
        foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
        {
            // Attempt to get the type from the GetItemsQueryBuilder
            if (MondayUtilities.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
            {
                // Append the query to the string builder.
                stringBuilder.Append(query);
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"query($limit: Int, $itemIds: [ID!]) {{
              items(ids: $itemIds, limit: $limit) {{
                id
                name
                state
                column_values {{
                  id
                  text
                  type
                  value
                }}
                {stringBuilder}
              }}
            }}",
            Variables = new
            {
                itemIds,
                limit
            }
        };

        // Execute The Query.
        GraphQLResponse<ItemsPageByColumnValue> graphQLResponse =
            await _graphQLHttpClient.SendQueryAsync<ItemsPageByColumnValue>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is not null || !(graphQLResponse.Data?.Items?.Count > 0))
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
            };
        }

        // Get The Column Property Map.
        Dictionary<string, string> columnPropertyMap = MondayUtilities.GetColumnPropertyMap<T>();
        
        // Create New Instance Of MondayResponse.
        MondayResponse<T> mondayResponse = new()
        {
            IsSuccessful = true,
            Response = []
        };
        
        // Loop through each item
        foreach (Item item in graphQLResponse.Data.Items)
        {
            // Check if we need to break out of the loop
            if (cancellationToken.IsCancellationRequested)
            {
                return new MondayResponse<T>()
                {
                    IsSuccessful = false,
                    Errors = ["Cancellation Requested."]
                };
            }
            // Create New Instance Of T Type.
            T dataInstance = Activator.CreateInstance<T>();
            
            // Attempt To Bind The Items.
            if (MondayUtilities.TryBindColumnDataAsync(columnPropertyMap!, item!, ref dataInstance))
            {
                // Add the data to the response
                mondayResponse.Response.Add(new MondayData<T>()
                {
                    Data = dataInstance
                });
            }
        }

        // Return The Response.
        return mondayResponse;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="boardId"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<MondayResponse<T>> GetBoardItemsAsync<T>(
        ulong boardId, int limit = 25,
        CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilities.UnsupportedTypes)
        {
            int count = instance.GetType().GetProperties()
                .Count(propertyInfo => propertyInfo.PropertyType == unSupportedType.Key);
            if (count > 1) throw new NotImplementedException(unSupportedType.Value);
        }

        // Create New
        StringBuilder stringBuilder = new();

        // Get each property in instance.
        foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
        {
            // Attempt to get the type from the GetItemsQueryBuilder
            if (MondayUtilities.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
            {
                // Append the query to the string builder.
                stringBuilder.Append(query);
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"query($limit: Int, $boardId: ID!) {{
                boards (ids: [$boardId]) {{
                    id
                    name
                    state
                    board_kind
                    board_folder_id
                    description
                    workspace_id
                    item_terminology
                    items_count
                    permissions
                    items_page (limit: $limit) {{
                        cursor
                        items {{
                            id
                            name
                            state
                            column_values {{
                                id
                                text
                                type
                                value
                            }}
                            {stringBuilder}
                        }}
                    }}
                }}
            }}",
            Variables = new
            {
                boardId,
                limit
            }
        };

        // Execute The Query.
        GraphQLResponse<GetBoardItemsResponse> graphQLResponse =
            await _graphQLHttpClient.SendQueryAsync<GetBoardItemsResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null
            && graphQLResponse.Data?.Boards?.Count > 0
            && graphQLResponse.Data?.Boards?.Any(x => x.ItemsCount > 0) == true)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilities.GetColumnPropertyMap<T>();

            // Create New Instance Of MondayResponse.
            MondayResponse<T>? mondayResponse = new()
            {
                IsSuccessful = true,
                Cursor = graphQLResponse.Data.Boards.Select(x => x.ItemsPage!.Cursor).FirstOrDefault(),
                Response = []
            };

            // Loop through each item
            foreach (Item item in graphQLResponse.Data.Boards.Select(x => x.ItemsPage!)
                         .Where(x => x.Items?.Count > 0)
                         .SelectMany(x => x.Items))
            {
                // Check if we need to break out of the loop
                if (cancellationToken.IsCancellationRequested)
                {
                    return new MondayResponse<T>()
                    {
                        IsSuccessful = false,
                        Errors = ["Cancellation Requested."]
                    };
                }

                // Create New Instance Of T Type.
                T dataInstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilities.TryBindColumnDataAsync(columnPropertyMap!, item!, ref dataInstance))
                {
                    // Add the data to the response
                    mondayResponse.Response.Add(new MondayData<T>()
                    {
                        Data = dataInstance
                    });
                }
            }

            return mondayResponse;
        }
        else if (graphQLResponse.Errors is null
                 && graphQLResponse.Data?.Boards?.Count > 0
                 && graphQLResponse.Data?.Boards?.Any(x => x.ItemsCount == 0) == true)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = true,
                Errors = ["No Items Found."]
            };
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet(),
            Response = null
        };
    }

    public async Task<MondayResponse<T>> CreateBoardItemsAsync<T>(
        ulong boardId, T[] items, CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id name}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Append the parameters
        parameters.Append("$boardId: ID!,");

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = new()
        {
            { "boardId", boardId }
        };

        foreach (var item in items.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (string.IsNullOrEmpty(item.value.Name))
            {
                return new MondayResponse<T>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Name Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"columnValues{item.i}";

            // Check if the column values are null
            List<ColumnBaseType> columnValues = [];

            // Store Group.
            Group? group = null;

            // Foreach property in the item
            foreach (PropertyInfo propertyInfo in item.value.GetType().GetProperties()
                         .Where(x => x.GetCustomAttribute<MondayColumnTypeUnsupportedWriteAttribute>() == null))
            {
                // Skip the name property.
                if (propertyInfo.Name == nameof(item.value.Name)) continue;

                // Skip the id property.
                if (propertyInfo.Name == nameof(item.value.Id)) continue;

                // Check if the property is a MondayGroupType.
                if (propertyInfo.PropertyType == typeof(Group))
                {
                    // Get the group.
                    group = (Group?)propertyInfo.GetValue(item.value);

                    // Check if the group is not null.
                    if (group is null || group.Id is null) continue;

                    variables.Add($"groupId_{item.i}", group.Id);
                    parameters.Append($"$groupId_{item.i}: String,");

                    continue;
                }

                // Check if the property is a ColumnBaseType
                if (propertyInfo.PropertyType.IsSubclassOf(typeof(ColumnBaseType)))
                {
                    // Get the column base type
                    ColumnBaseType? columnBaseType = (ColumnBaseType?)propertyInfo.GetValue(item.value);

                    // Check if the column base type is not null
                    if (columnBaseType is not null)
                    {
                        // Check there is an id.
                        if (string.IsNullOrEmpty(columnBaseType.Id))
                        {
                            // Check if there is an attribute.
                            if (propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>() is not null)
                            {
                                // Set the id to the attribute id.
                                columnBaseType.Id = propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>()!
                                    .ColumnId;
                            }
                            else
                            {
                                // Use the property name as the id.
                                columnBaseType.Id = propertyInfo.Name;
                            }
                        }

                        // Add the column base type to the list
                        columnValues.Add(columnBaseType);
                    }
                }
            }

            // Add the variable to the dictionary
            variables.Add(variableName, MondayUtilities.ToColumnValuesJson(columnValues));

            // Append the parameters
            parameters.Append($"${variableName}: JSON,");

            // Append the mutation
            mutation.Append(
                $"create_item_{item.i}: create_item(board_id: $boardId, item_name: \"{item.value.Name}\", {(group is not null ? $"group_id: $groupId_{item.i}," : "")} column_values: ${variableName}) {RESPONSE_PARAMS}");
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Item>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            // Loop through each response and assign the id to each item.
            foreach (var item in items.Select((value, i) => new { i, value }))
            {
                // Attempt to get the item id.
                if (graphQLResponse.Data.TryGetValue($"create_item_{item.i}", out Item? createdItem))
                {
                    // Assign the id to the item.
                    item.value.Id = createdItem.Id;
                }
            }

            return new MondayResponse<T>()
            {
                IsSuccessful = true,
                Response = items.Select(x => new MondayData<T>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    public async Task<MondayResponse<T>> CreateBoardSubItemsAsync<T>(
        ulong parentItemId, T[] subItems, CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Check if the parent item id is null.
        if (parentItemId == 0)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Parent Item Id Is Not Valid."]
            };
        }

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id name}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Append the parameters
        parameters.Append("$parentItemId: ID!,");

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = new()
        {
            { "parentItemId", parentItemId }
        };

        foreach (var item in subItems.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (string.IsNullOrEmpty(item.value.Name))
            {
                return new MondayResponse<T>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Name Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"columnValues{item.i}";

            // Check if the column values are null
            List<ColumnBaseType> columnValues = [];

            // Foreach property in the item
            foreach (PropertyInfo propertyInfo in item.value.GetType().GetProperties()
                         .Where(x => x.GetCustomAttribute<MondayColumnTypeUnsupportedWriteAttribute>() == null))
            {
                // Skip the name property.
                if (propertyInfo.Name == nameof(item.value.Name)) continue;

                // Skip the id property.
                if (propertyInfo.Name == nameof(item.value.Id)) continue;

                // Check if the property is a ColumnBaseType
                if (propertyInfo.PropertyType.IsSubclassOf(typeof(ColumnBaseType)))
                {
                    // Get the column base type
                    ColumnBaseType? columnBaseType = (ColumnBaseType?)propertyInfo.GetValue(item.value);

                    // Check if the column base type is not null
                    if (columnBaseType is not null)
                    {
                        // Check there is an id.
                        if (string.IsNullOrEmpty(columnBaseType.Id))
                        {
                            // Check if there is an attribute.
                            if (propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>() is not null)
                            {
                                // Set the id to the attribute id.
                                columnBaseType.Id = propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>()!
                                    .ColumnId;
                            }
                            else
                            {
                                // Use the property name as the id.
                                columnBaseType.Id = propertyInfo.Name;
                            }
                        }

                        // Add the column base type to the list
                        columnValues.Add(columnBaseType);
                    }
                }
            }

            // Add the variable to the dictionary
            variables.Add(variableName, MondayUtilities.ToColumnValuesJson(columnValues));

            // Append the parameters
            parameters.Append($"${variableName}: JSON,");

            // Append the mutation
            mutation.Append(
                $" create_subitem_{item.i}:  create_subitem(parent_item_id: $parentItemId, item_name: \"{item.value.Name}\", column_values: ${variableName}) {RESPONSE_PARAMS}");
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Item>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            // Loop through each response and assign the id to each item.
            foreach (var item in subItems.Select((value, i) => new { i, value }))
            {
                // Attempt to get the item id.
                if (graphQLResponse.Data.TryGetValue($"create_subitem_{item.i}", out Item? createdItem))
                {
                    // Assign the id to the item.
                    item.value.Id = createdItem.Id;
                }
            }

            return new MondayResponse<T>()
            {
                IsSuccessful = true,
                Response = subItems.Select(x => new MondayData<T>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    public async Task<MondayResponse<T>> UpdateBoardItemsAsync<T>(
        ulong boardId, T[] items, CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id name}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Append the parameters
        parameters.Append("$boardId: ID!,");

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = new()
        {
            { "boardId", boardId }
        };

        foreach (var item in items.Select((value, i) => new { i, value }))
        {
            // Generate a unique variable name based on the item index
            string variableName = $"columnValues{item.i}";

            // Add Item Id To The Variables.
            variables.Add($"itemId{item.i}", item.value.Id);

            // Add Item Id To The Parameters.
            parameters.Append($"$itemId{item.i}: ID!,");

            // Check if the column values are null
            List<ColumnBaseType> columnValues = [];

            // Foreach property in the item that doesnt contain the attribute 'MondayColumnTypeUnsupportedWriteAttribute'
            foreach (PropertyInfo propertyInfo in item.value.GetType().GetProperties()
                         .Where(x =>
                             x.PropertyType.GetCustomAttribute<MondayColumnTypeUnsupportedWriteAttribute>() is null))
            {
                // Skip the id property.
                if (propertyInfo.Name == nameof(item.value.Id)) continue;

                // Check if the property is a MondayGroupType.
                if (propertyInfo.PropertyType == typeof(Group))
                {
                    continue;
                }

                // Check if the property is a MondayRow
                if (propertyInfo.DeclaringType == typeof(MondayRow)
                    && propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>()!.ColumnId == MondayDefaultColumnIds.Name)
                {
                    columnValues.Add(new ColumnText(MondayDefaultColumnIds.Name, item.value.Name));
                    continue;
                }

                // Check if the property is a ColumnBaseType
                if (propertyInfo.PropertyType.IsSubclassOf(typeof(ColumnBaseType)))
                {
                    // Get the column base type
                    ColumnBaseType? columnBaseType = (ColumnBaseType?)propertyInfo.GetValue(item.value);

                    // Check if the column base type is not null
                    if (columnBaseType is not null)
                    {
                        // Check there is an id.
                        if (string.IsNullOrEmpty(columnBaseType.Id))
                        {
                            // Check if there is an attribute.
                            if (propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>() is not null)
                            {
                                // Set the id to the attribute id.
                                columnBaseType.Id = propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>()!
                                    .ColumnId;
                            }
                            else
                            {
                                // Use the property name as the id.
                                columnBaseType.Id = propertyInfo.Name;
                            }
                        }
                    }
                    else
                    {
                        // Check if there is an attribute.
                        if (propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>() is not null)
                        {
                            // Get Type of ColumnBaseType.
                            columnBaseType = (ColumnBaseType?)Activator.CreateInstance(propertyInfo.PropertyType,
                                propertyInfo.GetCustomAttribute<MondayColumnHeaderAttribute>()!.ColumnId);
                        }
                        else
                        {
                            // Get Type of ColumnBaseType.
                            columnBaseType = (ColumnBaseType?)Activator.CreateInstance(propertyInfo.PropertyType);
                        }

                        if (columnBaseType is null) continue;
                    }

                    // Add the column base type to the list
                    columnValues.Add(columnBaseType);
                }
            }

            // Add the variable to the dictionary
            variables.Add(variableName, MondayUtilities.ToColumnValuesJson(columnValues));

            // Append the parameters
            parameters.Append($"${variableName}: JSON!,");

            // Append the mutation
            mutation.Append(
                $" change_multiple_column_values_{item.i}: change_multiple_column_values(item_id: $itemId{item.i}, board_id: $boardId, column_values: ${variableName}) {RESPONSE_PARAMS}");
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Item>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = true,
                Response = items.Select(x => new MondayData<T>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="boardId"></param>
    /// <param name="items"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MondayResponse<Item>> CreateBoardItemsAsync(
        ulong boardId, Item[] items,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<Item>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Create The Response Parameters.
        const string responseParams = @$"{{id name}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Append the parameters
        parameters.Append("$boardId: ID!,");

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = new()
        {
            { "boardId", boardId }
        };

        foreach (var item in items.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (string.IsNullOrEmpty(item.value.Name))
            {
                return new MondayResponse<Item>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Name Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"columnValues{item.i}";

            // Check if the column values are null
            if (item.value.ColumnValues is { Count: > 0 })
            {
                // Add the variable to the dictionary
                variables.Add(variableName,
                    MondayUtilities.ToColumnValuesJson(item.value.ColumnValues.Select(x => x.ColumnBaseType)
                        .ToList()!));

                // Append the parameters
                parameters.Append($"${variableName}: JSON,");

                // Append the mutation
                mutation.Append(
                    $"create_item_{item.i}: create_item(board_id: $boardId, item_name: \"{item.value.Name}\", {(item.value.Group is not null ? $"group_id: $groupId_{item.i}," : "")} column_values: ${variableName}) {responseParams}");
            }
            else
            {
                // Append the mutation
                mutation.Append(
                    $"create_item_{item.i}: create_item(board_id: $boardId, item_name: \"{item.value.Name}\", , {(item.value.Group is not null ? $"group_id: $groupId_{item.i}," : "")}) {responseParams}");
            }

            if (item.value.Group is not null
                && !string.IsNullOrEmpty(item.value.Group.Id))
            {
                variables.Add($"groupId_{item.i}", item.value.Group.Id);
                parameters.Append($"$groupId_{item.i}: String,");
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Item>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            // Loop through each response and assign the id to each item.
            foreach (var item in items.Select((value, i) => new { i, value }))
            {
                // Attempt to get the item id.
                if (graphQLResponse.Data.TryGetValue($"create_item_{item.i}", out Item? createdItem))
                {
                    // Assign the id to the item.
                    item.value.Id = createdItem.Id;
                }
            }

            return new MondayResponse<Item>()
            {
                IsSuccessful = true,
                Response = items.Select(x => new MondayData<Item>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<Item>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    public async Task<MondayResponse<Item>> CreateBoardSubItemsAsync(
        ulong parentItemId, Item[] subItems,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<Item>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Check if the parent item id is null.
        if (parentItemId == 0)
        {
            return new MondayResponse<Item>()
            {
                IsSuccessful = false,
                Errors = ["Parent Item Id Is Not Valid."]
            };
        }

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id name}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Append the parameters
        parameters.Append("$parentItemId: ID!,");

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = new()
        {
            { "parentItemId", parentItemId }
        };

        foreach (var item in subItems.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (string.IsNullOrEmpty(item.value.Name))
            {
                return new MondayResponse<Item>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Name Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"columnValues{item.i}";

            // Check if the column values are null
            if (item.value.ColumnValues is not null && item.value.ColumnValues is { Count: > 0 })
            {
                // Add the variable to the dictionary
                variables.Add(variableName,
                    MondayUtilities.ToColumnValuesJson(item.value.ColumnValues.Select(x => x.ColumnBaseType)
                        .ToList()!));

                // Append the parameters
                parameters.Append($"${variableName}: JSON,");

                // Append the mutation
                mutation.Append(
                    $" create_subitem_{item.i}:  create_subitem(parent_item_id: $parentItemId, item_name: \"{item.value.Name}\", column_values: ${variableName}) {RESPONSE_PARAMS}");
            }
            else
            {
                // Append the mutation
                mutation.Append(
                    $" create_subitem_{item.i}:  create_subitem(parent_item_id: $parentItemId, item_name: \"{item.value.Name}\") {RESPONSE_PARAMS}");
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Item>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            // Loop through each response and assign the id to each item.
            foreach (var item in subItems.Select((value, i) => new { i, value }))
            {
                // Attempt to get the item id.
                if (graphQLResponse.Data.TryGetValue($"create_subitem_{item.i}", out Item? createdItem))
                {
                    // Assign the id to the item.
                    item.value.Id = createdItem.Id;
                }
            }

            return new MondayResponse<Item>()
            {
                IsSuccessful = true,
                Response = subItems.Select(x => new MondayData<Item>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<Item>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="cursor"></param>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<MondayResponse<T>> GetBoardItemsAsync<T>(
        string? cursor, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The Cursor Is Null, Return Null.
        if (cursor == null)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Cursor Is Null."]
            };
        }

        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilities.UnsupportedTypes)
        {
            int count = instance.GetType().GetProperties()
                .Count(propertyInfo => propertyInfo.PropertyType == unSupportedType.Key);
            if (count > 1) throw new NotImplementedException(unSupportedType.Value);
        }

        // Create New
        StringBuilder stringBuilder = new();

        // Get each property in instance.
        foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
        {
            // Attempt to get the type from the GetItemsQueryBuilder
            if (MondayUtilities.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
            {
                // Append the query to the string builder.
                stringBuilder.Append(query);
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"query($cursor: String!, $limit: Int) {{
                next_items_page (cursor: $cursor, limit: $limit) {{
                    cursor
                    items {{
                        id
                        name
                        state
                        column_values {{
                            id
                            text
                            type
                            value
                        }}
                        {stringBuilder}
                    }}
                }}
            }}",
            Variables = new
            {
                cursor,
                limit
            }
        };

        // Execute The Query.
        GraphQLResponse<NextItemsPageResponse> graphQLResponse =
            await _graphQLHttpClient.SendQueryAsync<NextItemsPageResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.NextItemsPage?.Items?.Count > 0)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilities.GetColumnPropertyMap<T>();

            // Create New Instance Of MondayResponse.
            MondayResponse<T> mondayResponse = new()
            {
                IsSuccessful = true,
                Cursor = graphQLResponse.Data.NextItemsPage.Cursor,
                Response = []
            };

            // Loop through each item
            foreach (Item item in graphQLResponse.Data.NextItemsPage.Items)
            {
                // Check if we need to break out of the loop
                if (cancellationToken.IsCancellationRequested)
                {
                    return new MondayResponse<T>()
                    {
                        IsSuccessful = false,
                        Errors = ["Cancellation Requested."]
                    };
                }

                // Create New Instance Of T Type.
                T dataInstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilities.TryBindColumnDataAsync(columnPropertyMap!, item!, ref dataInstance))
                {
                    mondayResponse.Response.Add(new MondayData<T>()
                    {
                        Data = dataInstance
                    });
                }
            }

            return mondayResponse;
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="updates"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<MondayResponse<Update>> CreateItemsUpdateAsync(
        Update[] updates,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<Update>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id text_body}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = [];

        // Append the parameters
        foreach (var update in updates.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (update.value.ItemId.GetValueOrDefault() == 0)
            {
                return new MondayResponse<Update>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Id Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"body{update.i}";

            // Check if the column values are null
            if (update.value.TextBody is not null)
            {
                // Add the variable to the dictionary
                variables.Add(variableName, update.value.TextBody);

                // Append the parameters
                parameters.Append($"${variableName}: String!,");

                // Append the mutation
                mutation.Append(
                    $"create_update_{update.i}: create_update(item_id: {update.value.ItemId}, body: ${variableName}) {RESPONSE_PARAMS}");
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Update>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Update>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            // Loop through each response and assign the id to each item.
            foreach (var update in updates.Select((value, i) => new { i, value }))
            {
                // Attempt to get the item id.
                if (graphQLResponse.Data.TryGetValue($"create_update_{update.i}", out Update? createdUpdate))
                {
                    // Assign the id to the item.
                    update.value.Id = createdUpdate.Id;
                }
            }

            return new MondayResponse<Update>()
            {
                IsSuccessful = true,
                Response = updates.Select(x => new MondayData<Update>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<Update>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="items"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<MondayResponse<Item>> DeleteItemsAsync(
        Item[] items,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<Item>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = [];

        // Append the parameters
        foreach (var item in items.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (item.value.Id == 0)
            {
                throw new NullReferenceException(nameof(item.value.Id));
            }

            // Generate a unique variable name based on the item index
            string variableName = $"itemId{item.i}";

            // Add the variable to the dictionary
            variables.Add(variableName, item.value.Id);

            // Append the parameters
            parameters.Append($"${variableName}: ID!,");

            // Append the mutation
            mutation.Append($"delete_item_{item.i}: delete_item(item_id: ${variableName}) {RESPONSE_PARAMS}");
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"mutation ({parameters}) {{
                {mutation}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<Dictionary<string, Item>> graphQLResponse =
            await _graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            return new MondayResponse<Item>()
            {
                IsSuccessful = true,
                Response = items.Select(x => new MondayData<Item>() { Data = x }).ToList()
            };
        }

        return new MondayResponse<Item>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MondayResponse<Board>> GetBoardsAsync(
        ulong[]? boardIds = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<Board>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // Create The Response Parameters.
        const string RESPONSE_PARAMS =
            @$"{{id name state board_kind board_folder_id description workspace_id item_terminology items_count permissions updated_at}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Append the parameters
        parameters.Append("$limit: Int,");

        // Create a dictionary to store variables dynamically
        Dictionary<string, object> variables = new()
        {
            { "limit", limit }
        };

        // Check if the board ids are not null and the length is greater than 0
        if (boardIds is not null && boardIds.Length > 0)
        {
            // Append the parameters
            parameters.Append("$boardIds: [ID!],");
            variables.Add("boardIds", boardIds);
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"query ({parameters}) {{
            boards(limit: $limit{(boardIds is not null && boardIds.Length > 0 ? ",ids: $boardIds" : "")})  
            {RESPONSE_PARAMS}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<GetBoardsResponse> graphQLResponse =
            await _graphQLHttpClient.SendQueryAsync<GetBoardsResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.Boards?.Count > 0)
        {
            return new MondayResponse<Board>()
            {
                IsSuccessful = true,
                Response = graphQLResponse.Data.Boards.Select(x => new MondayData<Board>()
                    {
                        Data = x
                    })
                    .ToList()
            };
        }

        return new MondayResponse<Board>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="updates"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MondayResponse<Asset>> UploadFileToUpdateAsync(
        Update[] updates,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<Asset>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Create a dictionary to store variables dynamically
        using MultipartFormDataContent multipartFormDataContent = [];

        // Append the parameters
        foreach (var update in updates.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (update.value.ItemId.GetValueOrDefault() == 0)
            {
                return new MondayResponse<Asset>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Id Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"file{update.i}";

            // Check if the column values are null
            if (update.value.FileUpload?.StreamContent is not null && update.value.FileUpload.FileName is not null)
            {
                // Append the parameters
                parameters.Append($"${variableName}: File!, $updateId{update.i}: ID!,");

                // Append the mutation
                mutation.Append(
                    $"add_file_to_update_{update.i}: add_file_to_update(update_id: $updateId{update.i}, file: ${variableName}) {RESPONSE_PARAMS}");

                // Add ByteArrayContent to the dictionary
                multipartFormDataContent.Add(update.value.FileUpload.StreamContent, $"variables[{variableName}]",
                    update.value.FileUpload.FileName);

                // Add the update id to the dictionary
                multipartFormDataContent.Add(new StringContent(update.value.ItemId.GetValueOrDefault().ToString()),
                    $"variables[updateId{update.i}]");
            }
        }

        // Create the query
        string query = $"mutation ({parameters}){{{mutation}}}";

        // Add the query to the dictionary
        multipartFormDataContent.Add(new StringContent(query), "query");

        // Execute The Query.
        using HttpResponseMessage httpResponseMessage =
            await _graphQLHttpClient.HttpClient.PostAsync($"{_mondayOptions!.EndPoint}/file", multipartFormDataContent,
                cancellationToken);

        // Read the response
        string rawResponse = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Parse the response
        using JsonDocument jsonDocument = JsonDocument.Parse(rawResponse);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (httpResponseMessage.IsSuccessStatusCode
            && jsonDocument.RootElement.TryGetProperty("data", out JsonElement data))
        {
            MondayResponse<Asset> response = new()
            {
                IsSuccessful = true,
                Response = []
            };

            // Loop through each property
            foreach (var property in data.EnumerateObject().Select((value, i) => new { i, value }))
            {
                // Check if the property is an object
                if (property.value.Value.ValueKind == JsonValueKind.Object)
                {
                    // Converstion was created with the Newtonsoft
                    Asset? asset =
                        Newtonsoft.Json.JsonConvert.DeserializeObject<Asset>(property.value.Value.GetRawText());

                    // Check if the asset is not null
                    if (asset is not null)
                    {
                        // Assign The item id to the asset.
                        asset.ItemId = updates[property.i].ItemId.GetValueOrDefault();

                        // Add the asset to the response
                        response.Response?.Add(new MondayData<Asset>()
                        {
                            Data = asset
                        });
                    }
                }
            }

            return response;
        }
        else
        {
            // Create the response
            MondayResponse<Asset> response = new()
            {
                IsSuccessful = false
            };

            // Check if the response has errors
            if (jsonDocument.RootElement.TryGetProperty("errors", out JsonElement errors))
            {
                // Return the errors
                response.Errors = errors
                    .EnumerateArray()
                    .Select(x => x.GetProperty("message").GetString())
                    .ToHashSet()!;
            }
            else if (jsonDocument.RootElement.TryGetProperty("error_message", out JsonElement errorMessage))
            {
                // Return the errors
                response.Errors = [errorMessage.GetString()!];
            }
            else
            {
                // Return the errors
                response.Errors = [rawResponse];
            }

            return response;
        }
    }

    public async Task<MondayResponse<Asset>> UploadFileToColumnAsync(
        Item[] items, CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
        {
            return new MondayResponse<Asset>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id}}";

        // Create parameters for the query
        StringBuilder parameters = new();

        // Create the mutation
        StringBuilder mutation = new();

        // Create a dictionary to store variables dynamically
        using MultipartFormDataContent multipartFormDataContent = [];

        // Append the parameters
        foreach (var item in items.Select((value, i) => new { i, value }))
        {
            // Check if there is an item name.
            if (item.value.Id == 0)
            {
                return new MondayResponse<Asset>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Id Is Null."]
                };
            }

            // Check if there is a file upload
            if (item.value.FileUpload is null)
            {
                return new MondayResponse<Asset>()
                {
                    IsSuccessful = false,
                    Errors = ["File Upload Is Null."]
                };
            }

            // Generate a unique variable name based on the item index
            string variableName = $"file{item.i}";

            // Check if the column values are null
            if (item.value.FileUpload?.StreamContent is not null && item.value.FileUpload.FileName is not null)
            {
                // Append the parameters
                parameters.Append($"${variableName}: File!, $itemId{item.i}: ID!,");

                // Append the mutation
                mutation.Append(
                    $"add_file_to_column_{item.i}: add_file_to_column(item_id: $itemId{item.i}, column_id: \"{item.value.FileUpload.ColumnId}\", file: ${variableName}) {RESPONSE_PARAMS}");

                // Add ByteArrayContent to the dictionary
                multipartFormDataContent.Add(item.value.FileUpload.StreamContent, $"variables[{variableName}]",
                    item.value.FileUpload.FileName);

                // Add the update id to the dictionary
                multipartFormDataContent.Add(new StringContent(item.value.Id.ToString()), $"variables[itemId{item.i}]");
            }
        }

        // Create the query
        string query = $"mutation ({parameters}){{{mutation}}}";

        // Add the query to the dictionary
        multipartFormDataContent.Add(new StringContent(query), "query");

        // Execute The Query.
        using HttpResponseMessage httpResponseMessage = await _graphQLHttpClient.HttpClient.PostAsync(
            $"{_mondayOptions!.EndPoint}/file", multipartFormDataContent, cancellationToken);

        // Read the response
        string rawResponse = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Parse the response
        using JsonDocument jsonDocument = JsonDocument.Parse(rawResponse);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (httpResponseMessage.IsSuccessStatusCode &&
            jsonDocument.RootElement.TryGetProperty("data", out JsonElement data))
        {
            MondayResponse<Asset> response = new()
            {
                IsSuccessful = true,
                Response = []
            };

            // Loop through each property
            foreach (var property in data.EnumerateObject().Select((value, i) => new { i, value }))
            {
                // Check if the property is an object
                if (property.value.Value.ValueKind == JsonValueKind.Object)
                {
                    // Converstion was created with the Newtonsoft
                    Asset? asset =
                        Newtonsoft.Json.JsonConvert.DeserializeObject<Asset>(property.value.Value.GetRawText());

                    // Check if the asset is not null
                    if (asset is not null)
                    {
                        // Assign The item id to the asset.
                        asset.ItemId = items[property.i].Id;

                        // Add the asset to the response
                        response.Response?.Add(new MondayData<Asset>()
                        {
                            Data = asset
                        });
                    }
                }
            }

            return response;
        }
        else
        {
            // Create the response
            MondayResponse<Asset> response = new()
            {
                IsSuccessful = false
            };

            // Check if the response has errors
            if (jsonDocument.RootElement.TryGetProperty("errors", out JsonElement errors))
            {
                // Return the errors
                response.Errors = errors
                    .EnumerateArray()
                    .Select(x => x.GetProperty("message").GetString())
                    .ToHashSet()!;
            }
            else if (jsonDocument.RootElement.TryGetProperty("error_message", out JsonElement errorMessage))
            {
                // Return the errors
                response.Errors = [errorMessage.GetString()!];
            }
            else
            {
                // Return the errors
                response.Errors = [rawResponse];
            }

            return response;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="itemId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<MondayResponse<T>> GetBoardItemAsync<T>(ulong itemId,
        CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (_graphQLHttpClient == null)
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilities.UnsupportedTypes)
        {
            int count = instance.GetType().GetProperties()
                .Count(propertyInfo => propertyInfo.PropertyType == unSupportedType.Key);
            if (count > 1) throw new NotImplementedException(unSupportedType.Value);
        }

        // Create New
        StringBuilder stringBuilder = new();

        // Get each property in instance.
        foreach (PropertyInfo propertyInfo in instance.GetType().GetProperties())
        {
            // Attempt to get the type from the GetItemsQueryBuilder
            if (MondayUtilities.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
            {
                // Append the query to the string builder.
                stringBuilder.Append(query);
            }
        }

        // Construct the GraphQL query
        GraphQLRequest keyValuePairs = new()
        {
            Query = $@"query($itemId: [ID!]) {{
                 items(ids: $itemId) {{
                    id
                    name
                    state
                    column_values {{
                        id
                        text
                        type
                        value
                    }}
                    {stringBuilder}
                 }}
            }}",
            Variables = new
            {
                itemId
            }
        };

        // Execute The Query.
        GraphQLResponse<ItemsPageByColumnValue> graphQLResponse =
            await _graphQLHttpClient.SendQueryAsync<ItemsPageByColumnValue>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.Items?.Count > 0)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilities.GetColumnPropertyMap<T>();

            // Loop through each item
            foreach (Item item in graphQLResponse.Data.Items)
            {
                // Create New Instance Of T Type.
                T dataInstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilities.TryBindColumnDataAsync(columnPropertyMap!, item, ref dataInstance))
                {
                    return new MondayResponse<T>()
                    {
                        IsSuccessful = true,
                        Response =
                        [
                            new MondayData<T>()
                            {
                                Data = dataInstance
                            }
                        ]
                    };
                }
            }
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
        };
    }
}