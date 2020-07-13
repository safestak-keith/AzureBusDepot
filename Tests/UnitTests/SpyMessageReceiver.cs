using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AzureBusDepot.Abstractions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;

namespace AzureBusDepot.UnitTests
{
    /// <summary>
    /// Basic spy required to invoke the Handler callback and track its own internal invokations
    /// </summary>
    public class SpyMessageReceiver : IMessageReceiver
    {
        public Func<Message, CancellationToken, Task> Handler { get; internal set; }
        public Func<ExceptionReceivedEventArgs, Task> ExceptionReceivedHandler { get; internal set; }
        public MessageHandlerOptions MessageHandlerOptions { get; internal set; }
        public bool RegisterMessageHandlerCalled { get; internal set; }
        public bool CloseAsyncCalled { get; internal set; }
        public bool CompleteAsyncCalled { get; internal set; }
        public bool AbandonAsyncCalled { get; internal set; }
        public bool DeadLetterAsyncCalled { get; internal set; }

        public SpyMessageReceiver(IEndpointHandlingConfig config)
        {
            ServiceBusConnection = new ServiceBusConnection(config.ConnectionString);
            Path = config.EntityPath;
        }

        public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, Func<ExceptionReceivedEventArgs, Task> exceptionReceivedHandler)
        {
            RegisterMessageHandler(handler, new MessageHandlerOptions(exceptionReceivedHandler));
        }

        public void RegisterMessageHandler(Func<Message, CancellationToken, Task> handler, MessageHandlerOptions messageHandlerOptions)
        {
            RegisterMessageHandlerCalled = true;
            MessageHandlerOptions = messageHandlerOptions;
            Handler = handler;
            ExceptionReceivedHandler = messageHandlerOptions.ExceptionReceivedHandler;
        }

        public Task CompleteAsync(string lockToken)
        {
            CompleteAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task AbandonAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
        {
            AbandonAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
        {
            DeadLetterAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task DeadLetterAsync(string lockToken, string deadLetterReason, string deadLetterErrorDescription = null)
        {
            DeadLetterAsyncCalled = true;
            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            CloseAsyncCalled = true;
            return Task.CompletedTask;
        }

        public string ClientId { get; }
        public bool IsClosedOrClosing { get; }
        public string Path { get; }
        public TimeSpan OperationTimeout { get; set; }
        public ServiceBusConnection ServiceBusConnection { get; }
        public bool OwnsConnection { get; }
        public IList<ServiceBusPlugin> RegisteredPlugins { get; }
        public int PrefetchCount { get; set; }
        public ReceiveMode ReceiveMode { get; }
        public long LastPeekedSequenceNumber { get; }

        public void RegisterPlugin(ServiceBusPlugin serviceBusPlugin)
        {
            throw new NotImplementedException();
        }

        public void UnregisterPlugin(string serviceBusPluginName)
        {
            throw new NotImplementedException();
        }

        public Task<Message> ReceiveAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Message> ReceiveAsync(TimeSpan operationTimeout)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Message>> ReceiveAsync(int maxMessageCount)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Message>> ReceiveAsync(int maxMessageCount, TimeSpan operationTimeout)
        {
            throw new NotImplementedException();
        }

        public Task<Message> ReceiveDeferredMessageAsync(long sequenceNumber)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Message>> ReceiveDeferredMessageAsync(IEnumerable<long> sequenceNumbers)
        {
            throw new NotImplementedException();
        }

        public Task CompleteAsync(IEnumerable<string> lockTokens)
        {
            throw new NotImplementedException();
        }

        public Task DeferAsync(string lockToken, IDictionary<string, object> propertiesToModify = null)
        {
            throw new NotImplementedException();
        }

        public Task RenewLockAsync(Message message)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime> RenewLockAsync(string lockToken)
        {
            throw new NotImplementedException();
        }

        public Task<Message> PeekAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IList<Message>> PeekAsync(int maxMessageCount)
        {
            throw new NotImplementedException();
        }

        public Task<Message> PeekBySequenceNumberAsync(long fromSequenceNumber)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Message>> PeekBySequenceNumberAsync(long fromSequenceNumber, int messageCount)
        {
            throw new NotImplementedException();
        }
    }
}
