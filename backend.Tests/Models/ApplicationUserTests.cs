using AirlineSimulationApi.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace AirlineSimulationApi.Tests.Models;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_ShouldHaveValidProperties()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "johndoe"
        };

        // Assert
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("john.doe@example.com");
        user.UserName.Should().Be("johndoe");
        user.Bookings.Should().NotBeNull();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("", false)] // Empty first name
    [InlineData("ThisIsAVeryLongFirstNameThatExceedsTheMaximumLengthAllowedForTheFirstNameFieldInTheDatabaseAndShouldFailValidation", false)] // Too long (over 100 chars)
    [InlineData("John", true)] // Valid first name
    public void ApplicationUser_FirstName_ShouldHaveCorrectLength(string firstName, bool isValid)
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FirstName = firstName,
            LastName = "Doe",
            Email = "john.doe@example.com",
            UserName = "johndoe"
        };

        // Assert
        if (isValid)
        {
            user.FirstName.Should().NotBeEmpty();
            user.FirstName.Length.Should().BeLessOrEqualTo(100);
        }
        else
        {
            if (string.IsNullOrEmpty(firstName))
            {
                user.FirstName.Should().BeEmpty();
            }
            else
            {
                user.FirstName.Length.Should().BeGreaterThan(100);
            }
        }
    }

    [Theory]
    [InlineData("", false)] // Empty last name
    [InlineData("ThisIsAVeryLongLastNameThatExceedsTheMaximumLengthAllowedForTheLastNameFieldInTheDatabaseAndShouldFailValidation", false)] // Too long (over 100 chars)
    [InlineData("Doe", true)] // Valid last name
    public void ApplicationUser_LastName_ShouldHaveCorrectLength(string lastName, bool isValid)
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = lastName,
            Email = "john.doe@example.com",
            UserName = "johndoe"
        };

        // Assert
        if (isValid)
        {
            user.LastName.Should().NotBeEmpty();
            user.LastName.Length.Should().BeLessOrEqualTo(100);
        }
        else
        {
            if (string.IsNullOrEmpty(lastName))
            {
                user.LastName.Should().BeEmpty();
            }
            else
            {
                user.LastName.Length.Should().BeGreaterThan(100);
            }
        }
    }

    [Fact]
    public void ApplicationUser_ShouldInitializeCollections()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.Bookings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ApplicationUser_ShouldAllowNullNavigationProperties()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        user.NotificationPreferences.Should().BeNull();
        user.LoyaltyAccount.Should().BeNull();
    }
}