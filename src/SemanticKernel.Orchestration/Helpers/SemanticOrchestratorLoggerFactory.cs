using System;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Orchestration.Helpers;

public static class SemanticOrchestratorLoggerFactory
{
    private static ILoggerFactory? _factory;

    public static void Init(ILoggerFactory loggerFactory) 
    {
        _factory = loggerFactory;
    }

    public static ILogger<T> Create<T>() 
    {
        if (_factory == null)
        {
            throw new InvalidOperationException("SemanticOrchestratorLoggerFactory has not been initialized.");
        }

        return _factory.CreateLogger<T>();
    }

    public static ILogger Create(Type type) 
    {
        if (_factory == null)
        {
            throw new InvalidOperationException("SemanticOrchestratorLoggerFactory has not been initialized.");
        }

        return _factory.CreateLogger(type);
    }
}
