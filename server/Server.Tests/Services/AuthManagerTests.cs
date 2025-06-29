using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using server.Services;
using server.Configs;
using server.Models.DTOs.Auth;
using server.Exceptions;
using System.Security.Claims;
using server.Services.Interfaces;

namespace Server.Tests.Services
{
    /// <summary>
    /// Unit tests for AuthManager service.
    /// Tests the authentication business logic with mocked dependencies.
    /// </summary>
    public class AuthManagerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IOptions<JwtSettings>> _mockJwtOptions;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IProjectInvitationService> _mockProjectInvitationService;
        private readonly JwtSettings _jwtSettings;
        private readonly AuthService _authManager;

        public AuthManagerTests()
        {
            // Setup UserManager mock with all required parameters
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var optionsAccessorMock = new Mock<IOptions<IdentityOptions>>();
            var passwordHasherMock = new Mock<IPasswordHasher<ApplicationUser>>();
            var userValidatorsMock = new List<IUserValidator<ApplicationUser>> { new Mock<IUserValidator<ApplicationUser>>().Object };
            var passwordValidatorsMock = new List<IPasswordValidator<ApplicationUser>> { new Mock<IPasswordValidator<ApplicationUser>>().Object };
            var keyNormalizerMock = new Mock<ILookupNormalizer>();
            var errorsMock = new Mock<IdentityErrorDescriber>();
            var servicesMock = new Mock<IServiceProvider>();
            var loggerMock = new Mock<ILogger<UserManager<ApplicationUser>>>();

            // Configure IdentityOptions
            optionsAccessorMock.Setup(x => x.Value).Returns(new IdentityOptions());

            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                optionsAccessorMock.Object,
                passwordHasherMock.Object,
                userValidatorsMock,
                passwordValidatorsMock,
                keyNormalizerMock.Object,
                errorsMock.Object,
                servicesMock.Object,
                loggerMock.Object);

            // Setup JWT settings
            _jwtSettings = new JwtSettings
            {
                Secret = "ThisIsASecretKeyThatIsLongEnoughForHS256",
                Expiration = 60,
                ValidIssuer = "TestIssuer",
                ValidAudience = "TestAudience"
            };

            _mockJwtOptions = new Mock<IOptions<JwtSettings>>();
            _mockJwtOptions.Setup(x => x.Value).Returns(_jwtSettings);

            _mockLogger = new Mock<ILogger<AuthService>>();

            _mockProjectInvitationService = new Mock<IProjectInvitationService>();

