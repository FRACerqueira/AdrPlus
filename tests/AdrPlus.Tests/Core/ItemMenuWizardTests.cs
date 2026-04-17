// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;

namespace AdrPlus.Tests.Core;

public class ItemMenuWizardTests
{
    [Fact]
    public void ItemMenuWizard_DefaultValues_AreSetCorrectly()
    {
        // Arrange & Act
        var item = new ItemMenuWizard();

        // Assert
        item.Id.Should().Be(string.Empty);
        item.Title.Should().Be(string.Empty);
        item.Description.Should().Be(string.Empty);
        item.EnabledWhenNotConfigured.Should().BeFalse();
    }

    [Fact]
    public void ItemMenuWizard_AllProperties_CanBeSet()
    {
        // Arrange & Act
        var item = new ItemMenuWizard
        {
            Id = "test-id",
            Title = "Test Title",
            Description = "Test Description",
            EnabledWhenNotConfigured = true
        };

        // Assert
        item.Id.Should().Be("test-id");
        item.Title.Should().Be("Test Title");
        item.Description.Should().Be("Test Description");
        item.EnabledWhenNotConfigured.Should().BeTrue();
    }

    [Fact]
    public void ItemMenuWizard_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var item1 = new ItemMenuWizard
        {
            Id = "test",
            Title = "Title",
            Description = "Desc",
            EnabledWhenNotConfigured = true
        };

        var item2 = new ItemMenuWizard
        {
            Id = "test",
            Title = "Title",
            Description = "Desc",
            EnabledWhenNotConfigured = true
        };

        // Act & Assert
        item1.Should().Be(item2);
    }

    [Fact]
    public void ItemMenuWizard_RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var item1 = new ItemMenuWizard { Id = "test1", Title = "Title1" };
        var item2 = new ItemMenuWizard { Id = "test2", Title = "Title2" };

        // Act & Assert
        item1.Should().NotBe(item2);
    }

    [Fact]
    public void ItemMenuWizard_WithExpression_CreatesNewInstance()
    {
        // Arrange
        var original = new ItemMenuWizard
        {
            Id = "original",
            Title = "Original Title",
            Description = "Original Desc",
            EnabledWhenNotConfigured = false
        };

        // Act
        var modified = original with { Title = "Modified Title" };

        // Assert
        modified.Id.Should().Be("original");
        modified.Title.Should().Be("Modified Title");
        modified.Description.Should().Be("Original Desc");
        modified.EnabledWhenNotConfigured.Should().BeFalse();
        original.Title.Should().Be("Original Title");
    }
}
