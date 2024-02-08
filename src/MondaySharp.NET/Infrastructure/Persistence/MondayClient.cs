using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using MondaySharp.NET.Application;
using MondaySharp.NET.Application.Common;
using MondaySharp.NET.Application.Entities;
using MondaySharp.NET.Application.Interfaces;
using MondaySharp.NET.Domain.ColumnTypes;
using MondaySharp.NET.Domain.Common;
using MondaySharp.NET.Infrastructure.Utilities;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            EndPoint = _mondayOptions.EndPoint,
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
    public MondayClient() { }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _logger?.LogInformation("Disposing Monday Client.");
                _graphQLHttpClient?.HttpClient?.Dispose();

                _logger?.LogInformation("Disposing GraphQL Client.");
                _graphQLHttpClient?.Dispose();
            }

            disposedValue = true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
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
    public async Task<Application.MondayResponse<T>> GetBoardItemsAsync<T>(
        ulong boardId, ColumnValue[] columnValues, int limit = 25,
        CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new MondayResponse<T>()
            {
                Errors = ["GraphQL Client Is Null."],
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilties.UnsupportedTypes)
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
            if (MondayUtilties.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
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
            await this._graphQLHttpClient.SendQueryAsync<GetBoardItemsByColumnValuesResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.ItemsPageByColumnValue?.Items?.Count > 0)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilties.GetColumnPropertyMap<T>();

            // Create New Instance Of MondayResponse.
            Application.MondayResponse<T> mondayResponse = new Application.MondayResponse<T>()
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
                    return new Application.MondayResponse<T>()
                    {
                        IsSuccessful = false,
                        Errors = ["Cancellation Requested."]
                    };
                }

                // Create New Instance Of T Type.
                T datanstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilties.TryBindColumnDataAsync(columnPropertyMap!, item!, ref datanstance))
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
        else if (graphQLResponse.Errors is not null)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
            };
        }

        return new Application.MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = []
        };
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
    public async Task<Application.MondayResponse<T>> GetBoardItemsAsync<T>(
        ulong boardId, int limit = 25, 
        CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilties.UnsupportedTypes)
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
            if (MondayUtilties.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
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
        GraphQLResponse<GetBoardItemsResponse> graphQLResponse = await this._graphQLHttpClient.SendQueryAsync<GetBoardItemsResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null
            && graphQLResponse.Data?.Boards?.Count > 0
            && graphQLResponse.Data?.Boards?.Any(x => x.ItemsCount > 0) == true)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilties.GetColumnPropertyMap<T>();

            // Create New Instance Of MondayResponse.
            Application.MondayResponse<T>? mondayResponse = new Application.MondayResponse<T>()
            {
                IsSuccessful = true,
                Cursor = graphQLResponse.Data.Boards.Select(x => x.ItemsPage!.Cursor).FirstOrDefault(),
                Response = [],
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
                if (MondayUtilties.TryBindColumnDataAsync(columnPropertyMap!, item!, ref dataInstance))
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
        else if (graphQLResponse.Errors is not null)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet(),
                Response = null
            };
        }

        return new Application.MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = [],
            Response = null
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="boardId"></param>
    /// <param name="items"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Application.MondayResponse<Dictionary<string, Item>?>> CreateBoardItemsAsync(
        ulong boardId, Item[] items, 
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null) return new Application.MondayResponse<Dictionary<string, Item>?>()
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
                throw new NullReferenceException(nameof(item.value.Name));
            }

            // Generate a unique variable name based on the item index
            string variableName = $"columnValues{item.i}";

            // Check if the column values are null
            if (item.value.ColumnValues is not null && item.value.ColumnValues is { Count: > 0})
            {
                // Add the variable to the dictionary
                variables.Add(variableName, MondayUtilties.ToColumnValuesJson(item.value.ColumnValues.Select(x => x.ColumnBaseType).ToList()!));

                // Append the parameters
                parameters.Append($"${variableName}: JSON,");

                // Append the mutation
                mutation.Append($"create_item_{item.i}: create_item(board_id: $boardId, item_name: \"{item.value.Name}\", {(item.value.Group is not null ? $"group_id: $groupId_{item.i}," : "")} column_values: ${variableName}) {RESPONSE_PARAMS}");
            }
            else
            {
                // Append the mutation
                mutation.Append($"create_item_{item.i}: create_item(board_id: $boardId, item_name: \"{item.value.Name}\", , {(item.value.Group is not null ? $"group_id: $groupId_{item.i}," : "")}) {RESPONSE_PARAMS}");
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
            await this._graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            return new Application.MondayResponse<Dictionary<string, Item>?>()
            {
                IsSuccessful = true,
                Response =
                [
                    new MondayData<Dictionary<string, Item>?>()
                    {
                        Data = graphQLResponse.Data
                    }
                ]
            };
        }

        return new Application.MondayResponse<Dictionary<string, Item>?>()
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
    public async Task<Application.MondayResponse<T>> GetBoardItemsAsync<T>(
        string? cursor, int limit = 25, CancellationToken cancellationToken = default) where T : MondayRow, new()
    {
        // If The Cursor Is Null, Return Null.
        if (cursor == null)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Cursor Is Null."]
            };
        }

        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // If The Limit Is Greater Than 500, Return Null.
        if (limit > 500)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Greater Than 500."]
            };
        }

        // If The Limit Is Less Than 0, Return Null.
        if (limit < 0)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["Limit Cannot Be Less Than 0."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilties.UnsupportedTypes)
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
            if (MondayUtilties.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
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
        GraphQLResponse<NextItemsPageResponse> graphQLResponse = await this._graphQLHttpClient.SendQueryAsync<NextItemsPageResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.NextItemsPage?.Items?.Count > 0)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilties.GetColumnPropertyMap<T>();

            // Create New Instance Of MondayResponse.
            Application.MondayResponse<T> mondayResponse = new Application.MondayResponse<T>()
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
                    return new Application.MondayResponse<T>()
                    {
                        IsSuccessful = false,
                        Errors = ["Cancellation Requested."]
                    };
                }

                // Create New Instance Of T Type.
                T dataInstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilties.TryBindColumnDataAsync(columnPropertyMap!, item!, ref dataInstance))
                {
                    mondayResponse.Response.Add(new MondayData<T>()
                    {
                        Data = dataInstance
                    });
                }
            }

            return mondayResponse;
        }
        else if (graphQLResponse.Errors is not null)
        {
            return new Application.MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
            };
        }

        return new Application.MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = []
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="updates"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public async Task<Application.MondayResponse<Dictionary<string, Update>?>> CreateItemsUpdateAsync(
        Update[] updates,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null) return new Application.MondayResponse<Dictionary<string, Update>?>()
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
            if (update.value.Id.GetValueOrDefault() == 0)
            {
                throw new NullReferenceException(nameof(update.value.Id));
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
                mutation.Append($"create_update_{update.i}: create_update(item_id: {update.value.Id}, body: ${variableName}) {RESPONSE_PARAMS}");
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
            await this._graphQLHttpClient.SendMutationAsync<Dictionary<string, Update>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            return new Application.MondayResponse<Dictionary<string, Update>?>()
            {
                IsSuccessful = true,
                Response =
                [
                    new MondayData<Dictionary<string, Update>?>()
                    {
                        Data = graphQLResponse.Data
                    }
                ]
            };
        }

        // Return Null.
        return new Application.MondayResponse<Dictionary<string, Update>?>()
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
    public async Task<Application.MondayResponse<Dictionary<string, Item>?>> DeleteItemsAsync(
        Item[] items,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null) return new Application.MondayResponse<Dictionary<string, Item>?>()
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
            await this._graphQLHttpClient.SendMutationAsync<Dictionary<string, Item>>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data != null)
        {
            return new Application.MondayResponse<Dictionary<string, Item>?>()
            {
                IsSuccessful = true,
                Response = [
                    new MondayData<Dictionary<string, Item>?>()
                    {
                        Data = graphQLResponse.Data
                    }
                ]
            };
        }
        else if (graphQLResponse.Errors is not null)
        {
            return new Application.MondayResponse<Dictionary<string, Item>?>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
            };
        }

        // Return Null.
        return new Application.MondayResponse<Dictionary<string, Item>?>()
        {
            IsSuccessful = false,
            Errors = []
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="limit"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Application.MondayResponse<List<Board>>> GetBoardsAsync(
        ulong[]? boardIds = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new Application.MondayResponse<List<Board>>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // Create The Response Parameters.
        const string RESPONSE_PARAMS = @$"{{id name state board_kind board_folder_id description workspace_id item_terminology items_count permissions}}";

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
            boards(limit: $limit{((boardIds is not null && boardIds.Length > 0) ? ",ids: $boardIds" : "")})  
            {RESPONSE_PARAMS}
            }}",
            Variables = variables
        };

        // Execute The Query.
        GraphQLResponse<GetBoardsResponse> graphQLResponse =
            await this._graphQLHttpClient.SendQueryAsync<GetBoardsResponse>(keyValuePairs, cancellationToken);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.Boards?.Count > 0)
        {
            return new Application.MondayResponse<List<Board>>()
            {
                IsSuccessful = true,
                Response = [
                    new MondayData<List<Board>>()
                    {
                        Data = graphQLResponse.Data.Boards
                    }
                ]
            };
        }
        else if (graphQLResponse.Errors is not null)
        {
            return new Application.MondayResponse<List<Board>>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
            };
        }

        // Return Null.
        return new Application.MondayResponse<List<Board>>()
        {
            IsSuccessful = false,
            Errors = []
        };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="updates"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Application.MondayResponse<Dictionary<string, Asset>>> UploadFileToUpdateAsync(
        Update[] updates,
        CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new Application.MondayResponse<Dictionary<string, Asset>>()
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
            if (update.value.Id.GetValueOrDefault() == 0)
            {
                return new Application.MondayResponse<Dictionary<string, Asset>>()
                {
                    IsSuccessful = false,
                    Errors = ["Update Id Is Null."]
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
                mutation.Append($"add_file_to_update_{update.i}: add_file_to_update(update_id: $updateId{update.i}, file: ${variableName}) {RESPONSE_PARAMS}");

                // Add ByteArrayContent to the dictionary
                multipartFormDataContent.Add(update.value.FileUpload.StreamContent, $"variables[{variableName}]", update.value.FileUpload.FileName);

                // Add the update id to the dictionary
                multipartFormDataContent.Add(new StringContent(update.value.Id.GetValueOrDefault().ToString()), $"variables[updateId{update.i}]");
            }
        }

        // Create the query
        string query = $"mutation ({parameters}){{{mutation}}}";

        // Add the query to the dictionary
        multipartFormDataContent.Add(new StringContent(query), "query");

        // Execute The Query.
        using HttpResponseMessage httpResponseMessage =
            await _graphQLHttpClient.HttpClient.PostAsync($"{this._mondayOptions!.EndPoint}/file", multipartFormDataContent, cancellationToken);

        // Read the response
        string rawResponse = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);
        
        // Parse the response
        using JsonDocument jsonDocument = JsonDocument.Parse(rawResponse);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (httpResponseMessage.IsSuccessStatusCode 
            && jsonDocument.RootElement.TryGetProperty("data", out JsonElement data))
        {
            Application.MondayResponse<Dictionary<string, Asset>> response = new()
            {
                IsSuccessful = true,
                Response = []
            };

            // Loop through each property
            foreach (JsonProperty property in data.EnumerateObject())
            {
                // Check if the property is an object
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    // Converstion was created with the Newtonsoft
                    Asset? asset = Newtonsoft.Json.JsonConvert.DeserializeObject<Asset>(property.Value.GetRawText());

                    // Check if the asset is not null
                    if (asset is not null)
                    {
                        response.Response.Add(new MondayData<Dictionary<string, Asset>>()
                        {
                            Data = new Dictionary<string, Asset>()
                            {
                                { property.Name, asset }
                            }  
                        });
                    }
                }
            }

            return response;
        }
        else
        {
            // Create the response
            Application.MondayResponse<Dictionary<string, Asset>> response = new()
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

    public async Task<Application.MondayResponse<Dictionary<string, Asset>>> UploadFileToColumnAsync(
        Item[] items, CancellationToken cancellationToken = default)
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new Application.MondayResponse<Dictionary<string, Asset>>()
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
                return new Application.MondayResponse<Dictionary<string, Asset>>()
                {
                    IsSuccessful = false,
                    Errors = ["Item Id Is Null."]
                };
            }

            // Check if there is a file upload
            if (item.value.FileUpload is null)
            {
                return new Application.MondayResponse<Dictionary<string, Asset>>()
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
                mutation.Append($"add_file_to_column_{item.i}: add_file_to_column(item_id: $itemId{item.i}, column_id: \"{item.value.FileUpload.ColumnId}\", file: ${variableName}) {RESPONSE_PARAMS}");

                // Add ByteArrayContent to the dictionary
                multipartFormDataContent.Add(item.value.FileUpload.StreamContent, $"variables[{variableName}]", item.value.FileUpload.FileName);

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
            $"{this._mondayOptions!.EndPoint}/file", multipartFormDataContent, cancellationToken);

        // Read the response
        string rawResponse = await httpResponseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Parse the response
        using JsonDocument jsonDocument = JsonDocument.Parse(rawResponse);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (httpResponseMessage.IsSuccessStatusCode && jsonDocument.RootElement.TryGetProperty("data", out JsonElement data))
        {
            Application.MondayResponse<Dictionary<string, Asset>> response = new()
            {
                IsSuccessful = true,
                Response = []
            };

            // Loop through each property
            foreach (JsonProperty property in data.EnumerateObject())
            {
                // Check if the property is an object
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    // Converstion was created with the Newtonsoft
                    Asset? asset = Newtonsoft.Json.JsonConvert.DeserializeObject<Asset>(property.Value.GetRawText());

                    // Check if the asset is not null
                    if (asset is not null)
                    {
                        // Add the asset to the response
                        response.Response?.Add(new MondayData<Dictionary<string, Asset>>()
                        {
                            Data = new Dictionary<string, Asset>()
                            {
                                { property.Name, asset }
                            }
                        });
                    }
                }
            }
            return response;
        }
        else
        {
            // Create the response
            Application.MondayResponse<Dictionary<string, Asset>> response = new()
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
    public async Task<MondayResponse<T>> GetBoardItemAsync<T>(ulong itemId) where T : MondayRow, new()
    {
        // If The GraphQL Client Is Null, Return Null.
        if (this._graphQLHttpClient == null)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = ["GraphQL Client Is Null."]
            };
        }

        // Create New Instance Of T Type.
        T instance = Activator.CreateInstance<T>();

        // Check for multiple properties of the same type
        foreach (KeyValuePair<Type, string> unSupportedType in MondayUtilties.UnsupportedTypes)
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
            if (MondayUtilties.GetItemsQueryBuilder.TryGetValue(propertyInfo.PropertyType, out string? query))
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
        GraphQLResponse<ItemsPageByColumnValue> graphQLResponse = await this._graphQLHttpClient.SendQueryAsync<ItemsPageByColumnValue>(keyValuePairs);

        // If The Response Is Not Null, And The Data Is Not Null, And The Errors Is Null, Return The Data.
        if (graphQLResponse.Errors is null && graphQLResponse.Data?.Items?.Count > 0)
        {
            // Get The Column Property Map.
            Dictionary<string, string> columnPropertyMap = MondayUtilties.GetColumnPropertyMap<T>();

            // Loop through each item
            foreach (Item item in graphQLResponse.Data.Items)
            {
                // Create New Instance Of T Type.
                T dataInstance = Activator.CreateInstance<T>();

                // Attempt To Bind The Items.
                if (MondayUtilties.TryBindColumnDataAsync(columnPropertyMap!, item, ref dataInstance))
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
        else if (graphQLResponse.Errors is not null)
        {
            return new MondayResponse<T>()
            {
                IsSuccessful = false,
                Errors = graphQLResponse.Errors?.Select(x => x.Message).ToHashSet()
            };
        }

        return new MondayResponse<T>()
        {
            IsSuccessful = false,
            Errors = []
        };
    }
}
