using System.CommandLine;
using Skattalheim.SwaggerGen.Core.Services;
using System.IO;

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

            try
            {
                var generator = new SwaggerCodeGenerator();
                var fileCount = await generator.GenerateAsync(input, output, requestImport);
                Console.WriteLine($"[ApiCodeGen] 写出到: {Path.GetFullPath(output)}");
                Console.WriteLine($"[ApiCodeGen] 成功生成 {fileCount} 个文件！");
                Console.WriteLine("[ApiCodeGen] 完成！");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"错误: {ex.Message}");
                Environment.Exit(1);
                return;
            }

        }, inputOption, outputOption, requestImportOption);

        return await rootCommand.InvokeAsync(args);
    }
}
