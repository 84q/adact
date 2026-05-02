# `adact select`

Select an item in a list / combo-box by exactly one of `--name`, `--index`,
or `--item-ref`. A snapshot is captured automatically.

## Synopsis

```
adact select <ref> (--name <name> | --index <i> | --item-ref <itemRef>)
                   [--no-snapshot] [--snapshot-dir <dir>] [--server <url>]
```

| Argument / Flag | Purpose                                                          |
| --------------- | ---------------------------------------------------------------- |
| `<ref>`         | Element ref of the container (List, ComboBox).                   |
| `--name`        | Name of the child item to select.                                |
| `--index`       | 0-based child index to select.                                   |
| `--item-ref`    | Element ref of the child ListItem (from a snapshot).             |

Provide exactly one of the three selection options.

## Examples

```
adact select s1e10 --name "Option B"
adact select s1e10 --index 2
adact select s1e10 --item-ref s1e15
```

## Error recovery

- `INVALID_ARGUMENT` — none / multiple of `--name|--index|--item-ref` were
  given. Re-run with exactly one.
- `ELEMENT_INTERACTION_FAILED` — the child does not support
  SelectionItemPattern, or the name / index does not match.
