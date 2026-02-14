using System.ComponentModel.DataAnnotations;

namespace Jullius.Domain.Domain.Entities;

public class Category
{
    [Key]
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Color { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Category(string name, string color)
    {
        Id = Guid.NewGuid();
        Name = name;
        Color = color;
        CreatedAt = DateTime.UtcNow;

        Validate();
    }

    // For Entity Framework
    private Category() { }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Name cannot be empty");

        if (string.IsNullOrWhiteSpace(Color))
            throw new ArgumentException("Color cannot be empty");
    }

    public void Update(string name, string color)
    {
        Name = name;
        Color = color;

        Validate();
    }
}

