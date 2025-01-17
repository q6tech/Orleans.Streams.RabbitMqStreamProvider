﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orleans.Providers.Streams.Common;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Streaming.RabbitMq.BatchContainer
{
    [Serializable]
    public class RabbitMqBatchContainer : IBatchContainer
    {
        private readonly List<object> _events;
        private readonly Dictionary<string, object> _requestContext;

        public EventSequenceToken EventSequenceToken { set; private get; }
        public StreamSequenceToken SequenceToken => EventSequenceToken;
        public Guid StreamGuid { get; }
        public string StreamNamespace { get; }

        public RabbitMqBatchContainer(Guid streamGuid, string streamNamespace, IEnumerable<object> events, Dictionary<string, object> requestContext = null)
        {
            StreamGuid = streamGuid;
            StreamNamespace = streamNamespace;
            _events = events.ToList();
            _requestContext = requestContext;
        }

        public bool ImportRequestContext()
        {
            if (_requestContext == null) return false;
            RequestContextExtensions.Import(_requestContext);
            return true;
        }

        public IEnumerable<Tuple<T, StreamSequenceToken>> GetEvents<T>()
        => _events
                .OfType<T>()
                .Select((e, i) => Tuple.Create<T, StreamSequenceToken>(e, EventSequenceToken?.CreateSequenceTokenForEvent(i)))
                .ToList();

        public bool ShouldDeliver(IStreamIdentity stream, object filterData, StreamFilterPredicate shouldReceiveFunc)
            => _events.Any(item => shouldReceiveFunc(stream, filterData, item));
    }
}