# File dialogs (Open / Save)

## What is a file dialog

A standard Windows file dialog (`OpenFileDialog` / `SaveFileDialog`) is an
OS-provided modal dialog for selecting or saving files. It appears as a child
`Window` under the attached application window in the snapshot.

> **Note**: Current ADACT snapshots show file dialogs as plain `Window` nodes,
> **not** tagged with `[modal]`. This may change in a future version.

## Fixed AutomationIds

### OpenFileDialog

| Element | AutomationId | ControlType | Note |
|---|---|---|---|
| File name input (outer) | `1148` | ComboBox | Contains the Edit and a DropDown button |
| File name input (inner) | `1148` | Edit | Target for `fill`. ValuePattern supported |
| File type filter | `1136` | ComboBox | Shows the current filter (e.g. `*.txt`) |
| Open button | `1` | Button | Label is locale-dependent (e.g. "Open(O)") |
| Cancel button | `2` | Button | Label is locale-dependent (e.g. "Cancel") |

### SaveFileDialog

| Element | AutomationId | ControlType | Note |
|---|---|---|---|
| File name input (outer) | `FileNameControlHost` | ComboBox | Contains the Edit and a DropDown button |
| File name input (inner) | `1001` | Edit | Target for `fill`. ValuePattern supported |
| File type filter | `FileTypeControlHost` | ComboBox | Shows the current filter |
| Save button | `1` | Button | Label is locale-dependent (e.g. "Save(S)") |
| Cancel button | `2` | Button | Label is locale-dependent (e.g. "Cancel") |

The Open/Save button (`aid="1"`) and Cancel button (`aid="2"`) are shared across
both dialog types and are stable across all Windows versions.

## Operation sequence

1. **Trigger the dialog** — click the button that opens the file dialog.
2. **`snapshot`** — find the `Window` with the dialog title (e.g. "Open File",
   "Save File"). It appears as a child of the main application window.
3. **Locate the file name Edit** — look for the Edit element with `aid="1148"`
   (Open) or `aid="1001"` (Save). This is the `fill` target.
4. **`fill <ref> "<path>"`** — write the full file path into the Edit field.
5. **`click`** the Open/Save button (`aid="1"`) to confirm, or Cancel
   (`aid="2"`) to dismiss.

### Example (OpenFileDialog)

```
snapshot → find Window "Open File"
         → find Edit [aid="1148"] → ref = s1e524
fill s1e524 "C:\path\to\file.txt"
click s1e529   ← Button [aid="1"] (Open)
```

## Scope

Only standard Win32 / WPF / WinForms file dialogs (`IFileDialog`-based) are
covered:

- `Microsoft.Win32.OpenFileDialog`
- `Microsoft.Win32.SaveFileDialog`
- `System.Windows.Forms.OpenFileDialog`
- `System.Windows.Forms.SaveFileDialog`

**Not covered**: `FolderBrowserDialog`, UWP `FileOpenPicker` / `FileSavePicker`,
and custom file-chooser controls.

## Fallback (if fill does not work)

In testing, `fill` works reliably on both OpenFileDialog and SaveFileDialog
file name inputs via ValuePattern. No fallback is typically needed.

If `fill` fails on a non-standard dialog variant, try:

1. `focus <edit-ref>` — set focus to the file name Edit.
2. `keypress Ctrl+A` — select all existing text.
3. `type <edit-ref> "<path>"` — type the path character by character.
4. `keypress Enter` — confirm.
