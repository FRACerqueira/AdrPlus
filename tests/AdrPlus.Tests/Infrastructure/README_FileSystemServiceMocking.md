# IFileSystemService Mocking Guide

Este guia demonstra como fazer mock da interface `IFileSystemService` usando NSubstitute para testes unitários.

## Índice

- [Configuração Básica](#configuração-básica)
- [Operações com Arquivos](#operações-com-arquivos)
- [Operações com Diretórios](#operações-com-diretórios)
- [Enumeração de Arquivos](#enumeração-de-arquivos)
- [Operações com Drives](#operações-com-drives)
- [Operações de Histórico](#operações-de-histórico)
- [Cenários Complexos](#cenários-complexos)

## Configuração Básica

### Criando um Mock

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();
```

## Operações com Arquivos

### Verificar Existência de Arquivo

```csharp
// Retorna true para um arquivo específico
var mockFileSystem = Substitute.For<IFileSystemService>();
mockFileSystem.FileExists("C:\\project\\test.txt").Returns(true);

var result = mockFileSystem.FileExists("C:\\project\\test.txt");
result.Should().BeTrue();
```

### Retornar true para Qualquer Caminho

```csharp
mockFileSystem.FileExists(Arg.Any<string>()).Returns(true);

mockFileSystem.FileExists("any-path.txt").Should().BeTrue();
```

### Ler Conteúdo de Arquivo

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();
var expectedContent = "Test content";

mockFileSystem.ReadAllTextAsync("file.txt", Arg.Any<CancellationToken>())
    .Returns(expectedContent);

var result = await mockFileSystem.ReadAllTextAsync("file.txt", CancellationToken.None);
result.Should().Be(expectedContent);
```

### Ler Linhas de Arquivo

```csharp
var expectedLines = new[] { "Line 1", "Line 2", "Line 3" };

mockFileSystem.ReadAllLinesAsync("file.txt", Arg.Any<CancellationToken>())
    .Returns(expectedLines);

var result = await mockFileSystem.ReadAllLinesAsync("file.txt", CancellationToken.None);
result.Should().HaveCount(3);
```

### Escrever em Arquivo

```csharp
var filePath = "C:\\project\\test.txt";
var content = "Test content";

await mockFileSystem.WriteAllTextAsync(filePath, content, CancellationToken.None);

// Verificar que o método foi chamado
await mockFileSystem.Received(1).WriteAllTextAsync(filePath, content, Arg.Any<CancellationToken>());
```

## Operações com Diretórios

### Verificar Existência de Diretório

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();
mockFileSystem.DirectoryExists("C:\\project\\docs").Returns(true);

var result = mockFileSystem.DirectoryExists("C:\\project\\docs");
result.Should().BeTrue();
```

### Criar Diretório

```csharp
var dirPath = "C:\\project\\newdir";
mockFileSystem.CreateDirectory(dirPath).Returns(dirPath);

var result = mockFileSystem.CreateDirectory(dirPath);
result.Should().Be(dirPath);
```

### Obter Caminho Completo de Diretório

```csharp
var relativePath = "docs\\adr";
var absolutePath = "C:\\project\\docs\\adr";

mockFileSystem.GetFullNameDirectory(relativePath).Returns(absolutePath);

var result = mockFileSystem.GetFullNameDirectory(relativePath);
result.Should().StartWith("C:\\");
```

## Enumeração de Arquivos

### Enumerar Arquivos com Padrão

```csharp
var expectedFiles = new[]
{
    "C:\\project\\docs\\ADR-0001.md",
    "C:\\project\\docs\\ADR-0002.md",
    "C:\\project\\docs\\ADR-0003.md"
};

mockFileSystem.EnumerateFiles("C:\\project\\docs", "*.md").Returns(expectedFiles);

var result = mockFileSystem.EnumerateFiles("C:\\project\\docs", "*.md").ToArray();
result.Should().HaveCount(3);
result.Should().AllSatisfy(f => f.Should().EndWith(".md"));
```

### Obter Arquivos com SearchOption

```csharp
var expectedFiles = new[]
{
    "C:\\project\\docs\\adr\\ADR-0001-Test.md",
    "C:\\project\\docs\\adr\\ADR-0002-Another.md"
};

mockFileSystem.GetFiles("C:\\project\\docs\\adr", "ADR-*.md", SearchOption.AllDirectories)
    .Returns(expectedFiles);

var result = mockFileSystem.GetFiles("C:\\project\\docs\\adr", "ADR-*.md", SearchOption.AllDirectories);
result.Length.Should().Be(2);
```

### Somente Diretório Superior

```csharp
var expectedFiles = new[] { "C:\\project\\docs\\README.md" };

mockFileSystem.GetFiles("C:\\project\\docs", "*.md", SearchOption.TopDirectoryOnly)
    .Returns(expectedFiles);

var result = mockFileSystem.GetFiles("C:\\project\\docs", "*.md", SearchOption.TopDirectoryOnly);
result.Should().ContainSingle();
```

### Obter Nome Completo de Arquivo

```csharp
var relativePath = "docs\\adr\\test.md";
var absolutePath = "C:\\project\\docs\\adr\\test.md";

mockFileSystem.GetFullNameFile(relativePath).Returns(absolutePath);

var result = mockFileSystem.GetFullNameFile(relativePath);
Path.IsPathRooted(result).Should().BeTrue();
```

## Operações com Drives

### Drive Único

```csharp
var expectedDrives = new[] { "C:\\" };
mockFileSystem.GetDrives().Returns(expectedDrives);

var result = mockFileSystem.GetDrives();
result.Should().ContainSingle();
result[0].Should().Be("C:\\");
```

### Múltiplos Drives

```csharp
var expectedDrives = new[] { "C:\\", "D:\\", "E:\\" };
mockFileSystem.GetDrives().Returns(expectedDrives);

var result = mockFileSystem.GetDrives();
result.Should().HaveCount(3);
result.Should().AllSatisfy(d => d.Should().EndWith("\\"));
```

## Operações de Histórico

### Salvar Histórico

```csharp
var fileKey = "test-history";
var content = new { Name = "Test", Value = 123 };

await mockFileSystem.SaveHistoryAsync(fileKey, content, CancellationToken.None);

await mockFileSystem.Received(1).SaveHistoryAsync(
    fileKey,
    Arg.Is<object>(x => x == content),
    Arg.Any<CancellationToken>());
```

### Ler Histórico com Sucesso

```csharp
var fileKey = "test-history";
var expectedData = new { Name = "Test", Value = 123 };

mockFileSystem.ReadHistoryAsync<object>(fileKey, Arg.Any<CancellationToken>())
    .Returns((Success: true, Result: expectedData));

var result = await mockFileSystem.ReadHistoryAsync<object>(fileKey, CancellationToken.None);
result.Success.Should().BeTrue();
result.Result.Should().Be(expectedData);
```

### Ler Histórico com Falha

```csharp
var fileKey = "nonexistent-history";

mockFileSystem.ReadHistoryAsync<object>(fileKey, Arg.Any<CancellationToken>())
    .Returns((Success: false, Result: (object?)null));

var result = await mockFileSystem.ReadHistoryAsync<object>(fileKey, CancellationToken.None);
result.Success.Should().BeFalse();
result.Result.Should().BeNull();
```

### Ler Histórico com Tipo Genérico

```csharp
public class TestConfig
{
    public string Setting1 { get; set; } = string.Empty;
    public int Setting2 { get; set; }
}

var expectedConfig = new TestConfig { Setting1 = "value1", Setting2 = 42 };

mockFileSystem.ReadHistoryAsync<TestConfig>("config-history", Arg.Any<CancellationToken>())
    .Returns((Success: true, Result: expectedConfig));

var result = await mockFileSystem.ReadHistoryAsync<TestConfig>("config-history", CancellationToken.None);
result.Success.Should().BeTrue();
result.Result!.Setting1.Should().Be("value1");
result.Result.Setting2.Should().Be(42);
```

## Cenários Complexos

### Workflow Completo: Criar, Escrever e Ler

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();
var dirPath = "C:\\project\\docs";
var filePath = "C:\\project\\docs\\test.txt";
var content = "Initial content";

// Configurar comportamento sequencial
mockFileSystem.DirectoryExists(dirPath).Returns(false, true);
mockFileSystem.CreateDirectory(dirPath).Returns(dirPath);
mockFileSystem.FileExists(filePath).Returns(false, true);
mockFileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>()).Returns(content);

// Criar diretório
var dirExists = mockFileSystem.DirectoryExists(dirPath);
dirExists.Should().BeFalse();

var createdDir = mockFileSystem.CreateDirectory(dirPath);
var dirExistsAfter = mockFileSystem.DirectoryExists(dirPath);
dirExistsAfter.Should().BeTrue();

// Escrever arquivo
await mockFileSystem.WriteAllTextAsync(filePath, content, CancellationToken.None);

// Ler arquivo
var readContent = await mockFileSystem.ReadAllTextAsync(filePath, CancellationToken.None);
readContent.Should().Be(content);
```

### Múltiplos Padrões de Arquivos

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();
var docsPath = "C:\\project\\docs";

mockFileSystem.GetFiles(docsPath, "*.md", Arg.Any<SearchOption>())
    .Returns(new[] { "file1.md", "file2.md" });

mockFileSystem.GetFiles(docsPath, "*.txt", Arg.Any<SearchOption>())
    .Returns(new[] { "file3.txt" });

mockFileSystem.GetFiles(docsPath, "ADR-*.md", Arg.Any<SearchOption>())
    .Returns(new[] { "ADR-0001.md", "ADR-0002.md" });

var mdFiles = mockFileSystem.GetFiles(docsPath, "*.md", SearchOption.AllDirectories);
var txtFiles = mockFileSystem.GetFiles(docsPath, "*.txt", SearchOption.AllDirectories);
var adrFiles = mockFileSystem.GetFiles(docsPath, "ADR-*.md", SearchOption.AllDirectories);

mdFiles.Should().HaveCount(2);
txtFiles.Should().ContainSingle();
adrFiles.Should().HaveCount(2);
```

### Simular Exceção

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();
var filePath = "C:\\protected\\file.txt";

mockFileSystem.ReadAllTextAsync(filePath, Arg.Any<CancellationToken>())
    .Returns<string>(x => throw new UnauthorizedAccessException("Access denied"));

var act = async () => await mockFileSystem.ReadAllTextAsync(filePath, CancellationToken.None);

await act.Should().ThrowAsync<UnauthorizedAccessException>()
    .WithMessage("Access denied");
```

### FileExists Condicional Baseado no Caminho

```csharp
var mockFileSystem = Substitute.For<IFileSystemService>();

// Retorna true para arquivos .md
mockFileSystem.FileExists(Arg.Is<string>(path => path.EndsWith(".md")))
    .Returns(true);

// Retorna false para outros arquivos
mockFileSystem.FileExists(Arg.Is<string>(path => !path.EndsWith(".md")))
    .Returns(false);

mockFileSystem.FileExists("test.md").Should().BeTrue();
mockFileSystem.FileExists("test.txt").Should().BeFalse();
mockFileSystem.FileExists("ADR-0001.md").Should().BeTrue();
mockFileSystem.FileExists("config.json").Should().BeFalse();
```

## Exemplo Completo em um Teste

```csharp
[Fact]
public async Task MyTest_WithFileSystemOperations_WorksCorrectly()
{
    // Arrange
    var mockFileSystem = Substitute.For<IFileSystemService>();
    var configPath = "C:\\project\\config.json";
    var configContent = "{\"setting\":\"value\"}";

    mockFileSystem.FileExists(configPath).Returns(true);
    mockFileSystem.ReadAllTextAsync(configPath, Arg.Any<CancellationToken>())
        .Returns(configContent);

    var handler = new MyHandler(mockFileSystem);

    // Act
    var result = await handler.ProcessAsync(CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    await mockFileSystem.Received(1).ReadAllTextAsync(configPath, Arg.Any<CancellationToken>());
}
```

## Referências

- Arquivo completo de exemplos: `tests\AdrPlus.Tests\Infrastructure\FileSystemServiceMockingExamplesTests.cs`
- Documentação do NSubstitute: https://nsubstitute.github.io/
- Documentação do FluentAssertions: https://fluentassertions.com/
