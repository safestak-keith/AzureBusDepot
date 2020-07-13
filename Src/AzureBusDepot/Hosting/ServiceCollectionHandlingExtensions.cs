using System;
using System.Linq;
using AzureBusDepot.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AzureBusDepot.Hosting
{
    /// <summary>
    /// Bunch of fluent extension methods on the IServiceCollection DI primitive.
    /// TODO: Determine feasibility of using HostBuilder to bring it one level up which also allows configuration and loggging bindings
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.1#extensibility
    /// </summary>
    public static class ServiceCollectionHandlingExtensions
    {
        public enum Lifetime
        {
            Transient,
            Scoped,
            Singleton
        }

        public static IServiceCollection AddAzureBusDepot<TSerialiser>(this IServiceCollection services)
            where TSerialiser : class, IMessageSerialiser
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.TryAddSingleton<IMessageReceiverFactory, MessageReceiverFactory>();
            services.TryAddSingleton<IMessageSenderFactory, MessageSenderFactory>();
            services.TryAddSingleton<IMessageSerialiser, TSerialiser>();

            return services;
        }

        /// <summary>
        /// Configures the necessary DI bindings for a generic approach to handling Messages of a single known type TMessage from a queue/topic
        /// </summary>
        /// <typeparam name="TConfig">The endpoint handling config implementing IEndpointHandlingConfig. Define an implementation per endpoint to ensure unique binding</typeparam>
        /// <typeparam name="TMessage">The message contract to handle</typeparam>
        /// <typeparam name="TMessageHandler">The handler of the message of type IMessageHandler</typeparam>
        /// <param name="services">IServiceCollection to extend</param>
        /// <param name="endpointHandlingConfigFactory">The provider of the endpoint config for listening to messages</param>
        /// <param name="lifetime">The lifetime to use for the processor and handler, defaults to singleton.</param>
        /// <returns>A mutated IServiceCollection with the DI bindings for handling Message TMessage</returns>
        public static IServiceCollection ConfigureSingleMessageTypeListener<TConfig, TMessage, TMessageHandler>(
            this IServiceCollection services, Func<TConfig> endpointHandlingConfigFactory, Lifetime lifetime = Lifetime.Singleton)
            where TConfig : class, IEndpointHandlingConfig
            where TMessage : class
            where TMessageHandler : class, IMessageHandler<TMessage>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (services.TypeMappingExists<IMessageListener<TConfig>>())
                throw new ArgumentException($"Listener for <{typeof(TConfig).Name}> has already been added.");

            services.AddHostedService<MessageListenerHostedService<TConfig>>();
            services.AddSingleton<IMessageListener<TConfig>, SingleMessageTypeListener<TConfig, TMessage>>();
            services.AddSingleton(s => endpointHandlingConfigFactory());
            services.AddMessageHandler<TMessage, TMessageHandler>(lifetime);

            return services;
        }

        public static IServiceCollection ConfigureSingleMessageTypeListener<TConfig, TMessage, TMessageHandler>(
            this IServiceCollection services, TConfig endpointConfig, Lifetime lifetime = Lifetime.Singleton)
            where TConfig : class, IEndpointHandlingConfig
            where TMessage : class
            where TMessageHandler : class, IMessageHandler<TMessage>
        {
            EnsureEndpointHandlingConfigInvariants(endpointConfig);

            return ConfigureSingleMessageTypeListener<TConfig, TMessage, TMessageHandler>(
                services, () => endpointConfig, lifetime);
        }

        /// <summary>
        /// Configures the necessary DI bindings for a generic approach to handling Messages from a queue/topic given different multiple TMessage types
        /// and a dispatcher to dispatch the right handler based on the Message.
        /// </summary>
        /// <typeparam name="TConfig">The endpoint handling config implementing IEndpointHandlingConfig. Define an implementation per endpoint to ensure unique binding</typeparam>
        /// <typeparam name="TMessageProcessorDispatcher">An implementation of IMessageProcessorDispatcher which dispatches the right handling pipeline</typeparam>
        /// <param name="services">IServiceCollection to extend</param>
        /// <param name="endpointHandlingConfigFactory">The provider of the endpoint config for listening to messages</param>
        /// <returns>A mutated IServiceCollection with the DI bindings for handling multiple messages</returns>
        public static IServiceCollection ConfigureMultiMessageTypeListener<TConfig, TMessageProcessorDispatcher>(
            this IServiceCollection services, Func<TConfig> endpointHandlingConfigFactory)
            where TConfig : class, IEndpointHandlingConfig
            where TMessageProcessorDispatcher : class, IMessageProcessorDispatcher
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (services.TypeMappingExists<IMessageListener<TConfig>>())
                throw new ArgumentException($"Listener for <{typeof(TConfig).Name}> has already been added.");

            services.AddHostedService<MessageListenerHostedService<TConfig>>();
            services.AddSingleton<IMessageListener<TConfig>, MultiMessageTypeListener<TConfig>>();
            services.AddSingleton<IMessageProcessorDispatcher, TMessageProcessorDispatcher>();
            services.AddSingleton(s => endpointHandlingConfigFactory());

            return services;
        }

        public static IServiceCollection ConfigureMessageListenerWithPropertyBasedDispatcher<TConfig>(
            this IServiceCollection services, Func<TConfig> endpointHandlingConfigFactory, string messageTypePropertyName)
            where TConfig : class, IEndpointHandlingConfig
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (services.TypeMappingExists<IMessageListener<TConfig>>())
                throw new ArgumentException($"Listener for <{typeof(TConfig).Name}> has already been added.");

            services.AddHostedService<MessageListenerHostedService<TConfig>>();
            services.AddSingleton<IMessageListener<TConfig>, MultiMessageTypeListener<TConfig>>();
            services.AddSingleton<IMessageProcessorDispatcher>(
                s => new MessagePropertyBasedDispatcher(
                    s.GetService<ILogger<MessagePropertyBasedDispatcher>>(), s, messageTypePropertyName));
            services.AddSingleton(s => endpointHandlingConfigFactory());

            return services;
        }

        public static IServiceCollection ConfigureMessageListenerWithPropertyBasedDispatcher<TConfig>(
            this IServiceCollection services, TConfig endpointConfig, string messageTypePropertyName)
            where TConfig : class, IEndpointHandlingConfig
        {
            EnsureEndpointHandlingConfigInvariants(endpointConfig);

            return ConfigureMessageListenerWithPropertyBasedDispatcher(
                services, () => endpointConfig, messageTypePropertyName);
        }

        /// <summary>
        /// Explicitly attaches processing and handling components for a known message type TMessage. 
        /// Used in conjunction with services.ConfigureMultiMessageTypeListener where a dispatcher will dispatch the right Processor and Handler.
        /// </summary>
        /// <typeparam name="TMessage">The message contract to handle</typeparam>
        /// <typeparam name="TMessageHandler">The handler of the message of type IMessageHandler</typeparam>
        /// <param name="services">IServiceCollection to extend</param>
        /// <param name="lifetime">The lifetime to use for the processor and handler, defaults to singleton.</param>
        /// <returns>A mutated IServiceCollection with the DI bindings for handling multiple messages</returns>
        public static IServiceCollection AddMessageHandler<TMessage, TMessageHandler>(
            this IServiceCollection services, Lifetime lifetime = Lifetime.Singleton)
            where TMessage : class
            where TMessageHandler : class, IMessageHandler<TMessage>
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (lifetime == Lifetime.Transient)
            {
                services.TryAddTransient<IMessageProcessor<TMessage>, MessageProcessor<TMessage>>();
                services.TryAddTransient<IMessageHandler<TMessage>, TMessageHandler>();
            }
            else if (lifetime == Lifetime.Scoped)
            {
                services.TryAddScoped<IMessageProcessor<TMessage>, MessageProcessor<TMessage>>();
                services.TryAddScoped<IMessageHandler<TMessage>, TMessageHandler>();
            }
            else 
            {
                services.TryAddSingleton<IMessageProcessor<TMessage>, MessageProcessor<TMessage>>();
                services.TryAddSingleton<IMessageHandler<TMessage>, TMessageHandler>();
            }

            return services;
        }

        /// <summary>
        /// Assigns a MessageSendingGateway for an endpoint 
        /// </summary>
        /// <typeparam name="TConfig">The endpoint handling config implementing IEndpointHandlingConfig. Define an implementation per endpoint to ensure unique binding</typeparam>
        /// <param name="services">IServiceCollection to extend</param>
        /// <param name="endpointConfigFactory">The provider of the endpoint config for sending messages</param>
        /// <returns></returns>
        public static IServiceCollection AddMessageSendingGateway<TConfig>(
            this IServiceCollection services, Func<TConfig> endpointConfigFactory)
            where TConfig : class, IEndpointConfig
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            if (services.TypeMappingExists<IMessageSendingGateway<TConfig>>())
                throw new ArgumentException($"MessageSendingGateway for <{typeof(TConfig).Name}> has already been added.");

            services.AddSingleton<IMessageSendingGateway<TConfig>, MessageSendingGateway<TConfig>>();
            services.AddSingleton(s => endpointConfigFactory());

            return services;
        }

        public static IServiceCollection AddMessageSendingGateway<TConfig>(
            this IServiceCollection services, TConfig endpointConfig)
            where TConfig : class, IEndpointConfig
        {
            EnsureEndpointConfigInvariants(endpointConfig);

            return AddMessageSendingGateway(services, () => endpointConfig);
        }

        public static bool TypeMappingExists<T>(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            return services.Any(d => d.ServiceType == typeof(T));
        }

        private static void EnsureEndpointConfigInvariants(IEndpointConfig endpointConfig)
        {
            if (endpointConfig == null)
                throw new ArgumentNullException(nameof(endpointConfig));
            if (string.IsNullOrWhiteSpace(endpointConfig.ConnectionString))
                throw new ArgumentException($"{nameof(endpointConfig.ConnectionString)} cannot be null or whitespace");
            if (string.IsNullOrWhiteSpace(endpointConfig.EntityPath))
                throw new ArgumentException($"{nameof(endpointConfig.EntityPath)} cannot be null or whitespace");
        }

        private static void EnsureEndpointHandlingConfigInvariants(IEndpointHandlingConfig endpointConfig)
        {
            EnsureEndpointConfigInvariants(endpointConfig);
            if (endpointConfig.MaxConcurrentCalls < 1)
                throw new ArgumentException($"{nameof(endpointConfig.MaxConcurrentCalls)} cannot be less than one");
        }
    }
}
