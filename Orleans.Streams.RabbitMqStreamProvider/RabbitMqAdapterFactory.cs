﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streaming.RabbitMq.Configuration;
using Orleans.Streaming.RabbitMq.RabbitMq;
using Orleans.Streams;

namespace Orleans.Streaming.RabbitMq
{
    public class RabbitMqAdapterFactory : IQueueAdapterFactory
    {
        private readonly IQueueAdapterCache _cache;
        private readonly IStreamQueueMapper _mapper;
        private readonly Task<IStreamFailureHandler> _failureHandler;
        private readonly IQueueAdapter _adapter;
        
        public RabbitMqAdapterFactory(
            string providerName,
            IOptionsMonitor<RabbitMqOptions> rmqOptionsAccessor,
            IOptionsMonitor<SimpleQueueCacheOptions> cachingOptionsAccessor,
            IServiceProvider serviceProvider,
            ILoggerFactory loggerFactory,
            IRabbitMqStreamQueueMapperFactory streamQueueMapperFactory)
        {

            if (string.IsNullOrEmpty(providerName)) throw new ArgumentNullException(nameof(providerName));
            if (rmqOptionsAccessor == null) throw new ArgumentNullException(nameof(rmqOptionsAccessor));
            if (cachingOptionsAccessor == null) throw new ArgumentNullException(nameof(cachingOptionsAccessor));
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            if (streamQueueMapperFactory == null) throw new ArgumentNullException(nameof(streamQueueMapperFactory));

            var rmqOptions = rmqOptionsAccessor.Get(providerName); 
            var cachingOptions = cachingOptionsAccessor.Get(providerName);

            _cache = new SimpleQueueAdapterCache(cachingOptions, providerName, loggerFactory);
            _mapper = streamQueueMapperFactory.Get(providerName);
            _failureHandler = Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler(false));

            var dataAdapter = serviceProvider.GetServiceByName<IQueueDataAdapter<RabbitMqMessage, IEnumerable<IBatchContainer>>>(providerName) ??
                    RabbitMqDataAdapter.Create(serviceProvider, providerName);

            _adapter = new RabbitMqAdapter(rmqOptions, dataAdapter, providerName, _mapper, serviceProvider.GetRequiredServiceByName<IRabbitMqConnectorFactory>(providerName));
        }

        public Task<IQueueAdapter> CreateAdapter() => Task.FromResult(_adapter);
        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => _failureHandler;
        public IQueueAdapterCache GetQueueAdapterCache() => _cache;
        public IStreamQueueMapper GetStreamQueueMapper() => _mapper;

        public static RabbitMqAdapterFactory Create(IServiceProvider services, string name)
            => ActivatorUtilities.CreateInstance<RabbitMqAdapterFactory>(
                services,
                name);
    }
}