/// <summary>
/// SwaggerGen Web 应用程序入口点
/// 提供基于 HTTP 的 Swagger 代码生成服务
/// </summary>
var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddControllers();
// 配置 OpenAPI 支持
builder.Services.AddOpenApi();

var app = builder.Build();

// 配置 HTTP 请求管道
if (app.Environment.IsDevelopment())
{
    // 在开发环境中启用 OpenAPI 文档
    app.MapOpenApi();
}

// 启用 HTTPS 重定向
app.UseHttpsRedirection();
// 启用授权中间件
app.UseAuthorization();
// 映射控制器路由
app.MapControllers();

// 运行应用程序
app.Run();
