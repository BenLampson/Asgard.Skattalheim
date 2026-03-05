namespace Skattalheim.SwaggerGen.Models;

public class ParamInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Optional { get; set; }
    /// <summary>path | query | body</summary>
    public string Source { get; set; } = "";
}

public class OperationModel
{
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Method { get; set; } = "get";
    public string Path { get; set; } = "";
    public string FunctionName { get; set; } = "";
    public string ParamsSignature { get; set; } = "";
    public string ReturnType { get; set; } = "void";
    public string UrlExpression { get; set; } = "''";
    public string AxiosConfig { get; set; } = "";
    public List<string> Imports { get; set; } = [];
    public List<ParamInfo> Params { get; set; } = [];
}

public class ApiFileModel
{
    public string TagName { get; set; } = "";
    public string FileName { get; set; } = "";
    public string RequestImport { get; set; } = "@/utils/request";
    public List<string> Imports { get; set; } = [];
    public List<OperationModel> Operations { get; set; } = [];
}
