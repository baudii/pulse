# Pulse - Lightweight job scheduler

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Pulse** — a lightweight background task management library for .NET.
The project already provides the basic functionality for running background jobs,
but new features and storage options will be added over time.

---

## Project Status
**MVP** - Pulse can already be used for simple scenarios, however, it is still in active development.

---

## Features (current)
- Define and run background jobs
- Implemented jobs storage providers:
  - Redis
  - File system
  - In memory
- Basic job lifecycle management (creation, update, deletion, storage etc.)

---

## Roadmap
- [ ] MS SQL, PostreSQL, MongoDB storage providers
- [ ] Flexible scheduling logic with continuations and cycling jobs
- [ ] Extended job monitoring and expiration handling
- [ ] Documentation and usage examples

---

## Contributing
Contributions and feedback are welcome!

---

## License
This project is licensed under the MIT License.
