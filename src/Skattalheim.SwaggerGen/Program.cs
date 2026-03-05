using System.CommandLine;
using Skattalheim.SwaggerGen.Services;
using System.Text.Json;

/// <summary>
/// Swagger → TypeScript+Axios API 代码生成器
/// 用于将 Swagger/OpenAPI 规范转换为 TypeScript 类型定义和 API 调用函数
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        // 定义命令行选项
        var inputOption = new Option<string>(
            aliases: ["--input", "-i"],
            description: "Swagger 文件路径或 HTTP URL")
        { IsRequired = true };

        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            getDefaultValue: () => "./generated",
            description: "TypeScript 输出目录");

        var requestImportOption = new Option<string>(
            name: "--request-import",
            getDefaultValue: () => "@/utils/request",
            description: "Axios 封装导入路径");

        // 创建根命令
        var rootCommand = new RootCommand("Swagger → TypeScript+Axios API 代码生成器");
        rootCommand.AddOption(inputOption);
        rootCommand.AddOption(outputOption);
        rootCommand.AddOption(requestImportOption);

        // 设置命令处理逻辑
        rootCommand.SetHandler(async (input, output, requestImport) =>
        {
            Console.WriteLine($"[ApiCodeGen] 读取 Swagger: {input}");

            // ─── 1. 读取并解析 Swagger ────────────────────────────
            JsonDocument swaggerDoc;
            try
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
                        Console.Error.WriteLine($"错误: 文件不存在 → {input}");
                        Environment.Exit(1);
                        return;
                    }
                    json = await File.ReadAllTextAsync(input);
                }
                // 解析 Swagger JSON
                swaggerDoc = JsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Swagger 读取/解析失败: {ex.Message}");
                Environment.Exit(1);
                return;
            }

            var root = swaggerDoc.RootElement;
            Console.WriteLine($"[ApiCodeGen] Swagger 解析成功，版本: {(root.TryGetProperty("openapi", out var v) ? v.GetString() : "2.0")}");

            // ─── 2. 解析 Schemas → interfaces / types ────────────
            var (interfaces, types) = SwaggerParser.ParseSchemas(root);
            Console.WriteLine($"  接口(interface): {interfaces.Count}，类型别名(type): {types.Count}");

            // ─── 3. 解析 Operations → API 文件模型 ───────────────
            var apiFiles = SwaggerParser.ParseOperations(root, requestImport, interfaces, types);
            Console.WriteLine($"  API 文件: {apiFiles.Count} 个 ({string.Join(", ", apiFiles.Select(a => a.FileName + ".ts"))})");

            // ─── 4. 渲染模板，写出 TypeScript 文件 ───────────────
            Console.WriteLine($"[ApiCodeGen] 写出到: {Path.GetFullPath(output)}");
            await TemplateRenderer.RenderAll(interfaces, types, apiFiles, output, requestImport);

            Console.WriteLine("[ApiCodeGen] 完成！");

        }, inputOption, outputOption, requestImportOption);

        return await rootCommand.InvokeAsync(args);
    }
}
