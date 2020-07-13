using System;
using System.Linq;
using AzureBusDepot.Abstractions;
using AzureBusDepot.Hosting;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AzureBusDepot.UnitTests.Hosting
{
    public class ServiceCollectionTypeMappingExistsExtensionShould
    {
        private readonly IServiceCollection _services;

        public ServiceCollectionTypeMappingExistsExtensionShould()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Collection_Is_Null_When_Checking_TypeMappingExists()
        {
            IServiceCollection nullCollection = null;
            Action x = () => nullCollection.TypeMappingExists<MultiMessageTypeListener<MyEndpointHandlingConfig>>();
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Return_True_When_Checking_TypeMappingExists_With_Existing_Type()
        {
            _services.AddSingleton<MultiMessageTypeListener<MyEndpointHandlingConfig>>();

            _services.TypeMappingExists<MultiMessageTypeListener<MyEndpointHandlingConfig>>()
                .Should().BeTrue();
        }

        [Fact]
        public void Return_False_When_Checking_TypeMappingExists_With_No_Type_Mapping()
        {
            _services.TypeMappingExists<MultiMessageTypeListener<MyEndpointHandlingConfig>>()
                .Should().BeFalse();
        }

        [Fact]
        public void Return_False_When_Checking_TypeMappingExists_With_Different_Generic_Type_Argument()
        {
            _services.AddSingleton<MultiMessageTypeListener<MyEndpointHandlingConfig>>();

            _services.TypeMappingExists<MultiMessageTypeListener<AnotherConfig>>()
                .Should().BeFalse();
        }

        private class AnotherConfig : IEndpointHandlingConfig
        {
            public string ConnectionString { get; set; }
            public string EntityPath { get; set; }
            public bool AutoComplete { get; set; }
            public int MaxConcurrentCalls { get; set; }
            public int MaxAutoRenewDurationSeconds { get; set; }
            public bool DeadLetterOnUnhandledException { get; set; }
        }
    }

    public class ServiceCollectionAddAzureBusDepotExtensionShould
    {
        private readonly IServiceCollection _services;

        public ServiceCollectionAddAzureBusDepotExtensionShould()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Collection_Is_Null()
        {
            IServiceCollection nullCollection = null;
            Action x = () => nullCollection.AddAzureBusDepot<JsonMessageSerialiser>();
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Not_Throw_When_Called_Multiple_Times()
        {
            _services.AddAzureBusDepot<JsonMessageSerialiser>();
            _services.AddAzureBusDepot<JsonMessageSerialiser>();
        }
    }

    public class ServiceCollectionConfigureSingleMessageTypeListenerExtensionShould
    {
        private readonly IServiceCollection _services;
        private readonly MyEndpointHandlingConfig _config = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.singlemessagetype/Subscriptions/SingleMessageTypeListenerShould",
            MaxConcurrentCalls = 2,
            MaxAutoRenewDurationSeconds = 60,
            AutoComplete = false,
        };

        public ServiceCollectionConfigureSingleMessageTypeListenerExtensionShould()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Add_Services()
        {
            _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(_config);
            _services.Any(d => d.ServiceType == typeof(IHostedService)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(IMessageListener<MyEndpointHandlingConfig>)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(IMessageProcessor<MyEvent>)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(IMessageHandler<MyEvent>)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(MyEndpointHandlingConfig)).Should().BeTrue();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Collection_Is_Null()
        {
            IServiceCollection nullCollection = null;
            Action x = () => nullCollection.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(_config);
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_No_ConnectionString()
        {
            Action x = () =>
            {
                _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(
                    new MyEndpointHandlingConfig { MaxConcurrentCalls = 4, EntityPath = "myqueue"});
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_No_EntityPath()
        {
            Action x = () =>
            {
                _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(
                    new MyEndpointHandlingConfig { MaxConcurrentCalls = 4, ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=" });
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_MaxConcurrentCalls_Less_Than_One()
        {
            Action x = () =>
            {
                _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(
                    new MyEndpointHandlingConfig
                    {
                        MaxConcurrentCalls = 0,
                        ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                        EntityPath = "myqueue",
                    });
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_When_Called_Multiple_Times_With_Same_Config_Type()
        {
            Action x = () =>
            {
                _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(_config);
                _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(_config);
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Does_Not_Throw_When_Called_Once()
        {
            _services.ConfigureSingleMessageTypeListener<MyEndpointHandlingConfig, MyEvent, MyEventHandler>(_config);
        }
    }

    public class ServiceCollectionConfigureMessageListenerWithPropertyBasedDispatcherExtensionShould
    {
        private readonly IServiceCollection _services;
        private readonly MyEndpointHandlingConfig _config = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.singlemessagetype/Subscriptions/SingleMessageTypeListenerShould",
            MaxConcurrentCalls = 2,
            MaxAutoRenewDurationSeconds = 60,
            AutoComplete = false,
        };

        public ServiceCollectionConfigureMessageListenerWithPropertyBasedDispatcherExtensionShould()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Add_Services()
        {
            _services.ConfigureMessageListenerWithPropertyBasedDispatcher(_config, "Type");
            _services.Any(d => d.ServiceType == typeof(IHostedService)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(IMessageListener<MyEndpointHandlingConfig>)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(IMessageProcessorDispatcher)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(MyEndpointHandlingConfig)).Should().BeTrue();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Collection_Is_Null()
        {
            IServiceCollection nullCollection = null;
            Action x = () => nullCollection.ConfigureMessageListenerWithPropertyBasedDispatcher(_config, "Type");
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_No_ConnectionString()
        {
            Action x = () =>
            {
                _services.ConfigureMessageListenerWithPropertyBasedDispatcher(
                    new MyEndpointHandlingConfig{ MaxConcurrentCalls = 4, EntityPath = "myqueue"}, "Type");
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_No_EntityPath()
        {
            Action x = () =>
            {
                _services.ConfigureMessageListenerWithPropertyBasedDispatcher(
                    new MyEndpointHandlingConfig { MaxConcurrentCalls = 4, ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=" }, "Type");
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_MaxConcurrentCalls_Less_Than_One()
        {
            Action x = () =>
            {
                _services.ConfigureMessageListenerWithPropertyBasedDispatcher(
                    new MyEndpointHandlingConfig
                    {
                        MaxConcurrentCalls = 0,
                        ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
                        EntityPath = "myqueue",
                    },
                    "PropertyName");
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_When_Called_Multiple_Times_With_Same_Config_Type()
        {
            Action x = () =>
            {
                _services.ConfigureMessageListenerWithPropertyBasedDispatcher(_config, "Type");
                _services.ConfigureMessageListenerWithPropertyBasedDispatcher(_config, "Type");
            };
            x.Should().ThrowExactly<ArgumentException>();
        }
    }

    public class ServiceCollectionAddMessageHandlerExtensionShould
    {
        private readonly IServiceCollection _services;

        public ServiceCollectionAddMessageHandlerExtensionShould()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Add_Services()
        {
            _services.AddMessageHandler<MyEvent, MyEventHandler>();
            _services.Any(d => d.ServiceType == typeof(IMessageProcessor<MyEvent>)).Should().BeTrue();
            _services.Any(d => d.ServiceType == typeof(IMessageHandler<MyEvent>)).Should().BeTrue();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Collection_Is_Null_When_Calling_AddMessageHandler()
        {
            IServiceCollection nullCollection = null;
            Action x = () => nullCollection.AddMessageHandler<MyEvent, MyEventHandler>();
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Does_Not_Throw_When_Called_Multiple_Times()
        {
            _services.AddMessageHandler<MyEvent, MyEventHandler>();
            _services.AddMessageHandler<MyEvent, MyEventHandler>();
        }
    }

    public class ServiceCollectionAddMessageSendingGatewayShould
    {
        private readonly IServiceCollection _services;
        private readonly MyEndpointHandlingConfig _config = new MyEndpointHandlingConfig
        {
            ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=",
            EntityPath = "adaptertests.singlemessagetype/Subscriptions/SingleMessageTypeListenerShould",
            MaxConcurrentCalls = 2,
            MaxAutoRenewDurationSeconds = 60,
            AutoComplete = false,
        };

        public ServiceCollectionAddMessageSendingGatewayShould()
        {
            _services = new ServiceCollection();
        }

        [Fact]
        public void Add_Services()
        {
            _services.AddMessageSendingGateway(_config);
            _services.Any(d => d.ServiceType == typeof(IMessageSendingGateway<MyEndpointHandlingConfig>)).Should().BeTrue();
        }

        [Fact]
        public void Throw_ArgumentNullException_Given_Collection_Is_Null_When_Calling_AddMessageSendingGateway()
        {
            IServiceCollection nullCollection = null;
            Action x = () => nullCollection.AddMessageSendingGateway(_config);
            x.Should().ThrowExactly<ArgumentNullException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_No_ConnectionString()
        {
            Action x = () =>
            {
                _services.AddMessageSendingGateway(
                    new MyEndpointHandlingConfig { MaxConcurrentCalls = 4, EntityPath = "myqueue" });
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_Given_Config_Has_No_EntityPath()
        {
            Action x = () =>
            {
                _services.AddMessageSendingGateway(
                    new MyEndpointHandlingConfig { MaxConcurrentCalls = 4, ConnectionString = "Endpoint=sb://your-sb.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx=" });
            };
            x.Should().ThrowExactly<ArgumentException>();
        }

        [Fact]
        public void Throw_ArgumentException_When_Called_Multiple_Times_With_Same_Config_Type()
        {
            Action x = () =>
            {
                _services.AddMessageSendingGateway(_config);
                _services.AddMessageSendingGateway(_config);
            };
            x.Should().ThrowExactly<ArgumentException>();
        }
    }
}