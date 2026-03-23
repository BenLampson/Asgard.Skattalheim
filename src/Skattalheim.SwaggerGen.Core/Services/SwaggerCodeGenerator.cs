using Skattalheim.SwaggerGen.Core.Models;
using Skattalheim.SwaggerGen.Core.Services;
using System.Text.Json;

namespace Skattalheim.SwaggerGen.Core.Services;

/// <summary>
/// Swagger 代码生成器，用于将 Swagger/OpenAPI 规范转换为 TypeScript 代码
/// </summary>
public class SwaggerCodeGenerator
{
    /// <summary>
    /// 从文件或 URL 读取 Swagger 文档并生成 TypeScript 代码
    /// </summary>
    /// <param name="input">Swagger 文件路径或 HTTP URL</param>
    /// <param name="outputDir">TypeScript 输出目录</param>
    /// <param name="requestImport">Axios 封装导入路径</param>
    /// <returns>生成的文件数量</returns>
    public async Task<int> GenerateAsync(string input, string outputDir, string requestImport = "@/utils/request")
    {
        // 读取并解析 Swagger
        var swaggerDoc = await ReadSwaggerAsync(input);
        var root = swaggerDoc.RootElement;

        // 解析 Schemas → interfaces / types
        var (interfaces, types) = SwaggerParser.ParseSchemas(root);

        // 解析 Operations → API 文件模型
        var apiFiles = SwaggerParser.ParseOperations(root, requestImport, interfaces, types);

        // 渲染模板，写出 TypeScript 文件
        await TemplateRenderer.RenderAll(interfaces, types, apiFiles, outputDir, requestImport);

        // 返回生成的文件数量（4个固定文件：interfaces/index.ts, types/index.ts, api/apiAll.ts, index.ts）
        return 4;
    }

    /// <summary>
    /// 从字符串读取 Swagger 文档并生成 TypeScript 代码
    /// </summary>
    /// <param name="swaggerJson">Swagger JSON 字符串</param>
    /// <param name="outputDir">TypeScript 输出目录</param>
    /// <param name="requestImport">Axios 封装导入路径</param>
    /// <returns>生成的文件数量</returns>
    public async Task<int> GenerateFromJsonAsync(string swaggerJson, string outputDir, string requestImport = "@/utils/request")
    {
        // 解析 Swagger JSON
        var swaggerDoc = JsonDocument.Parse(swaggerJson);
        var root = swaggerDoc.RootElement;

        // 解析 Schemas → interfaces / types
        var (interfaces, types) = SwaggerParser.ParseSchemas(root);

        // 解析 Operations → API 文件模型
        var apiFiles = SwaggerParser.ParseOperations(root, requestImport, interfaces, types);

        // 渲染模板，写出 TypeScript 文件
        await TemplateRenderer.RenderAll(interfaces, types, apiFiles, outputDir, requestImport);

        // 返回生成的文件数量
        return 4;
    }

    /// <summary>
    /// 读取 Swagger 文档
    /// </summary>
    /// <param name="input">Swagger 文件路径或 HTTP URL</param>
    /// <returns>Swagger 文档</returns>
    private async Task<JsonDocument> ReadSwaggerAsync(string input)
    {
        string json;
        if (input.StartsWith("http://") || input.StartsWith("https://"))
        {
            // 从 HTTP URL 读取 Swagger
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            json = await http.GetStringAsync(input);
        }
        else
        {
            // 从本地文件读取 Swagger
            if (!File.Exists(input))
            {
                throw new FileNotFoundException($"文件不存在: {input}");
            }
            json = await File.ReadAllTextAsync(input);
        }
        // 解析 Swagger JSON
        return JsonDocument.Parse(json);
    }
}
