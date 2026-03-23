using Microsoft.AspNetCore.Mvc;
using Skattalheim.SwaggerGen.Core.Services;
using System;
using System.IO;
using System.IO.Compression;

namespace Skattalheim.SwaggerGen.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SwaggerGenController : ControllerBase
{
    private readonly SwaggerCodeGenerator _generator;

    public SwaggerGenController()
    {
        _generator = new SwaggerCodeGenerator();
    }

    /// <summary>
    /// 从 Swagger URL 或文件路径生成 TypeScript 代码
    /// </summary>
    /// <param name="input">Swagger 文件路径或 HTTP URL</param>
    /// <param name="outputDir">TypeScript 输出目录</param>
    /// <param name="requestImport">Axios 封装导入路径</param>
    /// <returns>生成结果</returns>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        [FromForm] string input,
        [FromForm] string outputDir = "./generated",
        [FromForm] string requestImport = "@/utils/request")
    {
        try
        {
            var fileCount = await _generator.GenerateAsync(input, outputDir, requestImport);
            var zipPath = await CreateZipFile(outputDir);
            
            // 读取zip文件并返回给用户下载
            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
            
            // 清理临时文件
            CleanupTempFiles(outputDir, zipPath);
            
            return File(zipBytes, "application/zip", "swagger-generated-code.zip");
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 从 Swagger JSON 字符串生成 TypeScript 代码
    /// </summary>
    /// <param name="request">生成请求</param>
    /// <returns>生成结果</returns>
    [HttpPost("generate-from-json")]
    public async Task<IActionResult> GenerateFromJson([FromBody] GenerateFromJsonRequest request)
    {
        try
        {
            var outputDir = request.OutputDir ?? "./generated";
            var fileCount = await _generator.GenerateFromJsonAsync(
                request.SwaggerJson,
                outputDir,
                request.RequestImport ?? "@/utils/request");

            var zipPath = await CreateZipFile(outputDir);
            
            // 读取zip文件并返回给用户下载
            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
            
            // 清理临时文件
            CleanupTempFiles(outputDir, zipPath);
            
            return File(zipBytes, "application/zip", "swagger-generated-code.zip");
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// 创建zip文件
    /// </summary>
    /// <param name="directoryPath">要压缩的目录路径</param>
    /// <returns>zip文件路径</returns>
    private async Task<string> CreateZipFile(string directoryPath)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"swagger-generated-{Guid.NewGuid()}.zip");
        
        // 创建zip文件
        ZipFile.CreateFromDirectory(directoryPath, zipPath);
        
        return zipPath;
    }

    /// <summary>
    /// 清理临时文件
    /// </summary>
    /// <param name="outputDir">输出目录</param>
    /// <param name="zipPath">zip文件路径</param>
    private void CleanupTempFiles(string outputDir, string zipPath)
    {
        try
        {
            // 删除生成的文件目录
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            
            // 删除zip文件
            if (System.IO.File.Exists(zipPath))
            {
                System.IO.File.Delete(zipPath);
            }
        }
        catch (Exception ex)
        {
            // 清理失败不影响返回结果，只记录错误
            Console.WriteLine($"清理临时文件失败: {ex.Message}");
        }
    }
}

public class GenerateFromJsonRequest
{
    public string SwaggerJson { get; set; } = "";
    public string? OutputDir { get; set; }
    public string? RequestImport { get; set; }
}
