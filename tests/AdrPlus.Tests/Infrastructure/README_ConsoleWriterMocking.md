# IConsoleWriter Mocking Guide

Este documento explica como criar testes unitários mockando a interface `IConsoleWriter` para simular interações do usuário sem usar o PromptPlus.

## Visão Geral

O arquivo `ConsoleWriterMockingExamplesTests.cs` contém exemplos práticos de como usar **NSubstitute** para criar mocks da interface `IConsoleWriter` e testar código que depende dela.

## Ferramentas Utilizadas

- **xUnit**: Framework de testes
- **NSubstitute**: Biblioteca de mocking
- **FluentAssertions**: Assertions fluentes e legíveis

## Padrões de Mocking

### 1. Verificar Chamadas de Métodos Void

Para métodos que apenas escrevem na console (sem retorno):

```csharp
[Fact]
public void MockWriteSuccess_VerifiesMethodWasCalled()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();
    var message = "Operation completed successfully";

    // Act
    mockConsole.WriteSuccess(message);

    // Assert
    mockConsole.Received(1).WriteSuccess(message);
}
```

**Padrões importantes:**
- `Substitute.For<IConsoleWriter>()` - cria o mock
- `Received(1)` - verifica que foi chamado exatamente 1 vez
- `Received()` - verifica que foi chamado pelo menos 1 vez
- `DidNotReceive()` - verifica que NÃO foi chamado

### 2. Verificar com Argumentos Genéricos

Quando não importa o valor exato do argumento:

```csharp
[Fact]
public void MockWriteInfo_VerifiesCallWithAnyString()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();

    // Act
    mockConsole.WriteInfo("Some info message");

    // Assert
    mockConsole.Received().WriteInfo(Arg.Any<string>());
}
```

**Uso de `Arg.Any<T>()`:**
- Aceita qualquer valor do tipo especificado
- Útil quando o valor exato não importa para o teste

### 3. Mockar Métodos com Retorno de Tupla

Muitos métodos em `IConsoleWriter` retornam tuplas com status de abort e conteúdo:

```csharp
[Fact]
public void MockPromptConfirm_ReturnsConfiguredResponse()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();
    mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, ConfirmYes: true));

    // Act
    var result = mockConsole.PromptConfirm("Do you want to continue?", CancellationToken.None);

    // Assert
    result.IsAborted.Should().BeFalse();
    result.ConfirmYes.Should().BeTrue();
}
```

**Pontos-chave:**
- `.Returns()` - configura o valor de retorno do mock
- Tuplas nomeadas facilitam a leitura: `(IsAborted: false, ConfirmYes: true)`
- FluentAssertions torna os asserts mais legíveis

### 4. Simular Abort do Usuário

```csharp
[Fact]
public void MockPromptConfirm_SimulatesUserAbort()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();
    mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns((IsAborted: true, ConfirmYes: false));

    // Act
    var result = mockConsole.PromptConfirm("Are you sure?", CancellationToken.None);

    // Assert
    result.IsAborted.Should().BeTrue();
}
```

### 5. Mockar Métodos com Tipos Complexos

Para métodos que recebem ou retornam tipos complexos:

```csharp
[Fact]
public void MockPromptEditFieldPrefix_ReturnsNewPrefix()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();
    var fieldsJson = new FieldsJson { Name = AppConstants.FieldPrefix, Value = "ADR" };
    var newPrefix = "RFC";

    mockConsole.PromptEditFieldPrefix(fieldsJson, Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, Content: newPrefix));

    // Act
    var result = mockConsole.PromptEditFieldPrefix(fieldsJson, CancellationToken.None);

    // Assert
    result.IsAborted.Should().BeFalse();
    result.Content.Should().Be(newPrefix);
}
```

### 6. Lidar com Classes Sealed (não mockáveis)

Algumas classes como `AdrPlusConfig` e `AdrPlusRepoConfig` são `sealed` e não podem ser mockadas. Use instâncias reais:

```csharp
[Fact]
public void MockPrompCalendar_ReturnsSelectedDate()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();
    var config = new AdrPlusConfig { Language = "en-US" }; // Instância real!
    var referenceDate = new DateTime(2024, 1, 1);
    var selectedDate = new DateTime(2024, 6, 15);

    mockConsole.PrompCalendar(Arg.Any<string>(), referenceDate, config, Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, Content: selectedDate));

    // Act
    var result = mockConsole.PrompCalendar("Select a date", referenceDate, config, CancellationToken.None);

    // Assert
    result.IsAborted.Should().BeFalse();
    result.Content.Should().Be(selectedDate);
}
```

**Importante:** Não tente usar `Substitute.For<>` em classes `sealed` - isso causará erro em runtime!

### 7. Simular Workflow Completo

Combine múltiplos mocks para simular fluxos de trabalho completos:

