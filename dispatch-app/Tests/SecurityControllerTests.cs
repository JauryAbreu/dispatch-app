using dispatch_app.Controllers;
using dispatch_app.Models;
using dispatch_app.Models.User;
using dispatch_app.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Xunit;

namespace dispatch_app.Tests
{
    public class SecurityControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<IOptions<SmtpSettings>> _smtpSettingsMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly SecurityController _controller;
        private readonly Mock<AuthUtil> _authUtilMock;
        private readonly Mock<EmailUtil> _emailUtilMock;

        public SecurityControllerTests()
        {
            _userManagerMock = MockUserManager<ApplicationUser>();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
                _userManagerMock.Object, new Mock<IHttpContextAccessor>().Object, new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
                null, null, null, null);
            _smtpSettingsMock = new Mock<IOptions<SmtpSettings>>();
            _smtpSettingsMock.Setup(s => s.Value).Returns(new SmtpSettings());
            _configurationMock = new Mock<IConfiguration>();
            _authUtilMock = new Mock<AuthUtil>(_configurationMock.Object, _userManagerMock.Object);
            _emailUtilMock = new Mock<EmailUtil>(_smtpSettingsMock.Object.Value);
            _controller = new SecurityController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _smtpSettingsMock.Object,
                _configurationMock.Object);
            _controller.GetType().GetField("_authUtil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_controller, _authUtilMock.Object);
            _controller.GetType().GetField("_emailUtil", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(_controller, _emailUtilMock.Object);

            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userClaims = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { }, "mock"));
            contextAccessor.Setup(a => a.HttpContext.User).Returns(userClaims);
            var httpContext = new DefaultHttpContext { User = userClaims };
            httpContext.Session = new Mock<ISession>().Object;
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost");
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public void Login_Get_ReturnsViewWithLoginModel()
        {
            var result = _controller.Login();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<LoginModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public void Login_Get_ReturnsDefaultView()
        {
            var result = _controller.Login();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task Login_Post_RedirectsToHome_WhenLoginSucceeds()
        {
            var model = new LoginModel { Email = "test@example.com", Password = "Password123", RememberMe = false };
            var user = new ApplicationUser { Id = "1", Email = model.Email, UserName = model.Email };
            _userManagerMock.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _signInManagerMock.Setup(s => s.PasswordSignInAsync(user, model.Password, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(u => u.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Dispatcher" });
            _authUtilMock.Setup(a => a.GenerateJwtToken(_userManagerMock.Object, user))
                .ReturnsAsync(("token", DateTime.UtcNow.AddHours(1)));

            var result = await _controller.Login(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            var model = new LoginModel();
            _controller.ModelState.AddModelError("Email", "Required");

            var result = await _controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("Datos de entrada no válidos.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public void ForgotPassword_Get_ReturnsViewWithForgotPasswordModel()
        {
            var result = _controller.ForgotPassword();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ForgotPasswordModel>(viewResult.Model);
            Assert.NotNull(model);
        }

        [Fact]
        public void ForgotPassword_Get_ReturnsDefaultView()
        {
            var result = _controller.ForgotPassword();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public async Task ForgotPassword_Post_RedirectsToLogin_WhenEmailExists()
        {
            var model = new ForgotPasswordModel { Email = "test@example.com" };
            var user = new ApplicationUser { Id = "1", Email = model.Email, FirstName = "John" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token");
            _emailUtilMock.Setup(e => e.SendEmailAsync(model.Email, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((true, string.Empty));

            var result = await _controller.ForgotPassword(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Se ha enviado un enlace para restablecer tu contraseña.", _controller.TempData[MessageEnum.Success.ToString()]);
        }

        [Fact]
        public async Task ForgotPassword_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            var model = new ForgotPasswordModel();
            _controller.ModelState.AddModelError("Email", "Required");

            var result = await _controller.ForgotPassword(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("Datos de entrada no válidos.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public void ResetPassword_Get_ReturnsViewWithResetPasswordModel_WhenTokenAndEmailValid()
        {
            var result = _controller.ResetPassword("valid-token", "test@example.com");

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ResetPasswordModel>(viewResult.Model);
            Assert.Equal("valid-token", model.Token);
            Assert.Equal("test@example.com", model.Email);
        }

        [Fact]
        public void ResetPassword_Get_RedirectsToLogin_WhenTokenOrEmailInvalid()
        {
            var result = _controller.ResetPassword(null, "test@example.com");

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Enlace de restablecimiento inválido.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public async Task ResetPassword_Post_RedirectsToLogin_WhenResetSucceeds()
        {
            var model = new ResetPasswordModel { Email = "test@example.com", Token = "valid-token", Password = "NewPassword123" };
            var user = new ApplicationUser { Id = "1", Email = model.Email };
            _userManagerMock.Setup(u => u.FindByEmailAsync(model.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.ResetPasswordAsync(user, model.Token, model.Password))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.ResetPassword(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Contraseña restablecida correctamente.", _controller.TempData[MessageEnum.Success.ToString()]);
        }

        [Fact]
        public async Task ResetPassword_Post_ReturnsView_WhenModelStateIsInvalid()
        {
            var model = new ResetPasswordModel();
            _controller.ModelState.AddModelError("Password", "Required");

            var result = await _controller.ResetPassword(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(model, viewResult.Model);
            Assert.Equal("Datos de entrada no válidos.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public async Task Logout_Get_RedirectsToLogin_WhenSignOutSucceeds()
        {
            _signInManagerMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

            var result = await _controller.Logout();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
        }

        [Fact]
        public async Task Logout_Get_RedirectsToLogin_WhenExceptionOccurs()
        {
            _signInManagerMock.Setup(s => s.SignOutAsync()).ThrowsAsync(new Exception("Sign out error"));

            var result = await _controller.Logout();

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Error al cerrar sesión: Sign out error", _controller.TempData[MessageEnum.Error.ToString()]);
        }

        [Fact]
        public void AccessDenied_Get_ReturnsViewWithDefaultErrorMessage()
        {
            var result = _controller.AccessDenied();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Acceso denegado. Contacte al administrador.", _controller.TempData[MessageEnum.Warning.ToString()]);
        }

        [Fact]
        public void AccessDenied_Get_ReturnsViewWithCustomErrorMessage()
        {
            _controller.ControllerContext.HttpContext.Items["ErrorMessage"] = "Custom access denied message";

            var result = _controller.AccessDenied();

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Custom access denied message", _controller.TempData[MessageEnum.Warning.ToString()]);
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