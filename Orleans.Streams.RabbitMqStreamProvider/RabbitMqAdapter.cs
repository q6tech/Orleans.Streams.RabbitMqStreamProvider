﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans.Streaming.RabbitMq.Configuration;
using Orleans.Streaming.RabbitMq.RabbitMq;
using Orleans.Streams;

namespace Orleans.Streaming.RabbitMq
{
    /// <summary>
    /// For RMQ client, it is necessary the Model (channel) is not accessed by multiple threads at once, because with each such access,
    /// the channel gets closed - this is a limitation of RMQ client, which unfortunately causes message loss.
    /// Here we handle it by creating new connection for each receiver which guarantees no overlapping calls from different threads.
    /// The real issue comes in publishing - here we need to identify the connections by thread!
    /// Otherwise it would cause a lot of trouble when publishing messages from StatelessWorkers which can run in parallel, thus
    /// overlapping calls from different threads would occur frequently.
    /// </summary>
    internal class RabbitMqAdapter : IQueueAdapter
    {
        private readonly IQueueDataAdapter<RabbitMqMessage, IEnumerable<IBatchContainer>> _dataAdapter;
        private readonly IStreamQueueMapper _mapper;
        private readonly ThreadLocal<IRabbitMqProducer> _producer; 
        private readonly IRabbitMqConnectorFactory _rmqConnectorFactory;
        private readonly RabbitMqOptions _rmqOptions;

        public RabbitMqAdapter(RabbitMqOptions rmqOptions, IQueueDataAdapter<RabbitMqMessage, IEnumerable<IBatchContainer>> dataAdapter, string providerName, IStreamQueueMapper mapper, IRabbitMqConnectorFactory rmqConnectorFactory)
        {
            _dataAdapter = dataAdapter;
            Name = providerName;
            _mapper = mapper;
            _rmqConnectorFactory = rmqConnectorFactory;
            _rmqOptions = rmqOptions;
            _producer = new ThreadLocal<IRabbitMqProducer>(() => _rmqConnectorFactory.CreateProducer());
        }

        public string Name { get; }
        public bool IsRewindable => false;
        public StreamProviderDirection Direction => _rmqOptions.Direction;
        public IQueueAdapterReceiver CreateReceiver(QueueId queueId) => new RabbitMqAdapterReceiver(_rmqConnectorFactory, queueId, _mapper, _dataAdapter, _rmqOptions);

        public async Task QueueMessageBatchAsync<T>(Guid streamGuid, string streamNamespace, IEnumerable<T> events, StreamSequenceToken token, Dictionary<string, object> requestContext)
        {
            if (token != null) throw new ArgumentException("RabbitMq stream provider does not support non-null StreamSequenceToken.", nameof(token));

            RabbitMqMessage message = _dataAdapter.ToQueueMessage(streamGuid, streamNamespace, events, token, requestContext);

            await _producer.Value.SendAsync(message);
        }
    }
}