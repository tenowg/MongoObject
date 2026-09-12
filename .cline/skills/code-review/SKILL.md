---
name: code-review
description: Review one source file at the user's direction and write the review to a reviews directory. Use when the user says review this file, code review, Qwen review, or asks for a local-llm file review with optional limited crawl of related files.
---

# Code review (one file, write to disk)

You review **one named file per turn**. You write the review as a markdown file under the reviews directory. You do not dump a whole-repo review. You do not apply the suggested patches unless the user later asks.

This skill is written for a local model (Qwen 3.8 35B). Stay inside the token budget. Prefer reading over guessing.

## Defaults (override if the user names different paths)

- Reviews directory — `.reviews/` at the project root. Create it if missing.
- Review filename — `.reviews/<yyyy-mm-dd>-<slug-of-source-path>.md`
  - Slug the source path by replacing `/` with `__` and stripping a leading `./`
  - Example — `Assets/Scripts/Combat/HitResolver.cs` → `.reviews/2026-09-12-Assets__Scripts__Combat__HitResolver.cs.md`
- If that name already exists, append `-2`, `-3`, etc. Never overwrite a prior review.
- Project root — the repo or workspace the user is in. Do not invent a second root.

If the user says "put reviews in X", use X for this session and later ones until they change it.

## When this skill fires

- User names a file and says review / code-review / look at this file.
- User says "next file" and names it.
- User asks to crawl a method or type referenced from the current file.

If they did not name a file, ask for the path. Do not pick a file for them.

## Hard limits (local model)

- Primary target — **one file**. Read it fully.
- Related files — only if needed to understand a symbol used in the target. Cap at **5 extra files** unless the user raises the cap.
- Do not recursively list the repo. Do not read every file in a folder.
- Do not read generated code, `Library/`, `obj/`, `bin/`, `node_modules/`, lockfiles, or binary assets unless the user names them.
- If a related file is huge, read the type/method region first, not the whole file.
- Stop the crawl when you can explain the call. Curiosity is not a reason to open more files.

## Workflow

1. Confirm the target path and the reviews directory.
2. Read the target file.
3. If a method, type, or interface is defined elsewhere and you cannot review the call without it, open that definition. Record every extra file you opened.
4. Write findings. Severity first. No throat-clearing.
5. Write the review file using the template in `assets/review-template.md`.
6. Reply with the review path, a 3–8 line summary, and ask which file is next. Do not paste the whole review into chat unless they ask.

## What to look for

Stay concrete. Cite path + approximate line or symbol name.

- Correctness — wrong control flow, off-by-one, null/default misuse, dropped errors, lifecycle mistakes.
- Contract breaks — public API that lies, ECS/system update order, serialization fields that will not survive a domain reload.
- Security / data loss — only if present. Do not invent threat models.
- Performance — hot-path allocations, per-frame LINQ, unbounded searches. Skip micro-nits on cold code.
- Clarity — only when a name or structure will cause a real bug later.

Do not style-nit. Do not demand a rewrite because you would have written it differently.

## Crawl rules

Allowed reasons to open another file

- The target calls a method whose body is not in the target and the review depends on what it actually does.
- The target implements an interface / overrides a base type and the contract lives elsewhere.
- A constant, enum, or serialized field meaning is defined in another file.

Not allowed

- Might be interesting
- Opening every caller
- Following usings / imports just because they exist

When you crawl, list in the review under Opened for context.

## Review file format

Copy `assets/review-template.md`. Fill every section. Use `n/a` rather than deleting a heading.

Severity tags — `blocker` | `should-fix` | `nit` | `question`

Each finding uses Where / What / Why it matters / Suggestion (smallest change).

End with Opened for context and Out of scope.

## What you must not do

- Do not edit the source file during a review pass.
- Do not mark the review approved as if you ran tests you did not run.
- Do not invent files, types, or Unity APIs.
- Do not review files the user did not name, except the bounded crawl above.
- Do not write the review only in chat. The file is the deliverable.
