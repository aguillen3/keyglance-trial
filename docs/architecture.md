# Architecture

```text
                HTTP
+------------------------------+
| KeyGlance.Server              |
| ImportJob queue               |
| lock => atomic Claim()        |
+---------------+--------------+
                |
                v
       +----------------+
       | C# Helper      |
       | claim job      |
       | exact HWND     |
       | foreground     |
       | exact AutomationId
       | type           |
       | read-back      |
       +--------+-------+
                |
                v
       +----------------+
       | MockTax WPF    |
       | exact title    |
       | RecipientName  |
       | Box2           |
       | Box14          |
       | Box22          |
       | Save           |
       +----------------+
```

The server lock protects the claim operation inside a single server process.

The helper uses a local execution guard so the same job object cannot be executed twice by the same helper process.

The helper never trusts typing alone. Each field is read back before the field is considered landed.

Foreground-window checks are performed before the run, before every field, before/after typing, and before individual characters. If the foreground HWND changes, the helper stops.

This is deliberately a small trial implementation rather than a production deployment architecture.
