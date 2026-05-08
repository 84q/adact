# Error codes — complete reference

Full list of ADACT error codes with typical causes and recovery steps.

## Frequent errors

| Error code                    | Typical cause                                          | Recovery                                                                                          |
| ----------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `INVALID_ARGUMENT`            | Missing or conflicting CLI arguments.                  | Reread the reference doc and supply the required argument combination.                            |
| `INVALID_REF_FORMAT`          | Element ref does not match `s<sid>e<eid>`.             | Use a ref copied verbatim from the latest snapshot.                                               |
| `WINDOW_NOT_FOUND`            | Daemon could not attach to the HWND for the given `windowRef`. | Re-run `adact list-windows` and confirm the target window is still open.                  |
| `REF_NOT_FOUND`               | The element behind the ref is no longer reachable.     | Run `adact snapshot`, locate the element again from the new snapshot, then retry with the new ref. |
| `ELEMENT_INTERACTION_FAILED`  | Click/fill could not be performed on the element.      | Make sure the window is foreground and the control is enabled and on-screen; re-snapshot and retry. |
| `CONNECTION_FAILED`           | Could not reach the ADACT daemon.                      | Start the daemon with `adact serve`, or pass `--server <url>`.                                    |
| `OPERATION_BLOCKED`           | Desktop locked, UAC prompt, or window not foreground.  | Unlock the desktop, dismiss any UAC/system dialog, and ensure the window is in the foreground.    |
| `WAIT_TIMEOUT`                | `wait-for-element` / `wait-for-window` timed out.      | Increase `--timeout`, verify the app reaches the expected state, or relax the search conditions.  |

## Rare / environment errors

| Error code                    | Typical cause                                          | Recovery                                                                                          |
| ----------------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------- |
| `INVALID_WINDOW_REF`          | Window ref is well-formed but unknown / retired.       | Re-run `adact list-windows` and use a freshly printed `windowRef`.                                |
| `NO_ACTIVE_SESSION`           | `snapshot` was called without an attached session.     | Call `adact attach` first, or pass `--sid` explicitly.                                            |
| `NOT_FOUND`                   | Target session does not exist.                         | Re-run `adact attach` to create a session, or pass a valid `--sid`.                               |
| `LAUNCH_FAILED`               | `launch` could not start the target executable.        | Verify the path / PATH name. For UWP, double-check the `shell:AppsFolder\<AUMID>` form. Confirm permissions and that the file exists. |
| `CLOSE_FAILED`                | `close-window` could not close the window.             | The window may be blocked by a modal dialog. Dismiss the dialog first, then retry.                |
| `KILL_FAILED`                 | `kill` could not terminate the process.                | The process may have already exited. Re-check with `adact list-windows`.                          |
| `SNAPSHOT_FAILED`             | UIA tree traversal failed during snapshot capture.     | The window may have been destroyed or become unresponsive. Re-attach and retry.                   |
| `LOCAL_ONLY`                  | Operation only valid against a localhost daemon.       | Run the command on the same host as the daemon.                                                   |
| `INTERNAL_ERROR`              | Unexpected internal failure.                           | Retry the operation. If persistent, restart the daemon with `adact serve`.                        |
| `ALREADY_RUNNING`             | Daemon is already running on the target port/pipe.     | Use the existing daemon, or stop it first with `adact daemon-stop`.                               |
| `NO_INTERACTIVE_SESSION`      | `serve` was started in a non-interactive desktop session. | Run the daemon in an interactive user session (not a Windows service or SSH without desktop).   |