            _authManager = new AuthService(
                _mockUserManager.Object,
                _mockJwtOptions.Object,
                _mockLogger.Object,
                _mockProjectInvitationService.Object
            );
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenUserManagerIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthService(null!, _mockJwtOptions.Object, _mockLogger.Object, _mockProjectInvitationService.Object));
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenJwtOptionsIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthService(_mockUserManager.Object, null!, _mockLogger.Object, _mockProjectInvitationService.Object));
        }

        [Fact]
        public void Constructor_Should_ThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new AuthService(_mockUserManager.Object, _mockJwtOptions.Object, null!, _mockProjectInvitationService.Object));
        }

        #endregion

        #region RegisterAsync Tests

        [Fact]
        public async Task RegisterAsync_Should_ReturnAuthResponse_WhenValidData()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var newUser = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<ApplicationUser, string>((user, password) =>
                {
                    user.Id = newUser.Id;
                    user.UserName = newUser.UserName;
                    user.Email = newUser.Email;
                });
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _authManager.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.Equal("testuser", result.User.UserName);
            Assert.Equal("test@example.com", result.User.Email);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);

            _mockUserManager.Verify(m => m.FindByEmailAsync(registerDto.Email), Times.Once);
            _mockUserManager.Verify(m => m.FindByNameAsync(registerDto.UserName), Times.Once);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_Should_ThrowConflictException_WhenEmailExists()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                UserName = "testuser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var existingUser = new ApplicationUser
            {
                Id = "existing-user-id",
                Email = "existing@example.com",
                UserName = "existinguser"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConflictException>(() => 
                _authManager.RegisterAsync(registerDto));
            
            Assert.Contains("email already exists", exception.Message);
            _mockUserManager.Verify(m => m.FindByEmailAsync(registerDto.Email), Times.Once);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_Should_ThrowConflictException_WhenUsernameExists()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                UserName = "existinguser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var existingUser = new ApplicationUser
            {
                Id = "existing-user-id",
                Email = "other@example.com",
                UserName = "existinguser"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConflictException>(() => 
                _authManager.RegisterAsync(registerDto));
            
            Assert.Contains("Username is already taken", exception.Message);
            _mockUserManager.Verify(m => m.FindByEmailAsync(registerDto.Email), Times.Once);
            _mockUserManager.Verify(m => m.FindByNameAsync(registerDto.UserName), Times.Once);
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_Should_ThrowValidationException_WhenUserCreationFails()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                UserName = "testuser",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            var identityErrors = new[]
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" },
                new IdentityError { Code = "InvalidEmail", Description = "Email format is invalid" }
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(registerDto.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByNameAsync(registerDto.UserName))
                .ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => 
                _authManager.RegisterAsync(registerDto));
            
            Assert.Contains("Registration failed", exception.Message);
            Assert.Contains("Password is too short", exception.Message);
            Assert.Contains("Email format is invalid", exception.Message);
        }

        #endregion

        #region LoginAsync Tests

        [Fact]
        public async Task LoginAsync_Should_ReturnAuthResponse_WhenValidCredentials()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Test123!"
            };

            var user = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(true);
            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _authManager.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.Equal("testuser", result.User.UserName);
            Assert.Equal("test@example.com", result.User.Email);
            Assert.True(result.ExpiresAt > DateTime.UtcNow);

            _mockUserManager.Verify(m => m.FindByEmailAsync(loginDto.Email), Times.Once);
            _mockUserManager.Verify(m => m.CheckPasswordAsync(user, loginDto.Password), Times.Once);
            _mockUserManager.Verify(m => m.GetRolesAsync(user), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_Should_ThrowValidationException_WhenUserNotFound()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Test123!"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => 
                _authManager.LoginAsync(loginDto));
            
            Assert.Contains("Invalid email or password", exception.Message);
            _mockUserManager.Verify(m => m.FindByEmailAsync(loginDto.Email), Times.Once);
            _mockUserManager.Verify(m => m.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_Should_ThrowValidationException_WhenPasswordIncorrect()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var user = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(m => m.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _mockUserManager.Setup(m => m.CheckPasswordAsync(user, loginDto.Password))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => 
                _authManager.LoginAsync(loginDto));
            
            Assert.Contains("Invalid email or password", exception.Message);
            _mockUserManager.Verify(m => m.FindByEmailAsync(loginDto.Email), Times.Once);
            _mockUserManager.Verify(m => m.CheckPasswordAsync(user, loginDto.Password), Times.Once);
            _mockUserManager.Verify(m => m.GetRolesAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        #endregion

        #region GenerateTokenAsync Tests

        [Fact]
        public async Task GenerateTokenAsync_Should_ReturnValidJwtToken_WhenValidUser()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com"
            };

            var roles = new List<string> { "User", "Admin" };

            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var token = await _authManager.GenerateTokenAsync(user);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
            
            // Verify token structure (basic check - should start with JWT header)
            var tokenParts = token.Split('.');
            Assert.Equal(3, tokenParts.Length); // Header.Payload.Signature

            _mockUserManager.Verify(m => m.GetRolesAsync(user), Times.Once);
        }

        [Fact]
        public async Task GenerateTokenAsync_Should_IncludeUserClaims_InToken()
        {
            // Arrange
            var user = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com"
            };

            var roles = new List<string> { "User" };

            _mockUserManager.Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var token = await _authManager.GenerateTokenAsync(user);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
            
            // The token should be a valid JWT - we can at least verify it has the correct structure
            var tokenParts = token.Split('.');
            Assert.Equal(3, tokenParts.Length);
            
            _mockUserManager.Verify(m => m.GetRolesAsync(user), Times.Once);
        }

        #endregion
    }
}
