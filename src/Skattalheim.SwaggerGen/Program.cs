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
        string input = null;
        string output = "./generated";
        string requestImport = "@/utils/request";
        int pathSplit = 2;

        // 解析命令行参数
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" || args[i] == "-i")
            {
                if (i + 1 < args.Length)
                {
                    input = args[i + 1];
                    i++;
                }
            }
            else if (args[i] == "--output" || args[i] == "-o")
            {
                if (i + 1 < args.Length)
                {
                    output = args[i + 1];
                    i++;
                }
            }
            else if (args[i] == "--request-import")
            {
                if (i + 1 < args.Length)
                {
                    requestImport = args[i + 1];
                    i++;
                }
            }
            else if (args[i] == "--path-split")
            {
                if (i + 1 < args.Length && int.TryParse(args[i + 1], out int split))
                {
                    pathSplit = split;
                    i++;
                }
            }
        }

        // 验证必需参数
        if (string.IsNullOrEmpty(input))
        {
            Console.Error.WriteLine("错误: 必须指定 Swagger 文件路径或 HTTP URL");
            Console.WriteLine("使用方法:");
            Console.WriteLine("  SwaggerGen --input <swagger文件或URL> [--output <输出目录>] [--request-import <axios导入路径>] [--path-split <切割位置>]");
            Console.WriteLine("  示例: SwaggerGen --input ./swagger.json --output ./api --request-import @/api/request --path-split 2");
            return 1;
        }

        Console.WriteLine($"[ApiCodeGen] 读取 Swagger: {input}");

        try
        {
            var generator = new SwaggerCodeGenerator();
            var fileCount = await generator.GenerateAsync(input, output, requestImport, pathSplit);
            Console.WriteLine($"[ApiCodeGen] 写出到: {Path.GetFullPath(output)}");
            Console.WriteLine($"[ApiCodeGen] 成功生成 {fileCount} 个文件！");
            Console.WriteLine("[ApiCodeGen] 完成！");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"错误: {ex.Message}");
            return 1;
        }
    }
}
