# OpenAPI Specification

The complete OpenAPI 3.0 specification for MagiDesk POS APIs is available in YAML format.

## OpenAPI Spec File

The OpenAPI specification is located at: [`openapi.yaml`](./openapi.yaml)

## Viewing the Spec

### Online Viewers

- **Swagger Editor**: [https://editor.swagger.io/](https://editor.swagger.io/)
  - Copy the contents of `openapi.yaml` and paste into Swagger Editor
- **Swagger UI**: Use Swagger UI to render the spec interactively

### Local Viewing

```powershell
# Install Swagger UI locally
npm install -g swagger-ui-serve

# Serve the OpenAPI spec
swagger-ui-serve docs/docs/api/openapi.yaml
```

### VS Code Extension

Install the "OpenAPI (Swagger) Editor" extension in VS Code to view and edit the spec with syntax highlighting and validation.

## API Versions

The OpenAPI spec documents both API versions:

- **v1**: Legacy endpoints (backward compatible, no RBAC)
- **v2**: RBAC-enabled endpoints (requires permissions)

## Using the Spec

### Code Generation

Generate client SDKs from the OpenAPI spec:

```powershell
# Install OpenAPI Generator
npm install -g @openapitools/openapi-generator-cli

# Generate C# client
openapi-generator-cli generate -i docs/docs/api/openapi.yaml -g csharp -o ./generated-client
```

### API Testing

Import the spec into API testing tools:
- **Postman**: Import OpenAPI spec
- **Insomnia**: Import OpenAPI spec
- **REST Client**: Use with VS Code REST Client extension

## Updating the Spec

When adding new endpoints:

1. Update `openapi.yaml` with new endpoints
2. Include request/response schemas
3. Document authentication requirements
4. Add examples
5. Validate the spec (use Swagger Editor)

## Validation

Validate the OpenAPI spec:

```powershell
# Install validator
npm install -g swagger-cli

# Validate
swagger-cli validate docs/docs/api/openapi.yaml
```
