namespace Skattalheim.SwaggerGen.Core.Models;

/// <summary>
/// 类型别名模型，用于表示 Swagger/OpenAPI 规范中的枚举类型
/// </summary>
public class TypeAliasModel
{
    /// <summary>
    /// 类型别名名称（PascalCase 格式）
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 联合类型字符串（例如：'value1' | 'value2'）
    /// </summary>
    public string Union { get; set; } = "unknown";
}