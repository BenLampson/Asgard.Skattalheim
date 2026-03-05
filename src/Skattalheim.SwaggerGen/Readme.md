# Skattalheim.SwaggerGen

Swagger → TypeScript+Axios API 代码生成器，用于将 Swagger/OpenAPI 规范转换为 TypeScript 类型定义和 API 调用函数。

## 功能特性

- 支持 Swagger 2.0 和 OpenAPI 3.x 规范
- 自动生成 TypeScript 接口（interfaces）和类型别名（types）
- 生成基于 Axios 的 API 调用函数
- 支持从本地文件或 HTTP URL 读取 Swagger 规范
- 可自定义 Axios 封装导入路径
- 生成模块化的文件结构

## 安装

### 方法 1：直接使用已编译的可执行文件

1. 从发布页面下载对应平台的可执行文件
2. 将可执行文件添加到系统 PATH 环境变量中

### 方法 2：从源码构建

```bash
git clone <repository-url>
cd Asgard.Skattalheim/src/Skattalheim.SwaggerGen
dotnet build -c Release
dotnet publish -c Release -o <output-directory>
```

## 使用方法

### 基本用法

```bash
# 从本地文件生成
Skattalheim.SwaggerGen --input swagger.json --output ./generated

# 从 HTTP URL 生成
Skattalheim.SwaggerGen --input https://api.example.com/swagger.json --output ./generated

# 自定义 Axios 封装导入路径
Skattalheim.SwaggerGen --input swagger.json --output ./generated --request-import @/api/request
```

### 命令行选项

| 选项 | 别名 | 描述 | 默认值 |
|------|------|------|--------|
| `--input` | `-i` | Swagger 文件路径或 HTTP URL | 必需 |
| `--output` | `-o` | TypeScript 输出目录 | `./generated` |
| `--request-import` | - | Axios 封装导入路径 | `@/utils/request` |

## 生成的文件结构

```
generated/
├── index.ts              # 导出所有 API
├── interfaces/
│   └── index.ts          # 生成的 TypeScript 接口
├── types/
│   └── index.ts          # 生成的 TypeScript 类型别名
└── api/
    └── apiAll.ts         # 生成的 API 调用函数
```

## 示例

### 输入：Swagger 规范

```json
{
  "openapi": "3.0.0",
  "info": {
    "title": "示例 API",
    "version": "1.0.0"
  },
  "paths": {
    "/users": {
      "get": {
        "summary": "获取用户列表",
        "responses": {
          "200": {
            "description": "成功",
            "content": {
              "application/json": {
                "schema": {
                  "type": "array",
                  "items": {
                    "$ref": "#/components/schemas/User"
                  }
                }
              }
            }
          }
        }
      }
    }
  },
  "components": {
    "schemas": {
      "User": {
        "type": "object",
        "properties": {
          "id": {
            "type": "integer"
          },
          "name": {
            "type": "string"
          }
        },
        "required": ["id", "name"]
      }
    }
  }
}
```

### 输出：生成的 TypeScript 代码

#### `generated/interfaces/index.ts`

```typescript
/* ------------------------------------------------------------------ */
/* 此文件由 ApiCodeGen 自动生成，请勿手动修改！                         */
/* 生成时间: 2026-03-05 12:00:00                                     */
/* ------------------------------------------------------------------ */

export interface User {
  id: number;
  name: string;
}
```

#### `generated/api/apiAll.ts`

```typescript
/* ------------------------------------------------------------------ */
/* 此文件由 ApiCodeGen 自动生成，请勿手动修改！                         */
/* 生成时间: 2026-03-05 12:00:00                                     */
/* ------------------------------------------------------------------ */

import request from '@/utils/request';
import type { User } from '../interfaces';

/**
 * 获取用户列表
 */
export function getUsers() {
  return request.get<User[]>('/users');
}
```

#### `generated/index.ts`

```typescript
/* ------------------------------------------------------------------ */
/* 此文件由 ApiCodeGen 自动生成，请勿手动修改！                         */
/* 生成时间: 2026-03-05 12:00:00                                     */
/* ------------------------------------------------------------------ */

export * from './api/apiAll';
export * from './interfaces';
export * from './types';
```

## 依赖项

- .NET 6.0 或更高版本
- [Scriban](https://github.com/scriban/scriban) - 模板引擎

## 注意事项

- 生成的文件会自动添加注释，标记为自动生成，请勿手动修改
- 支持基本的 Swagger/OpenAPI 规范，对于复杂的规范可能需要手动调整
- 生成的 API 函数使用 Axios 进行 HTTP 请求，需要确保项目中已安装 Axios

## 许可证

MIT