#pragma warning disable CA1506 // Avoid excessive class coupling
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA2000 // Dispose objects before losing scope
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dapper;
using Exos.Platform.AspNetCore.Helpers;
using Exos.Platform.AspNetCore.Middleware;
using Exos.Platform.AspNetCore.Security;
using Exos.Platform.Persistence.Encryption;
using Exos.Platform.Persistence.EventTracking;
using Exos.Platform.Persistence.Policies;
using Exos.Platform.Persistence.Tests.DbContext;
using Exos.Platform.Persistence.Tests.Entities;
using Exos.Platform.Persistence.Tests.Model;
using Exos.Platform.TenancyHelper.Interfaces;
using Exos.Platform.TenancyHelper.MultiTenancy;
using Exos.Platform.TenancyHelper.PersistenceService;
using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Exos.Platform.Persistence.Tests
{
    /// <summary>
    /// Test methods for IEncryption.
    /// </summary>
    [TestClass]
    public class TestDbEncryption
    {
        private static ServiceProvider _serviceProvider;
        private static TestDbContextEncrypted _testDbContextEncrypted;
        private static TestDbContext _testDbContext;
        private static DbConnection _dbConnection;
        private static ILogger<TestDbContext> _logger;
        private static IEventTrackingService _eventTrackingService;

        /// <summary>
        /// Executes once for the test class.
        /// </summary>
        /// <param name="testContext"><see cref="TestContext"/>.</param>
        [ClassInitialize]
        public static void TestFixtureSetup(TestContext testContext)
        {
            // Configure Mapster to ignore case
            TypeAdapterConfig.GlobalSettings.Default.NameMatchingStrategy(NameMatchingStrategy.IgnoreCase);
            TypeAdapterConfig<string, byte[]>.NewConfig().MapWith(str => (str != null) ? Convert.FromBase64String(str) : null);
            TypeAdapterConfig<byte[], string>.NewConfig().MapWith(str => (str != null) ? Convert.ToBase64String(str) : null);

            // Json Serialization
            JsonConvert.DefaultSettings = () =>
            {
                JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                jsonSerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
                jsonSerializerSettings.Formatting = Formatting.Indented;
                return jsonSerializerSettings;
            };

            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.Development.json");
            IConfiguration configuration = configurationBuilder.Build();

            // DB Connection
            _dbConnection = new SqliteConnection("DataSource=:memory:");
            _dbConnection.Open();

            IServiceCollection serviceCollection = new ServiceCollection()
                .AddLogging(options => options.AddConsole().AddConfiguration(configuration.GetSection("Logging")))
                .AddDistributedMemoryCache()
                .Configure<UserContextOptions>(configuration.GetSection("UserContext"))
                .AddScoped<IUserContextService, UserContextService>()
                .AddScoped<IUserContextAccessor, UserContextAccessor>()
                .AddScoped<IHttpContextAccessor, MockHttpContextAccessor>()
                .AddScoped<IUserAccessor, UserAccessor>()
                .AddScoped<IUserContext, UserContext>()
                .AddScoped<IPolicyHelper, PolicyHelper>()
                .AddSingleton<IDocumentClientAccessor, MockDocumentClientAccessor>()
                .Configure<RepositoryOptions>(configuration.GetSection("TenancyPolicyDocumentRepository"))
                .AddTransient<IUserHttpContextAccessorService, UserHttpContextAccessorService>()
                .AddEncryptionServices(configuration)
                .AddExosRetrySqlPolicy(configuration)
                .AddDbContext<PlatformDbContext, TestDbContextEncrypted>(options =>
                {
                    options.UseSqlite(_dbConnection);
                })
                .AddDbContext<PlatformDbContext, TestDbContext>(options =>
                {
                    options.UseSqlite(_dbConnection);
                });
            serviceCollection.AddEventTrackingService<EventQueueEntity>(configuration);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _testDbContextEncrypted = (TestDbContextEncrypted)_serviceProvider.GetService<PlatformDbContext>();
            _testDbContextEncrypted.Database.EnsureCreated();

            _logger = _serviceProvider.GetService<ILogger<TestDbContext>>();
            DbContextOptions<TestDbContext> dbcontextOptions = _serviceProvider.GetService<DbContextOptions<TestDbContext>>();
            IUserHttpContextAccessorService userHttpContextAccessorService = _serviceProvider.GetService<IUserHttpContextAccessorService>();
            _testDbContext = new TestDbContext(_logger, dbcontextOptions, userHttpContextAccessorService);
            _eventTrackingService = _serviceProvider.GetService<IEventTrackingService>();
        }

        /// <summary>
        /// Runs once after all tests in  are executed. (Optional).
        /// </summary>
        [ClassCleanup]
        public static void TestFixtureTearDown()
        {
            if (_dbConnection != null)
            {
                _dbConnection.Dispose();
            }
        }

        /// <summary>
        /// Test method for EncryptEntity.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestEncryptEntity()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            BorrowerEntity borrowerEntity = GetBorrowerEntity();
            await _testDbContextEncrypted.AddAsync(borrowerEntity, cancellationToken).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Saved Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
            // Read entity from DB Context that doesn't implement encryption.
            BorrowerEntity encryptedBorrowerEntity = _testDbContext.BorrowerEntities.Find(borrowerEntity.BorrowerId);
            Console.WriteLine($"Encrypted Entity:{LoggerHelper.SanitizeValue(encryptedBorrowerEntity)}");
            Assert.IsTrue(string.Equals(borrowerEntity.FirstName, encryptedBorrowerEntity.FirstName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The TestExplicitEvents.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestExplicitEvents()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            BorrowerEntity borrowerEntity = GetBorrowerEntity();

            Dictionary<string, string> eventMetadata = new Dictionary<string, string>
            {
                { "MetadataKey1", "MetadataValue1" },
                { "MetadataKey2", "MetadataValue2" },
                { "X-Client-Tag", "boa.dev.exostechnology.com" }
            };

            // Add an Explicit Event with metadata with client tag
            await _eventTrackingService.CreateExplicitEvent(new ExplicitEvent()
            {
                EntityName = "Borrower",
                EventName = "Borrower.Add",
                Payload = System.Text.Json.JsonSerializer.Serialize(borrowerEntity),
                PrimaryKeyValue = borrowerEntity.BorrowerId.ToString(CultureInfo.InvariantCulture),
                Metadata = System.Text.Json.JsonSerializer.Serialize(eventMetadata, new JsonSerializerOptions { PropertyNameCaseInsensitive = false }),
            }).ConfigureAwait(false);

            // Add an Explicit Event with metadata with client tag using  System.Text.Json.JsonSerialize
            await _eventTrackingService.CreateExplicitEvent(new ExplicitEvent()
            {
                EntityName = "Borrower",
                EventName = "Borrower.Add",
                Payload = System.Text.Json.JsonSerializer.Serialize(borrowerEntity),
                PrimaryKeyValue = borrowerEntity.BorrowerId.ToString(CultureInfo.InvariantCulture),
                Metadata = System.Text.Json.JsonSerializer.Serialize(eventMetadata, new JsonSerializerOptions())
            }).ConfigureAwait(false);

            // Add an Explicit Event with metadata without client tag
            eventMetadata.Remove("X-Client-Tag");
            await _eventTrackingService.CreateExplicitEvent(new ExplicitEvent()
            {
                EntityName = "Borrower",
                EventName = "Borrower.Add",
                Payload = System.Text.Json.JsonSerializer.Serialize(borrowerEntity),
                PrimaryKeyValue = borrowerEntity.BorrowerId.ToString(CultureInfo.InvariantCulture),
                Metadata = System.Text.Json.JsonSerializer.Serialize(eventMetadata),
            }).ConfigureAwait(false);

            // Add an Explicit Event without metadata
            await _eventTrackingService.CreateExplicitEvent(new ExplicitEvent()
            {
                EntityName = "Borrower",
                EventName = "Borrower.Add",
                Payload = System.Text.Json.JsonSerializer.Serialize(borrowerEntity),
                PrimaryKeyValue = borrowerEntity.BorrowerId.ToString(CultureInfo.InvariantCulture),
            }).ConfigureAwait(false);

            await _testDbContextEncrypted.AddAsync(borrowerEntity, cancellationToken).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"Saved Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
        }

        /// <summary>
        /// Test method for EncryptEntity.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestUpdateEncryptedEntity()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            BorrowerEntity borrowerEntity = GetBorrowerEntity();
            BorrowerEntity encryptedBorrowerEntity;
            using (var insertTransaction = _testDbContextEncrypted.Database.BeginTransaction())
            {
                await _testDbContextEncrypted.AddAsync(borrowerEntity, cancellationToken).ConfigureAwait(false);
                await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await insertTransaction.CommitAsync();
                Console.WriteLine($"Saved Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
                // Read entity from DB Context that doesn't implement encryption.
                encryptedBorrowerEntity = _testDbContext.BorrowerEntities.Find(borrowerEntity.BorrowerId);
                Console.WriteLine($"Encrypted Entity:{LoggerHelper.SanitizeValue(encryptedBorrowerEntity)}");
                Assert.IsTrue(string.Equals(borrowerEntity.FirstName, encryptedBorrowerEntity.FirstName, StringComparison.OrdinalIgnoreCase));
            }

            _testDbContext.ChangeTracker.Entries().ToList().ForEach(c => c.State = EntityState.Detached);
            _testDbContextEncrypted.ChangeTracker.Entries().ToList().ForEach(c => c.State = EntityState.Detached);

            using var updateTransaction = _testDbContextEncrypted.Database.BeginTransaction();
            borrowerEntity.FirstName = "Updated_FirstName";
            _testDbContextEncrypted.Update(borrowerEntity);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await updateTransaction.CommitAsync();
            BorrowerModel borrowerModel = borrowerEntity.Adapt<BorrowerModel>();
            _testDbContextEncrypted.DatabaseEncryption.DecryptObject(borrowerModel);

            Console.WriteLine($"Saved Updated Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
            BorrowerEntity encryptedUpdatedBorrowerEntity = _testDbContext.BorrowerEntities.Find(borrowerEntity.BorrowerId);
            Console.WriteLine($"Encrypted Updated Entity:{LoggerHelper.SanitizeValue(encryptedUpdatedBorrowerEntity)}");
            Assert.IsFalse(string.Equals(borrowerEntity.FirstName, borrowerModel.FirstName, StringComparison.OrdinalIgnoreCase));
            Assert.IsFalse(string.Equals(encryptedBorrowerEntity.FirstName, encryptedUpdatedBorrowerEntity.FirstName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Test method for EncryptEntity.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestDeleteEncryptedEntity()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            BorrowerEntity borrowerEntity = GetBorrowerEntity();
            BorrowerEntity encryptedBorrowerEntity;
            using (var insertTransaction = _testDbContextEncrypted.Database.BeginTransaction())
            {
                await _testDbContextEncrypted.AddAsync(borrowerEntity, cancellationToken).ConfigureAwait(false);
                await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await insertTransaction.CommitAsync();
                Console.WriteLine($"Saved Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
                // Read entity from DB Context that doesn't implement encryption.
                encryptedBorrowerEntity = _testDbContext.BorrowerEntities.Find(borrowerEntity.BorrowerId);
                Console.WriteLine($"Encrypted Entity:{LoggerHelper.SanitizeValue(encryptedBorrowerEntity)}");
            }

            _testDbContext.ChangeTracker.Entries().ToList().ForEach(c => c.State = EntityState.Detached);
            _testDbContextEncrypted.ChangeTracker.Entries().ToList().ForEach(c => c.State = EntityState.Detached);
            using var deleteTransaction = _testDbContextEncrypted.Database.BeginTransaction();
            _testDbContextEncrypted.Remove(borrowerEntity);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await deleteTransaction.CommitAsync();
            BorrowerEntity deletedBorrowerEntity = _testDbContext.BorrowerEntities.Find(borrowerEntity.BorrowerId);
            Assert.IsTrue(deletedBorrowerEntity == null);
        }

        /// <summary>
        /// Test Dapper Queries.
        /// </summary>
        /// <returns>Dapper queries reading encrypted data.</returns>
        [TestMethod]
        public async Task TestDapperQuery()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            // Override key to use its configured to use boa.dev.exostechnology.com
            databaseEncryption.SetEncryptionKey("api.dev.exostechnology.com");

            // Create seed Data
            List<BorrowerEntity> borrowerEntities = new List<BorrowerEntity>();
            for (int i = 1; i <= 5; i++)
            {
                BorrowerEntity newBorrowerEntity = new BorrowerEntity()
                {
                    FirstName = $"FirstName - {i}",
                    MiddleName = $"MiddleName - {i}",
                    LastName = $"XX-Last Name - {i}",
                    SSN = $"{i}56-{i}3-098{i}",
                    DayPhone = $"949-1{i}4-98{i}6",
                    EMail = $"borrower_email_{i}@borrower.com",
                    EvenPhone = $"714-4{i}1-58{i}5",
                    Addr1 = $"322{i} El Camino Real",
                    City = "TESTCITY",
                    State = "CA",
                    Zip = "92602",
                };
                borrowerEntities.Add(newBorrowerEntity);
            }

            await _testDbContextEncrypted.AddRangeAsync(borrowerEntities, cancellationToken).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Pick a custom city to differentiate this from other tests using borrowerEntities.
            string borrowerListQuery = "SELECT * FROM BorrowerEntities where City='TESTCITY'";

            DbConnection connection = _testDbContextEncrypted.Database.GetDbConnection();

            var borrowerEntityList = await connection.QueryAsyncConnection<BorrowerEntity>(borrowerListQuery.ToString(), databaseEncryption, true, _logger).ConfigureAwait(false);
            var borrowerModelList = await connection.QueryAsyncConnection<BorrowerModel>(borrowerListQuery.ToString(), databaseEncryption, true, _logger).ConfigureAwait(false);
            var encryptedBorrowerList = await connection.QueryAsync<BorrowerEntity>(borrowerListQuery.ToString()).ConfigureAwait(false);

            LogBorrowerEntityListItems(borrowerEntityList.AsList(), string.Empty);
            LogBorrowerModelListItems(borrowerModelList.AsList(), string.Empty);
            LogBorrowerEntityListItems(encryptedBorrowerList.AsList(), "Encrypted");

            var borrowerId = borrowerEntities[3].BorrowerId;
            var borrowerEntity = borrowerEntityList.AsList().Find(b => b.BorrowerId == borrowerId);
            Console.WriteLine($"Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
            var encryptedBorrowerEntity = encryptedBorrowerList.AsList().Find(b => b.BorrowerId == borrowerId);
            Console.WriteLine($"Encrypted Entity:{LoggerHelper.SanitizeValue(encryptedBorrowerEntity)}");

            Assert.IsFalse(string.Equals(borrowerEntity.FirstName, encryptedBorrowerEntity.FirstName, StringComparison.OrdinalIgnoreCase));

            string borrowerQuery = "SELECT * FROM BorrowerEntities WHERE BorrowerId = @BorrowerId";
            BorrowerEntity decryptedBorrowerEntity = await connection.QueryFirstAsyncConnection<BorrowerEntity>(borrowerQuery, databaseEncryption, true, _logger, new { BorrowerId = borrowerId }).ConfigureAwait(false);
            BorrowerModel decryptedBorrowerModel = await connection.QueryFirstAsyncConnection<BorrowerModel>(borrowerQuery, databaseEncryption, true, _logger, new { BorrowerId = borrowerId }).ConfigureAwait(false);

            Assert.IsTrue(string.Equals(decryptedBorrowerEntity.FirstName.Substring(0, 5), "First", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(decryptedBorrowerModel.FirstName.Substring(0, 5), "First", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"Read Decrypted Single Entity:{LoggerHelper.SanitizeValue(decryptedBorrowerEntity)}");
        }

        /// <summary>
        /// The TestDapperQueryMultipleClients.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestDapperMultipleClients()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            databaseEncryption.SetEncryptionKey("boa.dev.exostechnology.com");

            using (var bofaTransaction = _testDbContextEncrypted.Database.BeginTransaction())
            {
                // Create records for Bank of America
                List<BorrowerEntity> bofaBorrowerEntityList = new List<BorrowerEntity>();
                for (int i = 1; i <= 5; i++)
                {
                    BorrowerEntity newBofaBorrowerEntity = new BorrowerEntity()
                    {
                        FirstName = $"BofaFirstName - {i}",
                        MiddleName = $"BofaMiddleName - {i}",
                        LastName = $"BofaLast Name - {i}",
                        SSN = $"{i}56-{i}3-098{i}",
                        DayPhone = $"949-1{i}4-98{i}6",
                        EMail = $"Bofaborrower_email_{i}@borrower.com",
                        EvenPhone = $"714-4{i}1-58{i}5",
                        Addr1 = $"322{i} El Camino Real",
                        City = "Irvine",
                        State = "CA",
                        Zip = "92602",
                    };
                    bofaBorrowerEntityList.Add(newBofaBorrowerEntity);
                }

                await _testDbContextEncrypted.AddRangeAsync(bofaBorrowerEntityList, cancellationToken).ConfigureAwait(false);
                await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await bofaTransaction.CommitAsync();
            }

            // Override key to use its configured to use boadev-s3.exostechnology.com
            databaseEncryption.SetEncryptionKey("wells.dev.exostechnology.com");

            _testDbContextEncrypted.ChangeTracker.Entries().ToList().ForEach(c => c.State = EntityState.Detached);

            using (var wellsTransaction = _testDbContextEncrypted.Database.BeginTransaction())
            {
                // Create Records for Wells
                List<BorrowerEntity> wellsBorrowerEntityList = new List<BorrowerEntity>();
                for (int i = 1; i <= 5; i++)
                {
                    BorrowerEntity newWellsBorrowerEntity = new BorrowerEntity()
                    {
                        FirstName = $"WellsFirstName - {i}",
                        MiddleName = $"WellsMiddleName - {i}",
                        LastName = $"WellsLast Name - {i}",
                        SSN = $"{i}56-{i}3-098{i}",
                        DayPhone = $"949-1{i}4-98{i}6",
                        EMail = $"Wellsborrower_email_{i}@borrower.com",
                        EvenPhone = $"714-4{i}1-58{i}5",
                        Addr1 = $"322{i} El Camino Real",
                        City = "Irvine",
                        State = "CA",
                        Zip = "92602",
                    };
                    wellsBorrowerEntityList.Add(newWellsBorrowerEntity);
                }

                await _testDbContextEncrypted.AddRangeAsync(wellsBorrowerEntityList, cancellationToken).ConfigureAwait(false);
                await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await wellsTransaction.CommitAsync();
            }

            string borrowerListQuery = "SELECT * FROM BorrowerEntities";

            DbConnection connection = _testDbContextEncrypted.Database.GetDbConnection();
            var encryptedBorrowerList = await connection.QueryAsync<BorrowerEntity>(borrowerListQuery.ToString()).ConfigureAwait(false);
            LogBorrowerEntityListItems(encryptedBorrowerList.AsList(), "Encrypted");

            var borrowerList = await connection.QueryAsyncConnection<BorrowerModel>(borrowerListQuery.ToString(), databaseEncryption, false, _logger).ConfigureAwait(false);
            LogBorrowerModelListItems(borrowerList.AsList(), string.Empty);

            var borrowerEntity = borrowerList.AsList().Find(b => b.BorrowerId == 3);
            Console.WriteLine($"Entity:{LoggerHelper.SanitizeValue(borrowerEntity)}");
            EncryptedFieldHeaderParser headerParser = new EncryptedFieldHeaderParser(borrowerEntity.FirstName);
            Assert.IsFalse(headerParser.IsEncrypted);

            var encryptedBorrowerEntity = encryptedBorrowerList.AsList().Find(b => b.BorrowerId == 3);
            headerParser = new EncryptedFieldHeaderParser(encryptedBorrowerEntity.FirstName);
            Console.WriteLine($"Encrypted Entity:{LoggerHelper.SanitizeValue(encryptedBorrowerEntity)}");
            Assert.IsTrue(headerParser.IsEncrypted);

            string borrowerQuery = "SELECT * FROM BorrowerEntities WHERE BorrowerId = @BorrowerId";
            BorrowerModel decryptedBorrower = await connection.QueryFirstAsyncConnection<BorrowerModel>(borrowerQuery, databaseEncryption, false, _logger, new { BorrowerId = 3 }).ConfigureAwait(false);
            headerParser = new EncryptedFieldHeaderParser(decryptedBorrower.FirstName);
            Assert.IsFalse(headerParser.IsEncrypted);
            Console.WriteLine($"Read Decrypted Single Entity:{LoggerHelper.SanitizeValue(decryptedBorrower)}");
        }

        /// <summary>
        /// Test Dapper Queries where encryption is generic fail-over (apidev) but decryption is specific (i.e. boa).  Should succeed via fail-over.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestDapperFailOverSuccessQuery()
        {
            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            Console.WriteLine($"{Environment.CurrentManagedThreadId}. . Success (api): {databaseEncryption.CurrentEncryptionKey.KeyIdentifier}");

            // Encrypt with key that IS a (the) common/fail-over key
            databaseEncryption.SetEncryptionKey("api.dev.exostechnology.com");
            var borrower = await SaveEntityTask("FirstName");

            // Change context to Boa, though encrypted data is apidev.
            databaseEncryption.SetEncryptionKey("boa.dev.exostechnology.com");
            var decryptedBorrowerEntity = await LoadEntityTask(databaseEncryption, borrower.BorrowerId);

            Assert.IsTrue(string.Equals(decryptedBorrowerEntity.FirstName, "FirstName", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"Read Decrypted Single Entity:{LoggerHelper.SanitizeValue(decryptedBorrowerEntity)}");
        }

        /// <summary>
        /// Test Dapper Queries where encryption is generic NON-fail-over (ui.dev) but decryption is specific (i.e. boa).  Should fail.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task TestDapperFailOverFailureQuery()
        {
            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            Console.WriteLine($"{Environment.CurrentManagedThreadId}. Failure (ui): {databaseEncryption.CurrentEncryptionKey.KeyIdentifier}");

            // Encrypt with basic (SL) key NOT marked as fail-over.
            databaseEncryption.SetEncryptionKey("wells.dev.exostechnology.com");
            var item = await SaveEntityTask("FirstName");

            // Change context to Boa, though encrypted data is uidev.
            databaseEncryption.SetEncryptionKey("boa.dev.exostechnology.com");

            // This call should throw Unauthorized Exception as actual data is not encrypted
            // with "our" key (boa) and the key it is encrypted with is not flagged to allow
            // fail-over "anyone" decryption.
            await LoadEntityTask(databaseEncryption, item.BorrowerId);
        }

        /// <summary>
        /// Tests the dapper with no x client tag header.
        /// </summary>
        /// <returns>A Task.</returns>
        [TestMethod]
        [ExpectedException(typeof(UnauthorizedException))]
        public async Task TestDapperNoXClientTagHeader()
        {
            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            Console.WriteLine($"{Environment.CurrentManagedThreadId}. Failure (ui): {databaseEncryption.CurrentEncryptionKey.KeyIdentifier}");

            // Encrypt with basic (SL) key NOT marked as fail-over.
            databaseEncryption.SetEncryptionKey("boa.dev.exostechnology.com");
            var item = await SaveEntityTask("FirstName");

            // Change context to empty, simulate that header is not present in request
            databaseEncryption.SetEncryptionKey(new EncryptionKey());

            // This call should throw Unauthorized Exception as actual data is not encrypted
            // with "our" key (boa) and the key it is encrypted with is not flagged to allow
            // fail-over "anyone" decryption.
            await LoadEntityTask(databaseEncryption, item.BorrowerId);
        }

        /// <summary>
        /// Test Dapper Queries where encryption performed with multiple versions of common key, but decryption is specific (i.e. boa).
        /// Should succeed via fail-over.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestDapperFailOverKeyVersionSuccessQuery()
        {
            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            Console.WriteLine($"{Environment.CurrentManagedThreadId}. . Success (api): {databaseEncryption.CurrentEncryptionKey.KeyIdentifier}");

            // Encrypt two records, each with different version of the common/fail-over key
            databaseEncryption.SetEncryptionKey("api.dev.exostechnology.com");
            var borrower1 = await SaveEntityTask("FirstName_1");

            databaseEncryption.SetEncryptionKey("api2.dev.exostechnology.com");
            var borrower2 = await SaveEntityTask("FirstName_2");

            // Change context to Boa, though encrypted data is apidev.
            databaseEncryption.SetEncryptionKey("boa.dev.exostechnology.com");
            var decryptedBorrowerEntity1 = await LoadEntityTask(databaseEncryption, borrower1.BorrowerId);
            var decryptedBorrowerEntity2 = await LoadEntityTask(databaseEncryption, borrower2.BorrowerId);

            Assert.IsTrue(string.Equals(decryptedBorrowerEntity1.FirstName, "FirstName_1", StringComparison.OrdinalIgnoreCase));
            Assert.IsTrue(string.Equals(decryptedBorrowerEntity2.FirstName, "FirstName_2", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine($"Read Decrypted Single Entities:{LoggerHelper.SanitizeValue(decryptedBorrowerEntity1)}, {LoggerHelper.SanitizeValue(decryptedBorrowerEntity2)}");
        }

        /// <summary>
        /// Test Encrypt files.
        /// </summary>
        /// <returns>Encrypted files.</returns>
        [TestMethod]
        public async Task TestEncryptFiles()
        {
            // Encrypt Files
            string textFileName = "Document.txt";
            Dictionary<string, string> txtFileMetadata = await TestEncryptFile(textFileName);
            string pdfFileName = "Document.pdf";
            Dictionary<string, string> pdfFileMetadata = await TestEncryptFile(pdfFileName);
            string wordFileName = "Document.docx";
            Dictionary<string, string> wordFileMetadata = await TestEncryptFile(wordFileName);

            // Decrypt Files
            await TestDecryptFile(textFileName, txtFileMetadata);
            await TestDecryptFile(pdfFileName, pdfFileMetadata);
            await TestDecryptFile(wordFileName, wordFileMetadata);
        }

        /// <summary>
        /// The TestGenerateKeys.
        /// </summary>
        [TestMethod]
        public void TestGenerateKeys()
        {
            string key1 = EncryptionKeyGenerator.NewKeyEncoded();
            Assert.IsTrue(Convert.FromBase64String(key1).Length == IDatabaseEncryption.EncryptionKeyLengthBits / 8);
            Console.WriteLine($"Key 1:{key1}");
            string key2 = EncryptionKeyGenerator.NewKeyEncoded();
            Assert.IsTrue(Convert.FromBase64String(key1).Length == IDatabaseEncryption.EncryptionKeyLengthBits / 8);
            Console.WriteLine($"Key 2:{key2}");
            Assert.IsFalse(key1.Equals(key2, StringComparison.OrdinalIgnoreCase));
            string key3 = EncryptionKeyGenerator.NewKeyEncoded();
            Assert.IsTrue(Convert.FromBase64String(key3).Length == IDatabaseEncryption.EncryptionKeyLengthBits / 8);
            Console.WriteLine($"Key 3:{key3}");
            Assert.IsFalse(key2.Equals(key3, StringComparison.OrdinalIgnoreCase));
            string key4 = EncryptionKeyGenerator.NewKeyEncoded();
            Assert.IsTrue(Convert.FromBase64String(key4).Length == IDatabaseEncryption.EncryptionKeyLengthBits / 8);
            Console.WriteLine($"Key 4:{key4}");
            Assert.IsFalse(key3.Equals(key4, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The TestDatabaseHashing.
        /// </summary>
        [TestMethod]
        public void TestDatabaseHashing()
        {
            IDatabaseHashing databaseHashing = _serviceProvider.GetService<IDatabaseHashing>();
            string valueToHash = "9999999999";
            var hashedValue = databaseHashing.HashStringToHex(valueToHash);
            Console.WriteLine($"To Hash: {valueToHash} - Hashed Value:{hashedValue}");
            valueToHash = "nobody@nowhere.com";
            hashedValue = databaseHashing.HashStringToHex(valueToHash);
            Console.WriteLine($"To Hash: {valueToHash} - Hashed Value:{hashedValue}");
            valueToHash = NormalizeString("REMOVED");
            hashedValue = databaseHashing.HashStringToHex(valueToHash);
            Console.WriteLine($"To Hash: {valueToHash} - Hashed Value:{hashedValue}");
            valueToHash = "Refer to LOE";
            hashedValue = databaseHashing.HashStringToHex(valueToHash);
            Console.WriteLine($"To Hash: {valueToHash} - Hashed Value:{hashedValue}");
        }

        /// <summary>
        /// The TestDapperQueryMultiple.
        /// </summary>
        /// <returns>The <see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestDapperQueryMultiple()
        {
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            databaseEncryption.SetEncryptionKey("boa.dev.exostechnology.com");
            var policyHelper = _serviceProvider.GetService<IPolicyHelper>();
            var distributedCache = _serviceProvider.GetService<IDistributedCache>();

            // Create seed Data
            List<BorrowerEntity> borrowerEntities = new List<BorrowerEntity>();
            for (int i = 1; i <= 2; i++)
            {
                BorrowerEntity newBorrowerEntity = new BorrowerEntity()
                {
                    FirstName = $"FirstName - {i}",
                    MiddleName = $"MiddleName - {i}",
                    LastName = $"Last Name - {i}",
                    SSN = $"{i}56-{i}3-098{i}",
                    DayPhone = $"949-1{i}4-98{i}6",
                    EMail = $"borrower_email_{i}@borrower.com",
                    EvenPhone = $"714-4{i}1-58{i}5",
                    Addr1 = $"322{i} El Camino Real",
                    City = "TESTCITY",
                    State = "CA",
                    Zip = "92602",
                };
                borrowerEntities.Add(newBorrowerEntity);
            }

            // Load Multitenancy Policy in memory cache
            string borrowerTenancyPolicy = File.ReadAllText(@"BorrowerPolicyDocument.json");
            await distributedCache.SetStringAsync("BorrowerPolicyDocument", borrowerTenancyPolicy).ConfigureAwait(false);

            await _testDbContextEncrypted.AddRangeAsync(borrowerEntities, cancellationToken).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            List<LoanEntity> loanEntities = new List<LoanEntity>
            {
                new LoanEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(0).BorrowerId,
                    LoanNumber = $"1234",
                    LoanDate = DateTime.Today,
                    Amount = 150000,
                    Description = "Loan Description"
                },
                new LoanEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(0).BorrowerId,
                    LoanNumber = $"5678",
                    LoanDate = DateTime.Today,
                    Amount = 160000,
                    Description = "Loan Description"
                },
                new LoanEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(1).BorrowerId,
                    LoanNumber = $"8765",
                    LoanDate = DateTime.Today,
                    Amount = 170000,
                    Description = "Loan Description"
                },
                new LoanEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(1).BorrowerId,
                    LoanNumber = $"4321",
                    LoanDate = DateTime.Today,
                    Amount = 180000,
                    Description = "Loan Description"
                }
            };

            List<PropertyEntity> propertyEntities = new List<PropertyEntity>
            {
                new PropertyEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(0).BorrowerId,
                    Address = $"3210 El Camino Real 1",
                    City = "TESTCITY",
                    State = "CA",
                    Zip = "92602",
                },
                new PropertyEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(0).BorrowerId,
                    Address = $"3211 El Camino Real 2",
                    City = "TESTCITY",
                    State = "CA",
                    Zip = "92602",
                },
                new PropertyEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(1).BorrowerId,
                    Address = $"3220 El Camino Real 3",
                    City = "TESTCITY",
                    State = "CA",
                    Zip = "92602",
                },
                new PropertyEntity
                {
                    BorrowerId = borrowerEntities.ElementAt<BorrowerEntity>(1).BorrowerId,
                    Address = $"3221 El Camino Real 4",
                    City = "TESTCITY",
                    State = "CA",
                    Zip = "92602",
                },
            };

            await _testDbContextEncrypted.AddRangeAsync(loanEntities, cancellationToken).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _testDbContextEncrypted.AddRangeAsync(propertyEntities, cancellationToken).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Queries to run,queries need to have the same number of paramteres
            string borrowerQuery = "SELECT * FROM BorrowerEntities WHERE BorrowerId=@BorrowerId";
            string loanQuery = "SELECT * FROM LoanEntities WHERE BorrowerId=@BorrowerId";
            string propertyQuery = "SELECT * FROM PropertyEntities WHERE BorrowerId=@BorrowerId";

            DbConnection connection = _testDbContextEncrypted.Database.GetDbConnection();

            DapperQuery<BorrowerEntity> borrowerDapperQuery = new DapperQuery<BorrowerEntity>()
            {
                Query = borrowerQuery,
                TableAlias = new List<string> { "BorrowerEntities" }
            };

            DapperQuery<LoanEntity> loanDapperQuery = new DapperQuery<LoanEntity>
            {
                Query = loanQuery,
                TableAlias = new List<string> { "LoanEntities" }
            };

            DapperQuery<PropertyEntity> propertyDapperQuery = new DapperQuery<PropertyEntity>
            {
                Query = propertyQuery,
                TableAlias = new List<string> { "PropertyEntities" }
            };

            var multipleResults = await connection.QueryMultipleAsyncConnection(
                new List<DapperQuery> { borrowerDapperQuery, loanDapperQuery, propertyDapperQuery },
                databaseEncryption,
                false,
                _logger,
                new { borrowerEntities.ElementAt<BorrowerEntity>(0).BorrowerId });

            Assert.IsTrue(multipleResults.ResultsCount() == 3);

            var borrowerEntityList = multipleResults.ReadQueryResults<BorrowerEntity>();
            var loanEntityList = multipleResults.ReadQueryResults<LoanEntity>();
            var propertyEntityList = multipleResults.ReadQueryResults<PropertyEntity>();

            // This query is not in the query results
            var eventQueueEntityList = multipleResults.ReadQueryResults<EventQueueEntity>();
            Assert.IsNull(eventQueueEntityList);

            Assert.IsTrue(borrowerEntityList.Count() == 1);
            Assert.IsTrue(loanEntityList.Count() == 2);
            Assert.IsTrue(propertyEntityList.Count() == 2);

            // Test with multitenancy
            var multipleTenancyResults = await connection.QueryMultipleAsyncTenancyConnection(
                new List<DapperQuery> { borrowerDapperQuery, loanDapperQuery, propertyDapperQuery },
                policyHelper,
                _logger,
                new { borrowerEntities.ElementAt<BorrowerEntity>(1).BorrowerId },
                databaseEncryption: databaseEncryption,
                validateKeyForDecryption: false);

            borrowerEntityList = multipleTenancyResults.ReadQueryResults<BorrowerEntity>();
            loanEntityList = multipleTenancyResults.ReadQueryResults<LoanEntity>();
            propertyEntityList = multipleTenancyResults.ReadQueryResults<PropertyEntity>();

            Assert.IsTrue(borrowerEntityList.Count() == 1);
            Assert.IsFalse(loanEntityList.Any());
            Assert.IsFalse(propertyEntityList.Any());
        }

        /// <summary>
        /// Test invalid X client tag header.
        /// </summary>
        /// <returns>A Task.</returns>
        [TestMethod]
        [ExpectedException(typeof(NotFoundException))]
        public async Task TestInvalidXClientTagHeader()
        {
            var databaseEncryption = _serviceProvider.GetService<IDatabaseEncryption>();
            // Encrypt with a header that doesn't exists in the configuration. Platform throw NotFoundException
            databaseEncryption.SetEncryptionKey("invalid.dev.exostechnology.com");
            var item = await SaveEntityTask("FirstName");
        }

        /// <summary>
        /// The NormalizeString.
        /// </summary>
        /// <param name="str">The str<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string NormalizeString(string str)
        {
            CultureInfo cultureInfo = new CultureInfo("en-US", false);
            str = Regex.Replace(str, "[^a-zA-Z0-9]", string.Empty);
            str = str.Trim();
            str = str.ToLower(cultureInfo);
            return str;
        }

        /// <summary>
        /// The SaveEntityTask.
        /// </summary>
        /// <param name="firstName">The firstName<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{BorrowerEntity}"/>.</returns>
        private static async Task<BorrowerEntity> SaveEntityTask(string firstName)
        {
            var borrowerEntities = new List<BorrowerEntity>
            {
                new BorrowerEntity { FirstName = firstName }
            };

            await _testDbContextEncrypted.AddRangeAsync(borrowerEntities, CancellationToken.None).ConfigureAwait(false);
            await _testDbContextEncrypted.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

            return borrowerEntities.First();
        }

        private static Task<BorrowerEntity> LoadEntityTask(IDatabaseEncryption databaseEncryption, long borrowerId)
        {
            const string borrowerQuery = "SELECT * FROM BorrowerEntities WHERE BorrowerId = @BorrowerId";
            var connection = _testDbContextEncrypted.Database.GetDbConnection();
            return connection.QueryFirstAsyncConnection<BorrowerEntity>(borrowerQuery, databaseEncryption, true, _logger, new { BorrowerId = borrowerId });
        }

        private async Task<Dictionary<string, string>> TestEncryptFile(string fileName)
        {
            IBlobEncryption blobEncryption = _serviceProvider.GetService<IBlobEncryption>();

            Dictionary<string, string> fileMetadata = new Dictionary<string, string>();

            // Read file to encrypt
            using (FileStream fileStream = new FileStream($"./TestFiles/{fileName}", FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Encrypted file
                using FileStream encryptedFileStream = new FileStream($"./TestFiles/Encrypted{fileName}", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
                // Encrypted Stream
                blobEncryption.Encrypt(fileStream, encryptedFileStream, out fileMetadata);
            }

            // Compare both encrypted and original file, content should be different
            byte[] fileBytes = await File.ReadAllBytesAsync($"./TestFiles/{fileName}");
            byte[] encryptedBytes = await File.ReadAllBytesAsync($"./TestFiles/Encrypted{fileName}");
            bool equal = fileBytes.SequenceEqual(encryptedBytes);
            Assert.IsFalse(equal);
            Assert.IsTrue(fileMetadata.Count == 4);
            foreach (var item in fileMetadata)
            {
                Console.WriteLine($"File Name:{fileName} - File Metadata: Key:{item.Key} - Value:{item.Value}");
            }

            return fileMetadata;
        }

        private async Task TestDecryptFile(string fileName, Dictionary<string, string> fileMetadata)
        {
            IBlobEncryption blobEncryption = _serviceProvider.GetService<IBlobEncryption>();

            // Read encrypted file
            using (FileStream fileStream = new FileStream($"./TestFiles/Encrypted{fileName}", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                // Decrypted File
                using FileStream decryptedFileStream = new FileStream($"./TestFiles/Decrypted{fileName}", FileMode.OpenOrCreate);
                // Decrypted Stream
                blobEncryption.Decrypt(fileStream, decryptedFileStream, fileMetadata);
            }

            // Compare both decrypted and encrypted file, content should be different
            byte[] encryptedBytes = await File.ReadAllBytesAsync($"./TestFiles/Encrypted{fileName}");
            byte[] decryptedBytes = await File.ReadAllBytesAsync($"./TestFiles/Decrypted{fileName}");
            bool equal = encryptedBytes.SequenceEqual(decryptedBytes);
            Assert.IsFalse(equal);
            // Compare both decrypted and original file, content should be the same
            byte[] originalBytes = await File.ReadAllBytesAsync($"./TestFiles/{fileName}");
            equal = originalBytes.SequenceEqual(decryptedBytes);
            Assert.IsTrue(equal);
        }

        private void LogBorrowerEntityListItems(List<BorrowerEntity> borrowerList, string borrowerType)
        {
            foreach (var item in borrowerList)
            {
                Console.WriteLine($"{borrowerType} Borrower Entity:{LoggerHelper.SanitizeValue(item)}");
            }
        }

        private void LogBorrowerModelListItems(List<BorrowerModel> borrowerList, string borrowerType)
        {
            foreach (var item in borrowerList)
            {
                Console.WriteLine($"{borrowerType} Borrower Entity:{LoggerHelper.SanitizeValue(item)}");
            }
        }

        private BorrowerEntity GetBorrowerEntity()
        {
            BorrowerEntity borrowerEntity = new BorrowerEntity()
            {
                FirstName = "FirstName",
                MiddleName = "MiddleName",
                LastName = "Last Name",
                SSN = "456-23-0987",
                DayPhone = "949-134-9876",
                EMail = "borrower_email@borrower.com",
                EvenPhone = "714-421-5835",
                Addr1 = "3220 El Camino Real",
                City = "Irvine",
                State = "CA",
                Zip = "92602",
            };
            return borrowerEntity;
        }
    }
}
#pragma warning restore CA1506 // Avoid excessive class coupling
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA2000 // Dispose objects before losing scope