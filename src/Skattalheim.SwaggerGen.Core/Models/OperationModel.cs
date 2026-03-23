namespace Skattalheim.SwaggerGen.Core.Models;

/// <summary>
/// 参数信息模型，用于表示 API 操作的参数
/// </summary>
public class ParamInfo
{
    /// <summary>
    /// 参数名称
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// 参数类型
    /// </summary>
    public string Type { get; set; } = "";
    
    /// <summary>
    /// 是否为可选参数
    /// </summary>
    public bool Optional { get; set; }
    
    /// <summary>
    /// 参数来源：path | query | body
    /// </summary>
    public string Source { get; set; } = "";
}

/// <summary>
/// 操作模型，用于表示 Swagger/OpenAPI 规范中的 API 操作
/// </summary>
public class OperationModel
{
    /// <summary>
    /// 操作摘要
    /// </summary>
    public string Summary { get; set; } = "";
    
    /// <summary>
    /// 操作描述
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string Method { get; set; } = "get";
    
    /// <summary>
    /// API 路径
    /// </summary>
    public string Path { get; set; } = "";
    
    /// <summary>
    /// 生成的函数名称
    /// </summary>
    public string FunctionName { get; set; } = "";
    
    /// <summary>
    /// 函数参数签名
    /// </summary>
    public string ParamsSignature { get; set; } = "";
    
    /// <summary>
    /// 返回类型
    /// </summary>
    public string ReturnType { get; set; } = "void";
    
    /// <summary>
    /// URL 表达式
    /// </summary>
    public string UrlExpression { get; set; } = "''";
    
    /// <summary>
    /// Axios 配置参数
    /// </summary>
    public string AxiosConfig { get; set; } = "";
    
    /// <summary>
    /// 需要导入的类型列表
    /// </summary>
    public List<string> Imports { get; set; } = [];
    
    /// <summary>
    /// 参数信息列表
    /// </summary>
    public List<ParamInfo> Params { get; set; } = [];
}

/// <summary>
/// API 文件模型，用于表示生成的 TypeScript API 文件
/// </summary>
public class ApiFileModel
{
    /// <summary>
    /// 标签名称
    /// </summary>
    public string TagName { get; set; } = "";
    
    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = "";
    
    /// <summary>
    /// Axios 封装导入路径
    /// </summary>
    public string RequestImport { get; set; } = "@/utils/request";
    
    /// <summary>
    /// 需要导入的类型列表
    /// </summary>
    public List<string> Imports { get; set; } = [];
    
    /// <summary>
    /// 操作模型列表
    /// </summary>
    public List<OperationModel> Operations { get; set; } = [];
}