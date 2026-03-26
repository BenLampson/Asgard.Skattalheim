namespace Skattalheim.SwaggerGen.Core.Models;

/// <summary>
/// 接口模型，用于表示 Swagger/OpenAPI 规范中的对象类型
/// </summary>
public class InterfaceModel
{
    /// <summary>
    /// 接口名称（PascalCase 格式）
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 接口描述
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 接口属性列表
    /// </summary>
    public List<PropertyModel> Properties { get; set; } = [];
    
    /// <summary>
    /// 接口分组
    /// </summary>
    public string Group { get; set; } = "default";
}

/// <summary>
/// 属性模型，用于表示接口的单个属性
/// </summary>
public class PropertyModel
{
    /// <summary>
    /// 属性名称（camelCase 格式）
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 属性类型（TypeScript 类型字符串）
    /// </summary>
    public string Type { get; set; } = "unknown";
    
    /// <summary>
    /// 是否为可选属性
    /// </summary>
    public bool Optional { get; set; }
    
    /// <summary>
    /// 属性描述
    /// </summary>
    public string? Description { get; set; }
}