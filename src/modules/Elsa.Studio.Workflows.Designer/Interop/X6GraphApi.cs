using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Elsa.Studio.Workflows.Designer.Interop;

/// <summary>
/// Provides a wrapper around the X6 graph API.
/// </summary>
public class X6GraphApi
{
    private readonly IJSObjectReference _module;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _containerId;

    /// <summary>
    /// Initializes a new instance of the <see cref="X6GraphApi"/> class.
    /// </summary>
    /// <param name="module">The JavaScript module reference.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="containerId">The ID of the container element.</param>
    public X6GraphApi(IJSObjectReference module, IServiceProvider serviceProvider, string containerId)
    {
        _module = module;
        _serviceProvider = serviceProvider;
        _containerId = containerId;
    }
    
    /// <summary>
    /// Reads the flowchart from the graph.
    /// </summary>
    /// <returns>The flowchart.</returns>
    public async Task<JsonElement> ReadGraphAsync() => await InvokeAsync(module => module.InvokeAsync<JsonElement>("readGraph", _containerId));
    
    /// <summary>
    /// Disposes the graph.
    /// </summary>
    public async Task DisposeGraphAsync() => await TryInvokeAsync(module => module.InvokeVoidAsync("disposeGraph", _containerId));
    
    
    /// <summary>
    /// Sets the grid color.
    /// </summary>
    /// <param name="color">The color.</param>
    public async Task SetGridColorAsync(string color) => await InvokeAsync(module => module.InvokeVoidAsync("setGridColor", _containerId, color));
    
    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="node">The node.</param>
    public async Task AddActivityNodeAsync(X6ActivityNode node)
    {
        var serializerOptions = GetSerializerOptions();
        var nodeElement = JsonSerializer.SerializeToElement(node, serializerOptions);
        
        await InvokeAsync(module => module.InvokeVoidAsync("addActivityNode", _containerId, nodeElement));
    }
    
    /// <summary>
    /// Adds the specified activity nodes and edges to the graph.
    /// </summary>
    /// <param name="activityNodes">The activity nodes.</param>
    /// <param name="edges">The edges.</param>
    public async Task PasteCellsAsync(IEnumerable<X6ActivityNode> activityNodes, X6Edge[] edges)
    {
        var serializerOptions = GetSerializerOptions();
        var activityNodeElements = JsonSerializer.SerializeToElement(activityNodes, serializerOptions);
        var edgeElements = JsonSerializer.SerializeToElement(edges, serializerOptions);
        await InvokeAsync(module => module.InvokeVoidAsync("pasteCells", _containerId, activityNodeElements, edgeElements));
    }

    /// <summary>
    /// Loads the specified model into the graph.
    /// </summary>
    /// <param name="graph">The model.</param>
    public async Task LoadGraphAsync(X6Graph graph)
    {
        await InvokeAsync(module => module.InvokeVoidAsync("loadGraph", _containerId, graph));
    }

    /// <summary>
    /// Zoom the canvas to fit the content.
    /// </summary>
    public async Task ZoomToFitAsync() => await InvokeAsync(module => module.InvokeVoidAsync("zoomToFit", _containerId));
    
    /// <summary>
    /// Center the canvas content.
    /// </summary>
    public async Task CenterContentAsync() => await InvokeAsync(module => module.InvokeVoidAsync("centerContent", _containerId));
    
    /// <summary>
    /// Updates the node with the specified activity. 
    /// </summary>
    /// <param name="id">The activity ID.</param>
    /// <param name="activity">The activity.</param>
    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        var serializerOptions = GetSerializerOptions();
        var mapperFactory = _serviceProvider.GetRequiredService<IMapperFactory>();
        var activityMapper = await mapperFactory.CreateActivityMapperAsync();
        var ports = activityMapper.GetPorts(activity);
        var activityElement = JsonSerializer.SerializeToElement(activity, serializerOptions);
        await InvokeAsync(module => module.InvokeVoidAsync("updateActivity", _containerId, id, activityElement, ports));
    }

    private async Task InvokeAsync(Func<IJSObjectReference, ValueTask> func) => await func(_module);
    
    private async Task TryInvokeAsync(Func<IJSObjectReference, ValueTask> func)
    {
        try
        {
            await func(_module);
        }
        catch (JSDisconnectedException)
        {
            // Ignore.
        }
    }

    private async Task<T> InvokeAsync<T>(Func<IJSObjectReference, ValueTask<T>> func) => await func(_module);
    
    private JsonSerializerOptions GetSerializerOptions()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        serializerOptions.Converters.Add(new ExpressionJsonConverterFactory());
        serializerOptions.Converters.Add(new JsonStringEnumConverter());
        return serializerOptions;
    }
}