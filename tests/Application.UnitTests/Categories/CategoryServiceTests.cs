using FluentAssertions;
using Moq;
using HungStore.Application.Categories;
using HungStore.Application.Categories.Dtos;
using HungStore.Domain.Entities;
using HungStore.Domain.Interfaces;

namespace HungStore.Application.UnitTests.Categories;

public class CategoryServiceTests
{
    private readonly Mock<ICategoryRepository> _categoryRepositoryMock = new();
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categoryRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownParent_ReturnsFailure()
    {
        var request = new CreateCategoryRequest { Name = "Phones", ParentCategoryId = Guid.NewGuid() };
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(request.ParentCategoryId!.Value)).ReturnsAsync((Category?)null);

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithValidParent_ReturnsSuccess()
    {
        var parentId = Guid.NewGuid();
        var parent = new Category { Id = parentId, Name = "Electronics" };
        var request = new CreateCategoryRequest { Name = "Phones", ParentCategoryId = parentId };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(parentId)).ReturnsAsync(parent);
        _categoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Category>())).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        result.Succeeded.Should().BeTrue();
        result.Data!.ParentCategoryName.Should().Be("Electronics");
    }

    [Fact]
    public async Task UpdateAsync_SettingSelfAsParent_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        var category = new Category { Id = id, Name = "Phones" };
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(category);

        var result = await _sut.UpdateAsync(id, new UpdateCategoryRequest { Name = "Phones", ParentCategoryId = id });

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithChildCategories_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(new Category { Id = id, Name = "Electronics" });
        _categoryRepositoryMock.Setup(x => x.HasChildCategoriesAsync(id)).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(id);

        result.Succeeded.Should().BeFalse();
        _categoryRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithProducts_ReturnsFailure()
    {
        var id = Guid.NewGuid();
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(new Category { Id = id, Name = "Phones" });
        _categoryRepositoryMock.Setup(x => x.HasChildCategoriesAsync(id)).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(x => x.HasProductsAsync(id)).ReturnsAsync(true);

        var result = await _sut.DeleteAsync(id);

        result.Succeeded.Should().BeFalse();
        _categoryRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithNoChildrenOrProducts_Succeeds()
    {
        var id = Guid.NewGuid();
        var category = new Category { Id = id, Name = "Phones" };
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(category);
        _categoryRepositoryMock.Setup(x => x.HasChildCategoriesAsync(id)).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(x => x.HasProductsAsync(id)).ReturnsAsync(false);
        _categoryRepositoryMock.Setup(x => x.DeleteAsync(category)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(id);

        result.Succeeded.Should().BeTrue();
    }
}
