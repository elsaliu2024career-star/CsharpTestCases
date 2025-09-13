namespace ApiTests;

using System.Text.Json.Serialization;
using FluentAssertions;
using System.Net;
using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Data.Sqlite;


public class NameListInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("person_name")]
    public string? PersonName { get; set; }  // maps to person_name
    [JsonPropertyName("job_role")]
    public string? JobRole { get; set; }     // maps to job_role
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
    [JsonPropertyName("rating")]
    public int Rating { get; set; }
    [JsonPropertyName("review")]
    public string? Review { get; set; }
}

public class Customer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task AddAsync(Customer customer);
    Task<IEnumerable<Customer>> GetAllAsync();
}

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; } = null!;
}

public class CustomerRepository : ICustomerRepository
{
    private readonly MyDbContext _context;

    public CustomerRepository(MyDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task AddAsync(Customer customer)
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _context.Customers.ToListAsync();
    }
}

public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _customerRepository.GetByIdAsync(id);
    }
    public async Task AddCustomerAsync(Customer customer)
    {
        await _customerRepository.AddAsync(customer);
    }
    public async Task<IEnumerable<Customer>> GetAllCustomersAsync()
    {
        return await _customerRepository.GetAllAsync();
    }

}


public class ReviewsApiTests
{
    private readonly HttpClient _client;
    public ReviewsApiTests()
    {
        _client = new HttpClient { BaseAddress = new Uri("http://172.20.10.2:8000") };
    }

    [Fact]
    [Trait("Category", "Real DB")]
    public async Task GetReviews_ReturnsSuccessAndCorrectContentType()
    {
        var response = await _client.GetAsync("/reviews/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var data = await response.Content.ReadFromJsonAsync<List<NameListInfo>>();
        data.Should().NotBeNull();
        data.Should().HaveCountGreaterThan(0);

        var firstPerson = data[0];
        firstPerson.Id.Should().BeGreaterThan(0);
        firstPerson.PersonName.Should().NotBeNullOrEmpty();
        firstPerson.Rating.Should().BeInRange(1, 5);

        //       var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        //       Console.WriteLine(json);
    }

    [Fact]
    [Trait("Category", "Real DB")]
    public async Task VerifyRootResponse()
    {
        var response = await _client.GetAsync("/");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Backend is running");
    }

    [Fact]
    [Trait("Category", "Real DB")]
    public async Task TestName()
    {
        var command = "curl http://172.20.10.2:8000/";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        await Task.Run(() => process.WaitForExit());

        // error.Should().BeEmpty();
        output.Should().Contain("Backend is running");
        Console.WriteLine($"Output: {output}");

    }

    public class CustomerServiceSqliteTests
    {
        private readonly CustomerService _customerService;

        public CustomerServiceSqliteTests()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<MyDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new MyDbContext(options);
            context.Database.EnsureCreated();

            ICustomerRepository repo = new CustomerRepository(context);
            _customerService = new CustomerService(repo);
        }

        [Fact]
        [Trait("Category", "fake DB")]
        public async Task AddCustomerVerify()
        {
            var newCustomer = new Customer
            {
                Id = 1,
                Name = "John Doe",
                Email = "elsaliu@gmail.com",
                Phone = "0420991325"
            };

            await _customerService.AddCustomerAsync(newCustomer);

            var retrievedCustomer = await _customerService.GetCustomerByIdAsync(1);
            Assert.NotNull(retrievedCustomer);
            Assert.Equal("John Doe", retrievedCustomer.Name);
            Assert.Equal("elsaliu@gmail.com", retrievedCustomer.Email);
            Assert.Equal("0420991325", retrievedCustomer.Phone);

        }
    }
}