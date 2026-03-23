using Skattalheim.SwaggerGen.Core.Models;
using Scriban;

namespace Skattalheim.SwaggerGen.Core.Services;

/// <summary>
/// 模板渲染器，用于将解析后的 Swagger 模型渲染为 TypeScript 代码
/// </summary>
public static class TemplateRenderer
{
    /// <summary>
    /// 加载模板文件
    /// </summary>
    /// <param name="name">模板文件名</param>
    /// <returns>模板内容</returns>
    private static string LoadTemplate(string name)
    {
        // 先从 EmbeddedResource 加载，Fallback 到本地 Templates/ 目录（开发时使用）
        var asm = typeof(TemplateRenderer).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith($".{name}", StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            using var stream = asm.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Fallback: 本地文件（方便开发调试）
        var localPath = Path.Combine(AppContext.BaseDirectory, "Templates", name);
        if (File.Exists(localPath))
            return File.ReadAllText(localPath);

        throw new FileNotFoundException($"模板文件未找到: {name}");
    }

    /// <summary>
    /// 渲染所有模板并生成 TypeScript 文件
    /// </summary>
    /// <param name="interfaces">接口模型列表</param>
    /// <param name="types">类型别名模型列表</param>
    /// <param name="apiFiles">API 文件模型列表</param>
    /// <param name="outputDir">输出目录</param>
    /// <param name="requestImport">Axios 封装导入路径</param>
    public static async Task RenderAll(
        List<InterfaceModel> interfaces,
        List<TypeAliasModel> types,
        List<ApiFileModel> apiFiles,
        string outputDir,
        string requestImport)
    {
        // 汇总所有需要从 types 导入的名称（供 interfaces 模板使用）
        var typeNames = types.Select(t => t.Name).ToList();

        // 文件头注释（写入所有生成文件）
        var header = $"""
/* ------------------------------------------------------------------ */
/* 此文件由 ApiCodeGen 自动生成，请勿手动修改！                         */
/* 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}                        */
/* ------------------------------------------------------------------ */

""";

        // 1. 渲染 interfaces/index.ts
        {
            var tpl    = LoadTemplate("interfaces.sbn");
            var model  = new { interfaces, imports = typeNames.Where(t => interfaces.SelectMany(i => i.Properties).Any(p => p.Type.Contains(t))).Distinct().ToList() };
            var result = Template.Parse(tpl).Render(model, m => ToScribanKey(m.Name));
            await WriteFile(Path.Combine(outputDir, "interfaces", "index.ts"), header + result);
        }

        // 2. 渲染 types/index.ts
        {
            var tpl    = LoadTemplate("types.sbn");
            var model  = new { types };
            var result = Template.Parse(tpl).Render(model, m => ToScribanKey(m.Name));
            await WriteFile(Path.Combine(outputDir, "types", "index.ts"), header + result);
        }

        // 3. 渲染 api/apiAll.ts —— 所有 API 合并到一个文件
        var apiFile = apiFiles.First();
        {
            var tpl    = LoadTemplate("api.sbn");
            var model  = new
            {
                request_import   = apiFile.RequestImport,
                imports          = apiFile.Imports,
                operations       = apiFile.Operations.Select(op => new
                {
                    summary          = op.Summary,
                    method           = op.Method,
                    path             = op.Path,
                    function_name    = op.FunctionName,
                    params_signature = op.ParamsSignature,
                    return_type      = op.ReturnType,
                    url_expression   = op.UrlExpression,
                    axios_config     = op.AxiosConfig,
                    @params          = op.Params.Select(p => new
                    {
                        name     = p.Name,
                        type     = p.Type,
                        optional = p.Optional,
                        source   = p.Source
                    }).ToList()
                }).ToList()
            };
            var result = Template.Parse(tpl).Render(model, m => ToScribanKey(m.Name));
            await WriteFile(Path.Combine(outputDir, "api", apiFile.FileName + ".ts"), header + result);
        }

        // 4. 渲染 index.ts
        {
            var tplIdx    = LoadTemplate("index.sbn");
            var modelIdx  = new { apis = new List<string> { apiFile.FileName } };
            var resultIdx = Template.Parse(tplIdx).Render(modelIdx, m => ToScribanKey(m.Name));
            await WriteFile(Path.Combine(outputDir, "index.ts"), header + resultIdx);
        }
    }

    /// <summary>
    /// 写入文件
    /// </summary>
    /// <param name="fullPath">文件全路径</param>
    /// <param name="content">文件内容</param>
    private static async Task WriteFile(string fullPath, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content, System.Text.Encoding.UTF8);
        Console.WriteLine($"  ✔ {fullPath}");
    }

    /// <summary>
    /// Scriban 的成员名解析器：把 C# 属性名转为 snake_case（Scriban 默认约定）
    /// </summary>
    /// <param name="memberName">成员名</param>
    /// <returns>snake_case 格式的成员名</returns>
    private static string ToScribanKey(string memberName) =>
        string.Concat(memberName.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
}