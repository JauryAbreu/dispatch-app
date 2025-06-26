using dispatch_app.Controllers;
using dispatch_app.Models;
using dispatch_app.Models.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace dispatch_app.Tests
{
    public class CustomersControllerTests
    {
        private readonly Mock<ApplicationDbContext> _contextMock;
        private readonly CustomersController _controller;
        private readonly Mock<DbSet<Customer>> _customersDbSetMock;

        public CustomersControllerTests()
        {
            _customersDbSetMock = new Mock<DbSet<Customer>>();
            _contextMock = new Mock<ApplicationDbContext>();
            _contextMock.Setup(c => c.Customers).Returns(_customersDbSetMock.Object);

            _controller = new CustomersController(_contextMock.Object);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "admin-id"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));
            contextAccessor.Setup(a => a.HttpContext.User).Returns(userClaims);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };

            // Necesario para TempData
            var tempData = new TempDataDictionary(_controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        private void SetupDbSetMock(IEnumerable<Customer> customers)
        {
            var queryable = customers.AsQueryable();
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.Provider).Returns(queryable.Provider);
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.Expression).Returns(queryable.Expression);
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            _customersDbSetMock.As<IQueryable<Customer>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
        }

        [Fact]
        public async Task Index_Get_ReturnsViewWithCustomers_WhenDataExists()
        {
            var customers = new List<Customer>
            {
                new Customer { Id = 1, FirstName = "John", LastName = "Doe" },
                new Customer { Id = 2, FirstName = "Jane", LastName = "Smith" }
            };
            SetupDbSetMock(customers);
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);

            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<Customer>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Index_Get_ReturnsEmptyView_WhenExceptionOccurs()
        {
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Throws(new Exception("Database error"));


            var result = await _controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<Customer>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("Error al cargar la lista de clientes: Database error", _controller.TempData[MessageEnum.Error.ToString()]);
        }

        [Fact]
        public void Create_Get_ReturnsViewWithNewCustomer()
        {
            var result = _controller.Create();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public async Task Create_Post_RedirectsToIndex_WhenModelIsValid()
        {
            var customer = new Customer
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            };

            _contextMock.Setup(c => c.Add(customer));
            _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await _controller.Create(customer);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Operación completada: Cliente creado correctamente.", _controller.TempData[MessageEnum.Success.ToString()]);
            _contextMock.Verify(c => c.Add(customer), Times.Once());
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once());
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            var customer = new Customer();
            _controller.ModelState.AddModelError("FirstName", "Required");

            var result = await _controller.Create(customer);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(customer, viewResult.Model);
            Assert.Equal("Datos de entrada no válidos.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public async Task Edit_Get_ReturnsViewWithCustomer_WhenCustomerExists()
        {
            var customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe" };
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);
            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync(customer);
            SetupDbSetMock(new List<Customer> { customer });

            var result = await _controller.Edit(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.Model);
            Assert.Equal(customer.Id, model.Id);
        }

        [Fact]
        public async Task Edit_Get_RedirectsToIndex_WhenCustomerNotFound()
        {
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);
            SetupDbSetMock(new List<Customer>());

            var result = await _controller.Edit(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("No se encontró el cliente.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public async Task Edit_Post_RedirectsToIndex_WhenUpdateSucceeds()
        {
            var customer = new Customer { Id = 1, FirstName = "Jane", LastName = "Smith" };

            _contextMock.Setup(c => c.Update(customer));
            _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);
            _contextMock.Setup(c => c.Customers.AnyAsync(c => c.Id == 1, default)).ReturnsAsync(true);

            var result = await _controller.Edit(1, customer);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Operación completada: Cliente actualizado correctamente.", _controller.TempData[MessageEnum.Success.ToString()]);
            _contextMock.Verify(c => c.Update(customer), Times.Once());
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once());
        }

        [Fact]
        public async Task Edit_Post_ReturnsView_WhenConcurrencyExceptionOccurs()
        {
            var customer = new Customer { Id = 1, FirstName = "Jane", LastName = "Smith" };

            _contextMock.Setup(c => c.Update(customer));
            _contextMock.Setup(c => c.SaveChangesAsync(default)).ThrowsAsync(new DbUpdateConcurrencyException());
            _contextMock.Setup(c => c.Customers.AnyAsync(c => c.Id == 1, default)).ReturnsAsync(true);

            var result = await _controller.Edit(1, customer);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(customer, viewResult.Model);
            Assert.Equal("Error de concurrencia al actualizar el cliente.", _controller.TempData[MessageEnum.Error.ToString()]);
        }

        [Fact]
        public async Task Delete_Get_ReturnsViewWithCustomer_WhenCustomerExists()
        {
            var customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe" };
            _contextMock.Setup(c => c.Customers.AsNoTracking()).Returns(_customersDbSetMock.Object);
            SetupDbSetMock(new List<Customer> { customer });

            var result = await _controller.Delete(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Customer>(viewResult.Model);
            Assert.Equal(customer.Id, model.Id);
        }

        [Fact]
        public async Task Delete_Get_RedirectsToIndex_WhenIdIsNull()
        {
            var result = await _controller.Delete(null);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("ID de cliente no proporcionado.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public async Task DeleteConfirmed_Post_RedirectsToIndex_WhenDeleteSucceeds()
        {
            var customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe" };

            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync(customer);
            _contextMock.Setup(c => c.Customers.Remove(customer));
            _contextMock.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await _controller.DeleteConfirmed(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Operación completada: Cliente eliminado correctamente.", _controller.TempData[MessageEnum.Success.ToString()]);
            _contextMock.Verify(c => c.Customers.Remove(customer), Times.Once());
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once());
        }

        [Fact]
        public async Task DeleteConfirmed_Post_RedirectsToIndex_WhenCustomerNotFound()
        {
            _contextMock.Setup(c => c.Customers.FindAsync(1)).ReturnsAsync((Customer)null);

            var result = await _controller.DeleteConfirmed(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("No se encontró el cliente.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }
    }
}
