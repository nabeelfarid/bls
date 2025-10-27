using BlsApi.Models;
using BlsApi.Utils;
using FluentAssertions;
using Xunit;

namespace BlsApi.Tests.Utils
{
    public class ValidationHelperTests
    {
        [Fact]
        public void Validate_WithValidBook_ShouldReturnTrue()
        {
            // Arrange
            var book = new Book
            {
                Title = "The Pragmatic Programmer",
                Author = "David Thomas",
                ISBN = "978-0135957059"
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_WithEmptyTitle_ShouldReturnFalse()
        {
            // Arrange
            var book = new Book
            {
                Title = "",
                Author = "David Thomas",
                ISBN = "978-0135957059"
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Title is required");
        }

        [Fact]
        public void Validate_WithEmptyAuthor_ShouldReturnFalse()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test Book",
                Author = "",
                ISBN = "978-0135957059"
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Author is required");
        }

        [Fact]
        public void Validate_WithEmptyISBN_ShouldReturnFalse()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test Book",
                Author = "Test Author",
                ISBN = ""
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("ISBN is required");
        }

        [Fact]
        public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var book = new Book
            {
                Title = "",
                Author = "",
                ISBN = ""
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().HaveCount(3);
            errors.Should().Contain("Title is required");
            errors.Should().Contain("Author is required");
            errors.Should().Contain("ISBN is required");
        }

        [Fact]
        public void Validate_WithTitleTooLong_ShouldReturnFalse()
        {
            // Arrange
            var book = new Book
            {
                Title = new string('A', 501), // 501 characters
                Author = "Test Author",
                ISBN = "978-0135957059"
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Title must be between 1 and 500 characters");
        }

        [Fact]
        public void Validate_WithAuthorTooLong_ShouldReturnFalse()
        {
            // Arrange
            var book = new Book
            {
                Title = "Test Book",
                Author = new string('A', 201), // 201 characters
                ISBN = "978-0135957059"
            };

            // Act
            var (isValid, errors) = ValidationHelper.Validate(book);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Author must be between 1 and 200 characters");
        }

        [Fact]
        public void Validate_WithNullObject_ShouldReturnFalse()
        {
            // Act
            var (isValid, errors) = ValidationHelper.Validate<Book>(null!);

            // Assert
            isValid.Should().BeFalse();
            errors.Should().Contain("Object cannot be null");
        }
    }
}

