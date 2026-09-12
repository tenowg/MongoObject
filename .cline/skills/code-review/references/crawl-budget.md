# Crawl budget

Default extra files — 5.

Raise only if the user says so.

Priority order when several symbols are unknown:

1. Method body of a call that can throw, allocate, mutate shared state, or talk to Unity objects.
2. Interface / abstract contract the target claims to implement.
3. Type that owns a field the target mutates.
4. Config / enum that changes behavior.
5. Stop.

Skip:

- Generated designers / `.meta` / asmdef unless named.
- Test files unless the user asked to include tests.
- Callers. Reviewing one file is not a blast-radius audit.
