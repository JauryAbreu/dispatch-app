using dispatch_app.Controllers;
using dispatch_app.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NuGet.ContentModel;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace dispatch_app.Tests
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            // Setup UserManager mock
            _userManagerMock = MockUserManager<ApplicationUser>();
            // Setup RoleManager mock
            _roleManagerMock = MockRoleManager<IdentityRole>();
            // Initialize controller
            _controller = new AccountController(_userManagerMock.Object, _roleManagerMock.Object);
            // Setup authenticated user context
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user-id"),
                new Claim(ClaimTypes.Role, "Dispatcher")
            }, "mock"));
            contextAccessor.Setup(a => a.HttpContext.User).Returns(userClaims);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userClaims }
            };
        }

        // Tests for EditProfile (GET)
        [Fact]
        public async Task EditProfile_Get_ReturnsViewWithModel_WhenUserIsAuthenticated()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id",
                FirstName = "John",
                LastName = "Doe",
                Phone = "1234567890"
            };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            // Act
            var result = await _controller.EditProfile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<UpdateProfileModel>(viewResult.Model);
            Assert.Equal(user.FirstName, model.FirstName);
            Assert.Equal(user.LastName, model.LastName);
            Assert.Equal(user.Phone, model.Phone);
        }

        [Fact]
        public async Task EditProfile_Get_RedirectsToLogin_WhenUserIsNotAuthenticated()
        {
            // Arrange
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _controller.EditProfile();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Security", redirectResult.ControllerName);
        }

        // Tests for EditProfile (POST)
        [Fact]
        public async Task EditProfile_Post_ReturnsViewWithSuccess_WhenModelIsValid()
        {
            // Arrange
            var model = new UpdateProfileModel
            {
                FirstName = "Jane",
                LastName = "Smith",
                Phone = "0987654321"
            };
            var user = new ApplicationUser { Id = "user-id", FirstName = "John", LastName = "Doe", Phone = "1234567890" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.EditProfile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("Perfil actualizado correctamente.", _controller.TempData["Success"]);
            Assert.Equal(model.FirstName, user.FirstName);
            Assert.Equal(model.LastName, user.LastName);
            Assert.Equal(model.Phone, user.Phone);
        }

        [Fact]
        public async Task EditProfile_Post_ReturnsViewWithErrors_WhenModelStateIsInvalid()
        {
            // Arrange
            var model = new UpdateProfileModel();
            _controller.ModelState.AddModelError("FirstName", "Required");
            var user = new ApplicationUser { Id = "user-id" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            // Act
            var result = await _controller.EditProfile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        // Tests for EditPassword (GET)
        [Fact]
        public void EditPassword_Get_ReturnsView()
        {
            // Act
            var result = _controller.EditPassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view
        }

        [Fact]
        public void EditPassword_Get_DoesNotRequireAuthentication()
        {
            // Arrange
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // No user

            // Act
            var result = _controller.EditPassword();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view
        }

        // Tests for EditPassword (POST)
        [Fact]
        public async Task EditPassword_Post_RedirectsToEditPassword_WhenPasswordChangeSucceeds()
        {
            // Arrange
            var model = new UpdatePasswordModel
            {
                LastPassword = "OldPassword123",
                NewPassword = "NewPassword123",
                RepeatPassword = "NewPassword123"
            };
            var user = new ApplicationUser { Id = "user-id" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.ChangePasswordAsync(user, model.LastPassword, model.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.EditPassword(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("EditPassword", redirectResult.ActionName);
            Assert.Equal("Contraseña actualizada correctamente.", _controller.TempData["Success"]);
        }

        [Fact]
        public async Task EditPassword_Post_ReturnsViewWithError_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var model = new UpdatePasswordModel
            {
                LastPassword = "OldPassword123",
                NewPassword = "NewPassword123",
                RepeatPassword = "DifferentPassword123"
            };
            var user = new ApplicationUser { Id = "user-id" };
            _userManagerMock.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

            // Act
            var result = await _controller.EditPassword(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("La nueva contraseña y su confirmación no coinciden.", _controller.TempData["Error"]);
        }

        private static Mock<UserManager<TIdentityUser>> MockUserManager<TIdentityUser>() where TIdentityUser : class
        {
            var store = new Mock<IUserStore<TIdentityUser>>();
            var mgr = new Mock<UserManager<TIdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            mgr.Object.UserValidators.Add(new UserValidator<TIdentityUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<TIdentityUser>());
            return mgr;
        }

        private static Mock<RoleManager<TRole>> MockRoleManager<TRole>() where TRole : class
        {
            var store = new Mock<IRoleStore<TRole>>();
            return new Mock<RoleManager<TRole>>(store.Object, null, null, null, null);
        }
    }
}