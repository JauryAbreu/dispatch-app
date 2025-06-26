using dispatch_app.Controllers;
using dispatch_app.Models;
using dispatch_app.Models.Home;
using dispatch_app.Models.Transactions;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace dispatch_app.Tests
{
    public class HomeControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<ApplicationDbContext> _contextMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<DbSet<Header>> _headersDbSetMock;
        private readonly Mock<DbSet<Customer>> _customersDbSetMock;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _userManagerMock = MockUserManager<ApplicationUser>();
            _headersDbSetMock = new Mock<DbSet<Header>>();
            _customersDbSetMock = new Mock<DbSet<Customer>>();
            _contextMock = new Mock<ApplicationDbContext>();
            _contextMock.Setup(c => c.Headers).Returns(_headersDbSetMock.Object);
            _contextMock.Setup(c => c.Customers).Returns(_customersDbSetMock.Object);
            _configurationMock = new Mock<IConfiguration>();
            _controller = new HomeController(_userManagerMock.Object, _contextMock.Object, _configurationMock.Object);

            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Role, "Dispatcher")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            _controller.ControllerContext.HttpContext.Session = new Mock<ISession>().Object;

            var tempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        private void SetupHeadersDbSetMock(IEnumerable<Header> headers)
        {
            var queryable = headers.AsQueryable();
            _headersDbSetMock.As<IQueryable<Header>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _headersDbSetMock.As<IQueryable<Header>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _headersDbSetMock.As<IQueryable<Header>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _headersDbSetMock.As<IQueryable<Header>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            _headersDbSetMock.As<IQueryable<Header>>().Setup(m => m.AsNoTracking()).Returns(_headersDbSetMock.Object);
        }

        private void SetupCustomersDbSetMock(IEnumerable<Customer> customers)
        {
            var queryable = customers.AsQueryable();
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.AsNoTracking()).Returns(_customersDbSetMock.Object);
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithMainData_WhenUserIsAuthenticatedAndDataExists()
        {
            var headers = new List<Header>
            {
                new Header { Id = 1, UserCode = "user-id", CustomerCode = "C1", ReceiptId = "R1", Quantity = 10, QuantityDispatched = 10, Status = DeliveryStatusEnum.Entrega_Completada, CreatedDate = DateTime.Now },
                new Header { Id = 2, UserCode = "user-id", CustomerCode = "C2", ReceiptId = "R2", Quantity = 5, QuantityDispatched = 3, Status = DeliveryStatusEnum.Pendiente, CreatedDate = DateTime.Now.AddDays(-5) }
            };

            var customers = new List<Customer>
            {
                new Customer { CustomerId = "C1", Company = "Company A" },
                new Customer { CustomerId = "C2", Company = "Company B" }
            };

            SetupHeadersDbSetMock(headers);
            SetupCustomersDbSetMock(customers);

            _contextMock.Setup(c => c.Headers.AsNoTracking()).Returns(_headersDbSetMock.Object);
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);

            var sessionMock = new Mock<ISession>();
            sessionMock.Setup(s => s.GetString("JWToken")).Returns("valid-token");
            _controller.ControllerContext.HttpContext.Session = sessionMock.Object;

            _controller.GetType().GetField("_authUtil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_controller, new Mock<AuthUtil>(_configurationMock.Object, _userManagerMock.Object) { CallBase = true }.Object);

            var result = await _controller.Index(null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MainData>(viewResult.Model);
            Assert.Equal("user-id", model.UserId);
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithEmptyMainData_WhenExceptionOccurs()
        {
            _contextMock.Setup(c => c.Headers.AsNoTracking()).Throws(new Exception("Database error"));

            var sessionMock = new Mock<ISession>();
            sessionMock.Setup(s => s.GetString("JWToken")).Returns("valid-token");
            _controller.ControllerContext.HttpContext.Session = sessionMock.Object;
            _controller.GetType().GetField("_authUtil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_controller, new Mock<AuthUtil>(_configurationMock.Object, _userManagerMock.Object) { CallBase = true }.Object);

            var result = await _controller.Index(null);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MainData>(viewResult.Model);
            Assert.Null(model.UserId);
            Assert.Equal("Error al cargar el dashboard: Database error", _controller.TempData[MessageEnum.Error.ToString()]);
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithMainData_WhenCodeIsProvided()
        {
            var headers = new List<Header>
            {
                new Header { Id = 1, UserCode = "code123", CustomerCode = "C1", ReceiptId = "R1", Quantity = 10, QuantityDispatched = 10, Status = DeliveryStatusEnum.Entrega_Completada, CreatedDate = DateTime.Now }
            };

            var customers = new List<Customer>
            {
                new Customer { CustomerId = "C1", Company = "Company A" }
            };

            SetupHeadersDbSetMock(headers);
            SetupCustomersDbSetMock(customers);

            _contextMock.Setup(c => c.Headers.AsNoTracking()).Returns(_headersDbSetMock.Object);
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);

            var result = await _controller.Index("code123");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MainData>(viewResult.Model);
            Assert.Equal("code123", model.UserId);
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithWarning_WhenInvalidCodeProvided()
        {
            var result = await _controller.Index("invalid-code");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MainData>(viewResult.Model);
            Assert.Null(model.UserId);
            Assert.Equal("Código de usuario inválido.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public async Task GetMainDataAsync_ReturnsMainData_WhenUserIdIsProvided()
        {
            var headers = new List<Header>
            {
                new Header { Id = 1, UserCode = "user-id", CustomerCode = "C1", ReceiptId = "R1", Quantity = 10, QuantityDispatched = 10, Status = DeliveryStatusEnum.Entrega_Completada, CreatedDate = DateTime.Now }
            };

            var customers = new List<Customer>
            {
                new Customer { CustomerId = "C1", Company = "Company A" }
            };

            SetupHeadersDbSetMock(headers);
            SetupCustomersDbSetMock(customers);

            _contextMock.Setup(c => c.Headers.AsNoTracking()).Returns(_headersDbSetMock.Object);
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);

            var result = await _controller.GetMainDataAsync("user-id");

            Assert.NotNull(result);
            Assert.Equal("user-id", result.UserId);
        }

        [Fact]
        public async Task GetMainDataAsync_ReturnsMainData_WhenUserIdIsNull()
        {
            var headers = new List<Header>
            {
                new Header { Id = 1, UserCode = "other-user", CustomerCode = "C1", ReceiptId = "R1", Quantity = 10, QuantityDispatched = 10, Status = DeliveryStatusEnum.Entrega_Completada, CreatedDate = DateTime.Now }
            };

            var customers = new List<Customer>
            {
                new Customer { CustomerId = "C1", Company = "Company A" }
            };

            SetupHeadersDbSetMock(headers);
            SetupCustomersDbSetMock(customers);

            _contextMock.Setup(c => c.Headers.AsNoTracking()).Returns(_headersDbSetMock.Object);
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);

            var result = await _controller.GetMainDataAsync(null);

            Assert.NotNull(result);
            Assert.Null(result.UserId);
        }

        [Fact]
        public void Error_ReturnsViewWithErrorViewModel()
        {
            var result = _controller.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.NotNull(model.RequestId);
        }

        [Fact]
        public void Error_ReturnsViewWithTraceIdentifier_WhenActivityIsNull()
        {
            _controller.ControllerContext.HttpContext.TraceIdentifier = "trace-123";

            var result = _controller.Error();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
            Assert.Equal("trace-123", model.RequestId);
        }

        private static Mock<UserManager<TIdentityUser>> MockUserManager<TIdentityUser>() where TIdentityUser : class
        {
            var store = new Mock<IUserStore<TIdentityUser>>();
            var mgr = new Mock<UserManager<TIdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TIdentityUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TIdentityUser>());
            return mgr;
        }
    }
}
