using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using winC2D.Mcp.Protocol;

namespace winC2D.Mcp;

/// <summary>
/// MCP (Model Context Protocol) server using JSON-RPC 2.0 over standard I/O.
/// Handles the initialize → tools/list → tools/call lifecycle that AI agents
/// use to discover and invoke winC2D migration capabilities.
/// </summary>
public sealed class McpServer : IDisposable
{
    private readonly IServiceProvider _services;
    private readonly McpToolRegistry _toolRegistry;
    private readonly ILogger<McpServer> _logger;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _initialized;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    public McpServer(
        IServiceProvider services,
        McpToolRegistry toolRegistry,
        ILogger<McpServer> logger)
        : this(services, toolRegistry, logger, Console.In, Console.Out)
    {
    }

    internal McpServer(
        IServiceProvider services,
        McpToolRegistry toolRegistry,
        ILogger<McpServer> logger,
        TextReader input,
        TextWriter output)
    {
        _services = services;
        _toolRegistry = toolRegistry;
        _logger = logger;
        _input = input;
        _output = output;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Run the MCP server loop. Reads JSON-RPC messages line-by-line from stdin,
    /// processes them, and writes responses to stdout. Stderr is reserved for
    /// diagnostic logging only (MCP protocol requirement).
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("winC2D MCP server starting on stdio transport");

        string? line;
        while ((line = await _input.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var response = await ProcessMessageAsync(line, cancellationToken);
                if (response != null)
                {
                    var json = JsonSerializer.Serialize(response, WriteOptions);
                    await _output.WriteLineAsync(json);
                    await _output.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error processing MCP message");
                var errorResponse = new JsonRpcResponse
                {
                    JsonRpc = "2.0",
                    Id = null,
                    Error = new JsonRpcError
                    {
                        Code = JsonRpcErrorCodes.InternalError,
                        Message = $"Internal error: {ex.Message}"
                    }
                };
                var json = JsonSerializer.Serialize(errorResponse, WriteOptions);
                await _output.WriteLineAsync(json);
                await _output.FlushAsync(cancellationToken);
            }
        }

        _logger.LogInformation("winC2D MCP server stopped");
    }

    private async Task<JsonRpcResponse?> ProcessMessageAsync(string raw, CancellationToken ct)
    {
        JsonRpcRequest request;
        try
        {
            request = JsonSerializer.Deserialize<JsonRpcRequest>(raw, _jsonOptions)
                ?? throw new JsonException("Null request");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON-RPC request");
            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = null,
                Error = new JsonRpcError
                {
                    Code = JsonRpcErrorCodes.ParseError,
                    Message = $"Parse error: {ex.Message}"
                }
            };
        }

        // Notifications (no id) don't get a response
        if (request.Id == null)
        {
            await HandleNotificationAsync(request, ct);
            return null;
        }

        try
        {
            var result = await HandleRequestAsync(request, ct);
            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Result = result
            };
        }
        catch (McpMethodException ex)
        {
            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = ex.Code,
                    Message = ex.Message
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling method: {Method}", request.Method);
            return new JsonRpcResponse
            {
                JsonRpc = "2.0",
                Id = request.Id,
                Error = new JsonRpcError
                {
                    Code = JsonRpcErrorCodes.InternalError,
                    Message = $"Internal error: {ex.Message}"
                }
            };
        }
    }

    private async Task<JsonElement> HandleRequestAsync(JsonRpcRequest request, CancellationToken ct)
    {
        switch (request.Method)
        {
            case "initialize":
                if (_initialized)
                    throw new McpMethodException(JsonRpcErrorCodes.InvalidRequest, "Server is already initialized");
                _initialized = true;
                return await HandleInitializeAsync(request.Params);

            case "notifications/initialized":
                // Acknowledge initialization completion (no-op)
                return JsonSerializer.SerializeToElement(new { }, WriteOptions);

            case "tools/list":
                EnsureInitialized();
                return await HandleToolsListAsync();

            case "tools/call":
                EnsureInitialized();
                return await HandleToolsCallAsync(request.Params, ct);

            case "ping":
                return JsonSerializer.SerializeToElement(new { }, WriteOptions);

            default:
                throw new McpMethodException(JsonRpcErrorCodes.MethodNotFound,
                    $"Method not found: {request.Method}");
        }
    }

    private Task HandleNotificationAsync(JsonRpcRequest request, CancellationToken ct)
    {
        switch (request.Method)
        {
            case "notifications/initialized":
                _logger.LogInformation("Client confirmed initialization");
                break;
            case "notifications/cancelled":
                _logger.LogInformation("Client sent cancellation notification");
                break;
            default:
                _logger.LogDebug("Ignoring unknown notification: {Method}", request.Method);
                break;
        }
        return Task.CompletedTask;
    }

    private static Task<JsonElement> HandleInitializeAsync(JsonElement? @params)
    {
        var initResult = new InitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities
            {
                Tools = new ToolsCapability { ListChanged = false }
            },
            ServerInfo = new ImplementationInfo
            {
                Name = "winC2D",
                Version = "1.0.0"
            }
        };

        var json = JsonSerializer.SerializeToElement(initResult, WriteOptions);
        return Task.FromResult(json);
    }

    private Task<JsonElement> HandleToolsListAsync()
    {
        var tools = _toolRegistry.GetTools();
        var result = new ListToolsResult { Tools = tools };
        var json = JsonSerializer.SerializeToElement(result, WriteOptions);
        return Task.FromResult(json);
    }

    private async Task<JsonElement> HandleToolsCallAsync(JsonElement? @params, CancellationToken ct)
    {
        if (@params == null)
            throw new McpMethodException(JsonRpcErrorCodes.InvalidParams, "Missing params");

        var callParams = JsonSerializer.Deserialize<ToolCallParams>(
            @params.Value.GetRawText(), _jsonOptions)
            ?? throw new McpMethodException(JsonRpcErrorCodes.InvalidParams, "Invalid tool call params");

        if (string.IsNullOrWhiteSpace(callParams.Name))
            throw new McpMethodException(JsonRpcErrorCodes.InvalidParams, "Tool name is required");

        var result = await _toolRegistry.CallToolAsync(callParams.Name, callParams.Arguments, ct);
        var json = JsonSerializer.SerializeToElement(result, WriteOptions);
        return json;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
            throw new McpMethodException(JsonRpcErrorCodes.InvalidRequest,
                "Server not initialized. Send 'initialize' first.");
    }

    public void Dispose()
    {
        // Stdio streams are owned by Console; don't dispose
    }
}

/// <summary>
/// Exception for MCP protocol-level errors with JSON-RPC error codes.
/// </summary>
public sealed class McpMethodException : Exception
{
    public int Code { get; }

    public McpMethodException(int code, string message) : base(message)
    {
        Code = code;
    }
}
