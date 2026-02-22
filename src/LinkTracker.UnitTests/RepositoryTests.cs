using LinkTracker.Bot.Repository;

namespace LinkTracker.UnitTests;

public class RepositoryTests
{
    private IUsersRepository repository;
    [SetUp]
    public void Setup()
    {
        repository = new DefaultRepository();
    }

    [Test]
    public async Task Store_Load_Existance_Test()
    {
        await repository.SaveUserState(1, this);
        Assert.That(await repository.LoadUserState(1), Is.SameAs(this));
    }

    [Test]
    public async Task Load_Unexistance_Test()
    {
        Assert.That(await repository.LoadUserState(2), Is.Null);
    }

    [Test]
    public async Task Store_Remove_Load_Test()
    {
        await repository.SaveUserState(3, this);
        await repository.SaveUserState(3, null);
        Assert.That(await repository.LoadUserState(3), Is.Null);
    }

    [Test]
    public async Task Store_Overwrite_Load_Test()
    {
        await repository.SaveUserState(4, this);
        await repository.SaveUserState(4, 42);
        Assert.That(await repository.LoadUserState(4), Is.EqualTo(42));
    }
}
