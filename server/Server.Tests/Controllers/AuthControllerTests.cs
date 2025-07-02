using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using server.Controllers;
using server.Models.DTOs.Auth;
using server.Services;
using server.Services.Interfaces;
using server.Exceptions;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Server.Tests.Controllers
{
    /// <summary>
    /// Unit tests for AuthController. 
    /// Tests only the controller's HTTP behavior, error handling, and response formatting.
    /// Service layer is mocked to isolate controller logic.
    /// </summary>
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenAuthServiceIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthController(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthController(_mockAuthService.Object, null!));
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task Register_Should_ReturnOkResult_WithAuthResponse_WhenValidData()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var expectedResponse = new AuthResponseDto
            {
                Token = "mock-jwt-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    UserName = "testuser",
                    Email = "test@example.com",
                }
            };

            _mockAuthService.Setup(m => m.RegisterAsync(registerDto))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var authResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
            
            Assert.Equal(expectedResponse.Token, authResponse.Token);
            Assert.Equal(expectedResponse.User.UserName, authResponse.User.UserName);
            Assert.Equal(expectedResponse.User.Email, authResponse.User.Email);

            _mockAuthService.Verify(m => m.RegisterAsync(registerDto), Times.Once);
        }

        [Fact]
        public async Task Register_Should_ThrowConflictException_WhenEmailAlreadyExists()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                UserName = "testuser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            _mockAuthService.Setup(m => m.RegisterAsync(registerDto))
                .ThrowsAsync(new ConflictException("User with this email already exists"));

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(() => _controller.Register(registerDto));
            _mockAuthService.Verify(m => m.RegisterAsync(registerDto), Times.Once);
        }

        [Fact]
        public async Task Register_Should_ThrowValidationException_WhenPasswordRequirementsNotMet()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                Password = "weak",
                ConfirmPassword = "weak"
            };

            _mockAuthService.Setup(m => m.RegisterAsync(registerDto))
                .ThrowsAsync(new ValidationException("Password does not meet requirements"));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _controller.Register(registerDto));
            _mockAuthService.Verify(m => m.RegisterAsync(registerDto), Times.Once);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_Should_ReturnOkResult_WithAuthResponse_WhenValidCredentials()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var expectedResponse = new AuthResponseDto
            {
                Token = "mock-jwt-token",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = new UserDto
                {
                    UserName = "testuser",
                    Email = "test@example.com",
                }
            };

            _mockAuthService.Setup(m => m.LoginAsync(loginDto))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var authResponse = Assert.IsType<AuthResponseDto>(okResult.Value);
            
            Assert.Equal(expectedResponse.Token, authResponse.Token);
            Assert.Equal(expectedResponse.User.UserName, authResponse.User.UserName);
            Assert.Equal(expectedResponse.User.Email, authResponse.User.Email);

            _mockAuthService.Verify(m => m.LoginAsync(loginDto), Times.Once);
        }

        [Fact]
        public async Task Login_Should_ThrowValidationException_WhenInvalidCredentials()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _mockAuthService.Setup(m => m.LoginAsync(loginDto))
                .ThrowsAsync(new ValidationException("Invalid email or password"));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _controller.Login(loginDto));
            _mockAuthService.Verify(m => m.LoginAsync(loginDto), Times.Once);
        }

        [Fact]
        public async Task Login_Should_ThrowValidationException_WhenUserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Test123!"
            };

            _mockAuthService.Setup(m => m.LoginAsync(loginDto))
                .ThrowsAsync(new ValidationException("Invalid email or password"));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _controller.Login(loginDto));
            _mockAuthService.Verify(m => m.LoginAsync(loginDto), Times.Once);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public void GetCurrentUser_Should_ReturnOkResult_WithUserDto_WhenValidClaims()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "user-id-123"),
                new(ClaimTypes.Name, "testuser"),
                new(ClaimTypes.Email, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act
            var result = _controller.GetCurrentUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var userDto = Assert.IsType<UserDto>(okResult.Value);
            
            Assert.Equal("testuser", userDto.UserName);
            Assert.Equal("test@example.com", userDto.Email);
        }

        [Fact]
        public void GetCurrentUser_Should_ReturnUnauthorized_WhenMissingNameIdentifierClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, "testuser"),
                new(ClaimTypes.Email, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act
            var result = _controller.GetCurrentUser();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid token claims", unauthorizedResult.Value);
        }

        [Fact]
        public void GetCurrentUser_Should_ReturnUnauthorized_WhenMissingUserNameClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "user-id-123"),
                new(ClaimTypes.Email, "test@example.com")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act
            var result = _controller.GetCurrentUser();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid token claims", unauthorizedResult.Value);
        }

        [Fact]
        public void GetCurrentUser_Should_ReturnUnauthorized_WhenMissingEmailClaim()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "user-id-123"),
                new(ClaimTypes.Name, "testuser")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };

            // Act
            var result = _controller.GetCurrentUser();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("Invalid token claims", unauthorizedResult.Value);
        }

        #endregion
    }
}
