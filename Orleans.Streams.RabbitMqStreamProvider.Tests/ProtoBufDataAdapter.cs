using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Streaming.RabbitMq;
using Orleans.Streaming.RabbitMq.BatchContainer;
using Orleans.Streaming.RabbitMq.RabbitMq;
using ProtoBuf;

namespace Orleans.Streams.RabbitMqStreamProvider.Tests
{
    public class ProtoBufDataAdapter : RabbitMqDataAdapterBase
    {
        public ProtoBufDataAdapter(IStreamQueueMapper mapper, ITopologyProvider topologyProvider) : base(mapper, topologyProvider)
        {
        }

        protected override byte[] Serialize(RabbitMqBatchContainer container)
        {
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, container.GetEvents<Message>().Single().Item1);
                return ms.ToArray();
            }
        }

        protected override RabbitMqBatchContainer Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var notification = Serializer.Deserialize<Message>(ms);
                return new RabbitMqBatchContainer(
                    Guid.NewGuid(),
                    Globals.StreamNameSpaceProtoBuf,
                    new List<object> { notification },
                    new Dictionary<string, object>());
            }
        }

        public static ProtoBufDataAdapter Create(IServiceProvider services, string name)
        {
            var topologyFactory = services.GetRequiredService<ITopologyProviderFactory>();
            var mapperFactory = services.GetRequiredService<IRabbitMqStreamQueueMapperFactory>();
            return new ProtoBufDataAdapter(mapperFactory.Get(name), topologyFactory.Get(name));
        }

    }
}
