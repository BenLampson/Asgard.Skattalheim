namespace Skattalheim.SwaggerGen.Models;

public class InterfaceModel
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<PropertyModel> Properties { get; set; } = [];
}

public class PropertyModel
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "unknown";
    public bool Optional { get; set; }
    public string? Description { get; set; }
}
