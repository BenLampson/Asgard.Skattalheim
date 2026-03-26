using Skattalheim.SwaggerGen.Core.Models;
using System.Text.Json;

namespace Skattalheim.SwaggerGen.Core.Services;

/// <summary>
/// Swagger 解析器，用于解析 Swagger/OpenAPI 规范并生成 TypeScript 代码模型
/// </summary>
public static class SwaggerParser
{
    /// <summary>
    /// 解析 Swagger 规范中的 Schemas 部分，生成接口和类型别名
    /// </summary>
    /// <param name="root">Swagger 文档的根元素</param>
    /// <param name="pathSplit">路径切割的起始位置</param>
    /// <returns>接口模型列表和类型别名模型列表</returns>
    public static (List<InterfaceModel> Interfaces, List<TypeAliasModel> Types) ParseSchemas(JsonElement root, int pathSplit = 2)
    {
        var interfaces = new List<InterfaceModel>();
        var types      = new List<TypeAliasModel>();

        JsonElement schemasEl;
        // 支持 OpenAPI 3.x (components.schemas) 和 Swagger 2.0 (definitions)
        if (root.TryGetProperty("components", out var comps) && comps.TryGetProperty("schemas", out schemasEl))
        { /* 3.x */ }
        else if (!root.TryGetProperty("definitions", out schemasEl))
            return (interfaces, types);

        // 收集所有路径信息，用于接口和类型的分组
        var pathGroups = new Dictionary<string, List<string>>();
        if (root.TryGetProperty("paths", out var pathsEl))
        {
            foreach (var pathProp in pathsEl.EnumerateObject())
            {
                var pathStr  = pathProp.Name;
                var pathSegments = pathStr.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length >= pathSplit)
                {
                    // 从指定位置开始提取前缀
                    var startIndex = pathSplit - 1;
                    var endIndex = Math.Min(startIndex + 2, pathSegments.Length);
                    var prefixSegments = pathSegments.Skip(startIndex).Take(endIndex - startIndex).ToList();
                    var groupName = string.Join("_", prefixSegments);
                    
                    if (!pathGroups.TryGetValue(groupName, out var paths))
                    {
                        paths = new List<string>();
                        pathGroups[groupName] = paths;
                    }
                    paths.Add(pathStr);
                }
            }
        }

