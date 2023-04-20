#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable CA2000 // Dispose objects before losing scope
namespace ICTLibUnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Dapper;
    using Exos.Platform.AspNetCore.Extensions;
    using Exos.Platform.ICTLibrary.Core;
    using Exos.Platform.ICTLibrary.Core.Extension;
    using Exos.Platform.ICTLibrary.Core.Model;
    using Exos.Platform.ICTLibrary.Repository;
    using Exos.Platform.ICTLibrary.Repository.Model;
    using Exos.Platform.Messaging.Core;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    [ExcludeFromCodeCoverage]
    public class IctUnitTest
    {
        private IServiceCollection _services;
        private IServiceProvider _serviceProvider;

        private IConfigurationRoot Configuration { get; set; }

        [TestInitialize]

        public void TestInitialize()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _services = new ServiceCollection().AddLogging();
            ILoggerFactory factory = new LoggerFactory();
            var logger = new Logger<IIctEventPublisher>(factory);
            _services.AddOptions();
            _services.AddMemoryCache();
            _services.ConfigureIct(Configuration);
            _services.AddStackExchangeRedisCache(options => options.Configuration = Configuration.GetConnectionString("distributedRedisCacheConfiguration"));
            _serviceProvider = _services.BuildServiceProvider();
        }

        [TestMethod]
        [Ignore]
        public void Test_Insert_PublishEvent_Using_Context()
        {
            var ictContext = _serviceProvider.GetService<IctContext>();
            // var builder = new DbContextOptionsBuilder<IctContext>();
            // builder.UseSqlServer(Configuration.GetConnectionString("IctConnection"));
            // context = new IctContext(builder.Options);
            var evt = new EventTracking
            {
                ApplicationName = "test",
                EntityName = "Test",
                TrackingId = Guid.NewGuid().ToString()
            };
            // ictContext.EventTracking.Add(evt);
            // await ictContext.SaveChangesAsync();
            using (IDbConnection connection = ictContext.Database.GetDbConnection())
            {
                connection.Open();
                string insertQuery = @"INSERT INTO [ict].[EventTracking]([TrackingId], [EventName], [EntityName], [ApplicationName], [TopicName], [CreatedDate],[CreatedBy],[LastUpdatedDate],[LastUpdatedBy])  VALUES  (@TrackingId, @EventName, @EntityName, @ApplicationName, @TopicName, @CreatedDate, @CreatedBy, @LastUpdatedDate, @LastUpdatedBy)";

                try
                {
                    var savedEntity = connection.Execute(insertQuery, new
                    {
                        TrackingId = evt.TrackingId,
                        EventName = evt.EventName,
                        EntityName = evt.EntityName,
                        ApplicationName = evt.ApplicationName,
                        TopicName = evt.TopicName,
                        CreatedDate = evt.CreatedDate,
                        CreatedBy = evt.CreatedBy,
                        LastUpdatedDate = evt.LastUpdatedDate,
                        LastUpdatedBy = evt.LastUpdatedBy
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                connection.Close();
            }
        }

        [TestMethod]
        public async Task Insert_PublishEvent_Using_IctEventPublisher()
        {
            // var ictEventPublisher = _serviceProvider.GetService<IIctEventPublisher>();
            var eventMessage = new IctEventMessage
            {
                EntityName = "WorkOrder",
                EventName = "IN_ORDER_SAVE",
                PublisherName = "GatewaySvc",
                // PublisherId = 1,
                Payload = "{\"Color\": \"Green\",\"Date\":\"" + DateTime.Now + "\"}",
                TrackingId = Guid.NewGuid().ToString(),
                AdditionalMessageHeaderData =
                   new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("KeyFromICtHeader", "IctHeaderValue")
                    }
            };

            string a = JsonSerializer.Serialize(eventMessage);
            var mockExosMessaging = new Mock<IExosMessaging>();
            var mockIctRepository = new Mock<IIctRepository>();
            var logger = Mock.Of<ILogger<IctEventPublisher>>();

            var mockEventPublisher = new IctEventPublisher(mockExosMessaging.Object, mockIctRepository.Object, logger);
            var result = await mockEventPublisher.PublishEvent(eventMessage);
            Assert.IsTrue(result);

            // await ictEventPublisher.PublishEvent(eventMessage);
        }

        [TestMethod]
        public void Get_Event_Entity_Topic_For_Application_WithEntity()
        {
            var ictRepository = _serviceProvider.GetService<IIctRepository>();
            var returnVal = ictRepository.GetEventEntityTopic("Event.OrderCreated", "OrderCreated", 1);
            Assert.IsNotNull(returnVal);
        }
    }
}
#pragma warning restore CA1031 // Do not catch general exception types
#pragma warning restore CA2000 // Dispose objects before losing scope