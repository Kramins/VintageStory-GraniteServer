# Granite.Tests

This project contains unit tests for the Granite Server system.

## Test Structure

### Messaging Tests

#### MessageBusServiceTests
Tests for the `MessageBusService` class, which provides the centralized event bus functionality using Reactive Extensions (Rx.NET).

**Test Coverage:**
- Publishing messages to subscribers
- Multiple subscriber broadcasting
- Replay buffer functionality for new subscribers
- Graceful shutdown
- Command and event creation with metadata
- Message ordering guarantees
- Subscription independence

#### CommandHandlerTests
Tests for the `ICommandHandler<T>` interface and command handling infrastructure.

**Test Coverage:**
- Command handler interface implementation
- Typed command handling
- Multiple concurrent handlers
- Interface polymorphism

#### EventHandlerTests
Tests for the `IEventHandler<T>` interface and event handling infrastructure.

**Test Coverage:**
- Event handler interface implementation
- Typed event handling
- Multiple concurrent handlers
- Sequential event processing
- Interface polymorphism

## Testing Stack

- **Test Framework**: xUnit
- **Assertion Library**: FluentAssertions
- **Mocking Library**: NSubstitute  
- **Reactive Testing**: Microsoft.Reactive.Testing

## Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run tests for specific project
dotnet test Granite.Tests/Granite.Tests.csproj
```

## Test Guidelines

1. **Use FluentAssertions** for readable assertions
2. **Follow AAA pattern**: Arrange, Act, Assert
3. **Test one concept per test** method
4. **Use descriptive test names** following the pattern: `MethodName_Scenario_ExpectedBehavior`
5. **Initialize test data properly** - ensure Data properties are set for CommandMessage/EventMessage instances

## Adding New Tests

When adding new tests for Command or Event System:

1. Create properly typed test commands/events inheriting from `CommandMessage<T>` or `EventMessage<T>`
2. Initialize the Data property with actual instances (not null)
3. Use FluentAssertions for readable assertions
4. Test both the typed interface and base interface handling

## Future Test Areas

- Integration tests for message routing
- Performance tests for high-throughput scenarios
- Concurrent subscriber stress tests
- Error handling and exception propagation tests
