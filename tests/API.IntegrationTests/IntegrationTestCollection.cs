namespace HungStore.API.IntegrationTests;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}
