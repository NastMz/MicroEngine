# MicroEngine.Core

Core engine logic. Contains all platform-independent functionality.

## Structure

- `/Core` - Engine loop, scenes, configuration
- `/ECS` - Entity Component System
- `/Math` - Math primitives (vectors, matrices)
- `/Physics` - 2D physics system
- `/Resources` - Resource management
- `/Logging` - Structured logging system
- `/Backends` - Backend interfaces (render, input, audio)

## Dependencies

None. This project must not have external dependencies except the .NET runtime.