        foreach (var prop in schemasEl.EnumerateObject())
        {
            var name   = prop.Name;
            var schema = prop.Value;

            // 为接口和类型分配分组
            var groupName = "default";
            // 使用与API相同的分组逻辑：根据路径前缀分组
            if (root.TryGetProperty("paths", out var pathsElForGroup))
            {
                foreach (var pathProp in pathsElForGroup.EnumerateObject())
                {
                    var pathStr  = pathProp.Name;
                    var pathSegments = pathStr.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (pathSegments.Length >= pathSplit)
                    {
                        // 从指定位置开始提取前缀
                        var startIndex = pathSplit - 1;
                        var endIndex = Math.Min(startIndex + 2, pathSegments.Length);
                        var prefixSegments = pathSegments.Skip(startIndex).Take(endIndex - startIndex).ToList();
                        var currentGroup = string.Join("_", prefixSegments);
                        
                        // 检查该路径的操作是否使用了当前模式
                        var pathItem = pathProp.Value;
                        var httpMethods = new[] { "get", "post", "put", "delete", "patch", "head", "options" };
                        foreach (var method in httpMethods)
                        {
                            if (!pathItem.TryGetProperty(method, out var operation)) continue;
                            
                            // 检查请求体
                            if (operation.TryGetProperty("requestBody", out var reqBody))
                            {
                                var bodySchema = GetJsonContentSchema(reqBody);
                                if (bodySchema.HasValue)
                                {
                                    var bodyType = ResolveType(bodySchema.Value);
                                    if (bodyType == ToPascalCase(name))
                                    {
                                        groupName = currentGroup;
                                        goto GroupFound;
                                    }
                                }
                            }
                            
                            // 检查响应
                            if (operation.TryGetProperty("responses", out var responses))
                            {
                                foreach (var code in new[] { "200", "201" })
                                {
                                    if (!responses.TryGetProperty(code, out var resp)) continue;
                                    var respSchema = GetJsonContentSchema(resp);
                                    if (respSchema.HasValue)
                                    {
                                        var respType = ResolveType(respSchema.Value);
                                        if (respType == ToPascalCase(name))
                                        {
                                            groupName = currentGroup;
                                            goto GroupFound;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            GroupFound:;

            if (IsEnumSchema(schema))
            {
                // 解析枚举类型，生成类型别名
                var values = GetEnumValues(schema);
                types.Add(new TypeAliasModel
                {
                    Name  = ToPascalCase(name),
                    Union = values.Count > 0 ? string.Join(" | ", values) : "string",
                    Group = groupName
                });
            }
            else if (IsObjectSchema(schema))
            {
                // 解析对象类型，生成接口
                var interfaceModel = BuildInterface(name, schema);
                interfaceModel.Group = groupName;
                interfaces.Add(interfaceModel);
            }
        }

        return (interfaces, types);
    }

    /// <summary>
    /// 解析 Swagger 规范中的 Operations 部分，生成 API 文件模型
    /// </summary>
    /// <param name="root">Swagger 文档的根元素</param>
    /// <param name="requestImport">Axios 封装导入路径</param>
    /// <param name="interfaces">接口模型列表</param>
    /// <param name="types">类型别名模型列表</param>
    /// <param name="pathSplit">路径切割的起始位置</param>
    /// <returns>API 文件模型列表</returns>
    public static List<ApiFileModel> ParseOperations(
        JsonElement root,
        string requestImport,
        List<InterfaceModel> interfaces,
        List<TypeAliasModel> types,
        int pathSplit = 2)
    {
        var knownInterfaces = interfaces.Select(i => i.Name).ToHashSet();
        var knownTypes      = types.Select(t => t.Name).ToHashSet();

        var apiFiles = new Dictionary<string, ApiFileModel>();
        var opIdCounter = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (!root.TryGetProperty("paths", out var pathsEl))
            return new List<ApiFileModel>();

        var httpMethods = new[] { "get", "post", "put", "delete", "patch", "head", "options" };

        foreach (var pathProp in pathsEl.EnumerateObject())
        {
            var pathStr  = pathProp.Name;
            var pathItem = pathProp.Value;

            // 收集路径级别的参数
            var pathLevelParams = new List<JsonElement>();
            if (pathItem.TryGetProperty("parameters", out var pathParams))
                pathLevelParams = pathParams.EnumerateArray().ToList();

            foreach (var method in httpMethods)
            {
                if (!pathItem.TryGetProperty(method, out var operation)) continue;

                var allParams = pathLevelParams.ToList();
                if (operation.TryGetProperty("parameters", out var opParams))
                    allParams.AddRange(opParams.EnumerateArray());

                // 从路径中提取前缀作为分组依据
                var prefix = "default";
                var pathSegments = pathStr.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length >= pathSplit)
                {
                    // 从指定位置开始提取前缀
                    var startIndex = pathSplit - 1;
                    var endIndex = Math.Min(startIndex + 2, pathSegments.Length);
                    var prefixSegments = pathSegments.Skip(startIndex).Take(endIndex - startIndex).ToList();
                    prefix = string.Join("_", prefixSegments);
                }

                // 确保前缀对应的文件模型存在
                if (!apiFiles.TryGetValue(prefix, out var apiFile))
                {
                    // 生成文件名，添加 api 前缀
                    var fileName = "api" + ToPascalCase(prefix);
                    // 移除所有非字母数字字符
                    fileName = new string(fileName.Where(char.IsLetterOrDigit).ToArray());
                    // 确保首字母小写
                    if (fileName.Length > 0)
                        fileName = char.ToLower(fileName[0]) + fileName.Substring(1);

                    apiFile = new ApiFileModel
                    {
                        TagName       = prefix,
                        FileName      = fileName,
                        RequestImport = requestImport,
                    };
                    apiFiles[prefix] = apiFile;
                }

                var opModel = BuildOperation(pathStr, method, operation, allParams, knownInterfaces, knownTypes, opIdCounter);
                apiFile.Operations.Add(opModel);

                // 收集需要导入的类型
                foreach (var imp in opModel.Imports)
                    if (!apiFile.Imports.Contains(imp))
                        apiFile.Imports.Add(imp);
            }
        }

        return apiFiles.Values.ToList();
    }

    /// <summary>
    /// 构建接口模型
    /// </summary>
    /// <param name="name">接口名称</param>
    /// <param name="schema">Schema 元素</param>
    /// <returns>接口模型</returns>
    private static InterfaceModel BuildInterface(string name, JsonElement schema)
    {
        var model    = new InterfaceModel
        {
            Name        = ToPascalCase(name),
            Description = schema.TryGetProperty("description", out var schemaDesc) ? schemaDesc.GetString() : null
        };
        var required = new HashSet<string>();

        // 收集必填字段
        if (schema.TryGetProperty("required", out var req))
            foreach (var r in req.EnumerateArray())
                required.Add(r.GetString() ?? "");

        // 处理 allOf 继承
        if (schema.TryGetProperty("allOf", out var allOf))
        {
            foreach (var sub in allOf.EnumerateArray())
            {
                if (sub.TryGetProperty("$ref", out var refEl))
                    model.Properties.Add(new PropertyModel { Name = "// extends " + RefToName(refEl.GetString()), Type = "", Optional = false });
                else if (sub.TryGetProperty("properties", out var subProps))
                    AppendProperties(model, subProps, required);
            }
            return model;
        }

        // 处理普通属性
        if (schema.TryGetProperty("properties", out var props))
            AppendProperties(model, props, required);

        return model;
    }

    /// <summary>
    /// 向接口模型添加属性
    /// </summary>
    /// <param name="model">接口模型</param>
    /// <param name="props">属性元素</param>
    /// <param name="required">必填字段集合</param>
    private static void AppendProperties(InterfaceModel model, JsonElement props, HashSet<string> required)
    {
        foreach (var p in props.EnumerateObject())
        {
            model.Properties.Add(new PropertyModel
            {
                Name        = ToCamelCase(p.Name),
                Type        = ResolveType(p.Value),
                Optional    = !required.Contains(p.Name),
                Description = p.Value.TryGetProperty("description", out var d) ? d.GetString() : null
            });
        }
    }

    /// <summary>
    /// 构建操作模型
    /// </summary>
    /// <param name="path">API 路径</param>
    /// <param name="method">HTTP 方法</param>
    /// <param name="operation">操作元素</param>
    /// <param name="allParams">所有参数</param>
    /// <param name="knownInterfaces">已知接口集合</param>
    /// <param name="knownTypes">已知类型集合</param>
    /// <param name="opIdCounter">操作 ID 计数器</param>
    /// <returns>操作模型</returns>
    private static OperationModel BuildOperation(
        string path,
        string method,
        JsonElement operation,
        List<JsonElement> allParams,
        HashSet<string> knownInterfaces,
        HashSet<string> knownTypes,
        Dictionary<string, int> opIdCounter)
    {
        var imports = new List<string>();

        var rawFnName = BuildFunctionName(method, path);

        // 处理函数名冲突
        if (opIdCounter.TryGetValue(rawFnName, out var cnt))
        {
            opIdCounter[rawFnName] = cnt + 1;
            rawFnName += (cnt + 1).ToString();
        }
        else
            opIdCounter[rawFnName] = 0;

        var functionName = rawFnName;
        var summary     = operation.TryGetProperty("summary", out var sum) ? sum.GetString() ?? "" : "";
        var description = operation.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";

        var pathParams  = allParams.Where(p => GetStr(p, "in") == "path").ToList();
        var queryParams = allParams.Where(p => GetStr(p, "in") == "query").ToList();
        var sigParts    = new List<string>();
        var paramInfos  = new List<ParamInfo>();

        // 处理路径参数
        foreach (var p in pathParams)
        {
            var pName = ToCamelCase(GetStr(p, "name") ?? "id");
            var pType = p.TryGetProperty("schema", out var ps) ? ResolveType(ps) : "string";
            sigParts.Add($"{pName}: {pType}");
            paramInfos.Add(new ParamInfo { Name = pName, Type = pType, Optional = false, Source = "path" });
        }

        // 处理请求体
        string? bodyTypeName = null;
        if (operation.TryGetProperty("requestBody", out var reqBody))
        {
            var bodySchema = GetJsonContentSchema(reqBody);
            if (bodySchema.HasValue)
            {
                bodyTypeName = ResolveType(bodySchema.Value);
                sigParts.Add($"data: {bodyTypeName}");
                CollectImport(bodyTypeName, knownInterfaces, knownTypes, imports);
                paramInfos.Add(new ParamInfo { Name = "data", Type = bodyTypeName, Optional = false, Source = "body" });
            }
        }

        // 处理查询参数
        if (queryParams.Count > 0)
        {
            // 检查是否有单个查询参数是一个对象引用
            if (queryParams.Count == 1)
            {
                var p = queryParams[0];
                if (p.TryGetProperty("schema", out var ps) && ps.TryGetProperty("$ref", out var refVal))
                {
                    var queryTypeName = RefToName(refVal.GetString());
                    sigParts.Add($"params?: {queryTypeName}");
                    paramInfos.Add(new ParamInfo { Name = "params", Type = queryTypeName, Optional = true, Source = "query" });
                    CollectImport(queryTypeName, knownInterfaces, knownTypes, imports);
                }
                else
                {
                    // 构建内联类型
                    var fields = queryParams.Select(p =>
                    {
                        var pName     = ToCamelCase(GetStr(p, "name") ?? "param");
                        var pType     = p.TryGetProperty("schema", out var ps) ? ResolveType(ps) : "string";
                        var pOptional = !(p.TryGetProperty("required", out var pr) && pr.GetBoolean());
                        return $"{pName}{(pOptional ? "?" : "")}: {pType}";
                    }).ToList();
                    var inlineType = "{ " + string.Join("; ", fields) + " }";
                    sigParts.Add($"params?: {inlineType}");
                    paramInfos.Add(new ParamInfo { Name = "params", Type = inlineType, Optional = true, Source = "query" });
                }
            }
            else
            {
                // 构建内联类型
                var fields = queryParams.Select(p =>
                {
                    var pName     = ToCamelCase(GetStr(p, "name") ?? "param");
                    var pType     = p.TryGetProperty("schema", out var ps) ? ResolveType(ps) : "string";
                    var pOptional = !(p.TryGetProperty("required", out var pr) && pr.GetBoolean());
                    return $"{pName}{(pOptional ? "?" : "")}: {pType}";
                }).ToList();
                var inlineType = "{ " + string.Join("; ", fields) + " }";
                sigParts.Add($"params?: {inlineType}");
                paramInfos.Add(new ParamInfo { Name = "params", Type = inlineType, Optional = true, Source = "query" });
            }
        }

        // 处理返回类型
        var returnType = "void";
        if (operation.TryGetProperty("responses", out var responses))
        {
            foreach (var code in new[] { "200", "201" })
            {
                if (!responses.TryGetProperty(code, out var resp)) continue;
                var respSchema = GetJsonContentSchema(resp);
                if (respSchema.HasValue)
                {
                    returnType = ResolveType(respSchema.Value);
                    CollectImport(returnType, knownInterfaces, knownTypes, imports);
                    break;
                }
            }
        }

        // 构建 URL 表达式
        var urlExpr = BuildUrlExpression(path, pathParams);

        // 构建 Axios 配置
        var axiosConfig = (queryParams.Count > 0, bodyTypeName != null) switch
        {
            (true,  true)  => ", data, { params }",
            (true,  false) => ", { params }",
            (false, true)  => (method == "get" || method == "delete") ? ", { data }" : ", data",
            _              => ""
        };

        return new OperationModel
        {
            Summary         = summary,
            Description     = description,
            Method          = method,
            Path            = path,
            FunctionName    = functionName,
            ParamsSignature = string.Join(", ", sigParts),
            ReturnType      = returnType,
            UrlExpression   = urlExpr,
            AxiosConfig     = axiosConfig,
            Imports         = imports,
            Params          = paramInfos
        };
    }

    /// <summary>
    /// 获取 JSON 内容的 Schema
    /// </summary>
    /// <param name="element">元素</param>
    /// <returns>Schema 元素</returns>
    private static JsonElement? GetJsonContentSchema(JsonElement element)
    {
        if (!element.TryGetProperty("content", out var content)) return null;
        foreach (var mediaType in content.EnumerateObject())
            if (mediaType.Name.Contains("json") || mediaType.Name == "*/*")
                if (mediaType.Value.TryGetProperty("schema", out var s))
                    return s;
        return null;
    }

    /// <summary>
    /// 构建 URL 表达式
    /// </summary>
    /// <param name="path">API 路径</param>
    /// <param name="pathParams">路径参数</param>
    /// <returns>URL 表达式</returns>
    private static string BuildUrlExpression(string path, List<JsonElement> pathParams)
    {
        if (pathParams.Count == 0) return $"'"  + path + $"'";
        var ts = path;
        foreach (var p in pathParams)
        {
            var name = GetStr(p, "name") ?? "";
            ts = ts.Replace("{" + name + "}", "${" + ToCamelCase(name) + "}");
        }
        return "`" + ts + "`";
    }

    /// <summary>
    /// 收集需要导入的类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <param name="knownInterfaces">已知接口集合</param>
    /// <param name="knownTypes">已知类型集合</param>
    /// <param name="imports">导入列表</param>
    private static void CollectImport(string typeName, HashSet<string> knownInterfaces, HashSet<string> knownTypes, List<string> imports)
    {
        var bare = typeName.TrimEnd('[', ']').Trim();
        if ((knownInterfaces.Contains(bare) || knownTypes.Contains(bare)) && !imports.Contains(bare))
            imports.Add(bare);
    }

    /// <summary>
    /// 解析类型
    /// </summary>
    /// <param name="schema">Schema 元素</param>
    /// <returns>TypeScript 类型字符串</returns>
    private static string ResolveType(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var refVal))
            return RefToName(refVal.GetString());

        if (schema.TryGetProperty("anyOf", out var anyOf))
            return string.Join(" | ", anyOf.EnumerateArray().Select(ResolveType));

        if (schema.TryGetProperty("enum", out _))
        {
            var vals = GetEnumValues(schema);
            return vals.Count > 0 ? string.Join(" | ", vals) : "string";
        }

        var type = schema.TryGetProperty("type", out var t) ? t.GetString() : null;
        return type switch
        {
            "integer" => "number",
            "number"  => "number",
            "boolean" => "boolean",
            "string"  => "string",
            "array"   => schema.TryGetProperty("items", out var items) ? ResolveType(items) + "[]" : "unknown[]",
            "object"  => "Record<string, unknown>",
            _         => "unknown"
        };
    }

    /// <summary>
    /// 判断是否为枚举 Schema
    /// </summary>
    /// <param name="schema">Schema 元素</param>
    /// <returns>是否为枚举</returns>
    private static bool IsEnumSchema(JsonElement schema) => schema.TryGetProperty("enum", out _);

    /// <summary>
    /// 判断是否为对象 Schema
    /// </summary>
    /// <param name="schema">Schema 元素</param>
    /// <returns>是否为对象</returns>
    private static bool IsObjectSchema(JsonElement schema)
    {
        if (schema.TryGetProperty("type", out var t) && t.GetString() == "object") return true;
        if (schema.TryGetProperty("properties", out _)) return true;
        if (schema.TryGetProperty("allOf", out _)) return true;
        return false;
    }

    /// <summary>
    /// 获取枚举值
    /// </summary>
    /// <param name="schema">Schema 元素</param>
    /// <returns>枚举值列表</returns>
    private static List<string> GetEnumValues(JsonElement schema)
    {
        var result = new List<string>();
        if (!schema.TryGetProperty("enum", out var enumEl)) return result;
        foreach (var item in enumEl.EnumerateArray())
        {
            switch (item.ValueKind)
            {
                case JsonValueKind.String: result.Add("'" + item.GetString() + "'"); break;
                case JsonValueKind.Number: result.Add(item.GetRawText());             break;
                case JsonValueKind.True:   result.Add("true");                        break;
                case JsonValueKind.False:  result.Add("false");                       break;
            }
        }
        return result;
    }

    /// <summary>
    /// 获取字符串属性
    /// </summary>
    /// <param name="el">元素</param>
    /// <param name="key">键</param>
    /// <returns>字符串值</returns>
    private static string? GetStr(JsonElement el, string key)
        => el.TryGetProperty(key, out var v) ? v.GetString() : null;

    /// <summary>
    /// 将引用路径转换为名称
    /// </summary>
    /// <param name="refStr">引用路径</param>
    /// <returns>名称</returns>
    private static string RefToName(string? refStr)
    {
        if (string.IsNullOrEmpty(refStr)) return "unknown";
        return ToPascalCase(refStr.Split('/').Last());
    }

    /// <summary>
    /// 转换为 PascalCase
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <returns>PascalCase 字符串</returns>
    public static string ToPascalCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var parts = s.Split(new[] { '_', '-', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>
    /// 转换为 CamelCase
    /// </summary>
    /// <param name="s">输入字符串</param>
    /// <returns>CamelCase 字符串</returns>
    public static string ToCamelCase(string s)
    {
        var pascal = ToPascalCase(s);
        if (string.IsNullOrEmpty(pascal)) return pascal;
        return char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    /// <summary>
    /// 构建函数名
    /// </summary>
    /// <param name="method">HTTP 方法</param>
    /// <param name="path">API 路径</param>
    /// <returns>函数名</returns>
    private static string BuildFunctionName(string method, string path)
    {
        var segments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.StartsWith('{') ? "By" + ToPascalCase(s.Trim('{', '}')) : ToPascalCase(s));
        return ToCamelCase(method) + string.Concat(segments);
    }
}