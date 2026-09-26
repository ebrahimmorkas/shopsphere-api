using ShopSphere.Domain.Categories;

namespace ShopSphere.Domain.UnitTests.Categories;

public class CategoryTests
{
    [Theory]
    [InlineData("Home & Kitchen", "home-kitchen")]
    [InlineData("  Gaming   Laptops ", "gaming-laptops")]
    [InlineData("4K TVs!", "4k-tvs")]
    public void Create_Should_Generate_Url_Friendly_Slug(string name, string expectedSlug)
    {
        Category.Create(name, null).Value.Slug.ShouldBe(expectedSlug);
    }

    [Fact]
    public void Create_Should_Fail_When_Name_Is_Empty()
    {
        Category.Create(" ", null).Error.ShouldBe(CategoryErrors.NameRequired);
    }
}
