# Popup and modal windows

## `[popup]` / `[modal]` flags

In the CLI `.txt` snapshot, popup and modal windows are tagged with square-bracket flags:

```text
- Window "Save As" [modal] [ref=s1e42]
- Menu "" [popup] [ref=s1e55]
```

In raw JSON, the corresponding fields are `isPopup: true` and `isModalDialog: true` on the tree node.

## Why they appear as separate windows

Windows OS creates menus, tooltips, context menus, and dialog boxes as independent HWNDs.
They exist as sibling windows directly under the desktop — not as children of the main app window.
ADACT automatically detects popup/modal windows belonging to the same process and injects them into the snapshot tree.

## Operating on popup/modal elements

- Elements inside a popup/modal use normal `s<sid>e<eid>` refs and can be clicked/filled like any other element.
- Popups are transient: once you interact with them (e.g. click a menu item), they typically close and will not appear in the next snapshot.
- Typical flow: trigger action → `snapshot` → find refs inside the popup → operate on them.

## Modal constraints

While a modal dialog is open the main window is disabled (`WS_DISABLED`).
Attempting to interact with the main window will fail with `ELEMENT_INTERACTION_FAILED` or `OPERATION_BLOCKED`.
Always close or dismiss the modal first, then resume operating on the main window.
