---
name: windows-shell
description: Use on Windows before a PowerShell bulk find/replace over repo files, or when writing a Bash heredoc or a script that contains backslash escapes - the mojibake and control-character pitfalls that corrupt non-ASCII prose and string literals, and how to avoid them.
---

# Windows shell pitfalls

## PowerShell `Get-Content` mojibakes non-ASCII text

Never use Windows PowerShell 5.1's `Get-Content -Raw` (without `-Encoding utf8`) or `Set-Content` for bulk find/replace on source files. It reads with the system ANSI codepage, not UTF-8, so a UTF-8 file without a BOM has each multi-byte character (`—` is `E2 80 94`) decoded as three characters. Writing that back bakes the corruption into every em dash and curly quote in the whole file, not only the lines the script touched.

For a PowerShell bulk replace, read with `[System.IO.File]::ReadAllText($path)` and write with `[System.IO.File]::WriteAllText($path, $text)`, which default to UTF-8 without a BOM. After any bulk script touches non-ASCII prose, grep the touched files for `â€` and `�` before calling it done. Prefer the Edit tool when correctness matters more than speed.

## Backslash escapes in Bash heredocs

A `\n`, `\\n` or `\r\n` typed inside a Bash-tool heredoc, even a quoted `<<'EOF'`, can reach the file as a real line break, splitting string literals across lines. The Write tool has turned `\a` in a literal into a raw BEL character.

Make any edit that contains a backslash escape with the Edit tool. In a Python helper, build a backslash as `chr(92)`. In test data, write control characters by code point (`(char)7`, `"\u0007"`), not a letter escape. Write long scripts to a file with the Write tool rather than a heredoc. After a scripted edit, scan the changed files for control characters.

## Line endings

Snapshot and fixture files compared byte for byte want a fixed line ending; pin them in `.gitattributes` (`*.verified.* text eol=lf`) rather than trusting `core.autocrlf`.