```csharp
[Fact]
public void MockConsoleWriter_InCompleteWorkflow_SimulatesUserInteraction()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();

    // Setup do workflow: confirm -> edit title -> edit scope -> confirm
    mockConsole.PromptConfirm("Start new ADR?", Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, ConfirmYes: true));

    mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, Content: "New ADR Title"));

    var repoConfig = new AdrPlusRepoConfig();
    mockConsole.PromptEditScopeAdr(Arg.Any<string>(), repoConfig, Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, Content: "backend"));

    // Act - Simula o workflow
    var startConfirm = mockConsole.PromptConfirm("Start new ADR?", CancellationToken.None);
    mockConsole.WriteInfo("Creating new ADR...");

    var title = mockConsole.PromptEditTitleAdr("", CancellationToken.None);
    var scope = mockConsole.PromptEditScopeAdr("", repoConfig, CancellationToken.None);

    mockConsole.WriteSuccess("ADR created successfully");

    // Assert
    startConfirm.ConfirmYes.Should().BeTrue();
    title.Content.Should().Be("New ADR Title");
    scope.Content.Should().Be("backend");

    mockConsole.Received(1).WriteInfo("Creating new ADR...");
    mockConsole.Received(1).WriteSuccess("ADR created successfully");
}
```

### 8. Verificar Ordem de Chamadas

Use `Received.InOrder()` para verificar que métodos foram chamados na ordem correta:

```csharp
[Fact]
public void MockConsoleWriter_VerifyCallOrder()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();

    // Act
    mockConsole.WriteStartCommand("test");
    mockConsole.WriteInfo("Step 1");
    mockConsole.WriteInfo("Step 2");
    mockConsole.WriteSuccess("Complete");
    mockConsole.WriteFinishedCommand("test");

    // Assert - Usando Received() na ordem
    Received.InOrder(() =>
    {
        mockConsole.WriteStartCommand("test");
        mockConsole.WriteInfo(Arg.Any<string>());
        mockConsole.WriteInfo(Arg.Any<string>());
        mockConsole.WriteSuccess(Arg.Any<string>());
        mockConsole.WriteFinishedCommand("test");
    });
}
```

### 9. Verificar que Método NÃO Foi Chamado

```csharp
[Fact]
public void MockConsoleWriter_VerifyMethodNotCalled()
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();

    // Act
    mockConsole.WriteInfo("Some info");

    // Assert
    mockConsole.DidNotReceive().WriteError(Arg.Any<string>());
}
```

### 10. Usar Theory para Múltiplos Cenários

```csharp
[Theory]
[InlineData("en-US")]
[InlineData("pt-BR")]
[InlineData("es-ES")]
public void MockPromptEditFieldLanguage_ReturnsValidLanguage(string language)
{
    // Arrange
    var mockConsole = Substitute.For<IConsoleWriter>();
    var fieldsJson = new FieldsJson { Name = AppConstants.FieldLanguage, Value = "en-US" };

    mockConsole.PromptEditFieldLanguage(fieldsJson, Arg.Any<CancellationToken>())
        .Returns((IsAborted: false, Content: language));

    // Act
    var result = mockConsole.PromptEditFieldLanguage(fieldsJson, CancellationToken.None);

    // Assert
    result.IsAborted.Should().BeFalse();
    result.Content.Should().Be(language);
}
```

## Como Usar nos Seus Testes

### Passo 1: Criar o Mock

```csharp
var mockConsole = Substitute.For<IConsoleWriter>();
```

### Passo 2: Configurar Comportamento

```csharp
mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
    .Returns((IsAborted: false, ConfirmYes: true));
```

### Passo 3: Executar o Código Sendo Testado

```csharp
var handler = new MeuCommandHandler(mockConsole, ...);
await handler.ExecuteAsync(args, CancellationToken.None);
```

### Passo 4: Verificar

```csharp
mockConsole.Received(1).WriteSuccess(Arg.Any<string>());
```

## Boas Práticas

1. **Use `Arg.Any<T>()` quando o valor exato não importa** - torna os testes menos frágeis
2. **Crie instâncias reais de classes `sealed`** - não tente mockar
3. **Separe arrange/act/assert claramente** - facilita leitura
4. **Verifique apenas o que é relevante** - não sobre-especifique
5. **Use nomes descritivos** - o nome do teste deve explicar o cenário
6. **Teste cenários de erro e abort** - não teste apenas o caminho feliz
7. **Combine múltiplos mocks se necessário** - mas mantenha simples

## Executar os Testes

```bash
# Todos os testes de mocking
dotnet test --filter "FullyQualifiedName~ConsoleWriterMockingExamplesTests"

# Teste específico
dotnet test --filter "FullyQualifiedName~MockPromptConfirm_ReturnsConfiguredResponse"
```

## Referências

- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
