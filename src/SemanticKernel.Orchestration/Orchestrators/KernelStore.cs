using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel;
using SemanticKernel.Orchestration.Helpers;

namespace SemanticKernel.Orchestration.Orchestrators;

/// <summary>
/// Contains information about a Kernel object of Semantic Kernel
/// </summary>
public class KernelInfo
{
    private Kernel? _kernel;

    public KernelInfo(IKernelBuilder builder, ModelInformation modelName, string description, string name)
    {
        Builder = builder;
        ModelInformation = modelName;
        Description = description;
        Name = name;
    }

    public IKernelBuilder Builder { get; }
    public ModelInformation ModelInformation { get; }
    public string Description { get; }
    public string Name { get; }

    /// <summary>
    /// Lazy build of the kernel object
    /// </summary>
    public Kernel Kernel => _kernel ??= Builder.Build();
}

public class KernelStore
{
    private readonly Dictionary<string, KernelInfo> _kernels = new();
    private readonly IServiceProvider _serviceProvider;
    private static AsyncLocal<ConversationContext?> _currentConversationContext = new();

    public KernelStore(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void AddKernel(
        string name,
        IKernelBuilder kernelBuilder,
        ModelInformation modelName,
        string description)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        _kernels[name] = new KernelInfo(kernelBuilder, modelName, description, name);
    }

    public bool TryGetKernel(string name, out Kernel? kernel)
    {
        if (_kernels.TryGetValue(name, out var info))
        {
            kernel = info.Kernel;
        }
        else
        {
            kernel = null;
        }
        return kernel != null;
    }

    private KernelInfo GetKernelInfo(string name)
    {
        if (!_kernels.TryGetValue(name, out var info))
        {
            throw new KeyNotFoundException($"Kernel '{name}' not found");
        }
        return info;
    }

    public Kernel GetKernel(string name)
    {
        return GetKernelInfo(name).Kernel;
    }

    public TiktokenTokenizer GetKernelTokenizer(string name)
    {
        return GetKernelInfo(name).ModelInformation.Tokenizer;
    }

    public void AddPlugin(string kernelName, object plugin)
    {
        if (!_kernels.TryGetValue(kernelName, out var kernel))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernel.Builder.Plugins.AddFromObject(plugin);
    }

    public void AddInterceptor<T>(string kernelName) where T : class, IChatInterceptorTool
    {
        if (!_kernels.TryGetValue(kernelName, out var kernelInfo))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernelInfo.Builder.Services.WithInterceptorTransient<T>();
    }

    public void AddWrapper<T>(string kernelName) where T : class, IChatWrappingTool
    {
        if (!_kernels.TryGetValue(kernelName, out var kernelInfo))
            throw new KeyNotFoundException($"Kernel '{kernelName}' not found");

        kernelInfo.Builder.Services.WithWrapperTransient<T>();
    }

    public void AddGlobalInterceptor<T>() where T : class, IChatInterceptorTool
    {
        foreach (var kernelInfo in _kernels.Values)
        {
            kernelInfo.Builder.Services.WithInterceptorTransient<T>();
        }
    }

    public void AddGlobalWrapper<T>() where T : class, IChatWrappingTool
    {
        foreach (var kernelInfo in _kernels.Values)
        {
            kernelInfo.Builder.Services.WithWrapperTransient<T>();
        }
    }

    public IEnumerable<KernelInfo> GetAvailableKernels(string excludeKernel = null)
    {
        return _kernels
            .Where(k => k.Key != excludeKernel)
            .Select(k => k.Value);
    }

    public void EnableInterception()
    {
        foreach (var kernelInfo in _kernels.Values)
        {
            kernelInfo.Builder.EnableInterception();
        }
    }

    /// <summary>
    /// Create a container scope that will be used to create a discussion with the orchestrator
    /// that continue on the same thread scope. This is useful only for directed connected client
    /// where we enter in a simple loop of question/answer until we exit.
    /// </summary>
    /// <returns></returns>
    public ConversationContext StartConversationContext()
    {
        var interceptors = _serviceProvider.GetServices<IChatInterceptorTool>().ToArray();
        var wrappers = _serviceProvider.GetServices<IChatWrappingTool>().ToArray();

        var container = new ConversationContext(interceptors, wrappers);
        _currentConversationContext.Value = container;
        return container;
    }

    /// <summary>
    /// When we have a disconnected client we usually store the orchestrator 
    /// somewhere then we do not know when another request will come, in this
    /// situation we usually store the interceptor container somewhere and we
    /// will dispose when the communication ends.
    /// </summary>
    /// <param name="container"></param>
    public void SetConversationContext(ConversationContext container)
    {
        _currentConversationContext.Value = container;
    }

    public static ConversationContext? GetActiveConversationContext()
    {
        return _currentConversationContext.Value;
    }

    internal static void ClearConversationContext()
    {
        _currentConversationContext.Value = null;
    }

    public static void SetProperty(string propertyName, object value)
    {
        var container = _currentConversationContext.Value;
        if (container == null)
        {
            //TODO: Log
            return;
        }

        container.Properties[propertyName] = value;
    }

    public static IReadOnlyCollection<(string Key, T Value)> GetAllPropertyValues<T>() where T : class
    {
        var container = _currentConversationContext.Value;
        if (container == null)
        {
            return Array.Empty<(string, T)>();
        }

        return container.Properties
            .Where(kvp => kvp.Value is T)
            .Select(kvp => (kvp.Key, (T) kvp.Value))
            .ToArray();
    }

    public T? GetInterceptor<T>() where T : class
    {
        var container = _currentConversationContext.Value;
        if (container == null)
        {
            return null;
        }

        return container.Interceptors.OfType<T>().FirstOrDefault()
            ?? container.Wrappers.OfType<T>().FirstOrDefault();
    }
}
