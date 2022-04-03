using AutoBogus;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Tests.Fakers;
using Bogus;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Integration.Repos;

public class LoginRepoTests
{
    // test GetByNameAsync valid name
    [Fact]
    public async Task GetByNameAsync_ValidName_ReturnsLogin()
    {
        // Arrange
        var logins = AutoFaker.Generate<LoginModel>(5);
        var expectedLogin = logins[1];

        using var context = Fakes.CreateWrContext(context =>
        {
            foreach (var login in logins)
            {
                context.Logins.Add(expectedLogin);
            }
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = await repo.GetByNameAsync(expectedLogin.Name);

        // Assert
        Assert.NotNull(actualLoginModel);

        foreach (var property in typeof(LoginModel).GetProperties())
        {
            Assert.Equal(property.GetValue(expectedLogin), property.GetValue(actualLoginModel));
        }
    }

    // test GetByNameAsync invalid name
    [Fact]
    public async Task GetByNameAsync_InvalidName_ReturnsNull()
    {
        // Arrange
        var expectedLogin = AutoFaker.Generate<LoginModel>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
            context.Logins.Add(expectedLogin);
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = await repo.GetByNameAsync("invalid");

        // Assert
        Assert.Null(actualLoginModel);
    }

    // test GetByName valid name
    [Fact]
    public void GetByName_ValidName_ReturnsLogin()
    {
        // Arrange
        var expectedLogin = AutoFaker.Generate<LoginModel>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
            context.Logins.Add(expectedLogin);
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = repo.GetByName(expectedLogin.Name);

        // Assert
        Assert.NotNull(actualLoginModel);

        foreach (var property in typeof(LoginModel).GetProperties())
        {
            Assert.Equal(property.GetValue(expectedLogin), property.GetValue(actualLoginModel));
        }
    }

    // test GetByName invalid name
    [Fact]
    public void GetByName_InvalidName_ReturnsNull()
    {
        // Arrange
        var expectedLogin = AutoFaker.Generate<LoginModel>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
            context.Logins.Add(expectedLogin);
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = repo.GetByName("invalid");

        // Assert
        Assert.Null(actualLoginModel);
    }

    [Fact]
    public async Task GetOrAddAsync_ExistingLogin_ReturnsThatLogin()
    {
        // Arrange
        var expectedLogin = AutoFaker.Generate<LoginModel>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
            context.Logins.Add(expectedLogin);
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = await repo.GetOrAddAsync(expectedLogin.Game, expectedLogin.Name, expectedLogin.Nickname!);

        // Assert
        Assert.NotNull(actualLoginModel);

        foreach (var property in typeof(LoginModel).GetProperties())
        {
            Assert.Equal(property.GetValue(expectedLogin), property.GetValue(actualLoginModel));
        }
    }
    
    [Fact]
    public async Task GetOrAddAsync_NonExistingLogin_ReturnsNewLogin()
    {
        // Arrange
        var expectedGame = AutoFaker.Generate<GameModel>();
        var expectedName = AutoFaker.Generate<string>();
        var expectedNickname = AutoFaker.Generate<string>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = await repo.GetOrAddAsync(expectedGame, expectedName, expectedNickname);

        // Assert
        Assert.NotNull(actualLoginModel);
        Assert.Equal(expectedGame, actualLoginModel.Game);
        Assert.Equal(expectedName, actualLoginModel.Name);
        Assert.Equal(expectedNickname, actualLoginModel.Nickname);
    }

    [Fact]
    public void GetOrAdd_ExistingLogin_ReturnsThatLogin()
    {
        // Arrange
        var expectedLogin = AutoFaker.Generate<LoginModel>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
            context.Logins.Add(expectedLogin);
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = repo.GetOrAdd(expectedLogin.Game, expectedLogin.Name, expectedLogin.Nickname!);

        // Assert
        Assert.NotNull(actualLoginModel);

        foreach (var property in typeof(LoginModel).GetProperties())
        {
            Assert.Equal(property.GetValue(expectedLogin), property.GetValue(actualLoginModel));
        }
    }

    [Fact]
    public void GetOrAdd_NonExistingLogin_ReturnsNewLogin()
    {
        // Arrange
        var expectedGame = AutoFaker.Generate<GameModel>();
        var expectedName = AutoFaker.Generate<string>();
        var expectedNickname = AutoFaker.Generate<string>();

        using var context = Fakes.CreateWrContext(context =>
        {
            context.Logins.Add(AutoFaker.Generate<LoginModel>());
        });

        var repo = new LoginRepo(context);

        // Act
        var actualLoginModel = repo.GetOrAdd(expectedGame, expectedName, expectedNickname);

        // Assert
        Assert.NotNull(actualLoginModel);
        Assert.Equal(expectedGame, actualLoginModel.Game);
        Assert.Equal(expectedName, actualLoginModel.Name);
        Assert.Equal(expectedNickname, actualLoginModel.Nickname);
    }
}
